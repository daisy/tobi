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
        private ObservableCollection<DescribedTreeNode> _mDescribedTreeNodes = new ObservableCollection<DescribedTreeNode>();
        private string m_searchString = string.Empty;

        private readonly DescriptionsNavigationView m_view;

        public DescriptionsNavigator(DescriptionsNavigationView view)
        {
            m_view = view;
        }

        public void AddDescribedTreeNode(TreeNode node)
        {
            RemoveDescribedTreeNode(node); // ensure no duplicate
            DescribedTreeNodes.Add(new DescribedTreeNode(node));
        }

        public void RemoveDescribedTreeNode(TreeNode node)
        {
            DescribedTreeNode toRemove = null;
            foreach (var describedTreeNode in DescribedTreeNodes)
            {
                if (describedTreeNode.TreeNode == node)
                {
                    toRemove = describedTreeNode;
                    break;
                }
            }
            if (toRemove != null)
                DescribedTreeNodes.Remove(toRemove);
        }

        public ObservableCollection<DescribedTreeNode> DescribedTreeNodes
        {
            get { return _mDescribedTreeNodes; }
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
                foreach (DescribedTreeNode mnode in DescribedTreeNodes)
                {
                    mnode.SearchMatch = false;
                }
                return;
            }
            bool atLeastOneFound = false;
            foreach (DescribedTreeNode mnode in DescribedTreeNodes)
            {
                bool found = !string.IsNullOrEmpty(mnode.Description) &&
                                   mnode.Description.ToLower().Contains(SearchTerm.ToLower());
                mnode.SearchMatch = found;
                if (found)
                {
                    atLeastOneFound = true;
                }
            }

            if (atLeastOneFound)
            {
                DescribedTreeNode sw = FindNext(false);
                if (sw == null)
                {
                    sw = FindPrevious(false);
                }
            }
        }

        public DescribedTreeNode FindNext(bool select)
        {
            DescribedTreeNode nextMatch = FindNextDescription(_mDescribedTreeNodes);
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
        public DescribedTreeNode FindPrevious(bool select)
        {
            DescribedTreeNode previousMatch = FindPrevDescription(_mDescribedTreeNodes);
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
        private static DescribedTreeNode FindNextDescription(ObservableCollection<DescribedTreeNode> pages)
        {
            DescribedTreeNode pResult = null;
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
        private static DescribedTreeNode FindPrevDescription(ObservableCollection<DescribedTreeNode> pages)
        {
            DescribedTreeNode pResult = null;
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