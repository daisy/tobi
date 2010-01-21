using System.IO;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdParser
{
    public class DTDProcessingInstruction : IDTDOutput
    {
        /** The processing instruction text */
        public string Text { get; set; }

        public DTDProcessingInstruction(string theText)
        {
            Text = theText;
        }

        public override string ToString()
        {
            return Text;
        }

        public void Write(StreamWriter writer)
        {
            writer.Write("<?");
            writer.Write(Text);
            writer.WriteLine("?>");
        }

        public override bool Equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDProcessingInstruction)) return false;

            DTDProcessingInstruction other = (DTDProcessingInstruction) ob;

            if (Text == null)
            {
                if (other.Text != null) return false;
            }
            else
            {
                if (!Text.Equals(other.Text)) return false;
            }

            return true;
        }
    }
}