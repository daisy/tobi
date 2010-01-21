using System.IO;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdParser
{
    
    public class DTDAny : DTDItem
    {

    /** Writes "ANY" to a print writer */
        public override void Write(StreamWriter writer)
        {
            writer.WriteLine("ANY");
            Cardinal.Write(writer);
        }

        public override bool Equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDAny)) return false;

            return base.Equals(ob);
        }
    }
}