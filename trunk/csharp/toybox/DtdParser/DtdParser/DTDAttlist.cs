using System.IO;
using System.Collections.Generic;

namespace DtdParser
{
    public class DTDAttlist : IDTDOutput
    {
    /** The name of the element */
        public string name;

    /** The attlist's attributes */
        public List<object> attributes { get; set; }

        public DTDAttlist()
        {
            attributes = new List<object>();
        }

        public DTDAttlist(string aName)
        {
            name = aName;

            attributes = new List<object>();
        }

    /** Writes out an ATTLIST declaration */
        public void write(StreamWriter writer)
            
        {
            writer.Write("<!ATTLIST ");
            writer.WriteLine(name);

            foreach (object obj in attributes)
            {
                writer.Write("           ");
                DTDAttribute attr = (DTDAttribute) obj;
                attr.write(writer);
                
                writer.WriteLine();
            }
            writer.WriteLine(">");
        }

        public bool equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDAttlist)) return false;

            DTDAttlist other = (DTDAttlist) ob;

            if ((name == null) && (other.name != null)) return false;
            if ((name != null) && name != other.name) return false;

            return attributes.Equals(other.attributes);
        }

    /** Returns the entity name of this attlist */
        public string getName()
        {
            return name;
        }

    /** Sets the entity name of this attlist */
        public void setName(string aName)
        {
            name = aName;
        }
    }
}