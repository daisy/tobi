using System.IO;

namespace DtdParser
{
    public class DTDNotation : IDTDOutput
    {
        public string name { get; set;}
        public DTDExternalID externalID { get; set;}

        public DTDNotation(string aName)
        {
            name = aName;
        }

        /** Writes out a declaration for this notation */
        public void write(StreamWriter writer)
            
        {
            writer.Write("<!NOTATION ");
            writer.Write(name);
            writer.Write(" ");
            externalID.write(writer);
            writer.WriteLine(">");
        }

        public bool equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDNotation)) return false;

            DTDNotation other = (DTDNotation) ob;

            if (name == null)
            {
                if (other.name != null) return false;
            }
            else
            {
                if (!name.Equals(other.name)) return false;
            }

            if (externalID == null)
            {
                if (other.externalID != null) return false;
            }
            else
            {
                if (!externalID.equals(other.externalID)) return false;
            }

            return true;
        }
    }
}