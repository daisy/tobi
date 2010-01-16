using System.IO;

namespace DtdParser
{

    public abstract class DTDExternalID : IDTDOutput
    {
        public string system { get; set;}

        /** Writes out a declaration for this external ID */
        public abstract void write(StreamWriter writer);

        public bool equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDExternalID)) return false;

            DTDExternalID other = (DTDExternalID) ob;

            if (system == null)
            {
                if (other.system != null) return false;
            }
            else
            {
                if (!system.Equals(other.system)) return false;
            }

            return true;
        }
    }
}