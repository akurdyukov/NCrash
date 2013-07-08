using NCrash.Core;
using NCrash.UI;

namespace NCrash.WinForms
{
    public class NormalWinFormsUserInterface : IUserInterface
    {
        public UIDialogResult DisplayBugReportUI(Report report)
        {
            using (var ui = new Normal())
            {
                return ui.ShowDialog(report);
            }
        }
    }
}
