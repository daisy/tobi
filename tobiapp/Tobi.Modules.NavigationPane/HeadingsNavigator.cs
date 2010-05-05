using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Tobi.Common;
using Tobi.Common.UI;
using urakawa;
using urakawa.core;
using urakawa.navigation;
using urakawa.xuk;

namespace Tobi.Plugin.NavigationPane
{

    public class HeadingsNavigator : AbstractFilterNavigator
    {
        private readonly Project m_Project;
        public HeadingPaneViewModel ViewModel { get; private set; }

        public HeadingsNavigator(Project project, HeadingPaneViewModel viewModel)
        {
            m_Project = project;
            ViewModel = viewModel;
        }

        private string m_searchString = string.Empty;

        private ObservableCollection<HeadingTreeNodeWrapper> m_roots;

        //public void ExpandAllThreaded()
        //{
        //    ThreadPool.SetMaxThreads(50, 50);
        //    foreach (HeadingTreeNodeWrapper node in Roots)
        //    {
        //        ThreadPool.QueueUserWorkItem(ExpandNodeCallback, node);
        //    }
        //}

        //private static void ExpandNodeCallback(object nodeObject)
        //{
        //    HeadingTreeNodeWrapper node = (HeadingTreeNodeWrapper)nodeObject;
        //    node.IsExpanded = true;
        //    if (!node.HasChildren) { return; }
        //    foreach (HeadingTreeNodeWrapper child in node.Children) { ThreadPool.QueueUserWorkItem(ExpandNodeCallback, child); }
        //}


        public void CollapseAll()
        {
            foreach (HeadingTreeNodeWrapper node in Roots)
            {
                Collapse(node);
            }
        }

        public static void CollapseAll(HeadingTreeNodeWrapper node)
        {
            Collapse(node);
            if (!node.HasChildren) { return; }
            foreach (HeadingTreeNodeWrapper child in node.Children)
            {
                CollapseAll(child);
            }
        }

        public static void Collapse(HeadingTreeNodeWrapper node)
        {
            node.IsExpanded = false;
        }

        public void ExpandAll()
        {
            foreach (HeadingTreeNodeWrapper node in Roots)
            {
                ExpandAll(node);
            }
        }

        private static void ExpandAll(HeadingTreeNodeWrapper node)
        {
            node.IsExpanded = true;
            if (!node.HasChildren)
            {
                return;
            }
            foreach (HeadingTreeNodeWrapper child in node.Children)
            {
                ExpandAll(child);
            }
        }

        public static void Expand(HeadingTreeNodeWrapper node)
        {
            node.IsExpanded = true;
        }

        public HeadingTreeNodeWrapper FindNext(bool select)
        {
            ExpandAll();

            HeadingTreeNodeWrapper nextMatch = FindNextMatch(m_roots);
            if (nextMatch != null)
            {
                if (select)
                {
                    //nextMatch.IsSelected = true;
                    ViewModel.View.SelectTreeNodeWrapper(nextMatch, true);
                }
                else
                {
                    var treeViewItem = VisualLogicalTreeWalkHelper.FindObjectInVisualTreeWithMatchingType<TreeViewItem>(
                        ViewModel.View.TreeView,
                        child =>
                        {
                            object dc = child.GetValue(FrameworkElement.DataContextProperty);
                            return dc != null && dc == nextMatch;
                        });
                    if (treeViewItem != null)
                    {
                        treeViewItem.BringIntoView();
                    }
                }
            }
            else
            {
                AudioCues.PlayBeep();
            }

            return nextMatch;
        }
        public HeadingTreeNodeWrapper FindPrevious(bool select)
        {
            ExpandAll();

            HeadingTreeNodeWrapper prevMatch = FindPrevMatch(m_roots);
            if (prevMatch != null)
            {
                if (select)
                {
                    //prevMatch.IsSelected = true;
                    ViewModel.View.SelectTreeNodeWrapper(prevMatch, true);
                }
                else
                {
                    var treeViewItem = VisualLogicalTreeWalkHelper.FindObjectInVisualTreeWithMatchingType<TreeViewItem>(
                        ViewModel.View.TreeView,
                        child =>
                        {
                            object dc = child.GetValue(FrameworkElement.DataContextProperty);
                            return dc != null && dc == prevMatch;
                        });
                    if (treeViewItem != null)
                    {
                        treeViewItem.BringIntoView();
                    }
                }
            }
            else
            {
                AudioCues.PlayBeep();
            }

            return prevMatch;
        }

        private static HeadingTreeNodeWrapper FindNextMatch(ObservableCollection<HeadingTreeNodeWrapper> nodes)
        {
            HeadingTreeNodeWrapper htnwResult = null;
            int iStarting = -1;
            for (int i = 0; i < nodes.Count; i++)
            {
                if ((nodes[i].HasMatches || nodes[i].SearchMatch) && iStarting == -1)
                {
                    iStarting = i;
                }
                if (!nodes[i].IsSelected && !nodes[i].ChildSelected)
                {
                    continue;
                }
                iStarting = i;
                break;
            }
            if (iStarting < 0)
            {
                return null;
            }
            if (nodes[iStarting].IsSelected)
            {
                if (nodes[iStarting].HasChildren && nodes[iStarting].HasMatches)
                {
                    htnwResult = nodes[iStarting].GetChildMatch();
                }
            }
            else if (nodes[iStarting].ChildSelected)
            {
                htnwResult = FindNextMatch(nodes[iStarting].Children);
            }
            else if (nodes[iStarting].SearchMatch)
            {
                htnwResult = nodes[iStarting];
            }
            else
            {
                htnwResult = nodes[iStarting].GetChildMatch();
            }
            if (htnwResult == null)
            {
                for (int i = iStarting + 1; i < nodes.Count; i++)
                {
                    if (nodes[i].SearchMatch)
                    {
                        htnwResult = nodes[i];
                        break;
                    }
                    if (!nodes[i].HasMatches)
                    {
                        continue;
                    }
                    htnwResult = nodes[i].GetChildMatch();
                    break;
                }
            }
            return htnwResult;
        }
        private static HeadingTreeNodeWrapper FindPrevMatch(ObservableCollection<HeadingTreeNodeWrapper> nodes)
        {
            HeadingTreeNodeWrapper htnwResult = null;
            int iStarting = -1;
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                if ((nodes[i].HasMatches || nodes[i].SearchMatch) && iStarting == -1)
                {
                    iStarting = i;
                }
                if (!nodes[i].IsSelected && !nodes[i].ChildSelected)
                {
                    continue;
                }
                iStarting = i;
                break;
            }
            if (iStarting < 0)
            {
                return null;
            }
            if (nodes[iStarting].ChildSelected)
            {
                htnwResult = FindPrevMatch(nodes[iStarting].Children);
                if (htnwResult == null && nodes[iStarting].SearchMatch)
                {
                    htnwResult = nodes[iStarting];
                }
            }
            else if (nodes[iStarting].SearchMatch)
            {
                htnwResult = nodes[iStarting];
            }
            else if (nodes[iStarting].HasMatches)
            {
                htnwResult = nodes[iStarting].GetPreviousChildMatch();
            }
            if (htnwResult == null || nodes[iStarting].IsSelected)
            {
                htnwResult = null;
                for (int i = iStarting - 1; i >= 0; i--)
                {
                    if (nodes[i].HasMatches)
                    {
                        htnwResult = nodes[i].GetPreviousChildMatch();
                        break;
                    }
                    if (!nodes[i].SearchMatch)
                    {
                        continue;
                    }
                    htnwResult = nodes[i];
                    break;
                }
            }
            return htnwResult;
        }


        //public void EditText(HeadingTreeNodeWrapper parameter)
        //{
        //    if (parameter == null) { return; }
        //    string sTitle = parameter.Title;
        //    if (sTitle.StartsWith("[")) { sTitle = sTitle.Substring(sTitle.IndexOf("] ") + 1); }
        //    InputBox myTest = new InputBox(UserInterfaceStrings.HeadingEdit_, sTitle.Trim());
        //    var windowPopup = new PopupModalWindow(m_ShellView,
        //                                           UserInterfaceStrings.EscapeMnemonic(
        //                                           UserInterfaceStrings.HeadingEdit),
        //                                           myTest,
        //                                           PopupModalWindow.DialogButtonsSet.OkCancel,
        //                                           PopupModalWindow.DialogButton.Ok,
        //                                           false, 350, 175);
        //    windowPopup.ShowDialog();
        //    if (windowPopup.ClickedDialogButton != PopupModalWindow.DialogButton.Ok) { return; }
        //    parameter.Title = myTest.tbInput.Text;
        //}

        public ObservableCollection<HeadingTreeNodeWrapper> Roots
        {
            get
            {
                if (m_roots == null)
                {
                    m_roots = new ObservableCollection<HeadingTreeNodeWrapper>();
                    TreeNode presentationRootNode = m_Project.Presentations.Get(0).RootNode;
                    int n = GetChildCount(presentationRootNode);
                    for (int index = 0; index < n; index++)
                    {
                        TreeNode node = GetChild(presentationRootNode, index);
                        m_roots.Add(new HeadingTreeNodeWrapper(this, node, null));
                    }
                }
                return m_roots;
            }
        }

        public static bool IsLevel(string localXmlName)
        {
            return localXmlName == "level1"
                   || localXmlName == "level2"
                   || localXmlName == "level3"
                   || localXmlName == "level4"
                   || localXmlName == "level5"
                   || localXmlName == "level6"
                   || localXmlName == "level";
        }

        public static bool IsHeading(string localXmlName)
        {
            return localXmlName == "h1"
                   || localXmlName == "h2"
                   || localXmlName == "h3"
                   || localXmlName == "h4"
                   || localXmlName == "h5"
                   || localXmlName == "h6"
                   || localXmlName == "hd"
                   || localXmlName == "levelhd"
                   || localXmlName == "doctitle";
        }

        public override bool IsIncluded(TreeNode node)
        {
            QualifiedName qname = node.GetXmlElementQName();
            return qname != null && (IsLevel(qname.LocalName) || IsHeading(qname.LocalName));
        }

        private HeadingTreeNodeWrapper findTreeNodeWrapper(TreeNode node)
        {
            foreach (HeadingTreeNodeWrapper root in Roots)
            {
                if (root.WrappedTreeNode_Level == node || root.WrappedTreeNode_LevelHeading == node)
                {
                    return root;
                }
                HeadingTreeNodeWrapper wrapperChild = root.FindTreeNodeWrapper(node);
                if (wrapperChild != null)
                {
                    return wrapperChild;
                }
            }
            return null;
        }

        public HeadingTreeNodeWrapper GetAncestorContainer(TreeNode node)
        {
            if (IsIncluded(node))
            {
                return findTreeNodeWrapper(node);
            }
            if (node.Parent != null)
            {
                return GetAncestorContainer(node.Parent);
            }
            return null;
        }

        public string SearchTerm
        {
            get { return m_searchString; }
            set
            {
                if (m_searchString == value) { return; }
                m_searchString = value;
                FlagSearchMatches();
            }
        }

        internal void FlagSearchMatches()
        {
            ExpandAll();

            bool atLeastOneFound = FlagSearchMatches(m_roots);

            if (atLeastOneFound)
            {
                HeadingTreeNodeWrapper nw = FindNext(false);
                if (nw == null)
                {
                    nw = FindPrevious(false);
                }
            }
        }

        private bool FlagSearchMatches(ObservableCollection<HeadingTreeNodeWrapper> nodes)
        {
            if (string.IsNullOrEmpty(SearchTerm))
            {
                foreach (HeadingTreeNodeWrapper node in nodes)
                {
                    node.SearchMatch = false;
                    if (node.Children != null && node.Children.Count > 0)
                    {
                        FlagSearchMatches(node.Children);
                    }
                }
                return false;
            }

            bool atLeastOneFound = false;

            foreach (HeadingTreeNodeWrapper node in nodes)
            {
                bool found = !string.IsNullOrEmpty(node.Title) &&
                             node.Title.ToLower().Contains(SearchTerm.ToLower());
                node.SearchMatch = found;
                if (found)
                {
                    atLeastOneFound = true;
                }

                if (node.Children != null && node.Children.Count > 0)
                {
                    found = FlagSearchMatches(node.Children);
                    if (found)
                    {
                        atLeastOneFound = true;
                    }
                }
            }

            return atLeastOneFound;
        }
    }
}
