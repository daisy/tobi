using System.IO;

namespace DtdParser
{
    
    public class DTDAny : DTDItem
    {

    /** Writes "ANY" to a print writer */
        public override void write(StreamWriter writer)
        {
            writer.WriteLine("ANY");
            cardinal.write(writer);
        }

        public bool equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDAny)) return false;

            return base.equals(ob);
        }
    }
}