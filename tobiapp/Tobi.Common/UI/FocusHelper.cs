using System.Windows;
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
    }
}
