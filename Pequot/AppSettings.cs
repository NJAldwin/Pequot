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
            var ser = new XmlSerializer(typeof(SerializableDictionary<string, string>));
            if (fileName == "") return;
            using (var fs = new FileStream(fileName, FileMode.Create))
            {
                ser.Serialize(fs, settings);
            }
        }
        public static void Load(string filename)
        {
            var ser = new XmlSerializer(typeof(SerializableDictionary<string, string>));
            if (File.Exists(filename))
            {
                using (var fs = new FileStream(filename, FileMode.Open))
                {
                    settings = (SerializableDictionary<string, string>)ser.Deserialize(fs);
                }
            }
            else
            {
                settings = new SerializableDictionary<string, string>();
                using (var fs = new FileStream(filename, FileMode.Create))
                {
                    ser.Serialize(fs, settings);
                }
            }
            fileName = filename;
        }
    }
}
