using System.IO;

namespace DtdParser
{
    
    public class DTDDecl : IDTDOutput
    {
        public static DTDDecl FIXED = new DTDDecl(0, "FIXED");
        public static DTDDecl REQUIRED = new DTDDecl(1, "REQUIRED");
        public static DTDDecl IMPLIED = new DTDDecl(2, "IMPLIED");
        public static DTDDecl VALUE = new DTDDecl(3, "VALUE");

        public int type { get; set;}
        public string name { get; set;}

        public DTDDecl(int aType, string aName)
        {
                type = aType;
                name = aName;
        }

        public bool equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDDecl)) return false;

            DTDDecl other = (DTDDecl) ob;
            if (other.type == type) return true;
            return false;
        }

        public void write(StreamWriter writer)
            
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