
using System.IO;

namespace DtdParser
{
    public class DTDPublic : DTDExternalID
    {
        public string Pub { get; set;}

        /** Writes out a public external ID declaration */
        public override void write(StreamWriter writer)
        {
            writer.Write("PUBLIC \"");
            writer.Write(Pub);
            writer.Write("\"");
            if (system != null)
            {
                writer.Write(" \"");
                writer.Write(system);
                writer.Write("\"");
            }
        }

        public bool equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDPublic)) return false;

            if (!base.equals(ob)) return false;

            DTDPublic other = (DTDPublic) ob;

            if (Pub == null)
            {
                if (other.Pub != null) return false;
            }
            else
            {
                if (!Pub.Equals(other.Pub)) return false;
            }

            return true;
        }
    }
}