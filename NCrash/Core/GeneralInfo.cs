using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using NCrash.Core.Util;

namespace NCrash.Core
{
    [Serializable]
    public class GeneralInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeneralInfo"/> class. This is the default constructor provided for XML
        /// serialization and de-serialization.
        /// </summary>
        public GeneralInfo()
        {
        }

        internal GeneralInfo(SerializableException serializableException)
        {
            HostApplication = AssemblyTools.EntryAssembly.GetLoadedModules()[0].Name;

            var version = FileVersionInfo.GetVersionInfo(AssemblyTools.EntryAssembly.Location).ProductVersion;
            HostApplicationVersion = version;
            NCrashVersion = version;

            NCrashVersion = FileVersionInfo.GetVersionInfo(Assembly.GetCallingAssembly().Location).ProductVersion;

            ClrVersion = Environment.Version.ToString();

            DateTime = System.DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);

            if (serializableException != null)
            {
                ExceptionType = serializableException.Type;

                if (!string.IsNullOrEmpty(serializableException.TargetSite))
                {
                    TargetSite = serializableException.TargetSite;
                }
                else if (serializableException.InnerException != null && !string.IsNullOrEmpty(serializableException.InnerException.TargetSite))
                {
                    TargetSite = serializableException.InnerException.TargetSite;
                }

                ExceptionMessage = serializableException.Message;
            }
        }

        public string ClrVersion { get; set; }

        public string DateTime { get; set; }

        public string ExceptionMessage { get; set; }

        public string ExceptionType { get; set; }

        public string HostApplication { get; set; }

        /// <summary>
        /// Gets or sets AssemblyFileVersion of host assembly.
        /// </summary>
        public string HostApplicationVersion { get; set; }

        /// <summary>
        /// Gets or sets AssemblyFileVersion of NCrash.dll assembly.
        /// </summary>
        public string NCrashVersion { get; set; }

        public string TargetSite { get; set; }

        public string UserDescription { get; set; }

        public override string ToString()
        {
            return string.Format("{{ClrVersion: {0}, DateTime: {1}, ExceptionMessage: {2}, ExceptionType: {3}, HostApplication: {4}, HostApplicationVersion: {5}, NCrashVersion: {6}, TargetSite: {7}, UserDescription: {8}}}", 
                ClrVersion, DateTime, ExceptionMessage, ExceptionType, HostApplication, HostApplicationVersion, NCrashVersion, TargetSite, UserDescription);
        }
    }
}
