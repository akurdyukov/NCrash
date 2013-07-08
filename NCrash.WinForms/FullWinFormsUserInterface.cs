using NCrash.Core;
using NCrash.UI;

namespace NCrash.WinForms
{
    public class FullWinFormsUserInterface : IUserInterface
    {
        public UIDialogResult DisplayBugReportUI(Report report)
        {
            using (var ui = new Full())
            {
                return ui.ShowDialog(report);
            }
        }
    }
}