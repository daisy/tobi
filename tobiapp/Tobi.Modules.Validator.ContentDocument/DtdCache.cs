using System.Collections;
using System.IO;
using System.Text.RegularExpressions;

namespace UrakawaDomValidation
{
    public class DtdCache
    {
        /// <summary>
        ///  Read from cache into a hashtable
        /// 
        /// Cache file format:
        /// line 1: Element name
        /// line 2: Regex string of element's allowed children
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static Hashtable ReadFromCache(StreamReader reader)
        {
            Hashtable hashtable = new Hashtable();
            string name = reader.ReadLine();
            string regExpStr = reader.ReadLine();

            while (name != null && regExpStr != null)
            {
                Regex regEx = new Regex(regExpStr);
                hashtable[name] = regEx;
                name = reader.ReadLine();
                regExpStr = reader.ReadLine();
            }
            reader.Close();
            return hashtable;
        }

        /// <summary>
        /// Write hashtable data to cache
        /// 
        /// The hashtable is formatted like:
        /// key: element name
        /// value: regex (object) of element's allowed children
        /// 
        /// Cache file format:
        /// line 1: Element name
        /// line 2: Regex string of element's allowed children
        /// </summary>
        /// <param name="hashtable"></param>
        /// <param name="writer"></param>
        public static void WriteToCache(Hashtable hashtable, StreamWriter writer)
        {
            foreach (DictionaryEntry entry in hashtable)
            {
                string name = (string)entry.Key;
                string regExpStr = ((Regex)entry.Value).ToString();
                writer.WriteLine(name);
                writer.WriteLine(regExpStr);
            }
            writer.Close();
        }
    }
}
