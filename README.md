#NCrash

Error reporting library for .NET

## Installation
You can add NCrash library to your project using NuGet. Or source code and build it yourself.

# Usage
Basically you need to perform 2 steps:

1. Create `ErrorReporter` instance
2. Install it as a handler for system events on unhandled something.
 
For example: 

	    var userInterface = new EmptyUserInterface {Flow = ExecutionFlow.BreakExecution};
	    var settings = new DefaultSettings {HandleProcessCorruptedStateExceptions = true, UserInterface = userInterface};
	    var reporter = new ErrorReporter(settings);
	
	    // Sample NCrash configuration for console applications
	    AppDomain.CurrentDomain.UnhandledException += reporter.UnhandledException;
	    TaskScheduler.UnobservedTaskException += reporter.UnobservedTaskException;

## User interfaces
There's several IUserInterface implementation provided

1. `EmptyUserInterface` have no user interaction, return flow status pre-configured (default)
2. `MinimalWinFormsUserInterface` asks user on continuation using WinForms simple modal MessageBox
3. `NormalWinFormsUserInterface` asks user on continuation using custom WinForms modal window
4. `FullWinFormsUserInterface` asks user showing all exception details in WinForms custom window (useful for debugging)
5. `NormalWpfUserInterface` asks user on continuation using Ookii.Dialogs WPF TaskWindow

You can implement your own UI by implementing `IUserInterface` interface and setting instance into settings.

## WinForms installation
Typically handlers are installed in `Program.cs` file before WinForms code starts:

		AppDomain.CurrentDomain.UnhandledException += reporter.UnhandledException;
		Application.ThreadException += reporter.ThreadException;
		System.Threading.Tasks.TaskScheduler.UnobservedTaskException += reporter.UnobservedTaskException;
	    
## WPF installation
Typically handlers are installed in main window's constructor like this:

		AppDomain.CurrentDomain.UnhandledException += reporter.UnhandledException;
		TaskScheduler.UnobservedTaskException += reporter.UnobservedTaskException;
		Application.Current.DispatcherUnhandledException += reporter.DispatcherUnhandledException;

## Reports sending
By default report is sent right after it's generation finishes. Default `ISender` implementation is `NoOpSender` which does nothing but printing some report information into logger.

Available senders:

* `HttpSender` sends report using HTTP POST to given URL using form-www-encoded form
* `MailSender` sends report using SMTP

Use `settings.Sender` attribute to set your custom sender.

You may implement your out sender using `ISender` interface.

## Logging
You may see NCrash logs by attaching Common.Logging to your favourite logging framework in app.config file. For example:

	<?xml version="1.0" encoding="utf-8" ?>
	<configuration>
	  <configSections>
	    <sectionGroup name="common">
	      <section name="logging" type="Common.Logging.ConfigurationSectionHandler, Common.Logging" />
	    </sectionGroup>
	  </configSections>
	
	  <common>
	    <logging>
	      <factoryAdapter type="Common.Logging.Simple.ConsoleOutLoggerFactoryAdapter, Common.Logging">
	        <arg key="level" value="TRACE" />
	      </factoryAdapter>
	    </logging>
	  </common>
	</configuration>

LICENSE
=====

NCrash is available under the GNU LGPL version 3 or later, or - at your option -
the GNU GPL version 3 or later.

See [Licensing FAQ](http://eigen.tuxfamily.org/index.php?title=Licensing_FAQ&oldid=1116) for a
good description of what that means.

AUTHORS
=======

NCrash is refactored fork on NBug project by Teoman Soygul. NCrash is written and maintained by Alik Kurdyukov <akurdyukov@gmail.com>.

Portions of code by Teoman Soygul ([http://www.soygul.com/about/](http://www.soygul.com/about/)).
