using System;

namespace NCrash.Storage
{
    public class TooManyReportsException : Exception
    {
        public TooManyReportsException(int maxQueuedReports)
        {
            MaxQueuedReports = maxQueuedReports;
        }

        public int MaxQueuedReports { get; private set; }
    }
}
