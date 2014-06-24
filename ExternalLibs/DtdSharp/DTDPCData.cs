using System.IO;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdSharp
{
    public class DTDPCData : DTDItem
    {
        /** Writes out the #PCDATA keyword */
        public override void Write(StreamWriter writer)
        {
            writer.Write("#PCDATA");
            Cardinal.Write(writer);
        }

        public override bool Equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDPCData)) return false;

            return base.Equals(ob);
        }
    }
}
