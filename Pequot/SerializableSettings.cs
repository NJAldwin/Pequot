using System;
using System.Collections.Generic;
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

        public void ReadXml(XmlReader reader)
        {
            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();

            if (wasEmpty)
                return;

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                // Convert from human-readable element names (see below)
                string key = XmlConvert.DecodeName(reader.Name.Replace("___", "_x0020_").Replace("__x005F__", "___"));
                string value = reader.ReadString();

                if(key!=null)
                {
                    if(ContainsKey(key))
                        // update if already exists
                        this[key] = value;
                    else
                        Add(key, value);
                }

                reader.Read();
            }
            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (String key in Keys)
            {
                // Convert to human-readable element names by substituting three underscores for an encoded space (2nd Replace)
                // and making sure existing triple-underscores will not cause confusion by substituting with partial encoding
                string encoded = XmlConvert.EncodeName(key);
                if (encoded == null) continue;
                writer.WriteStartElement(encoded.Replace("___", "__x005F__").Replace("_x0020_", "___"));
                writer.WriteString(this[key]);
                writer.WriteEndElement();
            }
        }
        #endregion

        /// <summary>
        /// Creates a <see cref="SerializableSettings"/> from a SerializableDictionary&lt;string, string&gt;.  
        /// Useful for converting from old settings files to new settings files.
        /// </summary>
        /// <param name="dict">The SerializableDictionary&lt;string, string&gt; to convert from.</param>
        /// <returns>A new <see cref="SerializableSettings"/> containing all the settings from the dictionary.</returns>
        internal static SerializableSettings FromSerializableDictionary(SerializableDictionary<string, string> dict)
        {
            var settings = new SerializableSettings();
            foreach(var pair in dict)
            {
                settings.Add(pair.Key, pair.Value);
            }
            return settings;
        }
    }
}
