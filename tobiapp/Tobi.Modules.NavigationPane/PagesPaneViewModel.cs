using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Documents;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Events;
using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Common.MVVM;
using urakawa;
using urakawa.core;

namespace Tobi.Modules.NavigationPane
{
    public partial class PagesPaneViewModel : ViewModelBase
    {
        private ObservableCollection<Page> m_Pages = new ObservableCollection<Page>();
        #region Construction

        //        protected IUnityContainer Container { get; private set; }
        public IEventAggregator EventAggregator { get; private set; }
        public ILoggerFacade Logger { get; private set; }

        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        public PagesPaneViewModel(IUnityContainer container, IEventAggregator eventAggregator, ILoggerFacade logger)
            : base(container)
        {
            //Container = container;
            EventAggregator = eventAggregator;
            Logger = logger;

            initialize();
        }

        ~PagesPaneViewModel()
        {
#if DEBUG
            Logger.Log("PagesPaneViewModel garbage collected.", Category.Debug, Priority.Medium);
#endif
        }

        #endregion Construction
        private string m_searchString = string.Empty;
        protected IPagePaneView View { get; private set; }
        public void SetView(IPagePaneView view)
        {
            View = view;
        }
        private void initialize()
        {
            intializeCommands();
            EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(onProjectLoaded, ThreadOption.UIThread);
            EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(onProjectUnLoaded, ThreadOption.UIThread);
            EventAggregator.GetEvent<PageFoundByFlowDocumentParserEvent>().Subscribe(onPageFoundByFlowDocumentParser, ThreadOption.UIThread);
            EventAggregator.GetEvent<TreeNodeSelectedEvent>().Subscribe(onTreeNodeSelected, ThreadOption.UIThread);
        }
        public ObservableCollection<Page> Pages
        {
            get
            {
                return m_Pages;
            }
        }
        private void onProjectLoaded(Project project)
        {
            Pages.Clear();
            View.LoadProject();
        }
        private void onProjectUnLoaded(Project project)
        {
            Pages.Clear();
        }
        private void onPageFoundByFlowDocumentParser(TextElement data)
        {
            Pages.Add(new Page(data));
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
        private void onTreeNodeSelected(TreeNode node)
        {
            View.UpdatePageListSelection(node);
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
            }
            else
            {
                MessageBox.Show(UserInterfaceStrings.TreeFindNext_FAILURE);
            }
        }
        public void FindPrevious()
        {
            Page nextMatch = FindPrevPage(m_Pages);
            if (nextMatch != null)
            {
                nextMatch.IsSelected = true;
            }
            else
            {
                MessageBox.Show(UserInterfaceStrings.TreeFindNext_FAILURE);
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
            if (!pages[iStarting].IsSelected && pages[iStarting].SearchMatch){ pResult = pages[iStarting]; }
            if (pResult==null)
            {
                for (int i = iStarting + 1; i < pages.Count; i++)
                {
                    if (!pages[i].SearchMatch) continue;
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
            for (int i = pages.Count-1; i >=0; i--)
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
