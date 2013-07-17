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
            //mycode
            settings.Sender = new localsender();
            settings.IncludeScreenshots = true;
            var reporter = new ErrorReporter(settings);
            reporter.HandleExceptions = true;
            //System.IO.IsolatedStorage.IsolatedStorageFile.Remove(System.IO.IsolatedStorage.IsolatedStorageScope.User);
            //mycode

            AppDomain.CurrentDomain.UnhandledException += reporter.UnhandledException;
            Application.ThreadException += reporter.ThreadException;
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += reporter.UnobservedTaskException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
