using System.IO;

namespace DtdParser
{
    public class DTDPCData : DTDItem
    {

        /** Writes out the #PCDATA keyword */
        public override void write(StreamWriter writer)
            
        {
            writer.Write("#PCDATA");
            cardinal.write(writer);
        }

        public bool equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDPCData)) return false;

            return base.equals(ob);
        }
    }
}
