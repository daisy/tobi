using System.Collections.Generic;
using System.IO;

namespace DtdParser
{
    public class DTDNotationList : IDTDOutput
    {
        public List<object> items { get; set; }

        /** Creates a new notation */
        public DTDNotationList()
        {
            items = new List<object>();
        }

        /** Writes a declaration for this notation */
        public void write(StreamWriter writer)
        {
            writer.Write("NOTATION ( ");

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
            if (!(ob is DTDNotationList)) return false;

            DTDNotationList other = (DTDNotationList)ob;
            return items.Equals(other.items);
        }

    }
}