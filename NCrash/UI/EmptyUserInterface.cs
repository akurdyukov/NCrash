using NCrash.Core;

namespace NCrash.UI
{
    /// <summary>
    /// Trivial user interface returning defined result
    /// </summary>
    public class EmptyUserInterface : IUserInterface
    {
        public EmptyUserInterface()
        {
            Flow = ExecutionFlow.ContinueExecution;
            SendReport = true;
        }

        public ExecutionFlow Flow { get; set; }
        public bool SendReport { get; set; }

        public UIDialogResult DisplayBugReportUI(Report report)
        {
            return new UIDialogResult(Flow, SendReport);
        }
    }
}