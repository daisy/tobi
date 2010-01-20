using System.IO;

namespace DtdParser
{
    public class DTDSequence : DTDContainer
    {
        public DTDSequence()
        {
        }

        /** Writes out a declaration for this sequence */
        public override void write(StreamWriter writer)
            
        {
            writer.Write("(");

            bool isFirst = true;

            foreach (DTDItem item in items)
            {
                if (!isFirst) writer.Write(",");
                isFirst = false;

                item.write(writer);
            }
            writer.Write(")");
            cardinal.write(writer);
        }

        public bool equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDSequence)) return false;

            return base.equals(ob);
        }
    }
}