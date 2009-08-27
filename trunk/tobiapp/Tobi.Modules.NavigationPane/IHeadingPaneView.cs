using System.Windows;
using urakawa.core;

namespace Tobi.Modules.NavigationPane
{
    public interface IHeadingPaneView
    {
        string ViewName { get; }
        void LoadProject();
        void UnloadProject();
        void SelectTreeNode(TreeNode node);

        UIElement ViewControl { get; }
        UIElement ViewFocusStart { get; }
    }
}
