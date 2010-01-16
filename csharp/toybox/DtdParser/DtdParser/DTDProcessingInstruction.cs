using System.IO;

namespace DtdParser
{
    public class DTDProcessingInstruction : IDTDOutput
    {
        /** The processing instruction text */
        public string text { get; set; }

        public DTDProcessingInstruction(string theText)
        {
            text = theText;
        }

        public override string ToString()
        {
            return text;
        }

        public void write(StreamWriter writer)
        {
            writer.Write("<?");
            writer.Write(text);
            writer.WriteLine("?>");
        }

        public bool equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDProcessingInstruction)) return false;

            DTDProcessingInstruction other = (DTDProcessingInstruction) ob;

            if (text == null)
            {
                if (other.text != null) return false;
            }
            else
            {
                if (!text.Equals(other.text)) return false;
            }

            return true;
        }
    }
}