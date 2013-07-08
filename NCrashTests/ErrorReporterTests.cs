using System;
using System.Threading;
using System.Threading.Tasks;
using NCrash;
using NCrash.UI;
using Xunit;

namespace NCrashTests
{
    public class ErrorReporterTests : IDisposable
    {
        private readonly ErrorReporter _reporter;
        private readonly DefaultSettings _settings;
        private readonly DummyUserInterface _userInterface;

        public ErrorReporterTests()
        {
            _userInterface = new DummyUserInterface(new UIDialogResult { Execution = ExecutionFlow.ContinueExecution, SendReport = true });

            _settings = new DefaultSettings {UserInterface = _userInterface};

            _reporter = new ErrorReporter(_settings)
                {
                    HandleExceptions = true // force exception handling with debugger
                };
        }

        public void Dispose()
        {
            _reporter.Dispose();
        }

        [Fact]
        public void TestHandleUnobservedTask()
        {
            // arrange
            _settings.HandleProcessCorruptedStateExceptions = true;

            // act
            _reporter.UnobservedTaskException(this, new UnobservedTaskExceptionEventArgs(new AggregateException("Task error", new Exception("Inner exception"))));

            // assert
            var report = _userInterface.GetReport();
            Assert.NotNull(report);
            Assert.Equal("System.AggregateException", report.GeneralInfo.ExceptionType);
            Assert.NotNull(report.Exception);
            Assert.Null(report.CustomInfo);
        }

        [Fact]
        public void CorruptThreadExceptionHandler()
        {
            // arrange
            _settings.HandleProcessCorruptedStateExceptions = true;

            // act
            _reporter.ThreadException(this, new ThreadExceptionEventArgs(new Exception("Testing a corrupt WinForms UI thread Exception.")));

            // assert
            var report = _userInterface.GetReport();
            Assert.NotNull(report);
            Assert.Equal("System.Exception", report.GeneralInfo.ExceptionType);
            Assert.NotNull(report.Exception);
            Assert.Null(report.CustomInfo);
        }

        [Fact]
        public void CorruptUnhandledExceptionHandler()
        {
            // arrange
            _settings.HandleProcessCorruptedStateExceptions = true;

            // Since there are no UI related assemblies loaded, this should behave as a console app exception
            _reporter.UnhandledException(this, new UnhandledExceptionEventArgs(new AccessViolationException("Testing a corrupt ConsoleApp main thread AccessViolationException."), true));

            // assert
            var report = _userInterface.GetReport();
            Assert.NotNull(report);
            Assert.Equal("System.AccessViolationException", report.GeneralInfo.ExceptionType);
            Assert.NotNull(report.Exception);
            Assert.Null(report.CustomInfo);
        }
    }
}
