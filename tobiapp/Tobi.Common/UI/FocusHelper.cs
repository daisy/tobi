using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Tobi.Common.UI
{
    public static class FocusHelper
    {
        public static void Focus(DependencyObject obj, UIElement ui)
        {
            ui.Focus();
            FocusManager.SetFocusedElement(obj, ui);
            Keyboard.Focus(ui);
        }

        public struct TextBoxSelection
        {
            public int start;
            public int length;
        }

        public static void ConfigureReadOnlyTextBoxHack(TextBox tb, string txt, TextBoxSelection tbSel)
        {
            tb.AcceptsReturn = true;
            tb.IsReadOnly = false;

            tb.SelectionChanged+=((sender, e)=>
                                      {
                                          tbSel.start = tb.SelectionStart;
                                          tbSel.length = tb.SelectionLength;
                                      });

            tb.TextChanged += ((sender, e) =>
            {
                int start = tbSel.start;
                int length = tbSel.length;
                tb.Text = txt;
                tb.Select(start, length);
            });
        }
    }
}
