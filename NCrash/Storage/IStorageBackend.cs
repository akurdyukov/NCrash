using System;
using System.IO;

namespace NCrash.Storage
{
    public interface IStorageBackend : IDisposable
    {
        /// <summary>
        /// Return report count in current storer
        /// </summary>
        /// <returns></returns>
        int GetReportCount();

        /// <summary>
        /// Trancate reports
        /// </summary>
        /// <param name="maxQueuedReports"></param>
        void TruncateReportFiles(int maxQueuedReports);

        /// <summary>
        /// Create stream for given report file name. Stream should closed and disposed outside.
        /// </summary>
        /// <returns></returns>
        Stream CreateReportFile();

        /// <summary>
        /// Return first report file as a stream. Stream should be closed and disposed outside.
        /// </summary>
        /// <returns></returns>
        Stream GetFirstReportFile(out string fileName);

        /// <summary>
        /// Remove element by name
        /// </summary>
        /// <param name="name">Element name</param>
        void Remove(string name);
    }
}
