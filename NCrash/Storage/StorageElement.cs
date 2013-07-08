using System;
using System.IO;
using NCrash.Core;

namespace NCrash.Storage
{
    internal class StorageElement : IDisposable
    {
        private readonly IStorageBackend _backend;

        public string Name { get; private set; }
        public Report Report { get; private set; }
        public Stream Stream { get; private set; }

        public StorageElement(string name, Report report, Stream stream, IStorageBackend backend)
        {
            Name = name;
            Report = report;
            Stream = stream;
            _backend = backend;
        }

        public void Dispose()
        {
            if (Stream != null)
            {
                Stream.Dispose();
                Stream = null;
            }
        }

        /// <summary>
        /// Remove this element
        /// </summary>
        public void Remove()
        {
            if (Stream != null)
            {
                Stream.Dispose();
                Stream = null;
            }

            _backend.Remove(Name);
        }
    }
}
