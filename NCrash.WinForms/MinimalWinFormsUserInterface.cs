using System.Windows.Forms;
using NCrash.Core;
using NCrash.UI;

namespace NCrash.WinForms
{
    public class MinimalWinFormsUserInterface : IUserInterface
    {
        public MinimalWinFormsUserInterface()
        {
            SendReport = true;
        }

        public bool SendReport { get; set; }

        public UIDialogResult DisplayBugReportUI(Report report)
        {
            var result = MessageBox.Show(
                new Form { TopMost = true },
                Messages.MinimalWinFormsUserInterface_Message_Text,
                string.Format(Messages.MinimalWinFormsUserInterface_Message_Title, report.GeneralInfo.HostApplication),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            return new UIDialogResult(result == DialogResult.Yes ? ExecutionFlow.ContinueExecution : ExecutionFlow.BreakExecution, SendReport);
        }
    }
}
