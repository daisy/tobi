using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Tobi.Infrastructure.UI
{
    public static class ListViewExtensions
    {
        public static ListViewItem GetItemAt(this ListView listView, Point clientRelativePosition)
        {
            var hitTestResult = VisualTreeHelper.HitTest(listView, clientRelativePosition);
            var selectedItem = hitTestResult.VisualHit;
            while (selectedItem != null)
            {
                if (selectedItem is ListViewItem)
                {
                    break;
                }
                selectedItem = VisualTreeHelper.GetParent(selectedItem);
            }
            return selectedItem != null ? ((ListViewItem)selectedItem) : null;
        }
    }
}
