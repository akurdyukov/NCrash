using System;
using System.Drawing;
using System.Windows.Forms;
using NCrash.Core;
using NCrash.UI;

namespace NCrash.WinForms
{
    internal partial class Full : Form
	{
		private UIDialogResult _uiDialogResult;

		internal Full()
		{
			InitializeComponent();
		    warningLabel.Text = Messages.Full_Message;
			generalTabPage.Text = Messages.Full_Tab_General;
			exceptionTabPage.Text = Messages.Full_Tab_Exception;
		    reportContentsTabPage.Text = Messages.Full_Report_Contents;
		    errorDescriptionLabel.Text = Messages.Full_Reproduce_Exception;
		    quitButton.Text = Messages.Full_Button_Quit;
		    sendAndQuitButton.Text = Messages.Full_Button_SendAndQuit;

			// ToDo: Displaying report contents properly requires some more work.
			mainTabs.TabPages.Remove(mainTabs.TabPages["reportContentsTabPage"]);
		}

		internal UIDialogResult ShowDialog(Report report)
		{
			Text = string.Format(Messages.Full_Window_Title, report.GeneralInfo.HostApplication);
			
			// Fill in the 'General' tab
			warningPictureBox.Image = SystemIcons.Warning.ToBitmap();
			exceptionTextBox.Text = report.Exception.Type;
            exceptionMessageTextBox.Text = report.Exception.Message;
            targetSiteTextBox.Text = report.Exception.TargetSite;
			applicationTextBox.Text = report.GeneralInfo.HostApplication + " [" + report.GeneralInfo.HostApplicationVersion + "]";
			ncrashTextBox.Text = report.GeneralInfo.NCrashVersion;
			dateTimeTextBox.Text = report.GeneralInfo.DateTime;
			clrTextBox.Text = report.GeneralInfo.ClrVersion;
			
			// Fill in the 'Exception' tab
            exceptionDetails.Initialize(report.Exception);
			
			// ToDo: Fill in the 'Report Contents' tab);

			ShowDialog();

			// Write back the user description (as we passed 'report' as a reference since it is a refence object anyway)
			report.GeneralInfo.UserDescription = descriptionTextBox.Text;
			return _uiDialogResult;
		}

		private void SendAndQuitButton_Click(object sender, EventArgs e)
		{
			_uiDialogResult = new UIDialogResult(ExecutionFlow.BreakExecution, true);
			Close();
		}

		private void QuitButton_Click(object sender, EventArgs e)
		{
			_uiDialogResult = new UIDialogResult(ExecutionFlow.BreakExecution, false);
			Close();
		}
	}
}
