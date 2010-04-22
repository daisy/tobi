using System;

namespace Tobi.Common
{
    public static class UserInterfaceStrings
    {
        public static string EscapeMnemonic(string str)
        {
            return str.Replace("_", "");
        }
    }
}