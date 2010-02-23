using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Events;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa;
using urakawa.core;

namespace Tobi.Plugin.DocumentPane
{
    /// <summary>
    /// Interaction logic for DocumentPaneView.xaml
    /// </summary>
    [Export(typeof(DocumentPaneView)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class DocumentPaneView : IPartImportsSatisfiedNotification // : INotifyPropertyChangedEx
    {

        public void OnImportsSatisfied()
        {
            trySearchCommands();
        }
        public RichDelegateCommand CommandFindFocus { get; private set; }
        //public RichDelegateCommand CommandFindNext { get; private set; }
        //public RichDelegateCommand CommandFindPrev { get; private set; }

        ~DocumentPaneView()
        {
            if (m_GlobalSearchCommand != null)
            {
                m_GlobalSearchCommand.CmdFindFocus.UnregisterCommand(CommandFindFocus);
                //m_GlobalSearchCommand.CmdFindNext.UnregisterCommand(CommandFindNext);
                //m_GlobalSearchCommand.CmdFindPrevious.UnregisterCommand(CommandFindPrev);
            }
#if DEBUG
            m_Logger.Log("DocumentPaneView garbage collected.", Category.Debug, Priority.Medium);
#endif
        }
        [Import(typeof(IGlobalSearchCommands), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = true, AllowDefault = true)]
        private IGlobalSearchCommands m_GlobalSearchCommand;

        private void trySearchCommands()
        {
            if (m_GlobalSearchCommand == null) { return; }

            m_GlobalSearchCommand.CmdFindFocus.RegisterCommand(CommandFindFocus);
            //m_GlobalSearchCommand.CmdFindNext.RegisterCommand(CommandFindNext);
            //m_GlobalSearchCommand.CmdFindPrevious.RegisterCommand(CommandFindPrev);
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            //if (m_ViewModel.HeadingsNavigator == null) { return; }
            //m_ViewModel.HeadingsNavigator.SearchTerm = SearchBox.Text;
        }

        //public event PropertyChangedEventHandler PropertyChanged;
        //public void RaisePropertyChanged(PropertyChangedEventArgs e)
        //{
        //    var handler = PropertyChanged;

        //    if (handler != null)
        //    {
        //        handler(this, e);
        //    }
        //}

        //private PropertyChangedNotifyBase m_PropertyChangeHandler;

        //public NavigationPaneView()
        //{
        //    m_PropertyChangeHandler = new PropertyChangedNotifyBase();
        //    m_PropertyChangeHandler.InitializeDependentProperties(this);
        //}

        public RichDelegateCommand CommandSwitchPhrasePrevious { get; private set; }
        public RichDelegateCommand CommandSwitchPhraseNext { get; private set; }

        private readonly ILoggerFacade m_Logger;

        private readonly IEventAggregator m_EventAggregator;

        private readonly IShellView m_ShellView;

        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        [ImportingConstructor]
        public DocumentPaneView(
            IEventAggregator eventAggregator,
            ILoggerFacade logger,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView)
        {
            m_EventAggregator = eventAggregator;
            m_Logger = logger;
            m_ShellView = shellView;

            DataContext = this;

            CommandFindFocus = new RichDelegateCommand(
                @"DUMMY TXT",
                @"DUMMY TXT",
                null, // KeyGesture set only for the top-level CompositeCommand
                null,
                () => FocusHelper.Focus(this.SearchBox),
                () => this.SearchBox.Visibility == Visibility.Visible,
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_TOCFindNext)
                );

            //CommandFindNext = new RichDelegateCommand(
            //    @"DUMMY TXT", //UserInterfaceStrings.TreeFindNext,
            //    @"DUMMY TXT", //UserInterfaceStrings.TreeFindNext_,
            //    null, // KeyGesture set only for the top-level CompositeCommand
            //    null,
            //    () => _headingsNavigator.FindNext(),
            //    () => _headingsNavigator != null,
            //    null, //Settings_KeyGestures.Default,
            //    null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_TOCFindNext)
            //    );

            //CommandFindPrev = new RichDelegateCommand(
            //    @"DUMMY TXT", //UserInterfaceStrings.TreeFindPrev,
            //    @"DUMMY TXT", //UserInterfaceStrings.TreeFindPrev_,
            //    null, // KeyGesture set only for the top-level CompositeCommand
            //    null,
            //    () => _headingsNavigator.FindPrevious(),
            //    () => _headingsNavigator != null,
            //    null, //Settings_KeyGestures.Default,
            //    null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_TOCFindPrev)
            //    );
            CommandSwitchPhrasePrevious = new RichDelegateCommand(
                UserInterfaceStrings.Event_SwitchPrevious,
                UserInterfaceStrings.Event_SwitchPrevious_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("format-indent-less"),
                () =>
                {
                    if (CurrentTreeNode == CurrentSubTreeNode)
                    {
                        TreeNode nextNode = CurrentTreeNode.GetPreviousSiblingWithText();
                        if (nextNode != null)
                        {
                            m_Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] DocumentPaneView.SwitchPhrasePrevious",
                                       Category.Debug, Priority.Medium);

                            m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(nextNode);
                            return;
                        }
                    }
                    else
                    {
                        TreeNode nextNode = CurrentSubTreeNode.GetPreviousSiblingWithText(CurrentTreeNode);
                        if (nextNode != null)
                        {
                            m_Logger.Log("-- PublishEvent [SubTreeNodeSelectedEvent] DocumentPaneView.SwitchPhrasePrevious",
                                       Category.Debug, Priority.Medium);

                            m_EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(nextNode);
                            return;
                        }
                        else
                        {
                            nextNode = CurrentTreeNode.GetPreviousSiblingWithText();
                            if (nextNode != null)
                            {
                                m_Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] DocumentPaneView.SwitchPhrasePrevious",
                                           Category.Debug, Priority.Medium);

                                m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(nextNode);
                                return;
                            }
                        }
                    }

                    SystemSounds.Beep.Play();
                },
                () => CurrentTreeNode != null,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Doc_Event_SwitchPrevious));

            m_ShellView.RegisterRichCommand(CommandSwitchPhrasePrevious);
            //
            CommandSwitchPhraseNext = new RichDelegateCommand(
                UserInterfaceStrings.Event_SwitchNext,
                UserInterfaceStrings.Event_SwitchNext_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("format-indent-more"),
                () =>
                {
                    if (CurrentTreeNode == CurrentSubTreeNode)
                    {
                        TreeNode nextNode = CurrentTreeNode.GetNextSiblingWithText();
                        if (nextNode != null)
                        {
                            m_Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] DocumentPaneView.SwitchPhraseNext",
                                       Category.Debug, Priority.Medium);

                            m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(nextNode);
                            return;
                        }
                    }
                    else
                    {
                        TreeNode nextNode = CurrentSubTreeNode.GetNextSiblingWithText(CurrentTreeNode);
                        if (nextNode != null)
                        {
                            m_Logger.Log("-- PublishEvent [SubTreeNodeSelectedEvent] DocumentPaneView.SwitchPhraseNext",
                                       Category.Debug, Priority.Medium);

                            m_EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(nextNode);
                            return;
                        }
                        else
                        {
                            nextNode = CurrentTreeNode.GetNextSiblingWithText();
                            if (nextNode != null)
                            {
                                m_Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] DocumentPaneView.SwitchPhraseNext",
                                           Category.Debug, Priority.Medium);

                                m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(nextNode);
                                return;
                            }
                        }
                    }

                    SystemSounds.Beep.Play();
                },
                () => CurrentTreeNode != null,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Doc_Event_SwitchNext));

            m_ShellView.RegisterRichCommand(CommandSwitchPhraseNext);
            //

            InitializeComponent();

            resetFlowDocument();

            var run = new Run(" "); //UserInterfaceStrings.No_Document);
            //setTextDecoration_ErrorUnderline(run);
            TheFlowDocument.Blocks.Add(new Paragraph(run));

            m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Subscribe(OnTreeNodeSelected, ThreadOption.UIThread);
            m_EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Subscribe(OnSubTreeNodeSelected, ThreadOption.UIThread);

            m_EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(OnProjectLoaded, ThreadOption.UIThread);
            m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(OnProjectUnLoaded, ThreadOption.UIThread);


            var focusAware = new FocusActiveAwareAdapter(this);
            focusAware.IsActiveChanged += (sender, e) =>
            {
                // ALWAYS ACTIVE ! CommandFindFocus.IsActive = focusAware.IsActive;

                //CommandFindNext.IsActive = focusAware.IsActive;
                //CommandFindPrev.IsActive = focusAware.IsActive;
            };
        }

        /*
        private void annotationsOn()
        {
            AnnotationService service = AnnotationService.GetService(FlowDocReader);

            if (service == null)
            {
                string dir = Path.GetDirectoryName(UserInterfaceStrings.LOG_FILE_PATH);
                Stream annoStream = new FileStream(dir + @"\annotations.xml", FileMode.OpenOrCreate);
                service = new AnnotationService(FlowDocReader);
                AnnotationStore store = new XmlStreamStore(annoStream);
                service.Enable(store);
            }

            AnnotationService.CreateTextStickyNoteCommand.CanExecuteChanged += new EventHandler(OnAnnotationCanExecuteChanged);
        }

        TextSelection m_TextSelection = null;

        public void OnAnnotationCanExecuteChanged(object o, EventArgs e)
        {
            if (m_TextSelection != FlowDocReader.Selection)
            {
                m_TextSelection = FlowDocReader.Selection;
                OnMouseUpFlowDoc();
            }
        }

        private void annotationsOff()
        {
            AnnotationService service = AnnotationService.GetService(FlowDocReader);

            if (service != null && service.IsEnabled)
            {
                service.Store.Flush();
                service.Disable();
                //AnnotationStream.Close();
            }
        }*/



        //private FlowDocument m_FlowDoc;


        private TextElement m_lastHighlighted;
        private Brush m_lastHighlighted_Background;
        private Brush m_lastHighlighted_Foreground;
        private Brush m_lastHighlighted_BorderBrush;
        private Thickness m_lastHighlighted_BorderThickness;

        private TextElement m_lastHighlightedSub;
        private Brush m_lastHighlightedSub_Background;
        private Brush m_lastHighlightedSub_Foreground;


        private Dictionary<string, TextElement> m_idLinkTargets;


        private void OnProjectUnLoaded(Project obj)
        {
            OnProjectLoaded(null);
        }

        private void OnProjectLoaded(Project project)
        {
            CurrentTreeNode = null;
            CurrentSubTreeNode = null;

            if (m_idLinkTargets != null)
            {
                m_idLinkTargets.Clear();
            }
            m_idLinkTargets = new Dictionary<string, TextElement>();

            m_lastHighlighted = null;
            m_lastHighlightedSub = null;

            resetFlowDocument();

            if (project == null)
            {
                var run = new Run(" "); //UserInterfaceStrings.No_Document);
                //setTextDecoration_ErrorUnderline(run);
                TheFlowDocument.Blocks.Add(new Paragraph(run));
                return;
            }

            createFlowDocumentFromXuk(project);

            //m_FlowDoc.IsEnabled = true;
            //m_FlowDoc.IsHyphenationEnabled = false;
            //m_FlowDoc.IsOptimalParagraphEnabled = false;
            //m_FlowDoc.ColumnWidth = Double.PositiveInfinity;
            //m_FlowDoc.IsColumnWidthFlexible = false;
            //m_FlowDoc.TextAlignment = TextAlignment.Left;

            //m_FlowDoc.MouseUp += OnMouseUpFlowDoc;

            //FlowDocReader.Document = m_FlowDoc;

            //annotationsOn();

            /*
            string dirPath = Path.GetDirectoryName(FilePath);
            string fullPath = Path.Combine(dirPath, "FlowDocument.xaml");

            using (FileStream stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                try
                {
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Encoding = Encoding.UTF8;
                    settings.NewLineHandling = NewLineHandling.Replace;
                    settings.NewLineChars = Environment.NewLine;
                    settings.Indent = true;
                    settings.IndentChars = "\t";
                    settings.NewLineOnAttributes = true;

                    XmlWriter xmlWriter = XmlWriter.Create(stream, settings);

                    XamlWriter.Save(m_FlowDoc, xmlWriter);
                }
                finally
                {
                    stream.Close();
                }
            }*/

        }

        private void resetFlowDocument()
        {
            //FlowDocReader.Document = new FlowDocument(new Paragraph(new Run(UserInterfaceStrings.No_Document)))
            //{
            //    IsEnabled = false,
            //    IsHyphenationEnabled = false,
            //    IsOptimalParagraphEnabled = false,
            //    ColumnWidth = Double.PositiveInfinity,
            //    IsColumnWidthFlexible = false,
            //    TextAlignment = TextAlignment.Center
            //};
            //FlowDocReader.Document.Blocks.Add(new Paragraph(new Run("Use 'new' or 'open' from the menu bar.")));

            TheFlowDocument.Blocks.Clear();
        }

        private TreeNode m_CurrentTreeNode;
        public TreeNode CurrentTreeNode
        {
            get
            {
                return m_CurrentTreeNode;
            }
            set
            {
                if (m_CurrentTreeNode == value) return;

                m_CurrentTreeNode = value;
                //RaisePropertyChanged(() => CurrentTreeNode);
            }
        }

        private TreeNode m_CurrentSubTreeNode;
        public TreeNode CurrentSubTreeNode
        {
            get
            {
                return m_CurrentSubTreeNode;
            }
            set
            {
                if (m_CurrentSubTreeNode == value) return;
                m_CurrentSubTreeNode = value;

                //RaisePropertyChanged(() => CurrentSubTreeNode);
            }
        }

        private void OnSubTreeNodeSelected(TreeNode node)
        {
            if (node == null || CurrentTreeNode == null)
            {
                return;
            }
            if (CurrentSubTreeNode == node)
            {
                return;
            }
            if (!node.IsDescendantOf(CurrentTreeNode))
            {
                return;
            }
            CurrentSubTreeNode = node;

            BringIntoViewAndHighlightSub(node);
        }

        private void OnTreeNodeSelected(TreeNode node)
        {
            if (node == null)
            {
                return;
            }
            if (CurrentTreeNode == node)
            {
                return;
            }

            TreeNode subTreeNode = null;

            if (CurrentTreeNode != null)
            {
                if (CurrentSubTreeNode == CurrentTreeNode)
                {
                    if (node.IsAncestorOf(CurrentTreeNode))
                    {
                        subTreeNode = CurrentTreeNode;
                    }
                }
                else
                {
                    if (node.IsAncestorOf(CurrentSubTreeNode))
                    {
                        subTreeNode = CurrentSubTreeNode;
                    }
                    else if (node.IsDescendantOf(CurrentTreeNode))
                    {
                        subTreeNode = node;
                    }
                }
            }

            if (subTreeNode == node)
            {
                CurrentTreeNode = node;
                CurrentSubTreeNode = CurrentTreeNode;
                BringIntoViewAndHighlight(node);
            }
            else
            {
                CurrentTreeNode = node;
                CurrentSubTreeNode = CurrentTreeNode;
                BringIntoViewAndHighlight(node);

                if (subTreeNode != null)
                {
                    m_Logger.Log("-- PublishEvent [SubTreeNodeSelectedEvent] DocumentPaneView.OnTreeNodeSelected",
                               Category.Debug, Priority.Medium);

                    m_EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(subTreeNode);
                }
            }
        }
        //DependencyObject FindVisualTreeRoot(DependencyObject initial)
        //{
        //    DependencyObject current = initial;
        //    DependencyObject result = initial;

        //    while (current != null)
        //    {
        //        result = current;
        //        if (current is Visual) // || current is Visual3D)
        //        {
        //            current = VisualTreeHelper.GetParent(current);
        //        }
        //        else
        //        {
        //            // If we're in Logical Land then we must walk 
        //            // up the logical tree until we find a 
        //            // Visual/Visual3D to get us back to Visual Land.
        //            current = LogicalTreeHelper.GetParent(current);
        //        }
        //    }

        //    return result;
        //}

        private void createFlowDocumentFromXuk(Project project)
        {
            TreeNode root = project.Presentations.Get(0).RootNode;
            TreeNode nodeBook = root.GetFirstChildWithXmlElementName("book");
            if (nodeBook == null)
            {
                return;
            }

            var converter = new XukToFlowDocument(m_Logger, m_EventAggregator,
                            OnMouseUpFlowDoc,
                            (textElem) =>
                            {
                                //var obj = FindVisualTreeRoot(textElem);

                                var node = textElem.Tag as TreeNode;
                                if (node == null)
                                {
                                    return;
                                }

                                selectNode(node);
                            },
                            (uri) =>
                            {
                                m_Logger.Log("DocumentPaneView.OnRequestNavigate", Category.Debug, Priority.Medium);

                                if (uri.ToString().StartsWith("#"))
                                {
                                    string id = uri.ToString().Substring(1);
                                    BringIntoViewAndHighlight(id);
                                }
                            },
                            (name, data) =>
                            {
                                m_idLinkTargets.Add(name, data);
                            });
            converter.Convert(nodeBook, TheFlowDocument);
        }

        private void selectNode(TreeNode node)
        {
            if (node == CurrentTreeNode)
            {
                var treeNode = node.GetFirstDescendantWithText();
                if (treeNode != null)
                {
                    m_Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] DocumentPaneView.selectNode",
                               Category.Debug, Priority.Medium);

                    m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(treeNode);
                }

                return;
            }

            if (CurrentTreeNode != null && CurrentSubTreeNode != CurrentTreeNode
                && node.IsDescendantOf(CurrentTreeNode))
            {
                m_Logger.Log(
                    "-- PublishEvent [SubTreeNodeSelectedEvent] DocumentPaneView.OnMouseDownTextElement",
                    Category.Debug, Priority.Medium);

                m_EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(node);
            }
            else
            {
                m_Logger.Log(
                    "-- PublishEvent [TreeNodeSelectedEvent] DocumentPaneView.OnMouseDownTextElement",
                    Category.Debug, Priority.Medium);

                m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(node);
            }
        }

        private void OnMouseUpFlowDoc()
        {
            m_Logger.Log("DocumentPaneView.OnMouseUpFlowDoc", Category.Debug, Priority.Medium);

            TextSelection selection = FlowDocReader.Selection;
            if (selection != null && !selection.IsEmpty)
            {
                TextPointer startPointer = selection.Start;
                TextPointer endPointer = selection.End;
                TextRange selectedRange = new TextRange(startPointer, endPointer);


                TextPointer leftPointer = startPointer;

                while (leftPointer != null
                    && (leftPointer.GetPointerContext(LogicalDirection.Backward) != TextPointerContext.ElementStart
                    || !(leftPointer.Parent is Run)))
                {
                    leftPointer = leftPointer.GetNextContextPosition(LogicalDirection.Backward);
                }
                if (leftPointer == null
                    || (leftPointer.GetPointerContext(LogicalDirection.Backward) != TextPointerContext.ElementStart
                    || !(leftPointer.Parent is Run)))
                {
                    return;
                }
                BringIntoViewAndHighlight((TextElement)leftPointer.Parent);
            }
        }
        private TextElement FindTextElement(TreeNode node, InlineCollection ic)
        {
            foreach (Inline inline in ic)
            {
                if (inline is Figure)
                {
                    TextElement te = FindTextElement(node, (Figure)inline);
                    if (te != null) return te;
                }
                else if (inline is Floater)
                {
                    TextElement te = FindTextElement(node, (Floater)inline);
                    if (te != null) return te;
                }
                else if (inline is Run)
                {
                    TextElement te = FindTextElement(node, (Run)inline);
                    if (te != null) return te;
                }
                else if (inline is LineBreak)
                {
                    TextElement te = FindTextElement(node, (LineBreak)inline);
                    if (te != null) return te;
                }
                else if (inline is InlineUIContainer)
                {
                    TextElement te = FindTextElement(node, (InlineUIContainer)inline);
                    if (te != null) return te;
                }
                else if (inline is Span)
                {
                    TextElement te = FindTextElement(node, (Span)inline);
                    if (te != null) return te;
                }
                else
                {
                    System.Diagnostics.Debug.Fail("TextElement type not matched ??");
                }
            }

            return null;
        }

        private TextElement FindTextElement(TreeNode node, Span span)
        {
            if (span.Tag == node) return span;
            return FindTextElement(node, span.Inlines);
        }

        private TextElement FindTextElement(TreeNode node, TableCellCollection tcc)
        {
            foreach (TableCell tc in tcc)
            {
                TextElement te = FindTextElement(node, tc);
                if (te != null) return te;
            }
            return null;
        }
        private TextElement FindTextElement(TreeNode node, TableRowCollection trc)
        {
            foreach (TableRow tr in trc)
            {
                TextElement te = FindTextElement(node, tr);
                if (te != null) return te;
            }
            return null;
        }
        private TextElement FindTextElement(TreeNode node, TableRowGroupCollection trgc)
        {
            foreach (TableRowGroup trg in trgc)
            {
                TextElement te = FindTextElement(node, trg);
                if (te != null) return te;
            }
            return null;
        }
        private TextElement FindTextElement(TreeNode node, ListItemCollection lic)
        {
            foreach (ListItem li in lic)
            {
                TextElement te = FindTextElement(node, li);
                if (te != null) return te;
            }
            return null;
        }

        private TextElement FindTextElement(TreeNode node, BlockCollection bc)
        {
            foreach (Block block in bc)
            {
                if (block is Section)
                {
                    TextElement te = FindTextElement(node, (Section)block);
                    if (te != null) return te;
                }
                else if (block is Paragraph)
                {
                    TextElement te = FindTextElement(node, (Paragraph)block);
                    if (te != null) return te;
                }
                else if (block is List)
                {
                    TextElement te = FindTextElement(node, (List)block);
                    if (te != null) return te;
                }
                else if (block is Table)
                {
                    TextElement te = FindTextElement(node, (Table)block);
                    if (te != null) return te;
                }
                else if (block is BlockUIContainer)
                {
                    TextElement te = FindTextElement(node, (BlockUIContainer)block);
                    if (te != null) return te;
                }
                else
                {
                    System.Diagnostics.Debug.Fail("TextElement type not matched ??");
                }
            }

            return null;
        }

        private TextElement FindTextElement(TreeNode node, TableCell tc)
        {
            if (tc.Tag == node) return tc;
            return FindTextElement(node, tc.Blocks);
        }

        private TextElement FindTextElement(TreeNode node, Run r)
        {
            if (r.Tag == node) return r;
            return null;
        }
        private TextElement FindTextElement(TreeNode node, LineBreak lb)
        {
            if (lb.Tag == node) return lb;
            return null;
        }
        private TextElement FindTextElement(TreeNode node, InlineUIContainer iuc)
        {
            if (iuc.Tag == node) return iuc;
            return null;
        }
        private TextElement FindTextElement(TreeNode node, BlockUIContainer b)
        {
            if (b.Tag == node) return b;
            return null;
        }
        private TextElement FindTextElement(TreeNode node, Floater f)
        {
            if (f.Tag == node) return f;
            return FindTextElement(node, f.Blocks);
        }
        private TextElement FindTextElement(TreeNode node, Figure f)
        {
            if (f.Tag == node) return f;
            return FindTextElement(node, f.Blocks);
        }
        private TextElement FindTextElement(TreeNode node, TableRow tr)
        {
            if (tr.Tag == node) return tr;
            return FindTextElement(node, tr.Cells);
        }
        private TextElement FindTextElement(TreeNode node, TableRowGroup trg)
        {
            if (trg.Tag == node) return trg;
            return FindTextElement(node, trg.Rows);
        }
        private TextElement FindTextElement(TreeNode node, ListItem li)
        {
            if (li.Tag == node) return li;
            return FindTextElement(node, li.Blocks);
        }
        private TextElement FindTextElement(TreeNode node)
        {
            return FindTextElement(node, TheFlowDocument.Blocks);
        }
        private TextElement FindTextElement(TreeNode node, Section section)
        {
            if (section.Tag == node) return section;
            return FindTextElement(node, section.Blocks);
        }
        private TextElement FindTextElement(TreeNode node, Paragraph para)
        {
            if (para.Tag == node) return para;
            return FindTextElement(node, para.Inlines);
        }
        private TextElement FindTextElement(TreeNode node, List list)
        {
            if (list.Tag == node) return list;
            return FindTextElement(node, list.ListItems);
        }
        private TextElement FindTextElement(TreeNode node, Table table)
        {
            if (table.Tag == node) return table;
            return FindTextElement(node, table.RowGroups);
        }


        public void BringIntoViewAndHighlight(TreeNode node)
        {
            TextElement textElement = FindTextElement(node);
            if (textElement != null)
            {
                BringIntoViewAndHighlight(textElement);
            }
        }

        public void BringIntoViewAndHighlightSub(TreeNode node)
        {
            TextElement textElement = FindTextElement(node);
            if (textElement != null)
            {
                BringIntoViewAndHighlightSub(textElement);
            }
        }

        public void BringIntoViewAndHighlight(string uid)
        {
            string id = XukToFlowDocument.IdToName(uid);

            TextElement textElement = null;
            if (m_idLinkTargets.ContainsKey(id))
            {
                textElement = m_idLinkTargets[id];
            }
            if (textElement == null)
            {
                textElement = TheFlowDocument.FindName(id) as TextElement;
            }
            if (textElement != null)
            {
                if (textElement.Tag is TreeNode)
                {
                    m_Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] DocumentPaneView.BringIntoViewAndHighlight", Category.Debug, Priority.Medium);

                    m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish((TreeNode)(textElement.Tag));
                }
                else
                {
                    BringIntoViewAndHighlight(textElement);
                }
            }
        }


        public void BringIntoViewAndHighlightSub(TextElement textElement)
        {
            textElement.BringIntoView();

            if (m_lastHighlightedSub != null)
            {
                m_lastHighlightedSub.Background = m_lastHighlightedSub_Background;
                m_lastHighlightedSub.Foreground = m_lastHighlightedSub_Foreground;

                setOrRemoveTextDecoration_SelectUnderline(m_lastHighlightedSub, true);
            }
            else
            {
                if (m_lastHighlighted != null)
                {
                    if (m_lastHighlighted is Block)
                    {
                        m_lastHighlighted_BorderBrush = ((Block)m_lastHighlighted).BorderBrush;
                        m_lastHighlighted_BorderThickness = ((Block)m_lastHighlighted).BorderThickness;
                        ((Block)m_lastHighlighted).BorderBrush = Brushes.OrangeRed;
                        ((Block)m_lastHighlighted).BorderThickness = new Thickness(1);
                    }

                    setOrRemoveTextDecoration_SelectUnderline(m_lastHighlighted, true);
                }
            }
            m_lastHighlightedSub = textElement;

            m_lastHighlightedSub_Background = m_lastHighlightedSub.Background;
            m_lastHighlightedSub.Background = Brushes.Yellow;

            m_lastHighlightedSub_Foreground = m_lastHighlightedSub.Foreground;
            m_lastHighlightedSub.Foreground = Brushes.Black;

            setOrRemoveTextDecoration_SelectUnderline(m_lastHighlightedSub, false);
        }

        public void BringIntoViewAndHighlight(TextElement textElement)
        {
            textElement.BringIntoView();

            if (m_lastHighlightedSub != null)
            {
                m_lastHighlightedSub.Background = m_lastHighlightedSub_Background;
                m_lastHighlightedSub.Foreground = m_lastHighlightedSub_Foreground;

                if (m_lastHighlighted != null && m_lastHighlighted is Block)
                {
                    ((Block)m_lastHighlighted).BorderBrush = m_lastHighlighted_BorderBrush;
                    ((Block)m_lastHighlighted).BorderThickness = m_lastHighlighted_BorderThickness;
                }

                setOrRemoveTextDecoration_SelectUnderline(m_lastHighlightedSub, true);
            }

            if (m_lastHighlighted != null)
            {
                m_lastHighlighted.Background = m_lastHighlighted_Background;
                m_lastHighlighted.Foreground = m_lastHighlighted_Foreground;

                setOrRemoveTextDecoration_SelectUnderline(m_lastHighlighted, true);
            }

            m_lastHighlighted = textElement;
            m_lastHighlightedSub = null;

            m_lastHighlighted_Background = m_lastHighlighted.Background;
            m_lastHighlighted.Background = Brushes.LightGoldenrodYellow;

            m_lastHighlighted_Foreground = m_lastHighlighted.Foreground;
            m_lastHighlighted.Foreground = Brushes.Black;

            setOrRemoveTextDecoration_SelectUnderline(m_lastHighlighted, false);
        }

        private void setOrRemoveTextDecoration_SelectUnderline(TextElement textElement, bool remove)
        {
            if (textElement is ListItem) // TEXT_ELEMENT
            {
                var blocks = ((ListItem)textElement).Blocks;
                foreach (var block in blocks)
                {
                    setOrRemoveTextDecoration_SelectUnderline(block, remove);
                }
            }
            else if (textElement is TableRowGroup) // TEXT_ELEMENT
            {
                var rows = ((TableRowGroup)textElement).Rows;
                foreach (var row in rows)
                {
                    setOrRemoveTextDecoration_SelectUnderline(row, remove);
                }
            }
            else if (textElement is TableRow) // TEXT_ELEMENT
            {
                var cells = ((TableRow)textElement).Cells;
                foreach (var cell in cells)
                {
                    setOrRemoveTextDecoration_SelectUnderline(cell, remove);
                }
            }
            else if (textElement is TableCell) // TEXT_ELEMENT
            {
                var blocks = ((TableCell)textElement).Blocks;
                foreach (var block in blocks)
                {
                    setOrRemoveTextDecoration_SelectUnderline(block, remove);
                }
            }
            else if (textElement is Table) // BLOCK
            {
                var rowGs = ((Table)textElement).RowGroups;
                foreach (var rowG in rowGs)
                {
                    setOrRemoveTextDecoration_SelectUnderline(rowG, remove);
                }
            }
            else if (textElement is Paragraph) // BLOCK
            {
                var inlines = ((Paragraph)textElement).Inlines;
                foreach (var inline in inlines)
                {
                    setOrRemoveTextDecoration_SelectUnderline_(inline, remove);
                }
            }
            else if (textElement is Section) // BLOCK
            {
                var blocks = ((Section)textElement).Blocks;
                foreach (var block in blocks)
                {
                    setOrRemoveTextDecoration_SelectUnderline(block, remove);
                }
            }
            else if (textElement is List) // BLOCK
            {
                var lis = ((List)textElement).ListItems;
                foreach (var li in lis)
                {
                    setOrRemoveTextDecoration_SelectUnderline(li, remove);
                }
            }
            else if (textElement is BlockUIContainer) // BLOCK
            {
                // ((BlockUIContainer)textElement).Child => not to be underlined !
            }
            else if (textElement is Span) // INLINE
            {
                var inlines = ((Span)textElement).Inlines;
                foreach (var inline in inlines)
                {
                    setOrRemoveTextDecoration_SelectUnderline_(inline, remove);
                }
            }
            else if (textElement is Floater) // INLINE
            {
                var blocks = ((Floater)textElement).Blocks;
                foreach (var block in blocks)
                {
                    setOrRemoveTextDecoration_SelectUnderline(block, remove);
                }
            }
            else if (textElement is Figure) // INLINE
            {
                var blocks = ((Figure)textElement).Blocks;
                foreach (var block in blocks)
                {
                    setOrRemoveTextDecoration_SelectUnderline(block, remove);
                }
            }
            else if (textElement is Inline) // includes InlineUIContainer, LineBreak and Run
            {
                setOrRemoveTextDecoration_SelectUnderline_((Inline)textElement, remove);
            }
            else
            {
#if DEBUG
                Debugger.Break();
#endif
            }
        }

        private void setOrRemoveTextDecoration_SelectUnderline_(Inline inline, bool remove)
        {
            if (remove)
            {
                inline.TextDecorations = null;
                return;
            }

            var decUnder = new TextDecoration(
                TextDecorationLocation.Underline,
                new Pen(Brushes.DarkGoldenrod, 1)
                {
                    DashStyle = DashStyles.Dot
                },
                2,
                TextDecorationUnit.Pixel,
                TextDecorationUnit.FontRecommended
            );

            var decOver = new TextDecoration(
                TextDecorationLocation.OverLine,
                new Pen(Brushes.DarkGoldenrod, 1)
                {
                    DashStyle = DashStyles.Dot
                },
                0,
                TextDecorationUnit.Pixel,
                TextDecorationUnit.FontRecommended
            );

            var decs = new TextDecorationCollection { decUnder, decOver };

            inline.TextDecorations = decs;
        }

        private void setTextDecoration_ErrorUnderline(Inline inline)
        {
            //if (textDecorations == null || !textDecorations.Equals(System.Windows.TextDecorations.Underline))
            //{
            //    textDecorations = System.Windows.TextDecorations.Underline;
            //}
            //else
            //{
            //    textDecorations = new TextDecorationCollection(); // or null
            //}

            var dec = new TextDecoration(
                TextDecorationLocation.Underline,
                new Pen(Brushes.Red, 1)
                {
                    DashStyle = DashStyles.Dot
                },
                1,
                TextDecorationUnit.FontRecommended,
                TextDecorationUnit.FontRecommended
            );

            //var decs = new TextDecorationCollection { dec };
            var decs = new TextDecorationCollection(TextDecorations.OverLine) { dec };

            inline.TextDecorations = decs;
        }

        public List<object> GetVisibleTextElements()
        {
            m_FoundVisible = false;
            temp_ParagraphVisualCount = 0;
            temp_ContainerVisualCount = 0;
            temp_OtherCount = 0;
            //List<object> list = GetVisibleTextObjects_Logical(TheFlowDocument);
            List<object> list = GetVisibleTextObjects_Visual(FlowDocReader);
            foreach (object obj in list)
            {
                //TODO: find the TextElement objects, and ultimately, the urakawa Nodes that correspond to this list
                //how to find a logical object from a visual one?
            }
            return list;
        }

        private bool m_FoundVisible;
        private ScrollViewer m_ScrollViewer;
      
        private List<object> GetVisibleTextObjects_Logical(DependencyObject obj)
        {
            List<object> elms = new List<object>();
            
            IEnumerable children = LogicalTreeHelper.GetChildren(obj);
            IEnumerator enumerator = children.GetEnumerator();
            
            while (enumerator.MoveNext())
            {    
                if (enumerator.Current is TextElement && IsTextObjectInView((TextElement)enumerator.Current))
                {
                    elms.Add(enumerator.Current);
                }
                if (enumerator.Current is DependencyObject)
                {
                    List<object> list = GetVisibleTextObjects_Logical((DependencyObject)enumerator.Current);
                    elms.AddRange(list);
                }
            }
            return elms;
        }
        

        //just for testing purposes
        private int temp_ContainerVisualCount;
        private int temp_ParagraphVisualCount;
        private int temp_OtherCount;

        private List<object> GetVisibleTextObjects_Visual(DependencyObject obj)
        {
            if (obj.DependencyObjectType.Name == "ParagraphVisual") temp_ParagraphVisualCount++;
            else if (obj is ContainerVisual) temp_ContainerVisualCount++;
            else temp_OtherCount++;

            if (obj is ScrollContentPresenter)
            {
                object view = ((ScrollContentPresenter) obj).Content;
            }
            List<object> elms = new List<object>();

            int childcount = VisualTreeHelper.GetChildrenCount(obj);
            
            for (int i = 0; i<childcount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child is ScrollViewer) m_ScrollViewer = (ScrollViewer) child;
                if (child != null)
                {
                    //there may be more types
                    if (child.DependencyObjectType.Name == "ParagraphVisual")
                    {
                        if (IsTextObjectInView((Visual)child))
                        {
                            m_FoundVisible = true;
                            elms.Add(child);
                            List<object> list = GetVisibleTextObjects_Visual(child);
                            if (list != null) elms.AddRange(list);
                        }
                        else
                        {
                            //if this is our first non-visible object
                            //after encountering one or more visible objects, assume we are out of the viewable region
                            //since it should only show contiguous objects
                            if (m_FoundVisible)
                            {
                                return null;
                            }
                            //else, we haven't found any visible text objects yet, so keep looking
                            else
                            {
                                List<object> list = GetVisibleTextObjects_Visual(child);
                                if (list != null) elms.AddRange(list);
                            }
                        }
                    }
                    //just recurse for non-text objects
                    else
                    {
                        List<object> list = GetVisibleTextObjects_Visual(child);
                        if (list != null) elms.AddRange(list);
                    }
                }

            }
            return elms;
        }
        //say whether the text object is in view on the screen.  assumed: obj is a text visual
        private bool IsTextObjectInView(Visual obj)
        {
            //ParagraphVisual objects are also ContainerVisual
            if (obj is ContainerVisual)
            {
                ContainerVisual cv = (ContainerVisual) obj;
                //express the visual object's coordinates in terms of the flow doc reader
                GeneralTransform paraTransform = obj.TransformToAncestor(m_ScrollViewer);
                Rect rect;
                if (cv.Children.Count > 0)
                    rect = cv.DescendantBounds;
                else
                    rect = cv.ContentBounds;
                Rect rectTransformed = paraTransform.TransformBounds(rect);

                //then figure out if these coordinates are in the currently visible document portion))
                Rect viewportRect = new Rect(0, 0, m_ScrollViewer.ViewportWidth, m_ScrollViewer.ViewportHeight);
                if (viewportRect.Contains(rectTransformed))
                    return true;
                else
                    return false;
            }
            return false;

        }
        private bool IsTextObjectInView(TextElement obj)
        {
            //how to find visibility information from a logical object??
            DependencyObject test = obj;
            while (test != null)
            {
                test = LogicalTreeHelper.GetParent(test);
                if (test is Visual)
                {
                    break;
                }
            }
            if (drillDown(test) != null)
            {
                return true;
            }
            return true;
        }

        private DependencyObject drillDown(DependencyObject test)
        {
            IEnumerable children = LogicalTreeHelper.GetChildren(test);
            foreach (DependencyObject obj in children)
            {
                if (obj is Visual)
                    return obj;
                else
                    return drillDown(obj);
            }
            return null;
        }
        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            List<object> list = GetVisibleTextElements();
            string str = "The visible text objects, perhaps with some redundancies:\n";
            foreach (object obj in list)
            {
                str += obj.ToString();
                str += "\n";

            }
            MessageBox.Show(str);
        }
    }
}
