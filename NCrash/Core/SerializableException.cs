using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NCrash.Core
{
    public class SerializableException
    {
        private static readonly HashSet<string> ExcludedProperties = new HashSet<string> {"Data", "InnerExceptions", "InnerException",  "Message", "Source", "StackTrace", "TargetSite", "HelpLink"};

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableException"/> class. 
        /// Default constructor provided for XML serialization and de-serialization.
        /// </summary>
        public SerializableException()
        {
        }

        public SerializableException(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException();
            }

            Type = exception.GetType().ToString();

            if (exception.Data.Count != 0)
            {
                foreach (DictionaryEntry entry in exception.Data)
                {
                    if (entry.Value != null)
                    {
                        // Assign 'Data' property only if there is at least one entry with non-null value
                        if (Data == null)
                        {
                            Data = new SerializableDictionary<object, object>();
                        }

                        Data.Add(entry.Key, entry.Value);
                    }
                }
            }

            if (exception.HelpLink != null)
            {
                HelpLink = exception.HelpLink;
            }

            if (exception.InnerException != null)
            {
                InnerException = new SerializableException(exception.InnerException);
            }

            var aggregateException = exception as AggregateException;
            if (aggregateException != null)
            {
                InnerExceptions = new List<SerializableException>();

                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    InnerExceptions.Add(new SerializableException(innerException));
                }

                if (InnerExceptions.Count > 1)
                {
                    // remove first in queue
                    InnerExceptions.RemoveAt(0);
                }
            }

            Message = exception.Message != string.Empty ? exception.Message : string.Empty;

            if (exception.Source != null)
            {
                Source = exception.Source;
            }

            if (exception.StackTrace != null)
            {
                StackTrace = exception.StackTrace;
            }

            if (exception.TargetSite != null)
            {
                TargetSite = exception.TargetSite + " @ " + exception.TargetSite.DeclaringType;
            }

            ExtendedInformation = GetExtendedInformation(exception);
        }

        public SerializableDictionary<object, object> Data { get; set; }

        public SerializableDictionary<string, object> ExtendedInformation { get; set; }

        public string HelpLink { get; set; }

        public SerializableException InnerException { get; set; }

        public List<SerializableException> InnerExceptions { get; set; }

        public string Message { get; set; }

        public string Source { get; set; }

        public string StackTrace { get; set; }

        // This will make TargetSite property XML serializable but RuntimeMethodInfo class does not have a parameterless
        // constructor thus the serializer throws an exception if full info is used
        public string TargetSite { get; set; }

        public string Type { get; set; }

        private SerializableDictionary<string, object> GetExtendedInformation(Exception exception)
        {
            var extendedInformation = new SerializableDictionary<string, object>();

            foreach (PropertyInfo info in exception.GetType().GetProperties())
            {
                if (ExcludedProperties.Contains(info.Name) || !info.CanRead)
                    continue;
                extendedInformation.Add(info.Name, info.GetValue(exception, null));
            }
            return extendedInformation.Count != 0 ? extendedInformation : null;
        }

        public override string ToString()
        {
            var sb = new StringBuilder("[");
            foreach (SerializableException inner in InnerExceptions)
            {
                sb.Append(inner).Append(",");
            }
            sb.Append("]");
            return string.Format("{{ExtendedInformation: {0}, HelpLink: {1}, InnerException: {2}, InnerExceptions: {3}, Message: {4}, Source: {5}, StackTrace: {6}, TargetSite: {7}, Type: {8}}}", 
                ExtendedInformation, HelpLink, InnerException, sb, Message, Source, StackTrace, TargetSite, Type);
        }
    }
}
