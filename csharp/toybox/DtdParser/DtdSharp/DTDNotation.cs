using System.IO;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdSharp
{
    public class DTDNotation : IDTDOutput
    {
        public string Name { get; set;}
        public DTDExternalID ExternalId { get; set;}

        public DTDNotation(string aName)
        {
            Name = aName;
        }

        /** Writes out a declaration for this notation */
        public void Write(StreamWriter writer)
        {
            writer.Write("<!NOTATION ");
            writer.Write(Name);
            writer.Write(" ");
            ExternalId.Write(writer);
            writer.WriteLine(">");
        }

        public override bool Equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDNotation)) return false;

            DTDNotation other = (DTDNotation) ob;

            if (Name == null)
            {
                if (other.Name != null) return false;
            }
            else
            {
                if (!Name.Equals(other.Name)) return false;
            }

            if (ExternalId == null)
            {
                if (other.ExternalId != null) return false;
            }
            else
            {
                if (!ExternalId.Equals(other.ExternalId)) return false;
            }

            return true;
        }
    }
}