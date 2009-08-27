using System.Windows;
using urakawa.core;

namespace Tobi.Modules.NavigationPane
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
