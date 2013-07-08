// ZipStorer, by Jaime Olivares
// Website: zipstorer.codeplex.com
// Version: 2.35 (March 14, 2010)

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace NCrash.Storage
{
    /// <summary>
    /// Unique class for compression/decompression file. Represents a Zip file.
    /// </summary>
    internal class ZipStorer : IDisposable
    {
        #region Constants and Fields

        /// <summary>
        ///   True if UTF8 encoding for filename and comments, false if default (CP 437)
        /// </summary>
        public bool EncodeUtf8;

        /// <summary>
        ///   Force deflate algotithm even if it inflates the stored file. Off by default.
        /// </summary>
        public bool ForceDeflating;

        /// <summary>
        ///   The crc table.
        /// </summary>
        private static readonly uint[] CrcTable;

        // Default filename encoder
        /// <summary>
        ///   The default encoding.
        /// </summary>
        private static readonly Encoding DefaultEncoding = Encoding.GetEncoding(437);

        // List of files to store
        /// <summary>
        ///   The files.
        /// </summary>
        private readonly List<ZipFileEntry> _files = new List<ZipFileEntry>();

        /// <summary>
        ///   The access.
        /// </summary>
        private FileAccess _access;

        // Filename of storage file

        // Central dir image
        /// <summary>
        ///   The central dir image.
        /// </summary>
        private byte[] _centralDirImage;

        /// <summary>
        ///   The comment.
        /// </summary>
        private string _comment = string.Empty;

        // Existing files in zip
        /// <summary>
        ///   The existing files.
        /// </summary>
        private ushort _existingFiles;

        /// <summary>
        ///   The file name.
        /// </summary>
        private string _fileName;

        // Stream object of storage file
        /// <summary>
        ///   The zip file stream.
        /// </summary>
        private Stream _zipFileStream;

        #endregion

        // File access for Open method

        // Static constructor. Just invoked once in order to create the CRC32 lookup table.
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes static members of the <see cref = "ZipStorer" /> class.
        /// </summary>
        static ZipStorer()
        {
            // Generate CRC32 table
            CrcTable = new uint[256];
            for (int i = 0; i < CrcTable.Length; i++)
            {
                var c = (UInt32)i;
                for (int j = 0; j < 8; j++)
                {
                    if ((c & 1) != 0)
                    {
                        c = 3988292384 ^ (c >> 1);
                    }
                    else
                    {
                        c >>= 1;
                    }
                }

                CrcTable[i] = c;
            }
        }

        #endregion

        #region Enums

        /// <summary>
        /// Compression method enumeration
        /// </summary>
        public enum Compression : ushort
        {
            /// <summary>
            ///   Uncompressed storage
            /// </summary>
            Store = 0,

            /// <summary>
            ///   Deflate compression method
            /// </summary>
            Deflate = 8
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Method to create a new storage file
        /// </summary>
        /// <param name="filename">
        /// Full path of Zip file to create
        /// </param>
        /// <param name="comment">
        /// General comment for Zip file
        /// </param>
        /// <returns>
        /// A valid ZipStorer object
        /// </returns>
        public static ZipStorer Create(string filename, string comment)
        {
            Stream stream = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite);

            ZipStorer zip = Create(stream, comment);
            zip._comment = comment;
            zip._fileName = filename;

            return zip;
        }

        /// <summary>
        /// Method to create a new zip storage in a stream
        /// </summary>
        /// <param name="stream">
        /// </param>
        /// <param name="comment">
        /// </param>
        /// <returns>
        /// A valid ZipStorer object
        /// </returns>
        public static ZipStorer Create(Stream stream, string comment)
        {
            var zip = new ZipStorer { _comment = comment, _zipFileStream = stream, _access = FileAccess.Write };

            return zip;
        }

        /// <summary>
        /// Method to open an existing storage file
        /// </summary>
        /// <param name="filename">
        /// Full path of Zip file to open
        /// </param>
        /// <param name="access">
        /// File access mode as used in FileStream constructor
        /// </param>
        /// <returns>
        /// A valid ZipStorer object
        /// </returns>
        public static ZipStorer Open(string filename, FileAccess access)
        {
            Stream stream = new FileStream(
                filename, FileMode.Open, access == FileAccess.Read ? FileAccess.Read : FileAccess.ReadWrite);

            ZipStorer zip = Open(stream, access);
            zip._fileName = filename;

            return zip;
        }

        /// <summary>
        /// Method to open an existing storage from stream
        /// </summary>
        /// <param name="stream">
        /// Already opened stream with zip contents
        /// </param>
        /// <param name="access">
        /// File access mode for stream operations
        /// </param>
        /// <returns>
        /// A valid ZipStorer object
        /// </returns>
        public static ZipStorer Open(Stream stream, FileAccess access)
        {
            if (!stream.CanSeek && access != FileAccess.Read)
            {
                throw new InvalidOperationException("Stream cannot seek");
            }

            var zip = new ZipStorer {_zipFileStream = stream, _access = access};

            if (zip.ReadFileInfo())
            {
                return zip;
            }

            throw new InvalidDataException();
        }

        /// <summary>
        /// Removes one of many files in storage. It creates a new Zip file.
        /// </summary>
        /// <param name="zip">
        /// Reference to the current Zip object
        /// </param>
        /// <param name="zfes">
        /// List of Entries to remove from storage
        /// </param>
        /// <returns>
        /// True if success, false if not
        /// </returns>
        /// <remarks>
        /// This method only works for storage of type FileStream
        /// </remarks>
        public static bool RemoveEntries(ref ZipStorer zip, List<ZipFileEntry> zfes)
        {
            if (!(zip._zipFileStream is FileStream))
            {
                throw new InvalidOperationException("RemoveEntries is allowed just over streams of type FileStream");
            }

            // Get full list of entries
            List<ZipFileEntry> fullList = zip.ReadCentralDir();

            // In order to delete we need to create a copy of the zip file excluding the selected items
            string tempZipName = Path.GetTempFileName();
            string tempEntryName = Path.GetTempFileName();

            try
            {
                ZipStorer tempZip = Create(tempZipName, string.Empty);

                foreach (ZipFileEntry zfe in fullList)
                {
                    if (!zfes.Contains(zfe))
                    {
                        if (zip.ExtractFile(zfe, tempEntryName))
                        {
                            tempZip.AddFile(zfe.Method, tempEntryName, zfe.FilenameInZip, zfe.Comment);
                        }
                    }
                }

                zip.Close();
                tempZip.Close();

                File.Delete(zip._fileName);
                File.Move(tempZipName, zip._fileName);

                zip = Open(zip._fileName, zip._access);
            }
            catch
            {
                return false;
            }
            finally
            {
                if (File.Exists(tempZipName))
                {
                    File.Delete(tempZipName);
                }

                if (File.Exists(tempEntryName))
                {
                    File.Delete(tempEntryName);
                }
            }

            return true;
        }

        /// <summary>
        /// Add full contents of a file into the Zip storage
        /// </summary>
        /// <param name="method">
        /// Compression method
        /// </param>
        /// <param name="pathname">
        /// Full path of file to add to Zip storage
        /// </param>
        /// <param name="filenameInZip">
        /// Filename and path as desired in Zip directory
        /// </param>
        /// <param name="fileComment">
        /// Comment for stored file
        /// </param>
        public void AddFile(Compression method, string pathname, string filenameInZip, string fileComment)
        {
            if (_access == FileAccess.Read)
            {
                throw new InvalidOperationException("Writing is not alowed");
            }

            var stream = new FileStream(pathname, FileMode.Open, FileAccess.Read);
            AddStream(method, filenameInZip, stream, File.GetLastWriteTime(pathname), fileComment);
            stream.Close();
        }

        /// <summary>
        /// Add full contents of a stream into the Zip storage
        /// </summary>
        /// <param name="method">
        /// Compression method
        /// </param>
        /// <param name="filenameInZip">
        /// Filename and path as desired in Zip directory
        /// </param>
        /// <param name="source">
        /// Stream object containing the data to store in Zip
        /// </param>
        /// <param name="modTime">
        /// Modification time of the data to store
        /// </param>
        /// <param name="fileComment">
        /// Comment for stored file
        /// </param>
        public void AddStream(Compression method, string filenameInZip, Stream source, DateTime modTime, string fileComment)
        {
            if (_access == FileAccess.Read)
            {
                throw new InvalidOperationException("Writing is not alowed");
            }

            // Prepare the fileinfo
            var zfe = new ZipFileEntry();
            zfe.Method = method;
            zfe.EncodeUTF8 = EncodeUtf8;
            zfe.FilenameInZip = NormalizedFilename(filenameInZip);
            zfe.Comment = fileComment ?? string.Empty;

            // Even though we write the header now, it will have to be rewritten, since we don't know compressed size or crc.
            zfe.Crc32 = 0; // to be updated later
            zfe.HeaderOffset = (uint)_zipFileStream.Position; // offset within file of the start of this local record
            zfe.ModifyTime = modTime;

            // Write local header
            WriteLocalHeader(ref zfe);
            zfe.FileOffset = (uint)_zipFileStream.Position;

            // Write file to zip (store)
            Store(ref zfe, source);
            //_source.Close();

            UpdateCrcAndSizes(ref zfe);

            _files.Add(zfe);
        }

        /// <summary>
        /// Updates central directory (if pertinent) and close the Zip storage
        /// </summary>
        /// <remarks>
        /// This is a required step, unless automatic dispose is used
        /// </remarks>
        public void Close()
        {
            if (_access != FileAccess.Read)
            {
                var centralOffset = (uint)_zipFileStream.Position;
                uint centralSize = 0;

                if (_centralDirImage != null)
                {
                    _zipFileStream.Write(_centralDirImage, 0, _centralDirImage.Length);
                }

                foreach (ZipFileEntry t in _files)
                {
                    long pos = _zipFileStream.Position;
                    WriteCentralDirRecord(t);
                    centralSize += (uint)(_zipFileStream.Position - pos);
                }

                if (_centralDirImage != null)
                {
                    WriteEndRecord(centralSize + (uint)_centralDirImage.Length, centralOffset);
                }
                else
                {
                    WriteEndRecord(centralSize, centralOffset);
                }
            }

            if (_zipFileStream != null)
            {
                _zipFileStream.Flush();
                _zipFileStream.Dispose();
                _zipFileStream = null;
            }
        }

        /// <summary>
        /// Copy the contents of a stored file into a physical file
        /// </summary>
        /// <param name="zfe">
        /// Entry information of file to extract
        /// </param>
        /// <param name="filename">
        /// Name of file to store uncompressed data
        /// </param>
        /// <returns>
        /// True if success, false if not.
        /// </returns>
        /// <remarks>
        /// Unique compression methods are Store and Deflate
        /// </remarks>
        public bool ExtractFile(ZipFileEntry zfe, string filename)
        {
            // Make sure the parent directory exist
            string path = Path.GetDirectoryName(filename);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            // Check it is directory. If so, do nothing
            if (Directory.Exists(filename))
            {
                return true;
            }

            Stream output = new FileStream(filename, FileMode.Create, FileAccess.Write);
            bool result = ExtractFile(zfe, output);
            if (result)
            {
                output.Close();
            }

            File.SetCreationTime(filename, zfe.ModifyTime);
            File.SetLastWriteTime(filename, zfe.ModifyTime);

            return result;
        }

        /// <summary>
        /// Copy the contents of a stored file into an opened stream
        /// </summary>
        /// <param name="zfe">
        /// Entry information of file to extract
        /// </param>
        /// <param name="stream">
        /// Stream to store the uncompressed data
        /// </param>
        /// <returns>
        /// True if success, false if not.
        /// </returns>
        /// <remarks>
        /// Unique compression methods are Store and Deflate
        /// </remarks>
        public bool ExtractFile(ZipFileEntry zfe, Stream stream)
        {
            if (!stream.CanWrite)
            {
                throw new InvalidOperationException("Stream cannot be written");
            }

            // check signature
            var signature = new byte[4];
            _zipFileStream.Seek(zfe.HeaderOffset, SeekOrigin.Begin);
            _zipFileStream.Read(signature, 0, 4);
            if (BitConverter.ToUInt32(signature, 0) != 0x04034b50)
            {
                return false;
            }

            // Select input stream for inflating or just reading
            Stream inStream;
            if (zfe.Method == Compression.Store)
            {
                inStream = _zipFileStream;
            }
            else if (zfe.Method == Compression.Deflate)
            {
                inStream = new DeflateStream(_zipFileStream, CompressionMode.Decompress, true);
            }
            else
            {
                return false;
            }

            // Buffered copy
            var buffer = new byte[16384];
            _zipFileStream.Seek(zfe.FileOffset, SeekOrigin.Begin);
            uint bytesPending = zfe.FileSize;
            while (bytesPending > 0)
            {
                int bytesRead = inStream.Read(buffer, 0, (int)Math.Min(bytesPending, buffer.Length));
                stream.Write(buffer, 0, bytesRead);
                bytesPending -= (uint)bytesRead;
            }

            stream.Flush();

            if (zfe.Method == Compression.Deflate)
            {
                inStream.Dispose();
            }

            return true;
        }

        /// <summary>
        /// Read all the file records in the central directory
        /// </summary>
        /// <returns>
        /// List of all entries in directory
        /// </returns>
        public List<ZipFileEntry> ReadCentralDir()
        {
            if (_centralDirImage == null)
            {
                throw new InvalidOperationException("Central directory currently does not exist");
            }

            var result = new List<ZipFileEntry>();

            for (int pointer = 0; pointer < _centralDirImage.Length; )
            {
                uint signature = BitConverter.ToUInt32(_centralDirImage, pointer);
                if (signature != 0x02014b50)
                {
                    break;
                }

                bool encodeUTF8 = (BitConverter.ToUInt16(_centralDirImage, pointer + 8) & 0x0800) != 0;
                ushort method = BitConverter.ToUInt16(_centralDirImage, pointer + 10);
                uint modifyTime = BitConverter.ToUInt32(_centralDirImage, pointer + 12);
                uint crc32 = BitConverter.ToUInt32(_centralDirImage, pointer + 16);
                uint comprSize = BitConverter.ToUInt32(_centralDirImage, pointer + 20);
                uint fileSize = BitConverter.ToUInt32(_centralDirImage, pointer + 24);
                ushort filenameSize = BitConverter.ToUInt16(_centralDirImage, pointer + 28);
                ushort extraSize = BitConverter.ToUInt16(_centralDirImage, pointer + 30);
                ushort commentSize = BitConverter.ToUInt16(_centralDirImage, pointer + 32);
                uint headerOffset = BitConverter.ToUInt32(_centralDirImage, pointer + 42);
                var headerSize = (uint)(46 + filenameSize + extraSize + commentSize);

                Encoding encoder = encodeUTF8 ? Encoding.UTF8 : DefaultEncoding;

                var zfe = new ZipFileEntry();
                zfe.Method = (Compression)method;
                zfe.FilenameInZip = encoder.GetString(_centralDirImage, pointer + 46, filenameSize);
                zfe.FileOffset = GetFileOffset(headerOffset);
                zfe.FileSize = fileSize;
                zfe.CompressedSize = comprSize;
                zfe.HeaderOffset = headerOffset;
                zfe.HeaderSize = headerSize;
                zfe.Crc32 = crc32;
                zfe.ModifyTime = DosTimeToDateTime(modifyTime);
                if (commentSize > 0)
                {
                    zfe.Comment = encoder.GetString(_centralDirImage, pointer + 46 + filenameSize + extraSize, commentSize);
                }

                result.Add(zfe);
                pointer += 46 + filenameSize + extraSize + commentSize;
            }

            return result;
        }

        #endregion

        #region Implemented Interfaces

        #region IDisposable

        /// <summary>
        /// Closes the Zip file stream
        /// </summary>
        public void Dispose()
        {
            this.Close();
        }

        #endregion

        #endregion

        /* DOS Date and time:
            MS-DOS date. The date is a packed value with the following format. Bits Description 
                0-4 Day of the month (1–31) 
                5-8 Month (1 = January, 2 = February, and so on) 
                9-15 Year offset from 1980 (add 1980 to get actual year) 
            MS-DOS time. The time is a packed value with the following format. Bits Description 
                0-4 Second divided by 2 
                5-10 Minute (0–59) 
                11-15 Hour (0–23 on a 24-hour clock) 
        */
        #region Methods

        /// <summary>
        /// The date time to dos time.
        /// </summary>
        /// <param name="dt">
        /// The _dt.
        /// </param>
        /// <returns>
        /// The date time to dos time.
        /// </returns>
        private uint DateTimeToDosTime(DateTime dt)
        {
            return
                (uint)
                ((dt.Second / 2) | (dt.Minute << 5) | (dt.Hour << 11) | (dt.Day << 16) | (dt.Month << 21) |
                 ((dt.Year - 1980) << 25));
        }

        /// <summary>
        /// The dos time to date time.
        /// </summary>
        /// <param name="dt">
        /// The _dt.
        /// </param>
        /// <returns>
        /// </returns>
        private DateTime DosTimeToDateTime(uint dt)
        {
            return new DateTime(
                (int)(dt >> 25) + 1980,
                (int)(dt >> 21) & 15,
                (int)(dt >> 16) & 31,
                (int)(dt >> 11) & 31,
                (int)(dt >> 5) & 63,
                (int)(dt & 31) * 2);
        }

        /// <summary>
        /// The get file offset.
        /// </summary>
        /// <param name="headerOffset">
        /// The _header offset.
        /// </param>
        /// <returns>
        /// The get file offset.
        /// </returns>
        private uint GetFileOffset(uint headerOffset)
        {
            var buffer = new byte[2];

            this._zipFileStream.Seek(headerOffset + 26, SeekOrigin.Begin);
            this._zipFileStream.Read(buffer, 0, 2);
            ushort filenameSize = BitConverter.ToUInt16(buffer, 0);
            this._zipFileStream.Read(buffer, 0, 2);
            ushort extraSize = BitConverter.ToUInt16(buffer, 0);

            return (uint)(30 + filenameSize + extraSize + headerOffset);
        }

        /* CRC32 algorithm
          The 'magic number' for the CRC is 0xdebb20e3.  
          The proper CRC pre and post conditioning
          is used, meaning that the CRC register is
          pre-conditioned with all ones (a starting value
          of 0xffffffff) and the value is post-conditioned by
          taking the one's complement of the CRC residual.
          If bit 3 of the general purpose flag is set, this
          field is set to zero in the local header and the correct
          value is put in the data descriptor and in the central
          directory.
        */

        // Replaces backslashes with slashes to store in zip header
        /// <summary>
        /// The normalized filename.
        /// </summary>
        /// <param name="_filename">
        /// The _filename.
        /// </param>
        /// <returns>
        /// The normalized filename.
        /// </returns>
        private string NormalizedFilename(string _filename)
        {
            string filename = _filename.Replace('\\', '/');

            int pos = filename.IndexOf(':');
            if (pos >= 0)
            {
                filename = filename.Remove(0, pos + 1);
            }

            return filename.Trim('/');
        }

        // Reads the end-of-central-directory record
        /// <summary>
        /// The read file info.
        /// </summary>
        /// <returns>
        /// The read file info.
        /// </returns>
        private bool ReadFileInfo()
        {
            if (this._zipFileStream.Length < 22)
            {
                return false;
            }

            try
            {
                this._zipFileStream.Seek(-17, SeekOrigin.End);
                var br = new BinaryReader(this._zipFileStream);
                do
                {
                    this._zipFileStream.Seek(-5, SeekOrigin.Current);
                    uint sig = br.ReadUInt32();
                    if (sig == 0x06054b50)
                    {
                        this._zipFileStream.Seek(6, SeekOrigin.Current);

                        ushort entries = br.ReadUInt16();
                        int centralSize = br.ReadInt32();
                        uint centralDirOffset = br.ReadUInt32();
                        ushort commentSize = br.ReadUInt16();

                        // check if comment field is the very last data in file
                        if (this._zipFileStream.Position + commentSize != this._zipFileStream.Length)
                        {
                            return false;
                        }

                        // Copy entire central directory to a memory buffer
                        this._existingFiles = entries;
                        this._centralDirImage = new byte[centralSize];
                        this._zipFileStream.Seek(centralDirOffset, SeekOrigin.Begin);
                        this._zipFileStream.Read(this._centralDirImage, 0, centralSize);

                        // Leave the pointer at the begining of central dir, to append new files
                        this._zipFileStream.Seek(centralDirOffset, SeekOrigin.Begin);
                        return true;
                    }
                }
                while (this._zipFileStream.Position > 0);
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        /// The store.
        /// </summary>
        /// <param name="_zfe">
        /// The _zfe.
        /// </param>
        /// <param name="_source">
        /// The _source.
        /// </param>
        private void Store(ref ZipFileEntry _zfe, Stream _source)
        {
            var buffer = new byte[16384];
            int bytesRead;
            uint totalRead = 0;
            Stream outStream;

            long posStart = this._zipFileStream.Position;
            long sourceStart = _source.Position;

            if (_zfe.Method == Compression.Store)
            {
                outStream = this._zipFileStream;
            }
            else
            {
                outStream = new DeflateStream(this._zipFileStream, CompressionMode.Compress, true);
            }

            _zfe.Crc32 = 0 ^ 0xffffffff;

            do
            {
                bytesRead = _source.Read(buffer, 0, buffer.Length);
                totalRead += (uint)bytesRead;
                if (bytesRead > 0)
                {
                    outStream.Write(buffer, 0, bytesRead);

                    for (uint i = 0; i < bytesRead; i++)
                    {
                        _zfe.Crc32 = CrcTable[(_zfe.Crc32 ^ buffer[i]) & 0xFF] ^ (_zfe.Crc32 >> 8);
                    }
                }
            }
            while (bytesRead == buffer.Length);
            outStream.Flush();

            if (_zfe.Method == Compression.Deflate)
            {
                outStream.Dispose();
            }

            _zfe.Crc32 ^= 0xffffffff;
            _zfe.FileSize = totalRead;
            _zfe.CompressedSize = (uint)(this._zipFileStream.Position - posStart);

            // Verify for real compression
            if (_zfe.Method == Compression.Deflate && !this.ForceDeflating && _source.CanSeek &&
                _zfe.CompressedSize > _zfe.FileSize)
            {
                // Start operation again with Store algorithm
                _zfe.Method = Compression.Store;
                this._zipFileStream.Position = posStart;
                this._zipFileStream.SetLength(posStart);
                _source.Position = sourceStart;
                this.Store(ref _zfe, _source);
            }
        }

        /// <summary>
        /// The update crc and sizes.
        /// </summary>
        /// <param name="_zfe">
        /// The _zfe.
        /// </param>
        private void UpdateCrcAndSizes(ref ZipFileEntry _zfe)
        {
            long lastPos = this._zipFileStream.Position; // remember position

            this._zipFileStream.Position = _zfe.HeaderOffset + 8;
            this._zipFileStream.Write(BitConverter.GetBytes((ushort)_zfe.Method), 0, 2); // zipping method

            this._zipFileStream.Position = _zfe.HeaderOffset + 14;
            this._zipFileStream.Write(BitConverter.GetBytes(_zfe.Crc32), 0, 4); // Update CRC
            this._zipFileStream.Write(BitConverter.GetBytes(_zfe.CompressedSize), 0, 4); // Compressed size
            this._zipFileStream.Write(BitConverter.GetBytes(_zfe.FileSize), 0, 4); // Uncompressed size

            this._zipFileStream.Position = lastPos; // restore position
        }

        /// <summary>
        /// The write central dir record.
        /// </summary>
        /// <param name="_zfe">
        /// The _zfe.
        /// </param>
        private void WriteCentralDirRecord(ZipFileEntry _zfe)
        {
            Encoding encoder = _zfe.EncodeUTF8 ? Encoding.UTF8 : DefaultEncoding;
            byte[] encodedFilename = encoder.GetBytes(_zfe.FilenameInZip);
            byte[] encodedComment = encoder.GetBytes(_zfe.Comment);

            this._zipFileStream.Write(new byte[] { 80, 75, 1, 2, 23, 0xB, 20, 0 }, 0, 8);
            this._zipFileStream.Write(BitConverter.GetBytes((ushort)(_zfe.EncodeUTF8 ? 0x0800 : 0)), 0, 2);

            // filename and comment encoding 
            this._zipFileStream.Write(BitConverter.GetBytes((ushort)_zfe.Method), 0, 2); // zipping method
            this._zipFileStream.Write(BitConverter.GetBytes(this.DateTimeToDosTime(_zfe.ModifyTime)), 0, 4);

            // zipping date and time
            this._zipFileStream.Write(BitConverter.GetBytes(_zfe.Crc32), 0, 4); // file CRC
            this._zipFileStream.Write(BitConverter.GetBytes(_zfe.CompressedSize), 0, 4); // compressed file size
            this._zipFileStream.Write(BitConverter.GetBytes(_zfe.FileSize), 0, 4); // uncompressed file size
            this._zipFileStream.Write(BitConverter.GetBytes((ushort)encodedFilename.Length), 0, 2); // Filename in zip
            this._zipFileStream.Write(BitConverter.GetBytes((ushort)0), 0, 2); // extra length
            this._zipFileStream.Write(BitConverter.GetBytes((ushort)encodedComment.Length), 0, 2);

            this._zipFileStream.Write(BitConverter.GetBytes((ushort)0), 0, 2); // disk=0
            this._zipFileStream.Write(BitConverter.GetBytes((ushort)0), 0, 2); // file type: binary
            this._zipFileStream.Write(BitConverter.GetBytes((ushort)0), 0, 2); // Internal file attributes
            this._zipFileStream.Write(BitConverter.GetBytes((ushort)0x8100), 0, 2); // External file attributes (normal/readable)
            this._zipFileStream.Write(BitConverter.GetBytes(_zfe.HeaderOffset), 0, 4); // Offset of header

            this._zipFileStream.Write(encodedFilename, 0, encodedFilename.Length);
            this._zipFileStream.Write(encodedComment, 0, encodedComment.Length);
        }

        /* End of central dir record:
            end of central dir signature    4 bytes  (0x06054b50)
            number of this disk             2 bytes
            number of the disk with the
            start of the central directory  2 bytes
            total number of entries in
            the central dir on this disk    2 bytes
            total number of entries in
            the central dir                 2 bytes
            size of the central directory   4 bytes
            offset of start of central
            directory with respect to
            the starting disk number        4 bytes
            zipfile comment length          2 bytes
            zipfile comment (variable size)
        */

        /// <summary>
        /// The write end record.
        /// </summary>
        /// <param name="_size">
        /// The _size.
        /// </param>
        /// <param name="_offset">
        /// The _offset.
        /// </param>
        private void WriteEndRecord(uint _size, uint _offset)
        {
            Encoding encoder = this.EncodeUtf8 ? Encoding.UTF8 : DefaultEncoding;
            byte[] encodedComment = encoder.GetBytes(this._comment);

            this._zipFileStream.Write(new byte[] { 80, 75, 5, 6, 0, 0, 0, 0 }, 0, 8);
            this._zipFileStream.Write(BitConverter.GetBytes((ushort)this._files.Count + this._existingFiles), 0, 2);
            this._zipFileStream.Write(BitConverter.GetBytes((ushort)this._files.Count + this._existingFiles), 0, 2);
            this._zipFileStream.Write(BitConverter.GetBytes(_size), 0, 4);
            this._zipFileStream.Write(BitConverter.GetBytes(_offset), 0, 4);
            this._zipFileStream.Write(BitConverter.GetBytes((ushort)encodedComment.Length), 0, 2);
            this._zipFileStream.Write(encodedComment, 0, encodedComment.Length);
        }

        /// <summary>
        /// The write local header.
        /// </summary>
        /// <param name="_zfe">
        /// The _zfe.
        /// </param>
        private void WriteLocalHeader(ref ZipFileEntry _zfe)
        {
            long pos = this._zipFileStream.Position;
            Encoding encoder = _zfe.EncodeUTF8 ? Encoding.UTF8 : DefaultEncoding;
            byte[] encodedFilename = encoder.GetBytes(_zfe.FilenameInZip);

            this._zipFileStream.Write(new byte[] { 80, 75, 3, 4, 20, 0 }, 0, 6); // No extra header
            this._zipFileStream.Write(BitConverter.GetBytes((ushort)(_zfe.EncodeUTF8 ? 0x0800 : 0)), 0, 2);

            // filename and comment encoding 
            this._zipFileStream.Write(BitConverter.GetBytes((ushort)_zfe.Method), 0, 2); // zipping method
            this._zipFileStream.Write(BitConverter.GetBytes(this.DateTimeToDosTime(_zfe.ModifyTime)), 0, 4);

            // zipping date and time
            this._zipFileStream.Write(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, 0, 12);

            // unused CRC, un/compressed size, updated later
            this._zipFileStream.Write(BitConverter.GetBytes((ushort)encodedFilename.Length), 0, 2); // filename length
            this._zipFileStream.Write(BitConverter.GetBytes((ushort)0), 0, 2); // extra length

            this._zipFileStream.Write(encodedFilename, 0, encodedFilename.Length);
            _zfe.HeaderSize = (uint)(this._zipFileStream.Position - pos);
        }

        #endregion

        /// <summary>
        /// Represents an entry in Zip file directory
        /// </summary>
        public struct ZipFileEntry
        {
            #region Constants and Fields

            /// <summary>
            ///   User comment for file
            /// </summary>
            public string Comment;

            /// <summary>
            ///   Compressed file size
            /// </summary>
            public uint CompressedSize;

            /// <summary>
            ///   32-bit checksum of entire file
            /// </summary>
            public uint Crc32;

            /// <summary>
            ///   True if UTF8 encoding for filename and comments, false if default (CP 437)
            /// </summary>
            public bool EncodeUTF8;

            /// <summary>
            ///   Offset of file inside Zip storage
            /// </summary>
            public uint FileOffset;

            /// <summary>
            ///   Original file size
            /// </summary>
            public uint FileSize;

            /// <summary>
            ///   Full path and filename as stored in Zip
            /// </summary>
            public string FilenameInZip;

            /// <summary>
            ///   Offset of header information inside Zip storage
            /// </summary>
            public uint HeaderOffset;

            /// <summary>
            ///   Size of header information
            /// </summary>
            public uint HeaderSize;

            /// <summary>
            ///   Compression method
            /// </summary>
            public Compression Method;

            /// <summary>
            ///   Last modification time of file
            /// </summary>
            public DateTime ModifyTime;

            #endregion

            #region Public Methods

            /// <summary>
            /// Overriden method
            /// </summary>
            /// <returns>
            /// Filename in Zip
            /// </returns>
            public override string ToString()
            {
                return this.FilenameInZip;
            }

            #endregion
        }
    }
}
