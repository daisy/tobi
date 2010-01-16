using System.IO;
namespace DtdParser
{
    public class DTDEmpty : DTDItem
    {
        public DTDEmpty()
        {
        }

        /** Writes out the keyword "EMPTY" */
        public override void write(StreamWriter writer)
        
    {
        writer.Write("EMPTY");
        cardinal.write(out);
    }

        public bool equals(object ob)
    {
        if (ob == this) return true;
        if (!(ob is DTDEmpty)) return false;
        return base.equals(ob);
    }
    }
}