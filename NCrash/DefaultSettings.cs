using System.Collections.Generic;
using NCrash.Core.MiniDump;
using NCrash.Sender;
using NCrash.Storage;
using NCrash.UI;

namespace NCrash
{
    public class DefaultSettings : ISettings
    {
        public DefaultSettings()
        {
            HandleProcessCorruptedStateExceptions = true;
            StopReportingAfter = -1;
            MaxQueuedReports = 10;
            MiniDumpType = MiniDumpType.Normal;

            UserInterface = new EmptyUserInterface();
            StorageBackend = new IsolatedStorageBackend(this);

            UseBackgroundSender = true;
            SendTimeout = 0;
            Sender = new NoOpSender();
            //mycode
            IncludeScreenshots = false;
            //mycode
        }

        public bool HandleProcessCorruptedStateExceptions { get; set; }
        public int StopReportingAfter { get; set; }
        public int MaxQueuedReports { get; set; }
        public IList<string> AdditionalReportFiles { get; set; }
        public MiniDumpType MiniDumpType { get; set; }
        public IUserInterface UserInterface { get; set; }
        public IStorageBackend StorageBackend { get; set; }
        public bool UseBackgroundSender { get; set; }
        public int SendTimeout { get; set; }
        public ISender Sender { get; set; }
        //mycode
        public bool IncludeScreenshots { get; set; }
        //mycode
    }
}
