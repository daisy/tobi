
using System.IO;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdSharp
{
    public class DTDPublic : DTDExternalID
    {
        public string Pub { get; set;}

        /** Writes out a public external ID declaration */
        public override void Write(StreamWriter writer)
        {
            writer.Write("PUBLIC \"");
            writer.Write(Pub);
            writer.Write("\"");
            if (System != null)
            {
                writer.Write(" \"");
                writer.Write(System);
                writer.Write("\"");
            }
        }

        public override bool Equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDPublic)) return false;

            if (!base.Equals(ob)) return false;

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