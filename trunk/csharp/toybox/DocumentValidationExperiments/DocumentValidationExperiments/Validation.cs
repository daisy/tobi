using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using System.Collections;
using System.Xml.Schema;

namespace DocumentValidationExperiments
{

    public class DTDValidation
    {
        private int m_Errors;
        public DTDValidation()
        {
            m_Errors = 0;
        }
        public void Open(string filename)
        {
            m_Errors = 0;
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ProhibitDtd = false;
            settings.ValidationType = ValidationType.DTD;
            settings.ValidationEventHandler += new System.Xml.Schema.ValidationEventHandler(validationEventHandler);

            XmlReader xmlReader = XmlReader.Create(filename, settings);

            Console.WriteLine(string.Format("File: {0}\nReading...", filename));
            try
            {
                while (xmlReader.Read())
                {
                }
            }
            catch (Exception e)
            {
                //some types of errors cause an exception instead of a validation event
                //e.g. <element attr="no closing quote ..../>
                Console.WriteLine(string.Format("Exception!\n\t{0}\n", e.Message));
                m_Errors++;
            }



        }

        void validationEventHandler(object sender, System.Xml.Schema.ValidationEventArgs e)
        {
            m_Errors++;

            Console.WriteLine(string.Format("Validation event:\n\t{0}, {1}\n", e.Message, e.Exception.LineNumber));
        }

        public int GetErrorCount()
        {
            return m_Errors;
        }
    }

    
    public class SchemaValidation
    {
        private List<XmlSchemaValidationException> m_Errors;
        public List<XmlSchemaValidationException> Errors { get { return m_Errors; } }

        public SchemaValidation()
        {
            m_Errors = null;
        }
        public void Validate(XmlDocument doc, string schema)
        {
            m_Errors = new List<XmlSchemaValidationException>();
            doc.Schemas.Add("", schema);
            doc.Validate(validationEventHandler);
        }

        void validationEventHandler(object sender, System.Xml.Schema.ValidationEventArgs e)
        {
            XmlSchemaValidationException ex = (XmlSchemaValidationException)e.Exception;
            m_Errors.Add(ex);
        }
    }

    /// <summary>
    /// create and maintain a DOM representation of the tree
    /// </summary>
    public class DomHelper
    {
        private XmlDocument m_Document;
        public XmlDocument Document { get { return m_Document; } }
        private Tree m_Tree;
        //using two hashmaps is not the most efficient but it's easiest for this experiment
        //XmlElement is the key and TreeNode is the value
        private Hashtable m_HashXml;
        //TreeNode is the key and XmlElement is the value
        private Hashtable m_HashTreeNode;

        public DomHelper()
        {
            m_Document = null;
        }

        public void Init(Tree tree)
        {
            m_Tree = tree;
            //leaking some memory with the events
            tree.NodeAdded += new EventHandler<NodeEventArgs>(TreeNodeAdded);
            m_Document = new XmlDocument();
            m_HashXml = new Hashtable();
            m_HashTreeNode = new Hashtable();
            ProcessTree();
        }

        void ProcessTree()
        {
            XmlElement elm = CreateDomNode(m_Tree.Root);
            m_Document.AppendChild(elm);
            ProcessSubtree(m_Tree.Root, elm);
        }

        void ProcessSubtree(TreeNode node, XmlElement nodeAsXml)
        {
            foreach (TreeNode n in node.Children)
            {
                XmlElement elm = CreateDomNode(n);
                nodeAsXml.AppendChild(elm);
                ProcessSubtree(n, elm);

            }
        }
        void TreeNodeAdded(object sender, NodeEventArgs e)
        {
            //look for its parent in the DOM-Node hash
            if (e.Node.Parent == null)
            {
                throw new Exception("Trying to add the ROOT!  This won't work in this test.");
            }
            XmlElement parent = (XmlElement)m_HashTreeNode[e.Node.Parent];
            XmlElement node = CreateDomNode(e.Node);
            parent.AppendChild(node);
        }

        XmlElement CreateDomNode(TreeNode node)
        {
            XmlElement elm = m_Document.CreateElement(node.XmlProperty);
            elm.SetAttribute("id", node.Id);
            m_HashXml.Add(elm, node);
            m_HashTreeNode.Add(node, elm);
            return elm;
        }


        internal TreeNode findTreeNode(XmlElement elm)
        {
            return (TreeNode) m_HashXml[elm];
        }
    }
}