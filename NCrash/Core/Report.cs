using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace NCrash.Core
{
    /// <summary>
    /// Error report to send
    /// </summary>
    [Serializable]
    public class Report
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Report"/> class to be filled with information later on.
        /// </summary>
        public Report()
        {
        }

        internal Report(SerializableException serializableException)
        {
            Exception = serializableException;
            GeneralInfo = new GeneralInfo(serializableException);
        }

        /// <summary>
        /// Gets or sets a custom object property to store user supplied information in the bug report. You need to handle
        /// <see cref="ErrorReporter.ProcessingException"/> event to fill this property with required information. 
        /// </summary>
        public object CustomInfo { get; set; }

        /// <summary>
        /// Gets or sets the general information about the exception and the system to be presented in the bug report.
        /// Public setter requred for de-serializing.
        /// </summary>
        public GeneralInfo GeneralInfo { get; set; }

        public SerializableException Exception { get; set; }

        public override string ToString()
        {
            return string.Format("{{CustomInfo: {0}, GeneralInfo: {1}, Exception: {2}}}", CustomInfo, GeneralInfo, Exception);
        }

        [XmlIgnoreAttribute]
        public IList<Tuple<string, string>> ScreenshotList{get;set;}
    }
}
