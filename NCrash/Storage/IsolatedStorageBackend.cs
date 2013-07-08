using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using Common.Logging;

namespace NCrash.Storage
{
    public class IsolatedStorageBackend : IStorageBackend
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
        private readonly ISettings _settings;

        private IsolatedStorageFile _isoStore;

        public IsolatedStorageBackend(ISettings settings)
        {
            _settings = settings;
        }

        public void Dispose()
        {
            // Dispose managed resources
            if (_isoStore != null)
            {
                lock (_isoStore)
                {
                    _isoStore.Close();
                }
            }
        }

        public int GetReportCount()
        {
            using (var isoFile = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly | IsolatedStorageScope.Domain, null, null))
            {
                return isoFile.GetFileNames("Exception_*.zip").Count();
            }
        }

        /// <summary>
        /// This function will get rid of the oldest files first.
        /// </summary>
        /// <param name="maxQueuedReports">Maximum number of queued files to be stored. Setting this to 0 deletes all files. Settings this
        /// to anything less than zero will store infinite number of files.</param>
        public void TruncateReportFiles(int maxQueuedReports)
        {
            if (maxQueuedReports < 0)
            {
                return;
            }

            using (IsolatedStorageFile isoFile = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly | IsolatedStorageScope.Domain, null, null))
            {
                int reportCount = isoFile.GetFileNames("Exception_*.zip").Count();

                if (reportCount <= maxQueuedReports)
                {
                    return;
                }

                int extraCount = reportCount - maxQueuedReports;

                if (maxQueuedReports == 0)
                {
                    Logger.Trace("Truncating all report files from the isolated storage.");
                }
                else
                {
                    Logger.Trace("Truncating extra " + extraCount + " report files from the isolates storage.");
                }

                foreach (var file in isoFile.GetFileNames("Exception_*.zip"))
                {
                    extraCount--;
                    isoFile.DeleteFile(file);

                    if (extraCount == 0)
                    {
                        break;
                    }
                }
                isoFile.Close();
            }
        }

        public Stream CreateReportFile()
        {
            var reportFileName = "Exception_" + DateTime.UtcNow.ToFileTime() + ".zip";

            if (_settings.MaxQueuedReports > 0 && GetReportCount() >= _settings.MaxQueuedReports)
                throw new TooManyReportsException(_settings.MaxQueuedReports);

            Logger.Trace("Creating report file to isolated storage path: [Isolated Storage Directory]\\" + reportFileName);
            return new IsolatedStorageFileStream(reportFileName, FileMode.Create, FileAccess.Write, FileShare.None);
        }

        /// <summary>
        /// Returns the first-in-queue report file. If there are no files queued, returns <see langword="null"/>.
        /// </summary>
        /// <returns>Report file stream.</returns>
        public Stream GetFirstReportFile(out string fileName)
        {
            _isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly | IsolatedStorageScope.Domain, null, null);

            fileName = _isoStore.GetFileNames("Exception_*.zip").FirstOrDefault();
            if (fileName == null)
            {
                return null;
            }

            try
            {
                return new IsolatedStorageFileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException exception)
            {
                // If the file is locked (as per IOException), then probably another instance of the library is already accessing
                // the file so let the other instance handle the file
                Logger.Error("Cannot access the report file at isolated storage (it is probably locked, see the inner exception): [Isolated Storage Directory]\\" + fileName, exception);
                return null;
            }
        }

        public void Remove(string name)
        {
            Logger.Trace("Deleting report file from isolated storage: " + name);
            _isoStore.DeleteFile(name);
        }
    }
}
