using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Tobi.Common._UnusedCode
{
    public static class FlowDocumentReaderExtensions
    {
        public static TextPointer GetPositionFromPoint(FlowDocumentReader thiz, Point searchForPoint)
        {
            foreach (Run curRun in WpfTreeHelper.GetChildren<Run>(thiz, true))
            {
                TextPointer ptr = RunExtensions.GetPositionFromPoint(curRun, searchForPoint);

                if (ptr != null)
                {
                    return ptr;
                }
            }
            return null;
        }

        private static MethodInfo m_FindMethod = null;

        public static TextRange FindText(TextPointer findContainerStartPosition, TextPointer findContainerEndPosition, String input, FindFlags flags, CultureInfo cultureInfo)
        {
            TextRange textRange = null;
            if (findContainerStartPosition.CompareTo(findContainerEndPosition) < 0)
            {
                try
                {
                    if (m_FindMethod == null)
                    {
                        m_FindMethod = typeof(FrameworkElement).Assembly.GetType("System.Windows.Documents.TextFindEngine").
                            GetMethod("Find", BindingFlags.Static | BindingFlags.Public);
                    }
                    Object result = m_FindMethod.Invoke(null, new Object[] { findContainerStartPosition,
                                                                             findContainerEndPosition,
                                                                             input, flags, CultureInfo.CurrentCulture });
                    textRange = result as TextRange;
                }
                catch (ApplicationException)
                {
                    textRange = null;
                }
            }

            return textRange;
        }
    }

    [Flags]
    public enum FindFlags
    {
        FindInReverse = 2,
        FindWholeWordsOnly = 4,
        MatchAlefHamza = 0x20,
        MatchCase = 1,
        MatchDiacritics = 8,
        MatchKashida = 0x10,
        None = 0
    }
}