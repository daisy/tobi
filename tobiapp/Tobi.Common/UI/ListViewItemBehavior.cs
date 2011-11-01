using System;
using System.Windows;
using System.Windows.Controls;

namespace Tobi.Common.UI
{
    public static class TreeViewItemBringIntoViewFocusWhenSelectedBehavior
    {
        public static bool GetIsBroughtIntoViewFocusWhenSelected(TreeViewItem treeViewItem)
        {
            return (bool)treeViewItem.GetValue(IsBroughtIntoViewFocusWhenSelectedProperty);
        }

        public static void SetIsBroughtIntoViewFocusWhenSelected(TreeViewItem treeViewItem, bool value)
        {
            treeViewItem.SetValue(IsBroughtIntoViewFocusWhenSelectedProperty, value);
        }

        public static readonly DependencyProperty IsBroughtIntoViewFocusWhenSelectedProperty =
            DependencyProperty.RegisterAttached(
            "IsBroughtIntoViewFocusWhenSelected",
            typeof(bool),
            typeof(TreeViewItemBringIntoViewFocusWhenSelectedBehavior),
            new UIPropertyMetadata(false, OnIsBroughtIntoViewFocusWhenSelectedChanged));

        static void OnIsBroughtIntoViewFocusWhenSelectedChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            TreeViewItem item = depObj as TreeViewItem;
            if (item == null)
                return;

            if (e.NewValue is bool == false)
                return;

            if ((bool)e.NewValue)
                item.Selected += OnTreeViewItemSelected;
            else
                item.Selected -= OnTreeViewItemSelected;
        }

        static void OnTreeViewItemSelected(object sender, RoutedEventArgs e)
        {
            // Only react to the Selected event raised by the TreeViewItem
            // whose IsSelected property was modified.  Ignore all ancestors
            // who are merely reporting that a descendant's Selected fired.
            if (!Object.ReferenceEquals(sender, e.OriginalSource))
                return;

            TreeViewItem item = e.OriginalSource as TreeViewItem;
            if (item != null)
            {
                item.BringIntoView();
                FocusHelper.Focus(item);
            }
        }
    }

    public static class TreeViewItemBringIntoViewNoFocusWhenSelectedBehavior
    {
        public static bool GetIsBroughtIntoViewNoFocusWhenSelected(TreeViewItem treeViewItem)
        {
            return (bool)treeViewItem.GetValue(IsBroughtIntoViewNoFocusWhenSelectedProperty);
        }

        public static void SetIsBroughtIntoViewNoFocusWhenSelected(TreeViewItem treeViewItem, bool value)
        {
            treeViewItem.SetValue(IsBroughtIntoViewNoFocusWhenSelectedProperty, value);
        }

        public static readonly DependencyProperty IsBroughtIntoViewNoFocusWhenSelectedProperty =
            DependencyProperty.RegisterAttached(
            "IsBroughtIntoViewNoFocusWhenSelected",
            typeof(bool),
            typeof(TreeViewItemBringIntoViewNoFocusWhenSelectedBehavior),
            new UIPropertyMetadata(false, OnIsBroughtIntoViewNoFocusWhenSelectedChanged));

        static void OnIsBroughtIntoViewNoFocusWhenSelectedChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            TreeViewItem item = depObj as TreeViewItem;
            if (item == null)
                return;

            if (e.NewValue is bool == false)
                return;

            if ((bool)e.NewValue)
                item.Selected += OnTreeViewItemSelected;
            else
                item.Selected -= OnTreeViewItemSelected;
        }

        static void OnTreeViewItemSelected(object sender, RoutedEventArgs e)
        {
            // Only react to the Selected event raised by the TreeViewItem
            // whose IsSelected property was modified.  Ignore all ancestors
            // who are merely reporting that a descendant's Selected fired.
            if (!Object.ReferenceEquals(sender, e.OriginalSource))
                return;

            TreeViewItem item = e.OriginalSource as TreeViewItem;
            if (item != null)
            {
                item.BringIntoView();
                //FocusHelper.Focus(item);
            }
        }
    }

    public static class ListViewItemBringIntoViewFocusWhenSelectedBehavior
    {
        public static bool GetIsBroughtIntoViewFocusWhenSelected(ListViewItem listViewItem)
        {
            return (bool)listViewItem.GetValue(IsBroughtIntoViewFocusWhenSelectedProperty);
        }

        public static void SetIsBroughtIntoViewFocusWhenSelected(ListViewItem listViewItem, bool value)
        {
            listViewItem.SetValue(IsBroughtIntoViewFocusWhenSelectedProperty, value);
        }

        public static readonly DependencyProperty IsBroughtIntoViewFocusWhenSelectedProperty =
            DependencyProperty.RegisterAttached(
            "IsBroughtIntoViewFocusWhenSelected",
            typeof(bool),
            typeof(ListViewItemBringIntoViewFocusWhenSelectedBehavior),
            new UIPropertyMetadata(false, OnIsBroughtIntoViewFocusWhenSelectedChanged));

        static void OnIsBroughtIntoViewFocusWhenSelectedChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            ListViewItem item = depObj as ListViewItem;
            if (item == null)
                return;

            if (!(e.NewValue is bool))
                return;

            if ((bool)e.NewValue)
                item.Selected += OnListViewItemSelected;
            else
                item.Selected -= OnListViewItemSelected;
        }

        static void OnListViewItemSelected(object sender, RoutedEventArgs e)
        {
            // Only react to the Selected event raised by the Item
            // whose IsSelected property was modified.  Ignore all ancestors
            // who are merely reporting that a descendant's Selected fired.
            if (!Object.ReferenceEquals(sender, e.OriginalSource))
                return;

            ListViewItem item = e.OriginalSource as ListViewItem;
            if (item != null)
            {
                item.BringIntoView();
                FocusHelper.Focus(item);
            }
        }
    }

    public static class ListViewItemBringIntoViewNoFocusWhenSelectedBehavior
    {
        public static bool GetIsBroughtIntoViewNoFocusWhenSelected(ListViewItem listViewItem)
        {
            return (bool)listViewItem.GetValue(IsBroughtIntoViewNoFocusWhenSelectedProperty);
        }

        public static void SetIsBroughtIntoViewNoFocusWhenSelected(ListViewItem listViewItem, bool value)
        {
            listViewItem.SetValue(IsBroughtIntoViewNoFocusWhenSelectedProperty, value);
        }

        public static readonly DependencyProperty IsBroughtIntoViewNoFocusWhenSelectedProperty =
            DependencyProperty.RegisterAttached(
            "IsBroughtIntoViewNoFocusWhenSelected",
            typeof(bool),
            typeof(ListViewItemBringIntoViewNoFocusWhenSelectedBehavior),
            new UIPropertyMetadata(false, OnIsBroughtIntoViewNoFocusWhenSelectedChanged));

        static void OnIsBroughtIntoViewNoFocusWhenSelectedChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            ListViewItem item = depObj as ListViewItem;
            if (item == null)
                return;

            if (!(e.NewValue is bool))
                return;

            if ((bool)e.NewValue)
                item.Selected += OnListViewItemSelected;
            else
                item.Selected -= OnListViewItemSelected;
        }

        static void OnListViewItemSelected(object sender, RoutedEventArgs e)
        {
            // Only react to the Selected event raised by the Item
            // whose IsSelected property was modified.  Ignore all ancestors
            // who are merely reporting that a descendant's Selected fired.
            if (!Object.ReferenceEquals(sender, e.OriginalSource))
                return;

            ListViewItem item = e.OriginalSource as ListViewItem;
            if (item != null)
            {
                item.BringIntoView();
                //FocusHelper.Focus(item);
            }
        }
    }
}
