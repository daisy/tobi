
using System.Collections.Generic;
using System.IO;

namespace DtdParser
{
    public class DTDEnumeration : IDTDOutput
    {
        public List<string> items { get; set;}

        /** Creates a new enumeration */
        public DTDEnumeration()
        {
            items = new List<string>();
        }

        /** Writes out a declaration for this enumeration */
        public void write(StreamWriter writer)
        {
            writer.Write("( ");

            bool isFirst = true;
            foreach (object item in items)
            {
                if (!isFirst) writer.Write(" | ");
                isFirst = false;

                writer.Write(item);
            }
            writer.Write(")");
        }

        public bool equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDEnumeration)) return false;

            DTDEnumeration other = (DTDEnumeration) ob;
            return items.Equals(other.items);
        }

    }
}