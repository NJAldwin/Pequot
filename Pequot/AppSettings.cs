using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace Pequot
{
    internal class AppSettings
    {
        private static SerializableSettings settings = new SerializableSettings();
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
            var ser = new XmlSerializer(typeof(SerializableSettings));
            if (fileName == "") return;
            using (var fs = new FileStream(fileName, FileMode.Create))
            {
                ser.Serialize(fs, settings);
            }
        }
        public static void Load(string filename)
        {
            var ser = new XmlSerializer(typeof(SerializableSettings));
            if (File.Exists(filename))
            {
                // check for old version of settings
                var xDoc = new XmlDocument();
                xDoc.Load(filename);
                // old settings files started with <dictionary>.  The new root is <settings>.
                bool oldVer = xDoc.DocumentElement != null && xDoc.DocumentElement.Name == "dictionary";

                using (var fs = new FileStream(filename, FileMode.Open))
                {
                    if(oldVer)
                    {
                        // read from old version of settings
                        ser = new XmlSerializer(typeof (SerializableDictionary<string, string>));
                        settings =
                            SerializableSettings.FromSerializableDictionary(
                                (SerializableDictionary<string, string>) ser.Deserialize(fs));
                    }
                    else
                    {
                        settings = (SerializableSettings) ser.Deserialize(fs);
                    }
                }
            }
            else
            {
                settings = new SerializableSettings();
                using (var fs = new FileStream(filename, FileMode.Create))
                {
                    ser.Serialize(fs, settings);
                }
            }
            fileName = filename;
        }
    }
}
