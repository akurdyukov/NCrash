using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Common.Logging;
using NCrash.Core;
using NCrash.Core.Util;
using NCrash.Storage;
using NCrash.UI;

namespace NCrash
{
    /// <summary>
    /// Main error reporting class. Should be created to report errors.
    /// </summary>
    public class ErrorReporter : IDisposable
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private readonly ISettings _settings;

        private readonly ReportStorage _storage;
        private readonly object _backgroundLock = new object();
        private readonly object _sendLock = new object();
        private readonly CancellationTokenSource _tokenSource;
        private readonly Task _backgroundTask;

        public ErrorReporter(ISettings settings)
        {
            _settings = settings;

            InstallationDate = File.GetLastWriteTime(AssemblyTools.EntryAssembly.Location);
            HandleExceptions = !Debugger.IsAttached; // do not handle exception if debugger is attached by default

            _backgroundLock = new object();
            _tokenSource = new CancellationTokenSource();

            _storage = new ReportStorage(_settings);

            if (_settings.UseBackgroundSender)
            {
                _backgroundTask = new Task(SendReportInBackground, _tokenSource.Token);
                _backgroundTask.Start();
            }

            Current = this;
        }

        /// <summary>
        /// Returns current (latest created) instance of ErrorReporter
        /// </summary>
        public static ErrorReporter Current { get; private set; }

        /// <summary>
        /// First parameters is the serializable exception object that is about to be processed, second parameter is Report. User can set any custom data
        /// object that the user wants to include in the report to Report.CustomInfo. Custom info should be serializable.
        /// </summary>
        public event Action<Exception, Report> ProcessingException;

        /// <summary>
        /// Date program was installed. Used to check 'stop reporting after..'
        /// </summary>
        public DateTime InstallationDate { get; set; }

        /// <summary>
        /// Used for handling general exceptions bound to the main thread.
        /// Handles the <see cref="AppDomain.UnhandledException"/> events in <see cref="System"/> namespace.
        /// </summary>
        public UnhandledExceptionEventHandler UnhandledException
        {
            get
            {
                if (_settings.HandleProcessCorruptedStateExceptions)
                {
                    return CorruptUnhandledExceptionHandler;
                }
                return UnhandledExceptionHandler;
            }
        }

        /// <summary>
        /// Used for handling WinForms exceptions bound to the UI thread.
        /// Handles the <see cref="System.Windows.Forms.Application.ThreadException"/> events in <see cref="System.Windows.Forms"/> namespace.
        /// </summary>
        public ThreadExceptionEventHandler ThreadException
        {
            get
            {
                if (_settings.HandleProcessCorruptedStateExceptions)
                {
                    return CorruptThreadExceptionHandler;
                }
                return ThreadExceptionHandler;
            }
        }

        /// <summary>
        /// Used for handling WPF exceptions bound to the UI thread.
        /// Handles the <see cref="DispatcherUnhandledException"/> events in <see cref="System.Windows"/> namespace.
        /// </summary>
        public DispatcherUnhandledExceptionEventHandler DispatcherUnhandledException
        {
            get
            {
                if (_settings.HandleProcessCorruptedStateExceptions)
                {
                    return CorruptDispatcherUnhandledExceptionHandler;
                }
                return DispatcherUnhandledExceptionHandler;
            }
        }

        /// <summary>
        /// Used for handling System.Threading.Tasks bound to a background worker thread.
        /// Handles the <see cref="UnobservedTaskException"/> event in <see cref="System.Threading.Tasks"/> namespace.
        /// </summary>
        public EventHandler<UnobservedTaskExceptionEventArgs> UnobservedTaskException
        {
            get
            {
                if (_settings.HandleProcessCorruptedStateExceptions)
                {
                    return CorruptUnobservedTaskExceptionHandler;
                }
                return UnobservedTaskExceptionHandler;
            }
        }

        /// <summary>
        /// Controls if NCrush should handle exceptions
        /// </summary>
        public bool HandleExceptions { get; set; }

        /// <summary>
        /// Used for handling general exceptions bound to the main thread.
        /// Handles the <see cref="AppDomain.UnhandledException"/> events in <see cref="System"/> namespace.
        /// </summary>
        /// <param name="sender">Exception sender object.</param>
        /// <param name="e">Real exception is in: ((Exception)e.ExceptionObject)</param>
        private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            if (!HandleExceptions)
            {
                return;
            }

            Logger.Trace("Starting to handle a System.AppDomain.UnhandledException.");
            var executionFlow = Report((Exception)e.ExceptionObject);//, ExceptionThread.Main);
            if (executionFlow == ExecutionFlow.BreakExecution)
            {
                Environment.Exit(0);
            }
        }

        [HandleProcessCorruptedStateExceptions]
        private void CorruptUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            UnhandledExceptionHandler(sender, e);
        }

        /// <summary>
        /// Used for handling WinForms exceptions bound to the UI thread.
        /// Handles the <see cref="System.Windows.Forms.Application.ThreadException"/> events.
        /// </summary>
        /// <param name="sender">Exception sender object.</param>
        /// <param name="e">Real exception is in: e.Exception</param>
        private void ThreadExceptionHandler(object sender, ThreadExceptionEventArgs e)
        {
            if (!HandleExceptions)
            {
                return;
            }

            Logger.Trace("Starting to handle a System.Windows.Forms.Application.ThreadException.");

            // WinForms UI thread exceptions do not propagate to more general handlers unless: Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            var executionFlow = Report(e.Exception);//, ExceptionThread.UI_WinForms);
            if (executionFlow == ExecutionFlow.BreakExecution)
            {
                Environment.Exit(0);
            }
        }

        [HandleProcessCorruptedStateExceptions]
        private void CorruptThreadExceptionHandler(object sender, ThreadExceptionEventArgs e)
        {
            ThreadExceptionHandler(sender, e);
        }

        /// <summary>
        /// Used for handling WPF exceptions bound to the UI thread.
        /// Handles the <see cref="System.Windows.Application.DispatcherUnhandledException"/> events in <see cref="System.Windows"/> namespace.
        /// </summary>
        /// <param name="sender">Exception sender object</param>
        /// <param name="e">Real exception is in: e.Exception</param>
        private void DispatcherUnhandledExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (!HandleExceptions)
            {
                return;
            }

            Logger.Trace("Starting to handle a System.Windows.Application.DispatcherUnhandledException.");
            var executionFlow = Report(e.Exception);
            if (executionFlow == ExecutionFlow.BreakExecution)
            {
                e.Handled = true;
                Environment.Exit(0);
            }
            else if (executionFlow == ExecutionFlow.ContinueExecution)
            {
                e.Handled = true;
            }
        }

        [HandleProcessCorruptedStateExceptions]
        private void CorruptDispatcherUnhandledExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            DispatcherUnhandledExceptionHandler(sender, e);
        }

        /// <summary>
        /// Used for handling System.Threading.Tasks bound to a background worker thread.
        /// Handles the <see cref="UnobservedTaskException"/> event in <see cref="System.Threading.Tasks"/> namespace.
        /// </summary>
        /// <param name="sender">Exception sender object.</param>
        /// <param name="e">Real exception is in: e.Exception.</param>
        private void UnobservedTaskExceptionHandler(object sender, UnobservedTaskExceptionEventArgs e)
        {
            if (!HandleExceptions)
            {
                Logger.Debug("UnobservedTaskExceptionHandler skipped");
                return;
            }

            Logger.Trace("Starting to handle a System.Threading.Tasks.UnobservedTaskException.");
            var executionFlow = Report(e.Exception); //, ExceptionThread.Task);
            if (executionFlow == ExecutionFlow.BreakExecution)
            {
                e.SetObserved();
                Environment.Exit(0);
            }
            else if (executionFlow == ExecutionFlow.ContinueExecution)
            {
                e.SetObserved();
            }
        }

        [HandleProcessCorruptedStateExceptions]
        private void CorruptUnobservedTaskExceptionHandler(object sender, UnobservedTaskExceptionEventArgs e)
        {
            UnobservedTaskExceptionHandler(sender, e);
        }

        /// <summary>
        /// Report exception and show UI in given thread.
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        protected virtual ExecutionFlow Report(Exception exception)
        {
            try
            {
                Logger.Trace("Starting to generate a bug report for the exception.");
                var serializableException = new SerializableException(exception);
                var report = new Report(serializableException);

                var handler = ProcessingException;
                if (handler != null)
                {
                    Logger.Trace("Notifying the user before handling the exception.");

                    // Allowing user to add any custom information to the report
                    handler(exception, report);
                }

                var uiDialogResult = _settings.UserInterface.DisplayBugReportUI(report);

                if (uiDialogResult.SendReport)
                {
                    // Test if it has NOT been more than x many days since entry assembly was last modified)
                    if (_settings.StopReportingAfter >= 0 &&
                        InstallationDate.AddDays(_settings.StopReportingAfter).CompareTo(DateTime.Now) <= 0)
                    {
                        // TODO: this should be moved to task
                        // clear written reports
                        Logger.Trace("As per setting 'Settings.StopReportingAfter(" + _settings.StopReportingAfter +
                                        ")', bug reporting feature was enabled for a certain amount of time which has now expired: Truncating all expired bug reports. Bug reporting is now disabled.");
                        _storage.Clear();
                    }
                    else
                    {
                        _storage.Write(report);

                        // notify dispatcher to dispatch
                        lock (_backgroundLock)
                        {
                            Monitor.PulseAll(_backgroundLock);
                        }
                    }

                }

                return uiDialogResult.Execution;
            }
            catch (Exception ex)
            {
                Logger.Error("An exception occurred during bug report generation process. See the inner exception for details.", ex);
                return ExecutionFlow.BreakExecution; // Since an internal exception occured
            }
        }

        public void Dispose()
        {
            // stop sending task
            _tokenSource.Cancel();
            lock (_backgroundLock)
            {
                Monitor.PulseAll(_backgroundLock);
            }
            _backgroundTask.Wait(); // TODO: check if task is really closed
        }

        /// <summary>
        /// Send reports
        /// </summary>
        public void SendReports()
        {
            lock (_sendLock)
            {
                while (_storage.HasReports())
                {
                    using (StorageElement element = _storage.GetFirst())
                    {
                        if (element == null)
                        {
                            // something went wrong with getting next element - try wait a little longer
                            break;
                        }
                        try
                        {
                            if (!_settings.Sender.Send(element.Stream, element.Name, element.Report))
                            {
                                Logger.WarnFormat("Error report {0} ({1}) failed to send", element.Name, element.Report);
                            }
                        }
                        catch (Exception ex)
                        {
                            // protection from sender exceptions
                            Logger.Error("Error sending error report", ex);
                        }
                        element.Remove();
                    }
                }
            }
        }

        private void SendReportInBackground()
        {
            while (!_tokenSource.IsCancellationRequested)
            {
                lock (_backgroundLock)
                {
                    Monitor.Wait(_backgroundLock);
                }

                if (_tokenSource.IsCancellationRequested)
                {
                    // ok, object is about to be disposed
                    break;
                }

                // wait
                if (_settings.SendTimeout >= 0)
                {
                    Thread.Sleep(_settings.SendTimeout);
                }

                // send
                SendReports();
            }
        }
    }
}
