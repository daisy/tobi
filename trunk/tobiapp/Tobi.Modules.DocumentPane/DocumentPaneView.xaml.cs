using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Media;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Events;
using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa;
using urakawa.core;
using urakawa.xuk;
using System.Diagnostics;

namespace Tobi.Plugin.DocumentPane
{
    /// <summary>
    /// Interaction logic for DocumentPaneView.xaml
    /// </summary>
    [Export(typeof(DocumentPaneView)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class DocumentPaneView // : INotifyPropertyChangedEx
    {
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
            //TheFlowDocument.Blocks.Add(new Paragraph(new Run(UserInterfaceStrings.No_Document)));

            m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Subscribe(OnTreeNodeSelected, ThreadOption.UIThread);
            m_EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Subscribe(OnSubTreeNodeSelected, ThreadOption.UIThread);

            m_EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(OnProjectLoaded, ThreadOption.UIThread);
            m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(OnProjectUnLoaded, ThreadOption.UIThread);
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
                TheFlowDocument.Blocks.Add(new Paragraph(new Run(UserInterfaceStrings.No_Document)));
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
                }
            }
            m_lastHighlightedSub = textElement;

            m_lastHighlightedSub_Background = m_lastHighlightedSub.Background;
            m_lastHighlightedSub.Background = Brushes.Yellow;

            m_lastHighlightedSub_Foreground = m_lastHighlightedSub.Foreground;
            m_lastHighlightedSub.Foreground = Brushes.Black;
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
            }
            if (m_lastHighlighted != null)
            {
                m_lastHighlighted.Background = m_lastHighlighted_Background;
                m_lastHighlighted.Foreground = m_lastHighlighted_Foreground;
            }

            m_lastHighlighted = textElement;
            m_lastHighlightedSub = null;

            m_lastHighlighted_Background = m_lastHighlighted.Background;
            m_lastHighlighted.Background = Brushes.LightGoldenrodYellow;

            m_lastHighlighted_Foreground = m_lastHighlighted.Foreground;
            m_lastHighlighted.Foreground = Brushes.Black;
        }
    }
}
