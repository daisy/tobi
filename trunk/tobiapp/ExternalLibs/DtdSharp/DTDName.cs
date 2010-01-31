using System.IO;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdSharp
{
    public class DTDName : DTDItem
    {
        public string Value { get; set; }

        public DTDName(string aValue)
        {
            Value = aValue;
        }

        /** Writes out the value of this name */
        public override void Write(StreamWriter writer)
        {
            writer.Write(Value);
            Cardinal.Write(writer);
        }

        public override bool Equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDName)) return false;
            if (!base.Equals(ob)) return false;

            DTDName other = (DTDName) ob;

            if (Value == null)
            {
                if (other.Value != null) return false;
            }
            else
            {
                if (!Value.Equals(other.Value)) return false;
            }
            return true;
        }
    }
}
