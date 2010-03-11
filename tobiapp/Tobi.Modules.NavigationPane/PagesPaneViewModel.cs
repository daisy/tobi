using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Events;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa;
using urakawa.core;

namespace Tobi.Plugin.NavigationPane
{
    [Export(typeof(PagesPaneViewModel)), PartCreationPolicy(CreationPolicy.Shared)]
    public class PagesPaneViewModel : ViewModelBase, IPartImportsSatisfiedNotification
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

            CommandFindFocusPage = new RichDelegateCommand(
                @"DUMMY TXT",
                @"DUMMY TXT",
                null, // KeyGesture set only for the top-level CompositeCommand
                null,
                () => { if (View != null) FocusHelper.Focus(View.SearchBox); },
                () => View != null && View.SearchBox.Visibility == Visibility.Visible,
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_TOCFindNext)
                );

            CommandFindNextPage = new RichDelegateCommand(
                @"DUMMY TXT", //UserInterfaceStrings.PageFindNext,
                @"DUMMY TXT", //UserInterfaceStrings.PageFindNext_,
                null, // KeyGesture set only for the top-level CompositeCommand
                null, () => _pagesNavigator.FindNext(),
                () => _pagesNavigator != null,
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_PageFindNext)
                );

            CommandFindPrevPage = new RichDelegateCommand(
                @"DUMMY TXT", //UserInterfaceStrings.PageFindPrev,
                @"DUMMY TXT", //UserInterfaceStrings.PageFindPrev_,
                null, // KeyGesture set only for the top-level CompositeCommand
                null, () => _pagesNavigator.FindPrevious(),
                () => _pagesNavigator != null,
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_PageFindPrev)
                );

            //m_ShellView.RegisterRichCommand(CommandFindNextPage);
            //m_ShellView.RegisterRichCommand(CommandFindPrevPage);

            m_EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(onProjectLoaded, ProjectLoadedEvent.THREAD_OPTION);
            m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(onProjectUnLoaded, ProjectUnLoadedEvent.THREAD_OPTION);

            m_EventAggregator.GetEvent<PageFoundByFlowDocumentParserEvent>().Subscribe(onPageFoundByFlowDocumentParser, PageFoundByFlowDocumentParserEvent.THREAD_OPTION);

            //m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Subscribe(onTreeNodeSelected, TreeNodeSelectedEvent.THREAD_OPTION);
            //m_EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Subscribe(onSubTreeNodeSelected, TreeNodeSelectedEvent.THREAD_OPTION);

            m_EventAggregator.GetEvent<TreeNodeSelectionChangedEvent>().Subscribe(OnTreeNodeSelectionChanged, TreeNodeSelectionChangedEvent.THREAD_OPTION);
        }

        public RichDelegateCommand CommandFindFocusPage { get; private set; }
        public RichDelegateCommand CommandFindNextPage { get; private set; }
        public RichDelegateCommand CommandFindPrevPage { get; private set; }

        ~PagesPaneViewModel()
        {
            if (m_GlobalSearchCommand != null)
            {
                m_GlobalSearchCommand.CmdFindFocus.UnregisterCommand(CommandFindFocusPage);
                m_GlobalSearchCommand.CmdFindNext.UnregisterCommand(CommandFindNextPage);
                m_GlobalSearchCommand.CmdFindPrevious.UnregisterCommand(CommandFindPrevPage);
            }
#if DEBUG
            m_Logger.Log("PagesPaneViewModel garbage collected.", Category.Debug, Priority.Medium);
#endif
        }
        #endregion Construction

        [Import(typeof(IGlobalSearchCommands), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = true, AllowDefault = true)]
        private IGlobalSearchCommands m_GlobalSearchCommand;

        private void trySearchCommands()
        {
            if (m_GlobalSearchCommand == null) { return; }

            m_GlobalSearchCommand.CmdFindFocus.RegisterCommand(CommandFindFocusPage);
            m_GlobalSearchCommand.CmdFindNext.RegisterCommand(CommandFindNextPage);
            m_GlobalSearchCommand.CmdFindPrevious.RegisterCommand(CommandFindPrevPage);
        }

        public PagesNavigator PagesNavigator
        {
            get { return _pagesNavigator; }
        }
        protected PagePanelView View { get; private set; }
        public void SetView(PagePanelView view)
        {
            View = view;

            var focusAware = new FocusActiveAwareAdapter(View);
            focusAware.IsActiveChanged += (sender, e) =>
            {
                // ALWAYS ACTIVE ! CommandFindFocusPage.IsActive = focusAware.IsActive;
                CommandFindNextPage.IsActive = focusAware.IsActive;
                CommandFindPrevPage.IsActive = focusAware.IsActive;
            };

            //IActiveAware activeAware = View as IActiveAware;
            //if (activeAware != null) { activeAware.IsActiveChanged += ActiveAwareIsActiveChanged; }
        }

        //private void ActiveAwareIsActiveChanged(object sender, EventArgs e)
        //{
        //    IActiveAware activeAware = (sender as IActiveAware);
        //    if (activeAware == null) { return; }
        //    CommandFindNextPage.IsActive = activeAware.IsActive;
        //    CommandFindPrevPage.IsActive = activeAware.IsActive;
        //}

        #region Events
        private void onProjectLoaded(Project project)
        {
            _pagesNavigator = new PagesNavigator(project);
            View.LoadProject();
        }
        private void onProjectUnLoaded(Project project)
        {
            View.UnloadProject();
            _pagesNavigator = null;
        }
        private void onPageFoundByFlowDocumentParser(TextElement data)
        {
            if (!Dispatcher.CheckAccess())
            {
                //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_WaveFormChunkMarkers));
                Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    (Action<TextElement>)onPageFoundByFlowDocumentParser, data);
                return;
            }
            _pagesNavigator.AddPage(data);
        }


        private void OnTreeNodeSelectionChanged(Tuple<TreeNode, TreeNode> treeNodeSelection)
        {
            View.UpdatePageListSelection(treeNodeSelection.Item2 ?? treeNodeSelection.Item1);
        }
        //private void onTreeNodeSelected(TreeNode node)
        //{
        //    View.UpdatePageListSelection(node);
        //}
        //private void onSubTreeNodeSelected(TreeNode node)
        //{
        //    View.UpdatePageListSelection(node);
        //}
        public void OnImportsSatisfied()
        {
            trySearchCommands();
        }
        #endregion
    }
}
