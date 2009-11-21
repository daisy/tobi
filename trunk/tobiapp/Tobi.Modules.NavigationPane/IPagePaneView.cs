using System.Windows;
using urakawa.core;

namespace Tobi.Plugin.NavigationPane
{
    public interface IPagePaneView
    {
        string ViewName { get; }
        void LoadProject();
        void UpdatePageListSelection(TreeNode node);

        UIElement ViewControl { get; }
        UIElement ViewFocusStart { get; }
    }
}
