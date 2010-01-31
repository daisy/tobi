using System.IO;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdParser
{
    public class DTDDecl : IDTDOutput
    {
        public static DTDDecl FIXED = new DTDDecl(0, "FIXED");
        public static DTDDecl REQUIRED = new DTDDecl(1, "REQUIRED");
        public static DTDDecl IMPLIED = new DTDDecl(2, "IMPLIED");
        public static DTDDecl VALUE = new DTDDecl(3, "VALUE");

        public int Type { get; set;}
        public string Name { get; set;}

        public DTDDecl(int aType, string aName)
        {
                Type = aType;
                Name = aName;
        }

        public override bool Equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDDecl)) return false;

            DTDDecl other = (DTDDecl) ob;
            if (other.Type == Type) return true;
            return false;
        }

        public void Write(StreamWriter writer)      
        {
            if (this == FIXED)
            {
                writer.Write(" #FIXED");
            }
            else if (this == REQUIRED)
            {
                writer.Write(" #REQUIRED");
            }
            else if (this == IMPLIED)
            {
                writer.Write(" #IMPLIED");
            }
            // Don't do anything for value since there is no associated DTD keyword
        }
    }
}