using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace NCrash.Core
{
    [Serializable]
    [XmlRoot("dictionary")]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableDictionary{TKey,TValue}"/> class.
        /// This is the default constructor provided for XML serializer.
        /// </summary>
        public SerializableDictionary()
        {
        }

        public SerializableDictionary(IDictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException();
            }

            foreach (var pair in dictionary)
            {
                Add(pair.Key, pair.Value);
            }
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            var xElement = XElement.Load(reader.ReadSubtree());
            if (xElement.HasElements)
            {
                foreach (var element in xElement.Elements())
                {
                    Add((TKey)Convert.ChangeType(element.Name.ToString(), typeof(TKey)), (TValue)Convert.ChangeType(element.Value, typeof(TValue)));
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (var key in Keys)
            {
                writer.WriteStartElement(key.ToString());
                writer.WriteValue(this[key]);
                writer.WriteEndElement();
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder("{");
            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                sb.Append(pair.Key).Append("=").Append(pair.Value).Append(",");
            }
            sb.Append("}");
            return sb.ToString();
        }
    }
}
