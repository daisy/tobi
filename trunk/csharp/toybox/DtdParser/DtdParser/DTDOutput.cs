using System.IO;

namespace DtdParser
{
    public interface IDTDOutput
    {
        void write(StreamWriter writer);
    }
}
