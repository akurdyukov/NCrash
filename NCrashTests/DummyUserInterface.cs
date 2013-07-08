using NCrash.Core;
using NCrash.UI;

namespace NCrashTests
{
    public class DummyUserInterface : IUserInterface
    {
        private Report _report;
        private readonly UIDialogResult _awaitedResult;

        public DummyUserInterface(UIDialogResult awaitedResult)
        {
            _awaitedResult = awaitedResult;
        }

        public UIDialogResult DisplayBugReportUI(Report report)
        {
            _report = report;
            return _awaitedResult;
        }

        public Report GetReport()
        {
            return _report;
        }
    }
}
