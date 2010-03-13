using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa;
using urakawa.core;
using urakawa.xuk;

namespace Tobi.Plugin.StructureTrailPane
{
    internal class TreeNodeWrapper
    {
        public TreeNode TreeNode;
        public Popup Popup;

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
            if (firstTime || !PathToCurrentTreeNode.Contains(treeNodeSel))
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
                    Foreground = (withMedia ? Brushes.Blue : Brushes.CadetBlue),
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
                        Foreground = Brushes.Black,
                        Cursor = Cursors.Cross,
                        FontWeight = FontWeights.ExtraBold,
                        Style = m_ButtonStyle,
                        Focusable = true,
                        IsTabStop = true
                    };

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

        private void OnBreadCrumbSeparatorClick(object sender, RoutedEventArgs e)
        {
            var ui = sender as Button;
            if (ui == null)
            {
                return;
            }

            var node = (TreeNode)ui.Tag;

            var popup = new Popup { IsOpen = false, Width = 120, Height = 150 };
            BreadcrumbPanel.Children.Add(popup);
            popup.PlacementTarget = ui;
            popup.LostFocus += OnPopupLostFocus;
            popup.LostMouseCapture += OnPopupLostFocus;
            popup.LostKeyboardFocus += OnPopupLostKeyboardFocus;

            var listOfNodes = new ListView();
            listOfNodes.SelectionChanged += OnListOfNodesSelectionChanged;

            foreach (TreeNode child in node.Children.ContentsAs_YieldEnumerable)
            {
                listOfNodes.Items.Add(new TreeNodeWrapper()
                {
                    Popup = popup,
                    TreeNode = child
                });
            }

            var scroll = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = listOfNodes
            };

            popup.Child = scroll;
            popup.IsOpen = true;

            FocusHelper.FocusBeginInvoke(listOfNodes);
        }

        private void OnPopupLostFocus(object sender, RoutedEventArgs e)
        {
            var ui = sender as Popup;
            if (ui == null)
            {
                return;
            }
            ui.IsOpen = false;
        }

        private void OnPopupLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            var ui = sender as Popup;
            if (ui == null)
            {
                return;
            }
            ui.IsOpen = false;
        }

        private void OnListOfNodesSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var ui = sender as ListView;
            if (ui == null)
            {
                return;
            }
            var wrapper = (TreeNodeWrapper)ui.SelectedItem;
            wrapper.Popup.IsOpen = false;

            m_UrakawaSession.PerformTreeNodeSelection(wrapper.TreeNode);
            //selectNode(wrapper.TreeNode, true);

            CommandFocus.Execute();
        }

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
                Tobi_Plugin_StructureTrailPane_Lang.Document_Focus,
                null,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("edit-select-all"),
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
                Foreground = Brushes.Black,
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

            m_EventAggregator.GetEvent<EscapeEvent>().Subscribe(obj => CommandFocus.Execute(), EscapeEvent.THREAD_OPTION);
        }

        private void OnProjectUnLoaded(Project obj)
        {
            BreadcrumbPanel.Children.Clear();
            BreadcrumbPanel.Children.Add(m_FocusStartElement);
            BreadcrumbPanel.Children.Add(new TextBlock(new Run(Tobi_Plugin_StructureTrailPane_Lang.No_Document)));

            PathToCurrentTreeNode = null;
            m_FocusStartElement.SetAccessibleNameAndNotifyScreenReaderAutomationIfKeyboardFocused(Tobi_Plugin_StructureTrailPane_Lang.No_Document);
        }

        private void OnProjectLoaded(Project project)
        {
            BreadcrumbPanel.Children.Clear();
            BreadcrumbPanel.Children.Add(m_FocusStartElement);

            PathToCurrentTreeNode = null;
            m_FocusStartElement.SetAccessibleNameAndNotifyScreenReaderAutomationIfKeyboardFocused("No selection");
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

        private void OnTreeNodeSelectionChanged(Tuple<TreeNode, TreeNode> treeNodeSelection)
        {
            TreeNode treeNode = treeNodeSelection.Item2 ?? treeNodeSelection.Item1;

            string audioInfo = treeNode.GetAudioMedia () != null || treeNode.GetFirstAncestorWithManagedAudio () != null ? "" : " No Audio "; // to do localize
            var qName = treeNode.GetXmlElementQName();
            string str = (qName == null ? Tobi_Plugin_StructureTrailPane_Lang.NoXML : String.Format(Tobi_Plugin_StructureTrailPane_Lang.XMLName, qName.LocalName)) + treeNode.GetTextMediaFlattened() ;
            if (str.Length > 100)
            {
                str = str.Substring(0, 100) + ". . ." ;
            }
            str = str + audioInfo;
            Console.WriteLine(@"}}}}}" + str);

            m_FocusStartElement.SetAccessibleNameAndNotifyScreenReaderAutomationIfKeyboardFocused(str);


            updateBreadcrumbPanel(treeNodeSelection);
        }

        //private void OnSubTreeNodeSelected(TreeNode node)
        //{
        //    if (node == null || CurrentTreeNode == null)
        //    {
        //        return;
        //    }
        //    if (CurrentSubTreeNode == node)
        //    {
        //        return;
        //    }
        //    if (!node.IsDescendantOf(CurrentTreeNode))
        //    {
        //        return;
        //    }
        //    CurrentSubTreeNode = node;

        //    updateBreadcrumbPanel(node);
        //}

        //private void OnTreeNodeSelected(TreeNode node)
        //{
        //    if (node == null)
        //    {
        //        return;
        //    }
        //    if (CurrentTreeNode == node)
        //    {
        //        return;
        //    }

        //    TreeNode subTreeNode = null;

        //    if (CurrentTreeNode != null)
        //    {
        //        if (CurrentSubTreeNode == CurrentTreeNode)
        //        {
        //            if (node.IsAncestorOf(CurrentTreeNode))
        //            {
        //                subTreeNode = CurrentTreeNode;
        //            }
        //        }
        //        else
        //        {
        //            if (node.IsAncestorOf(CurrentSubTreeNode))
        //            {
        //                subTreeNode = CurrentSubTreeNode;
        //            }
        //            else if (node.IsDescendantOf(CurrentTreeNode))
        //            {
        //                subTreeNode = node;
        //            }
        //        }
        //    }

        //    if (subTreeNode == node)
        //    {
        //        CurrentTreeNode = node;
        //        CurrentSubTreeNode = CurrentTreeNode;

        //        updateBreadcrumbPanel(node);
        //    }
        //    else
        //    {
        //        CurrentTreeNode = node;
        //        CurrentSubTreeNode = CurrentTreeNode;

        //        updateBreadcrumbPanel(node);

        //        if (subTreeNode != null)
        //        {
        //            m_Logger.Log("-- PublishEvent [SubTreeNodeSelectedEvent] StructureTrailPaneView.OnTreeNodeSelected",
        //                       Category.Debug, Priority.Medium);

        //            m_EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(subTreeNode);
        //        }
        //    }
        //}
    }
}
