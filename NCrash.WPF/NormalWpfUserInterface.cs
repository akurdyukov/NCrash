using System.Drawing;
using NCrash.Core;
using NCrash.UI;
using Ookii.Dialogs.Wpf;

namespace NCrash.WPF
{
    public class NormalWpfUserInterface : IUserInterface
    {
        public NormalWpfUserInterface()
        {
            SendReport = true;
        }

        public bool SendReport { get; set; }

        public UIDialogResult DisplayBugReportUI(Report report)
        {
            using (var dialog = new TaskDialog())
            {
                dialog.WindowTitle = string.Format(Messages.Normal_Window_Title, report.GeneralInfo.HostApplication);
                dialog.Content = Messages.Normal_Window_Message;
                dialog.CustomMainIcon = SystemIcons.Warning;

                var continueButton = new TaskDialogButton("Continue");
                var quitButton = new TaskDialogButton("Quit");
                dialog.Buttons.Add(continueButton);
                dialog.Buttons.Add(quitButton);

                TaskDialogButton button = dialog.ShowDialog();
                //if (button == continueButton)
                return new UIDialogResult(button == continueButton ? ExecutionFlow.ContinueExecution : ExecutionFlow.BreakExecution, SendReport);
            }
        }
    }
}
