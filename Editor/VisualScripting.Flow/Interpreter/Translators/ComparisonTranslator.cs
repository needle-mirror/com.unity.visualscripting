using System;
using System.Linq;
using JetBrains.Annotations;
using Unity.VisualScripting.Interpreter;
using UnityEngine.Assertions;

namespace Unity.VisualScripting
{
    [UsedImplicitly]
    internal class ComparisonTranslator : NodeTranslator<Comparison>
    {
        protected override INode Translate(GraphBuilder builder, Comparison unit, PortMapper _)
        {
            if (!unit.numeric)
                throw new NotImplementedException("Generic comparisons are not supported yet");
            // the same unit port cannot be mapped to multiple runtime ports for now, so create passthrough nodes.
            // one passthrough per input: otherwise, when pulling a or b, both inputs of the passthrough would be pulled
            // each time.
            // TODO: skip passthrough if only one output is connected. maybe a passthrough stripping pass in release ?
            var passthroughA = new PassthroughNode { Input = { DataCount = 1 }, Output = { DataCount = 1 } };
            var passthroughB = new PassthroughNode { Input = { DataCount = 1 }, Output = { DataCount = 1 } };
            var mappingA = new PortMapper();
            mappingA.AddMultiPortIndexed(builder, i => unit.a, ref passthroughA.Input);
            mappingA.AddMultiPort(builder, null, ref passthroughA.Output);
            builder.AddNodeFromModel(unit, passthroughA, mappingA);
            var mappingB = new PortMapper();
            mappingB.AddMultiPortIndexed(builder, i => unit.b, ref passthroughB.Input);
            mappingB.AddMultiPort(builder, null, ref passthroughB.Output);
            builder.AddNodeFromModel(unit, passthroughB, mappingB);

            var outputA = passthroughA.Output.SelectPort(0);
            var outputB = passthroughB.Output.SelectPort(0);
            ProcessPort<LessNode>(outputA, outputB, builder, unit, unit.aLessThanB);
            ProcessPort<LessOrEqualNode>(outputA, outputB, builder, unit, unit.aLessThanOrEqualToB);
            ProcessPort<EqualNode>(outputA, outputB, builder, unit, unit.aEqualToB);
            ProcessPort<NotEqualNode>(outputA, outputB, builder, unit, unit.aNotEqualToB);
            ProcessPort<GreaterOrEqualNode>(outputA, outputB, builder, unit, unit.aGreaterThanOrEqualToB);
            ProcessPort<GreaterNode>(outputA, outputB, builder, unit, unit.aGreatherThanB);
            return null;
        }

        private void ProcessPort<T>(OutputDataPort outputA, OutputDataPort outputB, GraphBuilder builder, Comparison unit,

            ValueOutput output) where T : struct, IDataNode
        {
            if (!output.hasValidConnection)
                return;
            var n = default(T);

            INode inode = n;
            PortMapper mapping = new PortMapper();

            IInputDataPort a = null;
            IInputDataPort b = null;
            foreach (var nodeInputPort in FlowGraphTranslator.GetNodeInputPorts(n))
            {
                Assert.IsTrue(nodeInputPort.Item1.Name == "A" || nodeInputPort.Item1.Name == "B");
                var inputDataPort = nodeInputPort.Item2;
                mapping.AddSinglePort(builder, null, ref inputDataPort);
                nodeInputPort.Item1.SetValue(inode, inputDataPort);

                if (nodeInputPort.Item1.Name == "A")
                    a = inputDataPort;
                else
                    b = inputDataPort;
            }


            var outputPort = FlowGraphTranslator.GetNodeOutputPorts(n).Single();
            mapping.AddSinglePort(builder, output, ref outputPort.Item2);
            outputPort.Item1.SetValue(inode, outputPort.Item2);

            builder.AddNodeFromModel(unit, inode, mapping);

            builder.CreateEdge(outputA, a);
            builder.CreateEdge(outputB, b);
        }
    }
}
