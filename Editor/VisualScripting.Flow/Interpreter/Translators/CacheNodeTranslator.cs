using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Interpreter;

namespace Unity.VisualScripting
{
    class CacheNodeTranslator : NodeTranslator<Cache>
    {
        protected override INode Translate(GraphBuilder builder, Cache unit, PortMapper mapping)
        {
            var n = new CacheNode { CopiedInputs = { DataCount = 1 }, CopiedOutputs = { DataCount = 1 } };
            mapping.AddSinglePort(builder, unit.enter, ref n.Enter);
            mapping.AddSinglePort(builder, unit.exit, ref n.Exit);
            mapping.AddMultiPort(builder, new List<IUnitPort> { unit.input }, ref n.CopiedInputs);
            mapping.AddMultiPort(builder, new List<IUnitPort> { unit.output }, ref n.CopiedOutputs);
            builder.AddNodeFromModel(unit, n, mapping);
            return n;
        }
    }
}
