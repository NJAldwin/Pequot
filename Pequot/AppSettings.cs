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
            XmlSerializer ser = new XmlSerializer(typeof(SerializableSettings));
            if (fileName == "") return;
            using (var fs = new FileStream(fileName, FileMode.Create))
            {
                ser.Serialize(fs, settings);
            }
        }
        public static void Load(string filename)
        {
            XmlSerializer ser = new XmlSerializer(typeof(SerializableSettings));
            if (File.Exists(filename))
            {
                using (var fs = new FileStream(filename, FileMode.Open))
                {
                    settings = (SerializableSettings)ser.Deserialize(fs);
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
