using System.IO;

/*
 * based on the Java Wutka DTD Parser by Mark Wutka (http://www.wutka.com/)
 */
namespace DtdSharp
{
    public interface IDTDOutput
    {
        void Write(StreamWriter writer);
    }
}
