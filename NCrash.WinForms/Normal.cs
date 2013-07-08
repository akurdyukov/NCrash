using System;
using System.Drawing;
using System.Windows.Forms;
using NCrash.Core;
using NCrash.UI;

namespace NCrash.WinForms
{
    internal partial class Normal : Form
    {
        private UIDialogResult _uiDialogResult;

        internal Normal()
        {
            InitializeComponent();
            warningPictureBox.Image = SystemIcons.Warning.ToBitmap();
            warningLabel.Text = Messages.Normal_Message;
            continueButton.Text = Messages.Normal_Button_Continue;
            quitButton.Text = Messages.Normal_Button_Quit;
        }

        internal UIDialogResult ShowDialog(Report report)
        {
            Text = string.Format(Messages.Normal_Window_Title, report.GeneralInfo.HostApplication);
            exceptionMessageLabel.Text = report.GeneralInfo.ExceptionMessage;

            ShowDialog();

            return _uiDialogResult;
        }

        private void ContinueButton_Click(object sender, EventArgs e)
        {
            _uiDialogResult = new UIDialogResult(ExecutionFlow.ContinueExecution, true);
            Close();
        }

        private void QuitButton_Click(object sender, EventArgs e)
        {
            _uiDialogResult = new UIDialogResult(ExecutionFlow.BreakExecution, true);
            Close();
        }
    }
}
