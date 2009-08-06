using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Tobi.Common._UnusedCode
{
    public static class ObjectExtensions
    {
        public static T DeepCopy<T>(this T obj) where T : class
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, obj);
            ms.Position = 0;
            T copy = bf.Deserialize(ms) as T;
            ms.Close();
            return copy;
        }
    }
}
