using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Tobi.Common;
using Tobi.Common.UI;
using urakawa.core;

namespace Tobi.Plugin.NavigationPane
{
    public class MarkersNavigator
    {
        private ObservableCollection<MarkedTreeNode> m_MarkedTreeNodes = new ObservableCollection<MarkedTreeNode>();
        private string m_searchString = string.Empty;

        private readonly MarkersPanelView m_view;

        public MarkersNavigator(MarkersPanelView view)
        {
            m_view = view;
        }

        public void AddMarkedTreeNode(TreeNode node)
        {
            RemoveMarkedTreeNode(node); // ensure no duplicate
            MarkedTreeNodes.Add(new MarkedTreeNode(node));
        }

        public void RemoveMarkedTreeNode(TreeNode node)
        {
            MarkedTreeNode toRemove = null;
            foreach (var markedTreeNode in MarkedTreeNodes)
            {
                if (markedTreeNode.TreeNode == node)
                {
                    toRemove = markedTreeNode;
                    break;
                }
            }
            if (toRemove != null)
                MarkedTreeNodes.Remove(toRemove);
        }

        public ObservableCollection<MarkedTreeNode> MarkedTreeNodes
        {
            get { return m_MarkedTreeNodes; }
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
                foreach (MarkedTreeNode mnode in MarkedTreeNodes)
                {
                    mnode.SearchMatch = false;
                }
                return;
            }
            bool atLeastOneFound = false;
            foreach (MarkedTreeNode mnode in MarkedTreeNodes)
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
                MarkedTreeNode sw = FindNext(false);
                if (sw == null)
                {
                    sw = FindPrevious(false);
                }
            }
        }

        public MarkedTreeNode FindNext(bool select)
        {
            MarkedTreeNode nextMatch = FindNextMarkers(m_MarkedTreeNodes);
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
        public MarkedTreeNode FindPrevious(bool select)
        {
            MarkedTreeNode previousMatch = FindPrevMarkers(m_MarkedTreeNodes);
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
        private static MarkedTreeNode FindNextMarkers(ObservableCollection<MarkedTreeNode> pages)
        {
            MarkedTreeNode pResult = null;
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
        private static MarkedTreeNode FindPrevMarkers(ObservableCollection<MarkedTreeNode> pages)
        {
            MarkedTreeNode pResult = null;
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
        //private MarkedTreeNode GetMarkers(string id)
        //{
        //    foreach (Markers page in Markers)
        //    {
        //        if (page.Id == id)
        //            return page;
        //    }
        //    return null;
        //}
    }
}
