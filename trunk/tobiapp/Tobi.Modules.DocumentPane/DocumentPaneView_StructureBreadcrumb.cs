using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Practices.Composite.Logging;
using Tobi.Infrastructure;
using urakawa.core;
using urakawa.xuk;

namespace Tobi.Modules.DocumentPane
{
    public partial class DocumentPaneView
    {
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

                Button butt = new Button
                {
                    Tag = n,
                    Style=null,
                    BorderThickness = new Thickness(0.0),
                    BorderBrush = null,
                    Background = Brushes.Transparent,
                    Foreground = Brushes.Blue,
                    Cursor = Cursors.Hand
                };

                Run run = new Run((qname != null ? qname.LocalName : "TEXT")) { TextDecorations = TextDecorations.Underline };
                butt.Content = run;

                butt.Click += OnBreadCrumbButtonClick;

                BreadcrumbPanel.Children.Add(butt);

                if (counter < PathToCurrentTreeNode.Count - 1)
                {
                    Run run2 = new Run(">");

                    Button tb = new Button
                    {
                        Content = run2,
                        Tag = n,
                        BorderBrush = null,
                        BorderThickness = new Thickness(0.0),
                        Background = Brushes.Transparent,
                        Foreground = Brushes.Black,
                        Cursor = Cursors.Cross,
                        FontWeight = FontWeights.ExtraBold
                    };

                    tb.Click += OnBreadCrumbSeparatorClick;

                    BreadcrumbPanel.Children.Add(tb);
                }

                if (n == node)
                {
                    run.FontWeight = FontWeights.Heavy;
                }

                counter++;
            }
        }

        private void OnBreadCrumbSeparatorClick(object sender, RoutedEventArgs e)
        {
            Button ui = sender as Button;
            if (ui == null)
            {
                return;
            }

            TreeNode node = (TreeNode)ui.Tag;

            Popup popup = new Popup();
            popup.IsOpen = false;
            popup.Width = 120;
            popup.Height = 150;
            BreadcrumbPanel.Children.Add(popup);
            popup.PlacementTarget = ui;
            popup.LostFocus += OnPopupLostFocus;
            popup.LostMouseCapture += OnPopupLostFocus;
            popup.LostKeyboardFocus += OnPopupLostKeyboardFocus;

            ListView listOfNodes = new ListView();
            listOfNodes.SelectionChanged += OnListOfNodesSelectionChanged;

            foreach (TreeNode child in node.ListOfChildren)
            {
                listOfNodes.Items.Add(new TreeNodeWrapper()
                {
                    Popup = popup,
                    TreeNode = child
                });
            }

            ScrollViewer scroll = new ScrollViewer();
            scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

            scroll.Content = listOfNodes;
            popup.Child = scroll;
            popup.IsOpen = true;
            popup.Focus();
        }

        private void OnPopupLostFocus(object sender, RoutedEventArgs e)
        {
            Popup ui = sender as Popup;
            if (ui == null)
            {
                return;
            }
            ui.IsOpen = false;
        }

        private void OnPopupLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Popup ui = sender as Popup;
            if (ui == null)
            {
                return;
            }
            ui.IsOpen = false;
        }

        private void OnListOfNodesSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView ui = sender as ListView;
            if (ui == null)
            {
                return;
            }
            TreeNodeWrapper wrapper = (TreeNodeWrapper)ui.SelectedItem;
            wrapper.Popup.IsOpen = false;

            Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] DocumentPaneView.OnListOfNodesSelectionChanged", Category.Debug, Priority.Medium);

            EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(wrapper.TreeNode);
        }

        private void OnBreadCrumbButtonClick(object sender, RoutedEventArgs e)
        {
            Button ui = sender as Button;
            if (ui == null)
            {
                return;
            }

            Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] DocumentPaneView.OnBreadCrumbButtonClick", Category.Debug, Priority.Medium);

            EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish((TreeNode)ui.Tag);
        }

    }
}
