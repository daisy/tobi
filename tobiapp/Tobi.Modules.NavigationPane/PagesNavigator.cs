using System.Collections.ObjectModel;
using System.Media;
using System.Windows.Documents;
using System.Windows;
using Tobi.Common;
using Tobi.Common.UI;
using urakawa;

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

        public void AddPage (TextElement data)
        {
            Pages.Add(new Page(data));
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
                SearchPages(m_Pages, m_searchString);
            }
        }
        private static void SearchPages(ObservableCollection<Page> pages, string searchTerm)
        {
            foreach (Page page in pages)
            {
                page.SearchMatch = !string.IsNullOrEmpty(searchTerm) &&
                                   !string.IsNullOrEmpty(page.Name) &&
                                   page.Name.ToLower().Contains(searchTerm.ToLower());
            }
        }

        public void FindNext()
        {
            Page nextMatch = FindNextPage(m_Pages);
            if (nextMatch != null)
            {
                nextMatch.IsSelected = true;
                FocusHelper.FocusBeginInvoke(m_view.m_LastListItemSelected);
            }
            else
            {
                AudioCues.PlayAsterisk();
            }
        }
        public void FindPrevious()
        {
            Page nextMatch = FindPrevPage(m_Pages);
            if (nextMatch != null)
            {
                nextMatch.IsSelected = true;
                FocusHelper.FocusBeginInvoke(m_view.m_LastListItemSelected);
            }
            else
            {
                AudioCues.PlayAsterisk();
            }

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
        private Page GetPage(string id)
        {
            foreach (Page page in Pages)
            {
                if (page.Id == id)
                    return page;
            }
            return null;
        }
    }
}
