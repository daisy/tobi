using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using System.Windows.Media;

namespace Tobi.Common.UI
{
    public static class FontsFix
    {
        private static ReadOnlyCollection<FontFamily> m_SystemFontFamilies;
        public static ReadOnlyCollection<FontFamily> SystemFontFamilies
        {
            get
            {
                if (m_SystemFontFamilies == null)
                {
                    var sysFontFamilies = new List<FontFamily>();
                    foreach (FontFamily sysFontFamily in Fonts.SystemFontFamilies)
                    {
                        try
                        {
                            var familyNames = sysFontFamily.FamilyNames; // exception possible

                            foreach (KeyValuePair<XmlLanguage, string> familyName in familyNames)
                            {
                                Console.WriteLine(familyName.Key + @" => " + familyName.Value);
                            }

                            sysFontFamilies.Add(sysFontFamily);
                        }
                        catch (ArgumentException)
                        {
                            // WPF 4 bug => possible exception with some font names (due to multiple CultureInfo)
                        }
                    }
                    m_SystemFontFamilies = sysFontFamilies.AsReadOnly();
                }

                return m_SystemFontFamilies;
            }
        }
    }
}
