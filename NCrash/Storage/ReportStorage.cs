using System;
using System.IO;
using System.Xml.Serialization;
using Common.Logging;
using NCrash.Core;
using NCrash.Core.MiniDump;
using NCrash.Core.ScreenShots;

namespace NCrash.Storage
{
    internal class ReportStorage
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        private readonly ISettings _settings;

        public ReportStorage(ISettings settings)
        {
            _settings = settings;

            _settings.StorageBackend.TruncateReportFiles(_settings.MaxQueuedReports);
        }

        /// <summary>
        /// Write report to storage
        /// </summary>
        /// <param name="report"></param>
        public void Write(Report report)
        {
            try
            {
                using (Stream reportStream = _settings.StorageBackend.CreateReportFile())
                {
                    using (var zipStorer = ZipStorer.Create(reportStream, string.Empty))
                    {
                        // write exception
                        WriteException(zipStorer, report.Exception);

                        // write report
                        WriteReport(zipStorer, report);

                        // Add the memory minidump to the report file (only if configured so)
                        var minidumpFilePath =
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                                            "Exception_MiniDump_" + DateTime.UtcNow.ToFileTime() + ".mdmp");
                        if (DumpWriter.Write(minidumpFilePath, _settings.MiniDumpType))
                        {
                            zipStorer.AddFile(ZipStorer.Compression.Deflate, minidumpFilePath, StoredItemFile.MiniDump,
                                                string.Empty);
                            File.Delete(minidumpFilePath);
                        }

                        // Add any user supplied files in the report (if any)
                        if (_settings.AdditionalReportFiles != null && _settings.AdditionalReportFiles.Count > 0)
                        {
                            AddAdditionalFiles(zipStorer);
                        }
                    }

                    Logger.Trace("Created a new report file. Currently the number of report files queued to be send is: " +
                                    _settings.StorageBackend.GetReportCount());
                }
            }
            catch (TooManyReportsException ex)
            {
                Logger.Trace("Current report count is at its limit as per 'Settings.MaxQueuedReports (" +
                                ex.MaxQueuedReports + ")' setting: Skipping bug report generation.");
            }
        }

        private void WriteReport(ZipStorer zipStorer, Report report)
        {
            using (var stream = new MemoryStream())
            {
                // Store the report as XML file
                try
                {
                    var serializer = report.CustomInfo != null
                                     ? new XmlSerializer(typeof(Report), new[] { report.CustomInfo.GetType() })
                                     : new XmlSerializer(typeof(Report));

                    serializer.Serialize(stream, report);
                }
                catch (Exception exception)
                {
                    if (report.CustomInfo != null)
                    {
                        Logger.Error(
                            string.Format(
                                "The given custom info of type [{0}] cannot be serialized. Make sure that given type and inner types are XML serializable.",
                                report.CustomInfo.GetType()), exception);
                    }
                    report.CustomInfo = null;
                    var serializer = new XmlSerializer(typeof(Report));
                    serializer.Serialize(stream, report);
                }

                stream.Position = 0;
                zipStorer.AddStream(ZipStorer.Compression.Deflate, StoredItemFile.Report, stream, DateTime.UtcNow,
                                    string.Empty);
            }
        }

        private void WriteException(ZipStorer zipStorer, SerializableException serializableException)
        {
            using (var stream = new MemoryStream())
            {
                // Store the exception as separate file
                var serializer = new XmlSerializer(typeof(SerializableException));
                serializer.Serialize(stream, serializableException);
                stream.Position = 0;
                zipStorer.AddStream(ZipStorer.Compression.Deflate, StoredItemFile.Exception, stream, DateTime.UtcNow,
                                    string.Empty);
            }
        }

        private void AddAdditionalFiles(ZipStorer zipStorer)
        {
            foreach (var mask in _settings.AdditionalReportFiles)
            {
                // Join before spliting because the mask may have some folders inside it
                var dir = Path.GetDirectoryName(mask);
                if (string.IsNullOrEmpty(dir))
                {
                    Logger.Warn("Skipped non-absolute mask " + mask);
                }
                var file = Path.GetFileName(mask);

                if (dir == null || !Directory.Exists(dir) || file == null)
                    continue;

                if (file.Contains("*") || file.Contains("?"))
                {
                    foreach (var item in Directory.GetFiles(dir, file))
                    {
                        AddToZip(zipStorer, dir, item);
                    }
                }
                else
                {
                    AddToZip(zipStorer, dir, mask);
                }
            }
        }

        private void AddToZip(ZipStorer zipStorer, string basePath, string path)
        {
            if (basePath == null)
                return;

            path = Path.GetFullPath(path);

            // If this is not inside basePath, lets change the basePath so at least some directories are kept
            if (!path.StartsWith(basePath))
            {
                basePath = Path.GetDirectoryName(path);
                if (basePath == null)
                    return;
            }

            if (Directory.Exists(path))
            {
                foreach (var file in Directory.GetFiles(path))
                    AddToZip(zipStorer, basePath, file);
                foreach (var dir in Directory.GetDirectories(path))
                    AddToZip(zipStorer, basePath, dir);
            }
            else if (File.Exists(path))
            {
                var nameInZip = path.Substring(basePath.Length);
                if (nameInZip.StartsWith("\\") || nameInZip.StartsWith("/"))
                    nameInZip = nameInZip.Substring(1);
                nameInZip = Path.Combine("files", nameInZip);

                zipStorer.AddFile(ZipStorer.Compression.Deflate, path, nameInZip, string.Empty);
            }
        }

        /// <summary>
        /// Returns first element of storage. Should be disposed by invoker.
        /// </summary>
        /// <returns></returns>
        public StorageElement GetFirst()
        {
            if (_settings.StorageBackend.GetReportCount() == 0)
            {
                return null;
            }

            string fileName;
            Stream stream = _settings.StorageBackend.GetFirstReportFile(out fileName);
            if (stream == null)
            {
                // cannot open next file
                return null;
            }
            Report report = ReadReport(stream);
            stream.Position = 0; // rewind

            return new StorageElement(fileName, report, stream, _settings.StorageBackend);
        }

        private Report ReadReport(Stream stream)
        {
			var zipStorer = ZipStorer.Open(stream, FileAccess.Read);

            // open temp stream
            using (Stream zipItemStream = new MemoryStream())
            {
                var zipDirectory = zipStorer.ReadCentralDir();

                foreach (var entry in zipDirectory)
                {
                    if (Path.GetFileName(entry.FilenameInZip) != StoredItemFile.Report)
                    {
                        continue;
                    }

                    zipItemStream.SetLength(0);
                    zipStorer.ExtractFile(entry, zipItemStream);
                    zipItemStream.Position = 0;

                    var deserializer = new XmlSerializer(typeof (Report));
                    return (Report) deserializer.Deserialize(zipItemStream);
                }
            }
            return null;
        }

        /// <summary>
        /// Clear storage
        /// </summary>
        public void Clear()
        {
            _settings.StorageBackend.TruncateReportFiles(0);
        }

        /// <summary>
        /// Return true if there's any reports in storage
        /// </summary>
        /// <returns></returns>
        public bool HasReports()
        {
            return _settings.StorageBackend.GetReportCount() > 0;
        }
    }
}
