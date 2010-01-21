using System.Collections.Generic;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdParser
{
    public abstract class DTDContainer : DTDItem
    {
        public List<DTDItem> Items{ get; set;}

    /** Creates a new DTDContainer */
        public DTDContainer()
        {
            Items = new List<DTDItem>();
        }

        public override bool Equals(object ob)
        {
           if (ob == this) return true;
            if (!(ob is DTDContainer)) return false;

            if (!base.Equals(ob)) return false;

            DTDContainer other = (DTDContainer) ob;

            return Items.Equals(other.Items);
        }

    }
}