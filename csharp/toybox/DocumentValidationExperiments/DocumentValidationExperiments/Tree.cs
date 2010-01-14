using System;
using System.Collections.Generic;
using System.Xml;

namespace DocumentValidationExperiments
{
    /// <summary>
    /// A very simple tree
    /// </summary>
    public class Tree
    {
        //all node added events for the tree
        public event EventHandler<NodeEventArgs> NodeAdded;
        private TreeNode m_Root;
        public TreeNode Root 
        { 
            get { return m_Root;} 
            set
            {
                m_Root = value;
                if (NodeAdded != null) NodeAdded(this, new NodeEventArgs(m_Root));
            }
        }

        public Tree()
        {
            m_Root = null;
        }
        public void Add (TreeNode node, TreeNode parent)
        {
            parent.AddChild(node);
            if (NodeAdded != null) NodeAdded(this, new NodeEventArgs(node));
        }


    }
    /// <summary>
    /// Kind of like an Urakawa tree node
    /// </summary>
    public class TreeNode
    {
        private List<TreeNode> m_Children;
        public List<TreeNode> Children
        {
            get { return m_Children; }
        }

        public string Id { get; set; }
        public string XmlProperty { get; set; }
        public TreeNode Parent { get; set; }

        public TreeNode()
        {
            Id = "None";
            XmlProperty = "None";
            Parent = null;
            m_Children = new List<TreeNode>();
        }

        //do not call directly!
        public void AddChild(TreeNode node)
        {
            node.Parent = this;
            m_Children.Add(node);
        }
        public void Print(int indentations)
        {
            string tabs = "";
            for (int i = 0; i < indentations; i++)
            {
                tabs += "\t";
            }
            Console.WriteLine(string.Format("{0}{1}", tabs, Id));

            foreach (TreeNode node in Children)
            {
                node.Print(indentations + 1);
            }
        }
    }

    public class NodeEventArgs : EventArgs
    {
        public readonly TreeNode Node;

        public NodeEventArgs(TreeNode child)
        {
            Node = child;
        }
    }

    public static class TreeLoader
    {
        public static Tree LoadXml(string file)
        {
            Tree tree = new Tree();
            XmlTextReader reader = new XmlTextReader(file);

            TreeNode temp = null;
            
            while (reader.Read())
            {
                if (reader.IsStartElement())
                {
                    switch (reader.Name)
                    {
                        case "doc":
                            {
                                TreeNode node = CreateTreeNode(reader);
                                tree.Root = node;
                            }
                            break;
                        case "chapter":
                            {
                                TreeNode node = CreateTreeNode(reader);
                                tree.Add(node, tree.Root);
                                temp = node;
                            }
                            break;
                        case "p":
                            {
                                TreeNode node = CreateTreeNode(reader);
                                tree.Add(node, temp);
                            }
                            break;
                    }
                }
            }
            return tree;
        }

        private static TreeNode CreateTreeNode(XmlTextReader reader)
        {
            TreeNode node = new TreeNode();
            node.Id = reader.GetAttribute("id");
            node.XmlProperty = reader.Name;
            return node;
        }
    }
    
}