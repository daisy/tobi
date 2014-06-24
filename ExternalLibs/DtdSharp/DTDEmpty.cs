using System.IO;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdSharp
{
    public class DTDEmpty : DTDItem
    {
        /** Writes out the keyword "EMPTY" */
        public override void Write(StreamWriter writer)   
        {
            writer.Write("EMPTY");
            Cardinal.Write(writer);
        }

        public override bool Equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDEmpty)) return false;
            return base.Equals(ob);
        }
    }
}