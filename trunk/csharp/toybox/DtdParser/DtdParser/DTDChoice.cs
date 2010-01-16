using System;
using System.IO;

namespace DtdParser
{
    public class DTDChoice : DTDContainer
    {  
        public override void write(StreamWriter writer)
        {
            writer.Write("(");
            bool isFirst = true;

            foreach (DTDItem item in items)
            {
                if (!isFirst) writer.Write(" | ");
                isFirst = false;
                item.write(writer);
            }

            writer.Write(")");
            cardinal.write(writer);
        }

        public bool equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDChoice)) return false;

            return base.equals(ob);
        }
    }
}