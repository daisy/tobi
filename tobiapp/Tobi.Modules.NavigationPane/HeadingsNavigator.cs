using System.Collections.ObjectModel;
using Tobi.Common;
using urakawa;
using urakawa.core;
using urakawa.navigation;
using urakawa.xuk;

namespace Tobi.Plugin.NavigationPane
{

    public class HeadingsNavigator : AbstractFilterNavigator
    {
        private string m_searchString = string.Empty;
        public HeadingsNavigator(Project project)
        {
            m_Project = project;
        }
        public HeadingsNavigator(Project project, HeadingPanelView view)
        {
            m_Project = project;
            m_View = view;
        }
        private ObservableCollection<HeadingTreeNodeWrapper> m_roots;
        private readonly Project m_Project;
        public readonly HeadingPanelView m_View;

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

        public void FindNext()
        {
            HeadingTreeNodeWrapper nextMatch = FindNextMatch(m_roots);
            if (nextMatch != null)
            {
                //nextMatch.IsSelected = true;
                m_View.SelectTreeNodeWrapper(nextMatch);
            }
            else
            {
                AudioCues.PlayAsterisk();
            }
        }
        public void FindPrevious()
        {
            HeadingTreeNodeWrapper prevMatch = FindPrevMatch(m_roots);
            if (prevMatch != null)
            {
                //prevMatch.IsSelected = true;
                m_View.SelectTreeNodeWrapper(prevMatch);
            }
            else
            {
                AudioCues.PlayAsterisk();
            }
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
                    break;
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
                    break;
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
                   || localXmlName == "levelhd";
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
                SearchNodes(m_roots, m_searchString);
            }
        }
        internal static void SearchNodes(ObservableCollection<HeadingTreeNodeWrapper> nodes, string searchTerm)
        {
            foreach (HeadingTreeNodeWrapper node in nodes)
            {
                node.SearchMatch = !string.IsNullOrEmpty(searchTerm) &&
                   !string.IsNullOrEmpty(node.Title) &&
                   node.Title.ToLower().Contains(searchTerm.ToLower());
                if (node.Children != null && node.Children.Count > 0) { SearchNodes(node.Children, searchTerm); }
            }
        }
    }
}
