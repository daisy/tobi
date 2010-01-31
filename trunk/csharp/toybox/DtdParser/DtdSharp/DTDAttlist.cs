using System.IO;
using System.Collections.Generic;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdParser
{
    public class DTDAttlist : IDTDOutput
    {
    /** The name of the element */
        public string Name { get; set;}

    /** The attlist's attributes */
        public List<DTDAttribute> Attributes { get; set; }

        public DTDAttlist()
        {
            Attributes = new List<DTDAttribute>();
        }

        public DTDAttlist(string aName)
        {
            Name = aName;

            Attributes = new List<DTDAttribute>();
        }

    /** Writes out an ATTLIST declaration */
        public void Write(StreamWriter writer)
            
        {
            writer.Write("<!ATTLIST ");
            writer.WriteLine(Name);

            foreach (DTDAttribute attr in Attributes)
            {
                writer.Write("           ");
                attr.Write(writer);
                writer.WriteLine();
            }
            writer.WriteLine(">");
        }

        public override bool Equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDAttlist)) return false;

            DTDAttlist other = (DTDAttlist) ob;

            if ((Name == null) && (other.Name != null)) return false;
            if ((Name != null) && Name != other.Name) return false;

            return Attributes.Equals(other.Attributes);
        }

    }
}