using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Tobi.Common;
using Tobi.Common.UI;
using urakawa.core;

namespace Tobi.Plugin.Descriptions
{
    public class DescriptionsNavigator
    {
        private ObservableCollection<DescribableTreeNode> _mDescribableTreeNodes = new ObservableCollection<DescribableTreeNode>();
        private string m_searchString = string.Empty;

        private readonly DescriptionsNavigationView m_view;

        public DescriptionsNavigator(DescriptionsNavigationView view)
        {
            m_view = view;
        }

        public void AddDescribableTreeNode(TreeNode node)
        {
            RemoveDescribableTreeNode(node); // ensure no duplicate
            DescribableTreeNodes.Add(new DescribableTreeNode(node));
        }

        public void RemoveDescribableTreeNode(TreeNode node)
        {
            DescribableTreeNode toRemove = null;
            foreach (var describableTreeNode in DescribableTreeNodes)
            {
                if (describableTreeNode.TreeNode == node)
                {
                    toRemove = describableTreeNode;
                    break;
                }
            }
            if (toRemove != null)
                DescribableTreeNodes.Remove(toRemove);
        }

        public ObservableCollection<DescribableTreeNode> DescribableTreeNodes
        {
            get { return _mDescribableTreeNodes; }
        }

        public string SearchTerm
        {
            get { return m_searchString; }
            set
            {
                if (m_searchString == value) { return; }
                m_searchString = value;
                FlagSearchMatches();
            }
        }

        private void FlagSearchMatches()
        {
            if (string.IsNullOrEmpty(SearchTerm))
            {
                foreach (DescribableTreeNode mnode in DescribableTreeNodes)
                {
                    mnode.SearchMatch = false;
                }
                return;
            }
            bool atLeastOneFound = false;
            foreach (DescribableTreeNode mnode in DescribableTreeNodes)
            {
                bool found = !string.IsNullOrEmpty(mnode.Description) &&
                                   mnode.Description.IndexOf(SearchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
                mnode.SearchMatch = found;
                if (found)
                {
                    atLeastOneFound = true;
                }
            }

            if (atLeastOneFound)
            {
                DescribableTreeNode sw = FindNext(false);
                if (sw == null)
                {
                    sw = FindPrevious(false);
                }
            }
        }

        public DescribableTreeNode FindNext(bool select)
        {
            DescribableTreeNode nextMatch = FindNextDescription(_mDescribableTreeNodes);
            if (nextMatch != null)
            {
                var listItem = VisualLogicalTreeWalkHelper.FindObjectInVisualTreeWithMatchingType<ListViewItem>(
                                 m_view.ListView,
                                 child =>
                                 {
                                     object dc = child.GetValue(FrameworkElement.DataContextProperty);
                                     return dc != null && dc == nextMatch;
                                 });
                if (select)
                {
                    nextMatch.IsSelected = true;
                    if (listItem != null)
                    {
                        FocusHelper.FocusBeginInvoke(listItem); //m_view.m_LastListItemSelected
                    }
                }
                else
                {
                    if (listItem != null)
                    {
                        listItem.BringIntoView();
                    }
                }
            }
            else
            {
                AudioCues.PlayBeep();
            }
            return nextMatch;
        }
        public DescribableTreeNode FindPrevious(bool select)
        {
            DescribableTreeNode previousMatch = FindPrevDescription(_mDescribableTreeNodes);
            if (previousMatch != null)
            {
                var listItem = VisualLogicalTreeWalkHelper.FindObjectInVisualTreeWithMatchingType<ListViewItem>(
                                 m_view.ListView,
                                 child =>
                                 {
                                     object dc = child.GetValue(FrameworkElement.DataContextProperty);
                                     return dc != null && dc == previousMatch;
                                 });
                if (select)
                {
                    previousMatch.IsSelected = true;
                    if (listItem != null)
                    {
                        FocusHelper.FocusBeginInvoke(listItem); //m_view.m_LastListItemSelected
                    }
                }
                else
                {
                    if (listItem != null)
                    {
                        listItem.BringIntoView();
                    }
                }
            }
            else
            {
                AudioCues.PlayBeep();
            }
            return previousMatch;
        }
        private static DescribableTreeNode FindNextDescription(ObservableCollection<DescribableTreeNode> pages)
        {
            DescribableTreeNode pResult = null;
            int iStarting = -1;
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i].SearchMatch && iStarting == -1) { iStarting = i; }
                if (!pages[i].IsSelected) { continue; }
                iStarting = i;
                break;
            }
            if (iStarting < 0) { return null; }
            if (!pages[iStarting].IsSelected && pages[iStarting].SearchMatch) { pResult = pages[iStarting]; }
            if (pResult == null)
            {
                for (int i = iStarting + 1; i < pages.Count; i++)
                {
                    if (!pages[i].SearchMatch)
                        continue;
                    pResult = pages[i];
                    break;
                }
            }
            return pResult;
        }
        private static DescribableTreeNode FindPrevDescription(ObservableCollection<DescribableTreeNode> pages)
        {
            DescribableTreeNode pResult = null;
            int iStarting = -1;
            for (int i = pages.Count - 1; i >= 0; i--)
            {
                if (pages[i].SearchMatch && iStarting == -1) { iStarting = i; }
                if (!pages[i].IsSelected) { continue; }
                iStarting = i;
                break;
            }
            if (iStarting < 0) { return null; }
            if (!pages[iStarting].IsSelected && pages[iStarting].SearchMatch) { pResult = pages[iStarting]; }
            if (pResult == null)
            {
                for (int i = iStarting - 1; i >= 0; i--)
                {
                    if (!pages[i].SearchMatch)
                        continue;
                    pResult = pages[i];
                    break;
                }
            }
            return pResult;
        }
    }
}