using System;
using System.Threading.Tasks;
using NCrash.UI;
using System.IO;
using System.Collections.Generic;

namespace NCrash.Examples.ConsoleExample
{
    class Program
    {
        static void Main()
        {
            var userInterface = new EmptyUserInterface {Flow = ExecutionFlow.BreakExecution};
            var settings = new DefaultSettings {HandleProcessCorruptedStateExceptions = true, UserInterface = userInterface};
            settings.Sender = new LocalSender();
            var reporter = new ErrorReporter(settings);
            reporter.HandleExceptions = true;
            /// Example how to add screenshots, or other files to report
            reporter.ProcessingException += (ex, report) =>
            {
                if (settings.AdditionalReportFiles == null)
                    settings.AdditionalReportFiles = new List<string>();
                foreach (Tuple<string, string> screenshot in report.ScreenshotList)
                {
                    settings.AdditionalReportFiles.Add(Path.Combine(screenshot.Item1, screenshot.Item2));
                }
            };

            // Sample NCrash configuration for console applications
            AppDomain.CurrentDomain.UnhandledException += reporter.UnhandledException;
            TaskScheduler.UnobservedTaskException += reporter.UnobservedTaskException;

            Console.WriteLine("Press E for current thread exception, T for task exception, X for exit");
            ConsoleKey key;
            do
            {
                key = Console.ReadKey().Key;
                Console.WriteLine();
                if (key == ConsoleKey.E)
                {
                    Console.WriteLine("Throwing exception in current thread");
                    throw new Exception("Test exception in main thread");
                }
                if (key == ConsoleKey.T)
                {
                    Console.WriteLine("Throwing exception in task thread");
                    var task = new Task(MakeExceptionInTask);
                    task.Start();
                    task.Wait();
                }
            } while (key != ConsoleKey.X);
        }

        private static void MakeExceptionInTask()
        {
            throw new Exception("Task exception in task");
        }
    }
}
