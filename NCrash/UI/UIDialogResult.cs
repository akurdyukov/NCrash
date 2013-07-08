namespace NCrash.UI
{
    public enum ExecutionFlow
    {
        /// <summary>
        /// This will handle all unhandled exceptions to be able to continue execution.
        /// </summary>
        ContinueExecution,

        /// <summary>
        /// This will handle all unhandled exceptions and exit the application.
        /// </summary>
        BreakExecution,
    }

    public struct UIDialogResult
    {
        public ExecutionFlow Execution;
        public bool SendReport;

        public UIDialogResult(ExecutionFlow execution, bool sendReport)
        {
            Execution = execution;
            SendReport = sendReport;
        }
    }
}
