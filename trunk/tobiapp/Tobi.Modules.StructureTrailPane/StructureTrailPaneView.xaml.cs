using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
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
using urakawa.property.xml;
using urakawa.xuk;

namespace Tobi.Plugin.StructureTrailPane
{
    internal class TreeNodeWrapper
    {
        public TreeNode TreeNode;
        //public Popup Popup;

        public override string ToString()
        {
            QualifiedName qname = TreeNode.GetXmlElementQName();
            if (qname != null)
            {
                return qname.LocalName;
            }
            return "TEXT";
        }
    }

    /// <summary>
    /// Interaction logic for StructureTrailPaneView.xaml
    /// </summary>
    [Export(typeof(StructureTrailPaneView)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class StructureTrailPaneView // : INotifyPropertyChangedEx
    {
        Style m_ButtonStyle = (Style)Application.Current.FindResource("ToolBarButtonBaseStyle");

        private List<TreeNode> PathToCurrentTreeNode;

        private void updateBreadcrumbPanel(Tuple<TreeNode, TreeNode> treeNodeSelection)
        {
            BreadcrumbPanel.Children.Clear();
            BreadcrumbPanel.Children.Add(m_FocusStartElement);

            bool firstTime = PathToCurrentTreeNode == null;

            TreeNode treeNodeSel = treeNodeSelection.Item2 ?? treeNodeSelection.Item1;
            if (true) // this was too confusing for the user: firstTime || !PathToCurrentTreeNode.Contains(treeNodeSel))
            {
                PathToCurrentTreeNode = new List<TreeNode>();
                TreeNode treeNode = treeNodeSel;
                do
                {
                    PathToCurrentTreeNode.Add(treeNode);
                } while ((treeNode = treeNode.Parent) != null);

                PathToCurrentTreeNode.Reverse();
            }

            int counter = 0;
            foreach (TreeNode n in PathToCurrentTreeNode)
            {
                QualifiedName qname = n.GetXmlElementQName();

                //TODO: could use Label+Hyperlink+TextBlock instead of button
                // (not with NavigateUri+RequestNavigate, because it requires a valid URI.
                // instead we use the Tag property which contains a reference to a TreeNode, 
                // so we can use the Click event)

                bool withMedia = n.GetManagedAudioMediaOrSequenceMedia() != null;

                var butt = new Button
                {
                    Tag = n,
                    BorderThickness = new Thickness(0.0),
                    BorderBrush = null,
                    Background = Brushes.Transparent,
                    Foreground = (withMedia ? SystemColors.HighlightBrush : SystemColors.ControlDarkBrush),
                    Cursor = Cursors.Hand,
                    Style = m_ButtonStyle
                };

                var run = new Run((qname != null ? qname.LocalName : "TEXT"))
                {
                    TextDecorations = TextDecorations.Underline
                };
                butt.Content = run;

                butt.Click += OnBreadCrumbButtonClick;

                BreadcrumbPanel.Children.Add(butt);

                if (counter < PathToCurrentTreeNode.Count && n.Children.Count > 0)
                {
                    var arrow = (Path)Application.Current.FindResource("Arrow");

                    var tb = new Button
                    {
                        Content = arrow,
                        Tag = n,
                        BorderBrush = null,
                        BorderThickness = new Thickness(0.0),
                        Background = Brushes.Transparent,
                        Foreground = SystemColors.ControlDarkDarkBrush, //ActiveBorderBrush,
                        Cursor = Cursors.Cross,
                        FontWeight = FontWeights.ExtraBold,
                        Style = m_ButtonStyle,
                        Focusable = true,
                        IsTabStop = true
                    };

                    tb.ContextMenu = new ContextMenu();

                    foreach (TreeNode child in n.Children.ContentsAs_YieldEnumerable)
                    {
                        bool childIsInPath = PathToCurrentTreeNode.Contains(child);

                        var menuItem = new MenuItem();
                        QualifiedName qnameChild = child.GetXmlElementQName();
                        if (qnameChild != null)
                        {
                            if (childIsInPath)
                            {
                                var runMenuItem = new Run(qnameChild.LocalName) { FontWeight = FontWeights.ExtraBold };
                                menuItem.Header = runMenuItem;
                            }
                            else
                            {
                                menuItem.Header = qnameChild.LocalName;
                            }
                        }
                        else
                        {
                            if (childIsInPath)
                            {
                                var runMenuItem = new Run("TXT") { FontWeight = FontWeights.ExtraBold };
                                menuItem.Header = runMenuItem;
                            }
                            else
                            {
                                menuItem.Header = "TXT";
                            }
                        }
                        menuItem.Tag = child;
                        menuItem.Click += menuItem_Click;
                        tb.ContextMenu.Items.Add(menuItem);
                    }

                    tb.Click += OnBreadCrumbSeparatorClick;

                    BreadcrumbPanel.Children.Add(tb);

                    tb.SetValue(AutomationProperties.NameProperty, Tobi_Plugin_StructureTrailPane_Lang.XMLChildren);                  // TODO LOCALIZE XMLChildren
                }

                bool selected = n == treeNodeSelection.Item2 || n == treeNodeSelection.Item1;
                if (selected)
                {
                    run.FontWeight = FontWeights.Heavy;
                }

                butt.SetValue(AutomationProperties.NameProperty,
                    (qname != null ? qname.LocalName : Tobi_Plugin_StructureTrailPane_Lang.NoXMLFound)
                    + (selected ? Tobi_Plugin_StructureTrailPane_Lang.Selected : "")
                    + (withMedia ? Tobi_Plugin_StructureTrailPane_Lang.Audio : ""));        // TODO LOCALIZE NoXMLFound
                // TODO LOCALIZE Selected
                // TODO LOCALIZE Audio

                counter++;
            }

            if (firstTime)
            {
                CommandFocus.Execute();
            }
        }

        private void menuItem_Click(object sender, RoutedEventArgs e)
        {
            var ui = sender as MenuItem;
            if (ui == null)
            {
                return;
            }

            m_UrakawaSession.PerformTreeNodeSelection((TreeNode)ui.Tag);
            //selectNode(wrapper.TreeNode, true);

            CommandFocus.Execute();
        }

        private void OnBreadCrumbSeparatorClick(object sender, RoutedEventArgs e)
        {
            var ui = sender as Button;
            if (ui == null)
            {
                return;
            }
            ui.ContextMenu.PlacementTarget = ui;
            ui.ContextMenu.Placement = PlacementMode.Bottom;
            //ui.ContextMenu.PlacementRectangle=ui.
            ui.ContextMenu.IsOpen = true;

            //var node = (TreeNode)ui.Tag;

            //var popup = new Popup { IsOpen = false, Width = 120, Height = 150 };
            //BreadcrumbPanel.Children.Add(popup);
            //popup.PlacementTarget = ui;
            //popup.LostFocus += OnPopupLostFocus;
            //popup.LostMouseCapture += OnPopupLostFocus;
            //popup.LostKeyboardFocus += OnPopupLostKeyboardFocus;

            //var listOfNodes = new ListView();
            //listOfNodes.SelectionChanged += OnListOfNodesSelectionChanged;

            //foreach (TreeNode child in node.Children.ContentsAs_YieldEnumerable)
            //{
            //    listOfNodes.Items.Add(new TreeNodeWrapper()
            //    {
            //        Popup = popup,
            //        TreeNode = child
            //    });
            //}

            //var scroll = new ScrollViewer
            //{
            //    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            //    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            //    Content = listOfNodes
            //};

            //popup.Child = scroll;
            //popup.IsOpen = true;

            //FocusHelper.FocusBeginInvoke(listOfNodes);
        }

        //private void OnPopupLostFocus(object sender, RoutedEventArgs e)
        //{
        //    var ui = sender as Popup;
        //    if (ui == null)
        //    {
        //        return;
        //    }
        //    ui.IsOpen = false;
        //}

        //private void OnPopupLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        //{
        //    var ui = sender as Popup;
        //    if (ui == null)
        //    {
        //        return;
        //    }
        //    ui.IsOpen = false;
        //}

        //private void OnListOfNodesSelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    var ui = sender as ListView;
        //    if (ui == null)
        //    {
        //        return;
        //    }
        //    var wrapper = (TreeNodeWrapper)ui.SelectedItem;
        //    wrapper.Popup.IsOpen = false;

        //    m_UrakawaSession.PerformTreeNodeSelection(wrapper.TreeNode);
        //    //selectNode(wrapper.TreeNode, true);

        //    CommandFocus.Execute();
        //}

        //private void selectNode(TreeNode node, bool toggleIntersection)
        //{
        //    if (node == null) return;

        //    if (toggleIntersection && node == CurrentTreeNode)
        //    {
        //        if (CurrentSubTreeNode != null)
        //        {
        //            m_Logger.Log("-- PublishEvent [SubTreeNodeSelectedEvent] DocumentPaneView.selectNode",
        //                       Category.Debug, Priority.Medium);

        //            m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(CurrentSubTreeNode);
        //        }
        //        else
        //        {
        //            var treeNode = node.GetFirstDescendantWithText();
        //            if (treeNode != null)
        //            {
        //                m_Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] DocumentPaneView.selectNode",
        //                             Category.Debug, Priority.Medium);

        //                m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(treeNode);
        //            }
        //        }

        //        return;
        //    }

        //    if (CurrentTreeNode != null && CurrentSubTreeNode != CurrentTreeNode
        //        && node.IsDescendantOf(CurrentTreeNode))
        //    {
        //        m_Logger.Log(
        //            "-- PublishEvent [SubTreeNodeSelectedEvent] DocumentPaneView.selectNode",
        //            Category.Debug, Priority.Medium);

        //        m_EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(node);
        //    }
        //    else
        //    {
        //        m_Logger.Log(
        //            "-- PublishEvent [TreeNodeSelectedEvent] DocumentPaneView.selectNode",
        //            Category.Debug, Priority.Medium);

        //        m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(node);
        //    }
        //}

        private void OnBreadCrumbButtonClick(object sender, RoutedEventArgs e)
        {
            var ui = sender as Button;
            if (ui == null)
            {
                return;
            }
            m_UrakawaSession.PerformTreeNodeSelection((TreeNode)ui.Tag);
            //selectNode((TreeNode)ui.Tag, true);

            CommandFocus.Execute();
        }

        private TextBlockWithAutomationPeer m_FocusStartElement;

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

        public RichDelegateCommand CommandFocus { get; private set; }

        private readonly ILoggerFacade m_Logger;

        private readonly IEventAggregator m_EventAggregator;

        private readonly IShellView m_ShellView;
        private readonly IUrakawaSession m_UrakawaSession;

        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        [ImportingConstructor]
        public StructureTrailPaneView(
            IEventAggregator eventAggregator,
            ILoggerFacade logger,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession urakawaSession,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView)
        {
            m_UrakawaSession = urakawaSession;
            m_EventAggregator = eventAggregator;
            m_Logger = logger;
            m_ShellView = shellView;

            DataContext = this;

            //
            CommandFocus = new RichDelegateCommand(
                Tobi_Plugin_StructureTrailPane_Lang.CmdDocumentFocus_ShortDesc,
                null,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadGnomeNeuIcon("Neu_format-indent-more"),
                () => FocusHelper.FocusBeginInvoke(m_FocusStartElement),
                () => true,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Focus_Doc));

            m_ShellView.RegisterRichCommand(CommandFocus);
            //

            InitializeComponent();

            var arrow = (Path)Application.Current.FindResource("Arrow");
            m_FocusStartElement = new TextBlockWithAutomationPeer
            {
                Text = " ",
                //Content = arrow,
                //BorderBrush = null,
                //BorderThickness = new Thickness(0.0),
                Background = Brushes.Transparent,
                //Foreground = Brushes.Black,
                Cursor = Cursors.Cross,
                FontWeight = FontWeights.ExtraBold,
                //Style = m_ButtonStyle,
                Focusable = true,
                //IsTabStop = true
            };

            OnProjectUnLoaded(null);

            //m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Subscribe(OnTreeNodeSelected, TreeNodeSelectedEvent.THREAD_OPTION);
            //m_EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Subscribe(OnSubTreeNodeSelected, SubTreeNodeSelectedEvent.THREAD_OPTION);

            m_EventAggregator.GetEvent<TreeNodeSelectionChangedEvent>().Subscribe(OnTreeNodeSelectionChanged, TreeNodeSelectionChangedEvent.THREAD_OPTION);

            m_EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(OnProjectLoaded, ProjectLoadedEvent.THREAD_OPTION);
            m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(OnProjectUnLoaded, ProjectUnLoadedEvent.THREAD_OPTION);

            //m_EventAggregator.GetEvent<EscapeEvent>().Subscribe(obj => CommandFocus.Execute(), EscapeEvent.THREAD_OPTION);
        }

        private void OnProjectUnLoaded(Project project)
        {
            if (!Dispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action<Project>)OnProjectUnLoaded, project);
                return;
            }

            BreadcrumbPanel.Children.Clear();
            BreadcrumbPanel.Background = SystemColors.ControlBrush;
            BreadcrumbPanel.Children.Add(m_FocusStartElement);
            var tb = new TextBlock(new Run(Tobi_Plugin_StructureTrailPane_Lang.No_Document)) { Margin = new Thickness(4, 2, 0, 2) };
            BreadcrumbPanel.Children.Add(tb);

            PathToCurrentTreeNode = null;
            m_FocusStartElement.SetAccessibleNameAndNotifyScreenReaderAutomationIfKeyboardFocused(Tobi_Plugin_StructureTrailPane_Lang.No_Document);
            m_FocusStartElement.ToolTip = Tobi_Plugin_StructureTrailPane_Lang.No_Document;

            if (project == null) return;

            project.Presentations.Get(0).UndoRedoManager.CommandDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandReDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandUnDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.TransactionEnded -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.TransactionCancelled -= OnUndoRedoManagerChanged;
        }

        private void OnProjectLoaded(Project project)
        {
            if (!Dispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action<Project>)OnProjectLoaded, project);
                return;
            }

            BreadcrumbPanel.Children.Clear();
            BreadcrumbPanel.Background = SystemColors.WindowBrush;
            BreadcrumbPanel.Children.Add(m_FocusStartElement);

            PathToCurrentTreeNode = null;
            m_FocusStartElement.SetAccessibleNameAndNotifyScreenReaderAutomationIfKeyboardFocused(". . .");

            if (project == null) return;

            project.Presentations.Get(0).UndoRedoManager.CommandDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandReDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandUnDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.TransactionEnded += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.TransactionCancelled += OnUndoRedoManagerChanged;
        }

        //private TreeNode m_CurrentTreeNode;
        //public TreeNode CurrentTreeNode
        //{
        //    get
        //    {
        //        return m_CurrentTreeNode;
        //    }
        //    set
        //    {
        //        if (m_CurrentTreeNode == value) return;

        //        m_CurrentTreeNode = value;
        //        //RaisePropertyChanged(() => CurrentTreeNode);
        //    }
        //}

        //private TreeNode m_CurrentSubTreeNode;
        //public TreeNode CurrentSubTreeNode
        //{
        //    get
        //    {
        //        return m_CurrentSubTreeNode;
        //    }
        //    set
        //    {
        //        if (m_CurrentSubTreeNode == value) return;
        //        m_CurrentSubTreeNode = value;


        //        //RaisePropertyChanged(() => CurrentSubTreeNode);
        //    }
        //}

        private void refreshData(Tuple<TreeNode, TreeNode> newTreeNodeSelection)
        {
            if (newTreeNodeSelection.Item1 == null) return;

            //TreeNode treeNode = newTreeNodeSelection.Item2 ?? newTreeNodeSelection.Item1;

            QualifiedName qName1 = newTreeNodeSelection.Item1.GetXmlElementQName();
            string qName1_ = (qName1 == null
                                  ? Tobi_Plugin_StructureTrailPane_Lang.NoXML
                                  : String.Format(Tobi_Plugin_StructureTrailPane_Lang.XMLName, qName1.LocalName)
                //+ (!string.IsNullOrEmpty(imgAlt) ? " // " + imgAlt : "")
                             );

            string str = null;
            if (newTreeNodeSelection.Item2 == null) // no sub-treenode
            {
                string audioInfo1 = newTreeNodeSelection.Item1.GetAudioMedia() != null ||
                                   newTreeNodeSelection.Item1.GetFirstAncestorWithManagedAudio() != null
                                       ? ""
                                       : Tobi_Plugin_StructureTrailPane_Lang.NoAudio;

                string text1 = newTreeNodeSelection.Item1.GetTextFlattened(true);
                if (!string.IsNullOrEmpty(text1)
                     && text1.Length > 100)
                {
                    text1 = text1.Substring(0, 100) + ". . .";
                }

                str = qName1_ + " // " + text1 + " *** " + audioInfo1;

                // IMAGE ALT IS NOW IN THE FLATTENED TREENODE TEXT
                //string imgAlt = null;
                //if (qName != null && qName.LocalName.ToLower() == "img")
                //{
                //    XmlAttribute xmlAttr = treeNode.GetXmlProperty().GetAttribute("alt");
                //    if (xmlAttr != null)
                //    {
                //        imgAlt = xmlAttr.Value;
                //    }
                //}
            }
            else
            {
                QualifiedName qName2 = newTreeNodeSelection.Item2.GetXmlElementQName();
                string qName2_ = (qName2 == null
                                      ? Tobi_Plugin_StructureTrailPane_Lang.NoXML
                                      : String.Format(Tobi_Plugin_StructureTrailPane_Lang.XMLName, qName2.LocalName)
                    //+ (!string.IsNullOrEmpty(imgAlt) ? " // " + imgAlt : "")
                                 );

                string audioInfo2 = newTreeNodeSelection.Item2.GetAudioMedia() != null ||
                                   newTreeNodeSelection.Item2.GetFirstAncestorWithManagedAudio() != null
                                       ? ""
                                       : Tobi_Plugin_StructureTrailPane_Lang.NoAudio;

                string text2 = newTreeNodeSelection.Item2.GetTextFlattened(true);
                if (!string.IsNullOrEmpty(text2)
                     && text2.Length > 100)
                {
                    text2 = text2.Substring(0, 100) + ". . .";
                }

                str = qName1_ + " >> " + qName2_ + " // " + text2 + " *** " + audioInfo2;
            }

            Console.WriteLine(@"}}}}}" + str);

            m_FocusStartElement.SetAccessibleNameAndNotifyScreenReaderAutomationIfKeyboardFocused(str);
            m_FocusStartElement.ToolTip = str;

            updateBreadcrumbPanel(newTreeNodeSelection);
        }

        private void OnTreeNodeSelectionChanged(Tuple<Tuple<TreeNode, TreeNode>, Tuple<TreeNode, TreeNode>> oldAndNewTreeNodeSelection)
        {
            //Tuple<TreeNode, TreeNode> oldTreeNodeSelection = oldAndNewTreeNodeSelection.Item1;
            Tuple<TreeNode, TreeNode> newTreeNodeSelection = oldAndNewTreeNodeSelection.Item2;

            refreshData(newTreeNodeSelection);
        }

        private void OnUndoRedoManagerChanged(object sender, UndoRedoManagerEventArgs eventt)
        {
            if (!Dispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action<object, UndoRedoManagerEventArgs>)OnUndoRedoManagerChanged, sender, eventt);
                return;
            }

            //m_Logger.Log("StructureTrailView.OnUndoRedoManagerChanged", Category.Debug, Priority.Medium);

            if (!(eventt is DoneEventArgs
                           || eventt is UnDoneEventArgs
                           || eventt is ReDoneEventArgs
                           || eventt is TransactionEndedEventArgs
                           || eventt is TransactionCancelledEventArgs
                           ))
            {
                Debug.Fail("This should never happen !!");
                return;
            }

            if (m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.IsTransactionActive)
            {
                Debug.Assert(eventt is DoneEventArgs || eventt is TransactionEndedEventArgs);
                //m_Logger.Log("StructureTrailView.OnUndoRedoManagerChanged (exit: ongoing TRANSACTION...)", Category.Debug, Priority.Medium);
                return;
            }

            if (!(eventt.Command is ManagedAudioMediaInsertDataCommand)
                && !(eventt.Command is TreeNodeChangeTextCommand)
                && !(eventt.Command is TreeNodeSetManagedAudioMediaCommand)
                && !(eventt.Command is TreeNodeAudioStreamDeleteCommand)
                && !(eventt.Command is CompositeCommand)
                )
            {
                return;
            }

            Tuple<TreeNode, TreeNode> newTreeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();
            refreshData(newTreeNodeSelection);
        }
    }
}
