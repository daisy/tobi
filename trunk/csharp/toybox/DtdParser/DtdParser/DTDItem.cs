using System;
using System.IO;

namespace DtdParser
{
    public abstract class DTDItem : IDTDOutput
    {
        /** Indicates how often the item may occur */
        public DTDCardinal cardinal { get; set}

        public DTDItem()
        {
            cardinal = DTDCardinal.NONE;
        }

        public DTDItem(DTDCardinal aCardinal)
        {
            cardinal = aCardinal;
        }

        /** Writes out a declaration for this item */
        public abstract void write(StreamWriter writer);

        public bool equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDItem)) return false;

            DTDItem other = (DTDItem)ob;

            if (cardinal == null)
            {
                if (other.cardinal != null) return false;
            }
            else
            {
                if (!cardinal.equals(other.cardinal)) return false;
            }

            return true;
        }

        
    }
}