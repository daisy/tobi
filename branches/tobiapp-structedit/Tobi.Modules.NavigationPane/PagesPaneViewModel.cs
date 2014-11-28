using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Practices.Composite;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa;
using urakawa.command;
using urakawa.commands;
using urakawa.core;
using urakawa.events.undo;
using System.Text;
using urakawa.property.xml;
using urakawa.daisy;
using System.Collections.Generic;
using System.Windows.Controls;
using urakawa.undo;

namespace Tobi.Plugin.NavigationPane
{
    [Export(typeof(PagesPaneViewModel)), PartCreationPolicy(CreationPolicy.Shared)]
    public class PagesPaneViewModel : ViewModelBase, IPartImportsSatisfiedNotification, UndoRedoManager.Hooker.Host
    {
        public RichDelegateCommand CommandRenumberPages { get; private set; }

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

            CommandRenumberPages = new RichDelegateCommand(
                Tobi_Plugin_NavigationPane_Lang.CmdNavigationRenumberPages_ShortDesc,
                Tobi_Plugin_NavigationPane_Lang.CmdNavigationRenumberPages_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                null, //m_ShellView.LoadTangoIcon("bookmark-new"),
                () =>
                {
                    if (PagesNavigator_Pages.Count <= 0)
                    {
                        return;
                    }

                    var textBox_pageNumberStringPrefix = new TextBox()
                   {
                       Text = ""
                   };

                    var label_pageNumberStringPrefix = new TextBlock()
                    {
                        Text = Tobi_Plugin_NavigationPane_Lang.PageNumberPrefix + ": ",
                        Margin = new Thickness(8, 0, 8, 0),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Focusable = true,
                        TextWrapping = TextWrapping.Wrap
                    };

                    var panel_pageNumberStringPrefix = new DockPanel()
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    label_pageNumberStringPrefix.SetValue(DockPanel.DockProperty, Dock.Left);
                    panel_pageNumberStringPrefix.Children.Add(label_pageNumberStringPrefix);
                    panel_pageNumberStringPrefix.Children.Add(textBox_pageNumberStringPrefix);

                    var textBox_pageNumberIntegerStart = new TextBox()
                   {
                       Text = "1"
                   };

                    var label_pageNumberIntegerStart = new TextBlock()
                    {
                        Text = Tobi_Plugin_NavigationPane_Lang.PageNumberStart + ": ",
                        Margin = new Thickness(8, 0, 8, 0),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Focusable = true,
                        TextWrapping = TextWrapping.Wrap
                    };

                    var panel_pageNumberIntegerStart = new DockPanel()
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    label_pageNumberIntegerStart.SetValue(DockPanel.DockProperty, Dock.Left);
                    panel_pageNumberIntegerStart.Children.Add(label_pageNumberIntegerStart);
                    panel_pageNumberIntegerStart.Children.Add(textBox_pageNumberIntegerStart);




                    var panel = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    panel.Children.Add(panel_pageNumberStringPrefix);
                    panel.Children.Add(panel_pageNumberIntegerStart);

                    var windowPopup = new PopupModalWindow(m_ShellView,
                                                           UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_NavigationPane_Lang.CmdNavigationRenumberPages_ShortDesc),
                                                           panel,
                                                           PopupModalWindow.DialogButtonsSet.OkCancel,
                                                           PopupModalWindow.DialogButton.Ok,
                                                           false, 250, 160, null, 40, null);
                    windowPopup.ShowModal();

                    if (windowPopup.ClickedDialogButton != PopupModalWindow.DialogButton.Ok)
                    {
                        return;
                    }


                    string prefix = "";
                    if (!String.IsNullOrEmpty(textBox_pageNumberStringPrefix.Text))
                    {
                        prefix = textBox_pageNumberStringPrefix.Text;
                    }

                    int pageNumber = 1;
                    if (!String.IsNullOrEmpty(textBox_pageNumberIntegerStart.Text))
                    {
                        try
                        {
                            pageNumber = Int32.Parse(textBox_pageNumberIntegerStart.Text);
                        }
                        catch (Exception ex)
                        {
                            return;
                        }
                    }




                    var treeNodes = new List<TreeNode>(PagesNavigator_Pages.Count);
                    foreach (Page marked in PagesNavigator_Pages)
                    {
                        treeNodes.Add(marked.TreeNode);
                    }

                    string pageNumberStr = "";

                    m_session.DocumentProject.Presentations.Get(0).UndoRedoManager.StartTransaction(Tobi_Plugin_NavigationPane_Lang.CmdNavigationRenumberPages_ShortDesc, Tobi_Plugin_NavigationPane_Lang.CmdNavigationRenumberPages_LongDesc);
                    foreach (TreeNode treeNode in treeNodes)
                    {
                        pageNumberStr = prefix + (pageNumber++);
                        var cmd = treeNode.Presentation.CommandFactory.CreateTreeNodeChangeTextCommand(treeNode, pageNumberStr);
                        treeNode.Presentation.UndoRedoManager.Execute(cmd);
                    }
                    m_session.DocumentProject.Presentations.Get(0).UndoRedoManager.EndTransaction();
                },
                () => m_session.DocumentProject != null && !m_session.isAudioRecording
                     && !m_session.IsXukSpine, //SelectedTreeNode != null, //!m_UrakawaSession.IsSplitMaster &&
                Settings_KeyGestures.Default,
                null) //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_RemoveAllDocMarks)
                ;

            m_ShellView.RegisterRichCommand(CommandRenumberPages);

            CommandFindFocusPage = new RichDelegateCommand(
                @"PAGES CommandFindFocus DUMMY TXT",
                @"PAGES CommandFindFocus DUMMY TXT",
                null, // KeyGesture set only for the top-level CompositeCommand
                null,
                () =>
                {
                    m_ShellView.RaiseEscapeEvent();

                    if (View != null)
                    {
                        IsSearchVisible = true;
                        FocusHelper.Focus(View.SearchBox);
                        View.SearchBox.SelectAll();
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
                null, () =>
                {
                    m_ShellView.RaiseEscapeEvent();

                    PagesNavigator.FindNext(true);
                },
                () => PagesNavigator != null && !string.IsNullOrEmpty(PagesNavigator.SearchTerm),
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_PageFindNext)
                );

            CommandFindPrevPage = new RichDelegateCommand(
                @"PAGES CommandFindPrevious DUMMY TXT", //UserInterfaceStrings.PageFindPrev,
                @"PAGES CommandFindPrevious DUMMY TXT", //UserInterfaceStrings.PageFindPrev_,
                null, // KeyGesture set only for the top-level CompositeCommand
                null, () =>
                {
                    m_ShellView.RaiseEscapeEvent();

                    PagesNavigator.FindPrevious(true);
                },
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

        private static bool isPageNumber(TreeNode treeNode)
        {
            string localName = treeNode.GetXmlElementLocalName();
            if (!string.IsNullOrEmpty(localName))
            {
                if (localName.Equals("pagenum", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                XmlProperty xmlProp = treeNode.GetXmlProperty();
                //XmlAttribute xmlAttr = xmlProp.GetAttribute("type");
                XmlAttribute xmlAttr = xmlProp.GetAttribute(DiagramContentModelHelper.NS_PREFIX_EPUB + ":type", DiagramContentModelHelper.NS_URL_EPUB);
                if (xmlAttr != null)
                {
                    return xmlAttr.Value.Equals("pagebreak", StringComparison.OrdinalIgnoreCase);
                }
            }
            return false;
        }

        private static bool isPage(TreeNode node)
        {
            if (!isPageNumber(node)) return false;

            string id = node.GetXmlElementId();

            string pageID = null;

            if (!string.IsNullOrEmpty(id))
            {
                pageID = id;
            }
            else
            {
                TreeNode.StringChunkRange range = node.GetTextFlattened_();
                if (range != null && range.First != null && !string.IsNullOrEmpty(range.First.Str))
                {
                    StringBuilder strBuilder = new StringBuilder(range.GetLength());
                    TreeNode.ConcatStringChunks(range, -1, strBuilder);

                    strBuilder.Replace(" ", "_");
                    strBuilder.Insert(0, "id_tobipage_");

                    pageID = strBuilder.ToString();
                }
            }

            if (!string.IsNullOrEmpty(pageID))
            {
                return true;
            }
            return false;
        }

        private bool checkTreeNodeFragmentRemoval(bool done, TreeNode node)
        {
            bool found = false;

            if (isPage(node))
            {
                if (done)
                {
                    PagesNavigator.RemovePage(node);
                }
                else
                {
                    PagesNavigator.AddPage(node);
                }
                RaisePropertyChanged(() => HasNotPages);

                found = true;
            }
            foreach (var child in node.Children.ContentsAs_Enumerable)
            {
                bool found_ = checkTreeNodeFragmentRemoval(done, child);
                found = found || found_;
            }

            return found;
        }

        private void InvalidatePages(bool forceInvalidate, TreeNode node)
        {
            foreach (var page in PagesNavigator_Pages)
            {
                if (forceInvalidate
                    || node == page.TreeNode
                    || node.IsDescendantOf(page.TreeNode))
                {
                    page.InvalidateName();
                }
            }
        }

        private void OnUndoRedoManagerChanged_TreeNodeChangeTextCommand(UndoRedoManagerEventArgs eventt, bool isTransactionActive, bool done, TreeNodeChangeTextCommand command)
        {
            InvalidatePages(false, command.TreeNode);
        }

        private void OnUndoRedoManagerChanged_TextNodeStructureEditCommand(UndoRedoManagerEventArgs eventt, bool isTransactionActive, bool done, TextNodeStructureEditCommand command)
        {
            DebugFix.Assert(command is TreeNodeInsertCommand || command is TreeNodeRemoveCommand);

            //TreeNode node = (command is TreeNodeInsertCommand) ? ((TreeNodeInsertCommand)command).TreeNode : ((TreeNodeRemoveCommand)command).TreeNode;
            TreeNode node = command.TreeNode;

            bool forceInvalidate = (command is TreeNodeInsertCommand && !done) || (command is TreeNodeRemoveCommand && done);
            InvalidatePages(forceInvalidate, node);

            bool done_ = (command is TreeNodeInsertCommand) ? !done : done;
            checkTreeNodeFragmentRemoval(done_, node);
        }

        public void OnUndoRedoManagerChanged(UndoRedoManagerEventArgs eventt, bool isTransactionActive, bool done, Command command)
        {
            if (!TheDispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                TheDispatcher.Invoke(DispatcherPriority.Normal, (Action<UndoRedoManagerEventArgs, bool, bool, Command>)OnUndoRedoManagerChanged, eventt, isTransactionActive, done, command);
                return;
            }

            if (command is CompositeCommand)
            {
#if DEBUG
                Debugger.Break();
#endif
            }
            else if (command is TreeNodeChangeTextCommand)
            {
                OnUndoRedoManagerChanged_TreeNodeChangeTextCommand(eventt, isTransactionActive, done, (TreeNodeChangeTextCommand)command);
            }
            else if (command is TextNodeStructureEditCommand)
            {
                OnUndoRedoManagerChanged_TextNodeStructureEditCommand(eventt, isTransactionActive, done, (TextNodeStructureEditCommand)command);
            }
        }

        private UndoRedoManager.Hooker m_UndoRedoManagerHooker = null;

        private void onProjectLoaded(Project project)
        {
            if (m_session.IsXukSpine)
            {
                return;
            }

            m_UndoRedoManagerHooker = project.Presentations.Get(0).UndoRedoManager.Hook(this, false);

            PagesNavigator = new PagesNavigator(View);

            View.LoadProject();
        }
        private void onProjectUnLoaded(Project project)
        {
            if (m_UndoRedoManagerHooker != null) m_UndoRedoManagerHooker.UnHook();
            m_UndoRedoManagerHooker = null;

            PagesNavigator = null;
            View.UnloadProject();
        }

        [NotifyDependsOn("PagesNavigator")]
        public bool HasNotPages
        {
            get
            {
                return PagesNavigator == null ? true : PagesNavigator.Pages.Count == 0;
            }
        }

        private void onPageFoundByFlowDocumentParser(TreeNode treeNode)
        {
            if (!TheDispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                TheDispatcher.Invoke(DispatcherPriority.Normal, (Action<TreeNode>)onPageFoundByFlowDocumentParser, treeNode);
                return;
            }
            PagesNavigator.AddPage(treeNode);

            RaisePropertyChanged(() => HasNotPages);
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
