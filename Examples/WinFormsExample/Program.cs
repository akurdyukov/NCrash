using System;
using System.Windows.Forms;
using NCrash.WinForms;

namespace NCrash.Examples.WinFormsExample
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // set NCrash handlers
            var userInterface = new NormalWinFormsUserInterface();
            var settings = new DefaultSettings { UserInterface = userInterface };
            var reporter = new ErrorReporter(settings);

            AppDomain.CurrentDomain.UnhandledException += reporter.UnhandledException;
            Application.ThreadException += reporter.ThreadException;
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += reporter.UnobservedTaskException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
