namespace Unity.VisualScripting.Interpreter
{
    /// <summary>
    /// Returned by nodes after their execution to indicate their state
    /// </summary>
    public enum Execution : byte
    {
        /// <summary>
        /// Will re resumed next frame for further execution
        /// </summary>
        Running,

        /// <summary>
        /// No further execution required
        /// </summary>
        Done,

        /// <summary>
        /// Will re resumed later during the same frame for further execution (eg. for loops bodu)
        /// </summary>
        Yield,

        /// <summary>
        /// Will be resumed as a coroutine yielding new WaitForEndOfFrame()
        /// </summary>
        YieldUntilEndOfFrame,
    }
}
