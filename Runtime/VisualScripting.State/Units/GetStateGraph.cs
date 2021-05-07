namespace Unity.VisualScripting
{
    /// <summary>
    /// Get a StateGraphAsset from a GameObject
    /// </summary>
    [TypeIcon(typeof(StateGraphAsset))]
    public class GetStateGraph : GetGraph<StateGraph, StateGraphAsset, StateMachine> { }
}
