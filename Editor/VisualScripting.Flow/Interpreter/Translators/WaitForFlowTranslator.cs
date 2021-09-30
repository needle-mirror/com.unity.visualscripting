using Unity.VisualScripting.Interpreter;
using UnityEngine.Assertions;

namespace Unity.VisualScripting
{
    class WaitForFlowTranslator : NodeTranslator<WaitForFlow>
    {
        protected override INode Translate(GraphBuilder builder, WaitForFlow unit, PortMapper mapping)
        {
            Assert.IsTrue(unit.inputCount <= 64, "waitForFlowUnit.inputCount must be 64 at most");
            var n = new WaitForFlowNode { Inputs = { DataCount = unit.inputCount }, ResetOnExit = unit.resetOnExit };
            mapping.AddMultiPortIndexed(builder, i => unit.awaitedInputs[i], ref n.Inputs);
            mapping.AddSinglePort(builder, unit.reset, ref n.Reset);
            mapping.AddSinglePort(builder, unit.exit, ref n.Exit);
            builder.AddNodeFromModel(unit, n, mapping);
            return n;
        }
    }
}
