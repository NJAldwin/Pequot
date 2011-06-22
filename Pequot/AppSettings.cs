using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace Pequot
{
    internal class AppSettings
    {
        //TODO: consider making this something else (e.g. a custom class) so that it serializes in a more human-readable format.
        private static SerializableDictionary<string, string> settings = new SerializableDictionary<string, string>();
        private static string fileName = "";
        public static string FileName { get { return fileName; } }

        public static string Get(string name)
        {
            return settings[name];
        }
        public static string Get(string name, string ifNotFound)
        {
            try
            {
                return Get(name);
            }
            catch
            {
                return ifNotFound;
            }
        }
        public static void Set(string name, string value)
        {
            settings[name] = value;
        }
        public static void Save()
        {
            XmlSerializer ser = new XmlSerializer(typeof(SerializableDictionary<string, string>));
            if (fileName != "")
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Create))
                {
                    ser.Serialize(fs, settings);
                }
            }
        }
        public static void Load(string filename)
        {
            XmlSerializer ser = new XmlSerializer(typeof(SerializableDictionary<string, string>));
            if (File.Exists(filename))
            {
                using (FileStream fs = new FileStream(filename, FileMode.Open))
                {
                    settings = (SerializableDictionary<string, string>)ser.Deserialize(fs);
                }
            }
            else
            {
                settings = new SerializableDictionary<string, string>();
                using (FileStream fs = new FileStream(filename, FileMode.Create))
                {
                    ser.Serialize(fs, settings);
                }
            }
            fileName = filename;
        }
    }
}
