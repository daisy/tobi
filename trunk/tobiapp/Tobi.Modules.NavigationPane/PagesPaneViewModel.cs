using System;
using System.ComponentModel.Composition;
using System.Windows.Documents;
using Microsoft.Practices.Composite;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Events;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using urakawa;
using urakawa.core;

namespace Tobi.Plugin.NavigationPane
{
    [Export(typeof(PagesPaneViewModel)), PartCreationPolicy(CreationPolicy.Shared)]
    public class PagesPaneViewModel : ViewModelBase
    {
        private PagesNavigator _pagesNavigator;
        #region Construction

        //        protected IUnityContainer Container { get; private set; }
        private readonly IEventAggregator m_EventAggregator;
        private readonly ILoggerFacade m_Logger;

        private readonly IShellView m_ShellView;

        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        [ImportingConstructor]
        public PagesPaneViewModel(
            IEventAggregator eventAggregator,
            ILoggerFacade logger,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView view)
        {
            m_EventAggregator = eventAggregator;
            m_Logger = logger;

            m_ShellView = view;


            m_Logger.Log("PagesPaneViewModel.initializeCommands", Category.Debug, Priority.Medium);

            CommandFindNextPage = new RichDelegateCommand(
                UserInterfaceStrings.PageFindNext,
                UserInterfaceStrings.PageFindNext_,
                null, // KeyGesture obtained from settings (see last parameters below)
                null, ()=>_pagesNavigator.FindNext(),
                () => _pagesNavigator != null,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_PageFindNext));

            CommandFindPrevPage = new RichDelegateCommand(
                UserInterfaceStrings.PageFindPrev,
                UserInterfaceStrings.PageFindPrev_,
                null, // KeyGesture obtained from settings (see last parameters below)
                null, () => _pagesNavigator.FindPrevious(),
                () => _pagesNavigator != null,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_PageFindPrev));

            m_ShellView.RegisterRichCommand(CommandFindNextPage);
            m_ShellView.RegisterRichCommand(CommandFindPrevPage);

            m_EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(onProjectLoaded, ThreadOption.UIThread);
            m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(onProjectUnLoaded, ThreadOption.UIThread);

            m_EventAggregator.GetEvent<PageFoundByFlowDocumentParserEvent>().Subscribe(onPageFoundByFlowDocumentParser, ThreadOption.UIThread);

            m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Subscribe(onTreeNodeSelected, ThreadOption.UIThread);
         }

        public RichDelegateCommand CommandFindNextPage { get; private set; }
        public RichDelegateCommand CommandFindPrevPage { get; private set; }

        ~PagesPaneViewModel()
        {
#if DEBUG
            m_Logger.Log("PagesPaneViewModel garbage collected.", Category.Debug, Priority.Medium);
#endif
        }

        #endregion Construction
        public PagesNavigator PagesNavigator
        {
            get { return _pagesNavigator; }
        }
        protected IPagePaneView View { get; private set; }
        public void SetView(IPagePaneView view)
        {
            View = view;
            IActiveAware activeAware = (IActiveAware) View;
            if (activeAware != null) { activeAware.IsActiveChanged += ActiveAwareIsActiveChanged; }
        }

        private void ActiveAwareIsActiveChanged(object sender, EventArgs e)
        {
            IActiveAware activeAware = (sender as IActiveAware);
            if (activeAware == null) { return; }
            CommandFindNextPage.IsActive = activeAware.IsActive;
            CommandFindPrevPage.IsActive = activeAware.IsActive;
        }

        #region Events
        private void onProjectLoaded(Project project)
        {
            _pagesNavigator =new PagesNavigator(project);
            View.LoadProject();
        }
        private void onProjectUnLoaded(Project project)
        {
            View.UnloadProject();
            _pagesNavigator = null;
        }
        private void onPageFoundByFlowDocumentParser(TextElement data)
        {
            _pagesNavigator.AddPage(data);
        }
        private void onTreeNodeSelected(TreeNode node)
        {
            View.UpdatePageListSelection(node);
        }
        #endregion
    }
}
