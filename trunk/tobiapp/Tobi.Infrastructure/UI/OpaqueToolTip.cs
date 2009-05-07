using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Tobi.Infrastructure.UI
{
    /// <summary>
    ///  <Button.ToolTip>
    ///      <my:OpaqueToolTip StaysOpen="True" >
    ///         <WebBrowser Source="http://www.me.com" />
    ///     </my:OpaqueToolTip>
    /// </Button.ToolTip>
    /// </summary>
    public class OpaqueToolTip : ToolTip
    {
        protected override void OnTemplateChanged(ControlTemplate oldTemplate, ControlTemplate newTemplate)
        {
            if (newTemplate != null)
            {
                this.Visibility = ((UIElement) this).Visibility.Collapsed;
                this.IsOpen = true;
                Popup popup = GetPopupFromVisualChild(this);
                if (popup != null) popup.AllowsTransparency = false;
                this.IsOpen = false;
                this.Visibility = ((UIElement) this).Visibility.Visible;
            }
        }

        private static Popup GetPopupFromVisualChild(Visual child)
        {
            Visual parent = child;
            FrameworkElement visualRoot = null;
            while (parent != null)
            {
                visualRoot = parent as FrameworkElement;
                parent = VisualTreeHelper.GetParent(parent) as Visual;
            }

            Popup popup = null;
            if (visualRoot != null)
            {
                popup = visualRoot.Parent as Popup;
            }

            return popup;
        }
    }
}
