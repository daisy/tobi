using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using Microsoft.Practices.Composite;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
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
        #region Construction

        //        protected IUnityContainer Container { get; private set; }
        private readonly IEventAggregator m_EventAggregator;
        private readonly ILoggerFacade m_Logger;

        private readonly IShellView m_ShellView;

        private readonly IUrakawaSession m_session;

        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        [ImportingConstructor]
        public PagesPaneViewModel(
            IEventAggregator eventAggregator,
            ILoggerFacade logger,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView view,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession session)
        {
            m_EventAggregator = eventAggregator;
            m_Logger = logger;

            m_ShellView = view;
            m_session = session;

            m_Logger.Log("PagesPaneViewModel.initializeCommands", Category.Debug, Priority.Medium);

            CommandFindFocusPage = new RichDelegateCommand(
                @"PAGES CommandFindFocus DUMMY TXT",
                @"PAGES CommandFindFocus DUMMY TXT",
                null, // KeyGesture set only for the top-level CompositeCommand
                null,
                () =>
                {
                    if (View != null)
                    {
                        IsSearchVisible = true;
                        FocusHelper.Focus(View.SearchBox);
                    }
                },
                () => View != null
                    //&& View.SearchBox.Visibility == Visibility.Visible
                    && View.SearchBox.IsEnabled,
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_TOCFindNext)
                );

            CommandFindNextPage = new RichDelegateCommand(
                @"PAGES CommandFindNext DUMMY TXT", //UserInterfaceStrings.PageFindNext,
                @"PAGES CommandFindNext DUMMY TXT", //UserInterfaceStrings.PageFindNext_,
                null, // KeyGesture set only for the top-level CompositeCommand
                null, () => PagesNavigator.FindNext(),
                () => PagesNavigator != null && !string.IsNullOrEmpty(PagesNavigator.SearchTerm),
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_PageFindNext)
                );

            CommandFindPrevPage = new RichDelegateCommand(
                @"PAGES CommandFindPrevious DUMMY TXT", //UserInterfaceStrings.PageFindPrev,
                @"PAGES CommandFindPrevious DUMMY TXT", //UserInterfaceStrings.PageFindPrev_,
                null, // KeyGesture set only for the top-level CompositeCommand
                null, () => PagesNavigator.FindPrevious(),
                () => PagesNavigator != null && !string.IsNullOrEmpty(PagesNavigator.SearchTerm),
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_PageFindPrev)
                );

            m_EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(onProjectLoaded, ProjectLoadedEvent.THREAD_OPTION);
            m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(onProjectUnLoaded, ProjectUnLoadedEvent.THREAD_OPTION);

            m_EventAggregator.GetEvent<PageFoundByFlowDocumentParserEvent>().Subscribe(onPageFoundByFlowDocumentParser, PageFoundByFlowDocumentParserEvent.THREAD_OPTION);

            m_EventAggregator.GetEvent<TreeNodeSelectionChangedEvent>().Subscribe(OnTreeNodeSelectionChanged, TreeNodeSelectionChangedEvent.THREAD_OPTION);
        }

        [NotifyDependsOn("PagesNavigator")]
        public bool IsSearchEnabled
        {
            get
            {
                return m_session.DocumentProject != null;
            }
        }

        private bool m_IsSearchVisible;
        public bool IsSearchVisible
        {
            get
            {
                return m_IsSearchVisible;
            }
            set
            {
                if (value == m_IsSearchVisible) return;
                m_IsSearchVisible = value;
                RaisePropertyChanged(() => IsSearchVisible);
            }
        }

        public RichDelegateCommand CommandFindFocusPage { get; private set; }
        public RichDelegateCommand CommandFindNextPage { get; private set; }
        public RichDelegateCommand CommandFindPrevPage { get; private set; }

        public RichDelegateCommand CmdFindNextGlobal { get; private set; }
        public RichDelegateCommand CmdFindPreviousGlobal { get; private set; }

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

        private bool m_GlobalSearchCommandDone = false;
        private void trySearchCommands()
        {
            if (m_GlobalSearchCommand == null || m_GlobalSearchCommandDone)
            {
                return;
            }
            m_GlobalSearchCommandDone = true;

            m_GlobalSearchCommand.CmdFindFocus.RegisterCommand(CommandFindFocusPage);
            m_GlobalSearchCommand.CmdFindNext.RegisterCommand(CommandFindNextPage);
            m_GlobalSearchCommand.CmdFindPrevious.RegisterCommand(CommandFindPrevPage);

            CmdFindNextGlobal = m_GlobalSearchCommand.CmdFindNext;
            RaisePropertyChanged(() => CmdFindNextGlobal);

            CmdFindPreviousGlobal = m_GlobalSearchCommand.CmdFindPrevious;
            RaisePropertyChanged(() => CmdFindPreviousGlobal);
        }

        protected PagePanelView View { get; private set; }
        public void SetView(PagePanelView view)
        {
            View = view;

            ActiveAware = new FocusActiveAwareAdapter(View);
            ActiveAware.IsActiveChanged += (sender, e) => refreshCommandsIsActive();
            m_ShellView.ActiveAware.IsActiveChanged += (sender, e) => refreshCommandsIsActive();
        }

        public IActiveAware ActiveAware { get; private set; }

        private void refreshCommandsIsActive()
        {
            CommandFindFocusPage.IsActive = m_ShellView.ActiveAware.IsActive && ActiveAware.IsActive;
            CommandFindNextPage.IsActive = m_ShellView.ActiveAware.IsActive && ActiveAware.IsActive;
            CommandFindPrevPage.IsActive = m_ShellView.ActiveAware.IsActive && ActiveAware.IsActive;
        }

        [NotifyDependsOn("PagesNavigator")]
        public ObservableCollection<Page> PagesNavigator_Pages
        {
            get
            {
                return PagesNavigator == null ? null : PagesNavigator.Pages;
            }
        }

        private PagesNavigator _pagesNavigator;
        public PagesNavigator PagesNavigator
        {
            private set
            {
                _pagesNavigator = value;
                RaisePropertyChanged(() => PagesNavigator);
            }
            get { return _pagesNavigator; }
        }

        private void onProjectLoaded(Project project)
        {
            PagesNavigator = new PagesNavigator(View);
            View.LoadProject();
        }
        private void onProjectUnLoaded(Project project)
        {
            PagesNavigator = null;
            View.UnloadProject();
        }

        private void onPageFoundByFlowDocumentParser(TreeNode treeNode)
        {
            if (!Dispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action<TreeNode>)onPageFoundByFlowDocumentParser, treeNode);
                return;
            }
            PagesNavigator.AddPage(treeNode);
        }

        private void OnTreeNodeSelectionChanged(Tuple<Tuple<TreeNode, TreeNode>, Tuple<TreeNode, TreeNode>> oldAndNewTreeNodeSelection)
        {
            Tuple<TreeNode, TreeNode> oldTreeNodeSelection = oldAndNewTreeNodeSelection.Item1;
            Tuple<TreeNode, TreeNode> newTreeNodeSelection = oldAndNewTreeNodeSelection.Item2;

            View.UpdatePageListSelection(newTreeNodeSelection.Item2 ?? newTreeNodeSelection.Item1);
        }

        public void OnImportsSatisfied()
        {
            trySearchCommands();
        }
    }
}
