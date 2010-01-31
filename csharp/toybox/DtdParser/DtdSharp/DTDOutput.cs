using System.IO;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdParser
{
    public interface IDTDOutput
    {
        void Write(StreamWriter writer);
    }
}
