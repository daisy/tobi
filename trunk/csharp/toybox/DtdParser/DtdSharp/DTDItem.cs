using System.IO;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdParser
{
    public abstract class DTDItem : IDTDOutput
    {
        /** Indicates how often the item may occur */
        public DTDCardinal Cardinal { get; set; }

        public DTDItem()
        {
            Cardinal = DTDCardinal.NONE;
        }

        public DTDItem(DTDCardinal aCardinal)
        {
            Cardinal = aCardinal;
        }

        /** Writes out a declaration for this item */
        public abstract void Write(StreamWriter writer);

        public override bool Equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDItem)) return false;

            DTDItem other = (DTDItem)ob;

            if (Cardinal == null)
            {
                if (other.Cardinal != null) return false;
            }
            else
            {
                if (!Cardinal.Equals(other.Cardinal)) return false;
            }

            return true;
        }

        
    }
}