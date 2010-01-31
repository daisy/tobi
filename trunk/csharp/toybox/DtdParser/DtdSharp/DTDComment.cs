using System.IO;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdParser
{
    public class DTDComment : IDTDOutput
    {
        /** The comment text */
        public string Text { get; set;}

        public DTDComment()
        {
        }

        public DTDComment(string theText)
        {
            Text = theText;
        }
       
        public override string ToString()
        {
            return Text;
        }

        public void Write(StreamWriter writer)
        {
            writer.Write("<!--");
            writer.Write(Text);
            writer.WriteLine("-->");
        }

        public override bool Equals(object ob)
        {
            if (ob == this) return true;
            if (!(ob is DTDComment)) return false;

            DTDComment other = (DTDComment) ob;
            if ((Text == null) && (other.Text != null)) return false;
            if ((Text != null) && !Text.Equals(other.Text)) return false;

            return true;
        }
    }
}