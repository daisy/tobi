
using System.Collections;
using System.IO;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdParser
{
    public class DTDElement : IDTDOutput
    {
        /** The name of the element */
        public string Name { get; set;}

        /** The element's attributes */
        public Hashtable Attributes { get; set;}
        
        /** The element's content */
        public DTDItem Content { get; set;}

        public DTDElement()
        {
            Attributes = new Hashtable();
        }

        public DTDElement(string aName)
        {
            Name = aName;
            Attributes = new Hashtable();
        }

        /** Writes out an element declaration and an attlist declaration (if necessary)
            for this element */
        public void Write(StreamWriter writer)     
        {
            writer.Write("<!ELEMENT ");
            writer.Write(Name);
            writer.Write(" ");
            if (Content != null)
            {
                Content.Write(writer);
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

        public override bool Equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDElement)) return false;

            DTDElement other = (DTDElement) ob;

            if (Name == null)
            {
                if (other.Name != null) return false;
            }
            else
            {
                if (!Name.Equals(other.Name)) return false;
            }

            if (Attributes == null)
            {
                if (other.Attributes != null) return false;
            }
            else
            {
                if (!Attributes.Equals(other.Attributes)) return false;
            }

            if (Content == null)
            {
                if (other.Content != null) return false;
            }
            else
            {
                if (!Content.Equals(other.Content)) return false;
            }

            return true;
        }

    }
}