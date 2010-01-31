
using System.Collections.Generic;
using System.IO;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdSharp
{
    public class DTDEnumeration : IDTDOutput
    {
        public List<string> Items { get; set;}

        /** Creates a new enumeration */
        public DTDEnumeration()
        {
            Items = new List<string>();
        }

        /** Writes out a declaration for this enumeration */
        public void Write(StreamWriter writer)
        {
            writer.Write("( ");

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
            if (!(ob is DTDEnumeration)) return false;

            DTDEnumeration other = (DTDEnumeration) ob;
            return Items.Equals(other.Items);
        }

    }
}