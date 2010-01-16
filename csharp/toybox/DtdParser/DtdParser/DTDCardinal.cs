using System.IO;

namespace DtdParser
{
   public class DTDCardinal : IDTDOutput
    {
    /** Indicates no cardinality (implies a single object) */
        public static DTDCardinal NONE = new DTDCardinal(0, "NONE");

    /** Indicates that an item is optional (zero-to-one) */
        public static DTDCardinal OPTIONAL = new DTDCardinal(1, "OPTIONAL");

    /** Indicates that there can be zero-to-many occurrances of an item */
        public static DTDCardinal ZEROMANY = new DTDCardinal(2, "ZEROMANY");

    /** Indicates that there can be one-to-many occurrances of an item */
        public static DTDCardinal ONEMANY = new DTDCardinal(3, "ONEMANY");

        public int type;
        public string name;

        public DTDCardinal(int aType, string aName)
        {
                type = aType;
                name = aName;
        }

        public bool equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDCardinal)) return false;

            DTDCardinal other = (DTDCardinal) ob;
            if (other.type == type) return true;
            return false;
        }

    /** Writes the notation for this cardinality value */
        public void write(StreamWriter writer)
            
        {
            if (this == NONE) return;
            if (this == OPTIONAL)
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