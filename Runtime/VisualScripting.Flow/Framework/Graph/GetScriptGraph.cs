namespace Unity.VisualScripting
{
    /// <summary>
    /// Get a ScriptGraphAsset from a GameObject
    /// </summary>
    [TypeIcon(typeof(ScriptGraphAsset))]
    public class GetScriptGraph : GetGraph<FlowGraph, ScriptGraphAsset, ScriptMachine> { }
}
