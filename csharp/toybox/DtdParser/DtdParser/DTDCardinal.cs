using System.IO;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdParser
{
   public class DTDCardinal : IDTDOutput
    {
    /** Indicates no cardinality (implies a single object) */
        public static DTDCardinal NONE = new DTDCardinal(0, "NONE");

    /** Indicates that there can be zero or one occurrances of an item "?" */
        public static DTDCardinal ZEROONE = new DTDCardinal(1, "ZEROONE");

    /** Indicates that there can be zero-to-many occurrances of an item "*" */
        public static DTDCardinal ZEROMANY = new DTDCardinal(2, "ZEROMANY");

    /** Indicates that there can be one-to-many occurrances of an item "+" */
        public static DTDCardinal ONEMANY = new DTDCardinal(3, "ONEMANY");

        public int Type { get; set;}
        public string Name { get; set; }

        public DTDCardinal(int aType, string aName)
        {
                Type = aType;
                Name = aName;
        }

        public override bool Equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDCardinal)) return false;

            DTDCardinal other = (DTDCardinal) ob;
            if (other.Type == Type) return true;
            return false;
        }

    /** Writes the notation for this cardinality value */
        public void Write(StreamWriter writer)
        {
            if (this == NONE) return;
            if (this == ZEROONE)
            {
                writer.Write("?");
            }
            else if (this == ZEROMANY)
            {
                writer.Write("*");
            }
            else if (this == ONEMANY)
            {
                writer.Write("+");
            }
        }
    }
}