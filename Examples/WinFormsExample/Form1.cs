using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NCrash.Examples.WinFormsExample
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            throw new Exception("System.Exception in UI Thread");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            throw new ArgumentException("System.ArgumentException was thrown.", "MyInvalidParameter", new Exception("Test inner exception for argument exception."));
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew(() => { throw new Exception(); });
            // Below code makes sure that exception is thrown as only after finalization, the aggregateexception is thrown.
            // As a side affect, unlike the normal behavior, the applicaiton will note continue its execution but will shut
            // down just like any main thread exceptions, even if there is no handle to UnobservedTaskException!
            // So remove below 3 lines to observe the normal continuation behavior.
            System.Threading.Thread.Sleep(200);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            AccessViolation();
        }

        private unsafe void AccessViolation()
        {
            byte b = *(byte*)(8762765876);
        }
    }
}
