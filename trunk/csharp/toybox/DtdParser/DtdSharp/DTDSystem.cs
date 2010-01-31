using System.IO;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdParser
{
    public class DTDSystem : DTDExternalID
    {
        /** Writes out a declaration for this SYSTEM ID */
        public override void Write(StreamWriter writer)
        {
            if (System != null)
            {
                writer.Write("SYSTEM \"");
                writer.Write(System);
                writer.Write("\"");
            }
        }

        public override bool Equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDSystem)) return false;

            return base.Equals(ob);
        }
    }
}
