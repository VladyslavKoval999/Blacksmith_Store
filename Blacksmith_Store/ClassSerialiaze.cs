using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace Blacksmith_Store
{
    public static class ClassSerialiaze
    {
        public static void SerializeToXml<T>(ref T inObject, string fileName)
        {
            try
            {
                XmlSerializer writer = new XmlSerializer(typeof(T));
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(fileName))
                {
                    writer.Serialize(file, inObject);
                }
            }
            catch (Exception ex) { MessageBox.Show("Помилка серіалізації: " + ex.Message); }
        }

        public static void DeserializeFromXml<T>(ref T inObject, string inFileName)
        {
            if (System.IO.File.Exists(inFileName))
            {
                System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(T));
                System.IO.StreamReader file = new System.IO.StreamReader(inFileName);
                inObject = (T)reader.Deserialize(file);
                file.Close();
            }
        }
    }
}
