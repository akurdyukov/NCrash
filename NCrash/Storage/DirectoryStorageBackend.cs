using System;
using System.IO;
using System.Linq;
using Common.Logging;

namespace NCrash.Storage
{
    internal class DirectoryStorageBackend : IStorageBackend
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private readonly string _path;
        private readonly ISettings _settings;

        public DirectoryStorageBackend(string path, ISettings settings)
        {
            _path = path;
            _settings = settings;
        }

        public int GetReportCount()
        {
            return Directory.Exists(_path) ? Directory.EnumerateFiles(_path, "Exception_*.zip").Count() : 0;
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

            TruncateFiles(_path, maxQueuedReports);
        }

        public Stream CreateReportFile()
        {
            var reportFileName = "Exception_" + DateTime.UtcNow.ToFileTime() + ".zip";

            if (_settings.MaxQueuedReports > 0 && GetReportCount() >= _settings.MaxQueuedReports)
                throw new TooManyReportsException(_settings.MaxQueuedReports);

            if (Directory.Exists(_path) == false)
            {
                Directory.CreateDirectory(_path);
            }

            var filePath = Path.Combine(_path, reportFileName);
            Logger.Trace("Creating report file to: " + filePath);

            return new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        }

        /// <summary>
        /// Returns the first-in-queue report file. If there are no files queued, returns <see langword="null"/>.
        /// </summary>
        /// <returns>Report file stream.</returns>
        public Stream GetFirstReportFile(out string fileName)
        {
            if (!Directory.Exists(_path))
            {
                fileName = null;
                return null;
            }

            string filePath = Directory.EnumerateFiles(_path, "Exception_*.zip").FirstOrDefault();
            if (filePath == null)
            {
                fileName = null;
                return null;
            }
            fileName = Path.GetFileName(filePath);
            Logger.Trace("Returning windows temp storage file " + filePath);

            try
            {
                return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException exception)
            {
                // If the file is locked (as per IOException), then probably another instance of the library is already accessing
                // the file so let the other instance handle the file
                Logger.Error(
                    "Cannot access the report file at Windows temp directory (it is probably locked, see the inner exception): " +
                    filePath, exception);
                return null;
            }
        }

        public void Remove(string name)
        {
            string filePath = Path.Combine(_path, name);
            Logger.Trace("Deleting report file from path: " + filePath);
            File.Delete(filePath);
        }

        private static void TruncateFiles(string path, int maxQueuedReports)
        {
            if (!Directory.Exists(path)) return;

            int reportCount = Directory.EnumerateFiles(path, "Exception_*.zip").Count();

            if (reportCount <= maxQueuedReports) return;

            int extraCount = reportCount - maxQueuedReports;

            if (maxQueuedReports == 0)
            {
                Logger.Trace("Truncating all report files from: " + path);
            }
            else
            {
                Logger.Trace("Truncating extra " + extraCount + " report files from: " + path);
            }

            foreach (var file in Directory.EnumerateFiles(path, "Exception_*.zip"))
            {
                extraCount--;
                File.Delete(file);

                if (extraCount == 0)
                {
                    break;
                }
            }
        }

        public void Dispose()
        {
            // nothing to do here
        }
    }
}
