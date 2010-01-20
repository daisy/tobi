using System.IO;

namespace DtdParser
{
    public class DTDComment : IDTDOutput
    {
        /** The comment text */
        public string text { get; set;}

        public DTDComment()
        {
        }

        public DTDComment(string theText)
        {
            text = theText;
        }
       
        public override string ToString()
        {
            return text;
        }

        public void write(StreamWriter writer)
        {
            writer.Write("<!--");
            writer.Write(text);
            writer.WriteLine("-->");
        }

        public bool equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDComment)) return false;

            DTDComment other = (DTDComment) ob;
            if ((text == null) && (other.text != null)) return false;
            if ((text != null) && !text.Equals(other.text)) return false;

            return true;
        }
    }
}