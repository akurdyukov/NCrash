using System;
using System.Windows.Forms;

namespace NCrash.WinForms.Panels
{
    internal partial class ExceptionDetailView : Form
    {
        public ExceptionDetailView()
        {
            InitializeComponent();
        }

        internal void ShowDialog(string property, string info)
        {
            propertyTextBox.Text = property;
            propertyInformationTextBox.Text = info;
            ShowDialog();
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
