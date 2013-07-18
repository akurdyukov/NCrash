using System.Collections.Generic;
using NCrash.Core.MiniDump;
using NCrash.Sender;
using NCrash.Storage;
using NCrash.UI;
using NCrash.Plugins;

namespace NCrash
{
    /// <summary>
    /// NCrush settings
    /// </summary>
    public interface ISettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether to handled exceptions even in a corrupted process thought the 'HandleProcessCorruptedStateExceptions'
        /// flag. The default value for this is false since generating bug reports for a corrupted process may not be successful so use with caution.
        /// </summary>
        bool HandleProcessCorruptedStateExceptions { get; }

        /// <summary>
        /// Gets or sets the number of days that NCrash will be collecting bug reports for the application. Most of the time, 30 to 60 days after the
        /// release, there will be a new release and the current one will be obsolete. Due to this, it is not logical to continue to create and submit
        /// bug reports after a given number of days. After the predefined no of days, the user will still get to see the bug report UI but the reports
        /// will not be actually submitted. Default value is 30 days.
        /// </summary>
        int StopReportingAfter { get; }

        /// <summary>
        /// Gets or sets the number of bug reports that can be queued for submission. Each time an unhandled occurs, the bug report is prepared to
        /// be send at the next application startup. If submission fails (i.e. there is no Internet connection), the queue grows with each additional
        /// unhandled exception and resulting bug reports. This limits the max no of queued reports to limit the disk space usage.
        /// Default value is 5.
        /// </summary>
        int MaxQueuedReports { get; }

        /// <summary>
        /// Gets or sets a list of additional files to be added to the report zip. The files can use * or ? in the same way as DOS modifiers.
        /// Paths should be absolute i.e. start with disk name.
        /// </summary>
        IList<string> AdditionalReportFiles { get; set; }

        /// <summary>
        /// Gets or sets the memory dump type. Memory dumps are quite useful for replicating the exact conditions that the application crashed (i.e.
        /// getting the stack trace, local variables, etc.) but they take up a great deal of space, so choose wisely. Options are:
        /// None: No memory dump is generated.
        /// Tiny: Dump size ~200KB compressed.
        /// Normal: Dump size ~20MB compressed.
        /// Full: Dump size ~100MB compressed.
        /// Default value is Tiny.
        /// </summary>
        MiniDumpType MiniDumpType { get; }

        /// <summary>
        /// User interface factory
        /// </summary>
        IUserInterface UserInterface { get; }

        /// <summary>
        /// StorageBackend to use for exceptions
        /// </summary>
        IStorageBackend StorageBackend { get; }

        /// <summary>
        /// If true sender is used in background. Sending starts after SendTimeout.
        /// </summary>
        bool UseBackgroundSender { get; }

        /// <summary>
        /// Timeout in milliseconds to wait after report is generated before send start. Should be positive or zero.
        /// </summary>
        int SendTimeout { get; }

        /// <summary>
        /// Sender to use for sending reports to
        /// </summary>
        ISender Sender { get; }

        /// <summary>
        /// List of plugins
        /// </summary>
        IList<IPlugin> Plugins { get; }
    }
}
