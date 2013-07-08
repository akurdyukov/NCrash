using System;
using System.Threading.Tasks;
using System.Windows;
using NCrash.WPF;

namespace NCrash.Examples.WpfExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var userInterface = new NormalWpfUserInterface();
            var settings = new DefaultSettings { HandleProcessCorruptedStateExceptions = true, UserInterface = userInterface };
            var reporter = new ErrorReporter(settings);

            AppDomain.CurrentDomain.UnhandledException += reporter.UnhandledException;
            TaskScheduler.UnobservedTaskException += reporter.UnobservedTaskException;
            Application.Current.DispatcherUnhandledException += reporter.DispatcherUnhandledException;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            throw new Exception("System.Exception in UI Thread");
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            throw new ArgumentException("System.ArgumentException was thrown.", "MyInvalidParameter", new Exception("Test inner exception for argument exception."));
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
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

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            AccessViolation();
        }

        private unsafe void AccessViolation()
        {
            byte b = *(byte*)(8762765876);
        }
    }
}
