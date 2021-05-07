namespace Unity.VisualScripting
{
    /// <summary>
    /// Get a list of all the ScriptGraphs from a GameObject
    /// </summary>
    [TypeIcon(typeof(ScriptGraphAsset))]
    public class GetScriptGraphs : GetGraphs<FlowGraph, ScriptGraphAsset, ScriptMachine> { }
}
