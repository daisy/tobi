using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
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
    public class PagesPaneViewModel : ViewModelBase
    {
        private ObservableCollection<Page> m_Pages = new ObservableCollection<Page>();
        #region Construction

//        protected IUnityContainer Container { get; private set; }
        public IEventAggregator EventAggregator { get; private set; }
        public ILoggerFacade Logger { get; private set; }

        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        public PagesPaneViewModel(IUnityContainer container, IEventAggregator eventAggregator, ILoggerFacade logger): base(container)
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
        protected IPagePaneView View { get; private set; }
        public void SetView(IPagePaneView view)
        {
            View = view;
        }
        private void initialize()
        {
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
    }
}
