using System;
using System.Windows.Forms;
using NCrash.WinForms;
using System.IO;
using System.Collections.Generic;

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
            settings.Sender = new LocalSender();
            var reporter = new ErrorReporter(settings);
            reporter.HandleExceptions = true;

            /// Example how to add screenshots, or other files to report
            reporter.ProcessingException += (ex,report) =>
             {
                 if(settings.AdditionalReportFiles == null)
                 settings.AdditionalReportFiles = new List<string>();
                 foreach (Tuple<string, string> screenshot in report.ScreenshotList)
                 {
                     settings.AdditionalReportFiles.Add(Path.Combine(screenshot.Item1, screenshot.Item2));
                 }
             };

            AppDomain.CurrentDomain.UnhandledException += reporter.UnhandledException;
            Application.ThreadException += reporter.ThreadException;
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += reporter.UnobservedTaskException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
