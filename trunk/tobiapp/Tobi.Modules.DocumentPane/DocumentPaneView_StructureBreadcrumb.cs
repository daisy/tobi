using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Practices.Composite.Logging;
using Tobi.Infrastructure;
using urakawa.core;
using urakawa.media.data.audio;
using urakawa.xuk;

namespace Tobi.Modules.DocumentPane
{
    public partial class DocumentPaneView
    {
        Style m_ButtonStyle = (Style)Application.Current.FindResource("ToolBarButtonBaseStyle");

        private List<TreeNode> PathToCurrentTreeNode;

        private void updateBreadcrumbPanel(TreeNode node)
        {

            BreadcrumbPanel.Children.Clear();

            if (PathToCurrentTreeNode == null || !PathToCurrentTreeNode.Contains(node))
            {
                PathToCurrentTreeNode = new List<TreeNode>();
                TreeNode treeNode = node;
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

                var seqMedia = n.GetAudioSequenceMedia();
                bool withMedia = n.GetManagedAudioMedia() != null
                    || (seqMedia != null && !seqMedia.AllowMultipleTypes
                        && seqMedia.Count > 0 && seqMedia.GetItem(0) is ManagedAudioMedia);

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

                var run = new Run((qname != null ? qname.LocalName : "TEXT")) { TextDecorations = TextDecorations.Underline };
                butt.Content = run;

                butt.Click += OnBreadCrumbButtonClick;

                BreadcrumbPanel.Children.Add(butt);

                if (counter < PathToCurrentTreeNode.Count - 1)
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
                        Style = m_ButtonStyle
                    };

                    tb.Click += OnBreadCrumbSeparatorClick;

                    BreadcrumbPanel.Children.Add(tb);
                }

                if (CurrentTreeNode == CurrentSubTreeNode)
                {
                    if (n == node)
                    {
                        run.FontWeight = FontWeights.Heavy;
                    }
                }
                else
                {
                    if (n == CurrentTreeNode)
                    {
                        run.FontWeight = FontWeights.Heavy;
                    }
                }

                counter++;
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

            var popup = new Popup {IsOpen = false, Width = 120, Height = 150};
            BreadcrumbPanel.Children.Add(popup);
            popup.PlacementTarget = ui;
            popup.LostFocus += OnPopupLostFocus;
            popup.LostMouseCapture += OnPopupLostFocus;
            popup.LostKeyboardFocus += OnPopupLostKeyboardFocus;

            var listOfNodes = new ListView();
            listOfNodes.SelectionChanged += OnListOfNodesSelectionChanged;

            foreach (TreeNode child in node.ListOfChildren)
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
            popup.Focus();
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

            Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] DocumentPaneView.OnListOfNodesSelectionChanged", Category.Debug, Priority.Medium);

            EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(wrapper.TreeNode);
        }

        private void OnBreadCrumbButtonClick(object sender, RoutedEventArgs e)
        {
            var ui = sender as Button;
            if (ui == null)
            {
                return;
            }

            Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] DocumentPaneView.OnBreadCrumbButtonClick", Category.Debug, Priority.Medium);

            EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish((TreeNode)ui.Tag);
        }

    }
}
