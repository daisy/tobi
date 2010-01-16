using System.Collections.Generic;

namespace DtdParser
{
    public abstract class DTDContainer : DTDItem
    {
        public List<DTDItem> items{ get; set;}

    /** Creates a new DTDContainer */
        public DTDContainer()
        {
            items = new List<DTDItem>();
        }

        public bool equals(object ob)
        {
           if (ob == this) return true;
            if (!(ob is DTDContainer)) return false;

            if (!base.equals(ob)) return false;

            DTDContainer other = (DTDContainer) ob;

            return items.Equals(other.items);
        }

   
        /*public override void write(StreamWriter writer)
        {
        }*/

    }
}