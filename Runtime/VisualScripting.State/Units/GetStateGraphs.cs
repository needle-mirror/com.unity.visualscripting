namespace Unity.VisualScripting
{
    /// <summary>
    /// Get a list of all the StateGraphs from a GameObject
    /// </summary>
    [TypeIcon(typeof(StateGraphAsset))]
    public class GetStateGraphs : GetGraphs<StateGraph, StateGraphAsset, StateMachine> { }
}
