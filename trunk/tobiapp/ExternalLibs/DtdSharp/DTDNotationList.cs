using System.Collections.Generic;
using System.IO;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdSharp
{
    public class DTDNotationList : IDTDOutput
    {
        public List<string> Items { get; set; }

        /** Creates a new notation */
        public DTDNotationList()
        {
            Items = new List<string>();
        }

        /** Writes a declaration for this notation */
        public void Write(StreamWriter writer)
        {
            writer.Write("NOTATION ( ");

            bool isFirst = true;
            foreach (string item in Items)
            {
                if (!isFirst) writer.Write(" | ");
                isFirst = false;
                writer.Write(item);
            }

            writer.Write(")");
        }

        public override bool Equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDNotationList)) return false;

            DTDNotationList other = (DTDNotationList)ob;
            return Items.Equals(other.Items);
        }

    }
}