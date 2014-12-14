using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Tobi.Common;
using Tobi.Common.UI;
using urakawa.core;

namespace Tobi.Plugin.NavigationPane
{
    public class PagesNavigator
    {
        private ObservableCollection<Page> m_Pages = new ObservableCollection<Page>();
        private string m_searchString = string.Empty;
        private readonly PagePanelView m_view;

        public PagesNavigator(PagePanelView view)
        {
            m_view = view;
        }

        public void AddPage(TreeNode node)
        {
            RemovePage(node); // ensure no duplicate

            bool inserted = false;
            foreach (Page page in Pages)
            {
                if (node.IsBefore(page.TreeNode))
                {
                    Pages.Insert(Pages.IndexOf(page), new Page(node));
                    inserted = true;
                    break;
                }
            }
            if (!inserted)
            {
                Pages.Add(new Page(node));
            }
        }

        public void RemovePage(TreeNode node)
        {
            Page toRemove = null;
            foreach (Page page in Pages)
            {
                if (page.TreeNode == node)
                {
                    toRemove = page;
                    break;
                }
            }
            if (toRemove != null)
                Pages.Remove(toRemove);
        }

        public ObservableCollection<Page> Pages
        {
            get { return m_Pages; }
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
                foreach (Page page in m_Pages)
                {
                    page.SearchMatch = false;
                }
                return;
            }
            bool atLeastOneFound = false;
            foreach (Page page in m_Pages)
            {
                bool found = !string.IsNullOrEmpty(page.Name) &&
                                   page.Name.IndexOf(SearchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
                page.SearchMatch = found;
                if (found)
                {
                    atLeastOneFound = true;
                }
            }

            if (atLeastOneFound)
            {
                Page sw = FindNext(false);
                if (sw == null)
                {
                    sw = FindPrevious(false);
                }
            }
        }

        public Page FindNext(bool select)
        {
            Page nextMatch = FindNextPage(m_Pages);
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
        public Page FindPrevious(bool select)
        {
            Page previousMatch = FindPrevPage(m_Pages);
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
        private static Page FindNextPage(ObservableCollection<Page> pages)
        {
            Page pResult = null;
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
        private static Page FindPrevPage(ObservableCollection<Page> pages)
        {
            Page pResult = null;
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
        //private Page GetPage(string id)
        //{
        //    foreach (Page page in Pages)
        //    {
        //        if (page.Id == id)
        //            return page;
        //    }
        //    return null;
        //}
    }
}
