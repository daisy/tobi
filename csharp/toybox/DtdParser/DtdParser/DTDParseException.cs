
using System;

namespace DtdParser
{
    public class DTDParseException : Exception
    {
        public string uriID { get; private set; }
        public int lineNumber { get; private set; }
        public int column { get; private set; }

        public DTDParseException()
        {
            lineNumber = -1;
            column = -1;
        }

        public DTDParseException(string message) : base(message)
        {
            uriID = "";
            lineNumber = -1;
            column = -1;
        }

        public DTDParseException(string message, int line, int col) : base("At line " + line + ", column " + col + ": " + message)
        {
            uriID = "";
            lineNumber = line;
            column = col;
        }

        public DTDParseException(string id, string message, int line, int col): base(((null != id && id.length() > 0) ? "URI " + id + " at " : "At ")
                    + "line " + line + ", column " + col + ": " + message)
        {
            uriID = "";
            if (null != id)
                uriID = id;

            lineNumber = line;
            column = col;
        }

    }
}