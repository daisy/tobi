
using System.Collections;
using System.IO;

namespace DtdParser
{
    public class DTDElement : IDTDOutput
    {
        /** The name of the element */
        public string name { get; set;}

        /** The element's attributes */
        public Hashtable attributes { get; set;}
        
        /** The element's content */
        public DTDItem content { get; set;}

        public DTDElement()
        {
            attributes = new Hashtable();
        }

        public DTDElement(string aName)
        {
            name = aName;

            attributes = new Hashtable();
        }

        /** Writes out an element declaration and an attlist declaration (if necessary)
            for this element */
        public void write(StreamWriter writer)
            
        {
            writer.Write("<!ELEMENT ");
            writer.Write(name);
            writer.Write(" ");
            if (content != null)
            {
                content.write(out);
            }
            else
            {
                writer.Write("ANY");
            }
            writer.WriteLine(">");
            writer.WriteLine();

    /* original java comment
     * 
            if (attributes.size() > 0)
            {
                writer.Write("<!ATTLIST ");
                writer.WriteLine(name);
	        TreeMap tm=new TreeMap(attributes);
	        Collection values=tm.values();
	        Iterator iterator=values.iterator();
	        while (iterator.hasNext())
	        {
                    writer.Write("           ");
                    DTDAttribute attr = (DTDAttribute) iterator.next();
                    attr.write(out);
		    if (iterator.hasNext())
                	    writer.WriteLine();
		    else
	                    writer.WriteLine(">");
	        }
            }
    */
        }

        public bool equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDElement)) return false;

            DTDElement other = (DTDElement) ob;

            if (name == null)
            {
                if (other.name != null) return false;
            }
            else
            {
                if (!name.Equals(other.name)) return false;
            }

            if (attributes == null)
            {
                if (other.attributes != null) return false;
            }
            else
            {
                if (!attributes.Equals(other.attributes)) return false;
            }

            if (content == null)
            {
                if (other.content != null) return false;
            }
            else
            {
                if (!content.equals(other.content)) return false;
            }

            return true;
        }

    }
}