using System.IO;

namespace DtdParser
{
    public class DTDName : DTDItem
    {
        public string value { get; set; }

        public DTDName(string aValue)
        {
            value = aValue;
        }

        /** Writes out the value of this name */
        public override void write(StreamWriter writer)
            
        {
            writer.Write(value);
            cardinal.write(writer);
        }

        public bool equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDName)) return false;
            if (!base.equals(ob)) return false;

            DTDName other = (DTDName) ob;

            if (value == null)
            {
                if (other.value != null) return false;
            }
            else
            {
                if (!value.Equals(other.value)) return false;
            }
            return true;
        }
    }
}
