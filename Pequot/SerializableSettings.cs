using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Pequot
{
    [XmlRoot("settings")]
    public class SerializableSettings
        : Dictionary<string, string>, IXmlSerializable
    {
        

        #region IXmlSerializable Members
        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();

            if (wasEmpty)
                return;

            while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
            {
                string key = XmlConvert.DecodeName(reader.Name);
                string value = reader.ReadString();

                this.Add(key, value);

                reader.Read();
            }
            reader.ReadEndElement();
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            foreach (String key in this.Keys)
            {
                writer.WriteStartElement(XmlConvert.EncodeName(key));
                writer.WriteString(this[key]);
                writer.WriteEndElement();
            }
        }
        #endregion
    }
}
