using System.IO;

namespace DtdParser
{
    public class DTDSystem : DTDExternalID
    {

        /** Writes out a declaration for this SYSTEM ID */
        public override void write(StreamWriter writer)
        {
            if (system != null)
            {
                writer.Write("SYSTEM \"");
                writer.Write(system);
                writer.Write("\"");
            }
        }

        public bool equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDSystem)) return false;

            return base.equals(ob);
        }
    }
}
