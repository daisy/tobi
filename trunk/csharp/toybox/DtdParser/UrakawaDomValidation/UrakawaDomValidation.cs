using System;
using System.Collections.Generic;
using System.IO;
using urakawa;
using urakawa.core;
using urakawa.daisy.import;
using urakawa.xuk;
using DtdParser;

namespace UrakawaDomValidation
{
    public class TestValidation
    {
        public static void Main(string[] args)
        {
            UrakawaDomValidation validator = new UrakawaDomValidation();
            string dtd = @"..\..\..\dtbook-2005-3.dtd";
            string simplebook = @"..\..\..\simplebook.xuk";
            string paintersbook = @"..\..\..\greatpainters.xuk";
            validator.AssignDtd(dtd);
            validator.LoadXuk(simplebook);
            bool res = validator.Validate();

            if (res) Console.WriteLine("VALID!!");
            else Console.WriteLine("INVALID!!");

            Console.WriteLine("Press any key to continue");
            Console.ReadKey(); 
        }
    }
    public class UrakawaDomValidation
    {
        private Project m_Project;
        private DTD m_Dtd;
        
        public bool Validate()
        {
            TreeNode root = m_Project.Presentations.Get(0).RootNode;
            return ValidateTree(root);
        }

        //untested so far
        public void ImportDtbook(string file)
        {
            var converter = new Daisy3_Import(file);
            m_Project = converter.Project;
        }

        public void LoadXuk(string file)
        {
            string fullpath = Path.GetFullPath(file);
            m_Project = new Project();
            var uri = new Uri(fullpath);
            var action = new OpenXukAction(m_Project, uri);
            action.Execute();
        }

        public void AssignDtd(string dtd)
        {
            StreamReader reader = new StreamReader(dtd);
            AssignDtd(reader);
        }
        public void AssignDtd(StreamReader dtd)
        {
            DTDParser parser = new DTDParser(dtd);
            m_Dtd = parser.Parse(true);
        }

        //**********************************************
        //All the Validate* functions are going to be rewritten... 
        //******************************************

        //entry point into entire tree validation
        private bool ValidateTree(TreeNode root)
        {
            //this check doesn't work w. urakawa trees, because they represent everything from
            //<book> onwards; not <dtbook> onwards.
            //check for root-ness
            /*DTDElement dtdRoot = m_Dtd.RootElement;
            if (root.GetXmlElementQName().LocalName != dtdRoot.Name)
                return false;
             */
            return ValidateNode(root);
        }
        
        /// <summary>
        /// a node is valid if its content model conforms to that in the DTD
        /// and if its children are all valid
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool ValidateNode(TreeNode node)
        {
            string nodeName = node.GetXmlElementQName().LocalName;
            DTDElement dtdElement = (DTDElement)m_Dtd.Elements[nodeName];
            //first check if this node's content is valid
            bool isNodeContentValid = ValidateNodeListGeneric(node.Children, dtdElement.Content);

            bool areAllChildrenValid = true;
            //then find out if all its children are valid
            foreach(TreeNode child in node.Children.ContentsAs_ListAsReadOnly)
            {
                areAllChildrenValid = areAllChildrenValid & ValidateNode(child);
            }

            return isNodeContentValid & areAllChildrenValid;
        }
        //validate a single node against 
        private bool ValidateNodeGeneric(TreeNode node, DTDItem dtdContent)
        {
            
        }
        //check the node's content model (generically typed as DTDItem)
        private bool ValidateNodeListGeneric(List<TreeNode> nodes, DTDItem dtdContent)
        {
            bool isNodeCardinalityValid = false;
            bool isNodeContentValid = false;

            //check the content model
            if (dtdContent is DTDAny)
            {
                isNodeContentValid = ValidateNodeList(nodes, (DTDAny)dtdContent);
            }
            else if (dtdContent is DTDEmpty)
            {
                isNodeContentValid = ValidateNodeList(nodes, (DTDEmpty)dtdContent);
            }
            else if (dtdContent is DTDName)
            {
                isNodeContentValid = ValidateNodeList(nodes, (DTDName) dtdContent);
            }
            else if (dtdContent is DTDChoice)
            {
                isNodeContentValid = ValidateNodeList(nodes, (DTDChoice)dtdContent);
            }
            else if (dtdContent is DTDSequence)
            {
                isNodeContentValid = ValidateNodeList(nodes, (DTDSequence)dtdContent);
            }
            else if (dtdContent is DTDMixed)
            {
                isNodeContentValid = ValidateNodeList(nodes, (DTDMixed)dtdContent);
            }
            else if (dtdContent is DTDPCData)
            {
                isNodeContentValid = ValidateNodeList(nodes, (DTDPCData)dtdContent);
            }

            //just for now ... 
            return false;
        }

        // (A | B | C)
        private bool ValidateNodeList(List<TreeNode> nodes, DTDChoice dtdChoice)
        {
            bool matches = false;
            //see if the content model matches any of the items to choose from
            foreach (DTDItem item in dtdChoice.Items)
            {
                if (ValidateNodeListGeneric(nodes, item))
                    matches = true;
            }
            return matches;
        }

        //do the children follow the sequence A, B, C?
        private bool ValidateNodeList(List<TreeNode> nodes, DTDSequence dtdSequence)
        {
            bool matches = true;
            int i = 0;
            //see if the content model matches any of the items to choose from
            foreach (DTDItem item in dtdSequence.Items)
            {
                matches = matches & ValidateNodeListGeneric(nodes[i], item);
            }
            return matches;
            
        }
        private bool ValidateNodeList(List<TreeNode> nodes, DTDMixed dtdMixed)
        {
            //treat DTDMixed like DTDChoice
            DTDChoice dtdChoice = new DTDChoice();
            dtdChoice.Items.AddRange(dtdMixed.Items);
            dtdChoice.Cardinal = dtdMixed.Cardinal;
            return ValidateNodeList(nodes, dtdChoice);
        }
        private bool ValidateNodeList(List<TreeNode> nodes, DTDPCData dtdPcData)
        {
            return nodes == null || nodes.Count == 0;
        }
        private bool ValidateNodeList(List<TreeNode> nodes, DTDEmpty dtdEmpty)
        {
            return nodes == null || nodes.Count <= 0;
        }
        private bool ValidateNodeList(List<TreeNode> nodes, DTDAny dtdAny)
        {
            return true;
        }

        //do any of the nodes in the list match the name?
        //todo: will we ever need to know if all the nodes match the name?
        private bool ValidateNodeList(List<TreeNode> nodes, DTDName dtdName)
        {
            return nodes.Exists(s => ValidateNode(s, dtdName));
        }
        //does the node match the name?
        private bool ValidateNode(TreeNode node, DTDName dtdName)
        {
            return node.GetXmlElementQName().LocalName == dtdName.Value;
        }

        //**********************************************
        //End of "All the Validate* functions are going to be rewritten... "
        //******************************************

    }
}
