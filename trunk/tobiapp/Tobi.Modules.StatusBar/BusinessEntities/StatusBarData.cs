
using System.Globalization;

namespace Tobi.Modules.StatusBar
{
    public class StatusBarData
    {
        private const string stringFormat = "[{0}] / {1}";

        public const string ViewName = "StatusBarView";

        public string Str1 { get; set; }
        public string Str2 { get; set; }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, stringFormat, Str1, Str2);
        }
    }
}
