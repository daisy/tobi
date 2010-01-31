using System.IO;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdSharp
{
    public abstract class DTDExternalID : IDTDOutput
    {
        public string System { get; set;}

        /** Writes out a declaration for this external ID */
        public abstract void Write(StreamWriter writer);

        public override bool Equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDExternalID)) return false;

            DTDExternalID other = (DTDExternalID) ob;

            if (System == null)
            {
                if (other.System != null) return false;
            }
            else
            {
                if (!System.Equals(other.System)) return false;
            }

            return true;
        }
    }
}