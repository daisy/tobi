using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using DtdSharp;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.Validation;
using urakawa.core;
using urakawa.ExternalFiles;

#if USE_ISOLATED_STORAGE
using System.IO.IsolatedStorage;
#endif //USE_ISOLATED_STORAGE

namespace Tobi.Plugin.Validator.ContentDocument
{
    /// <summary>
    /// The main validator class
    /// </summary>
    [Export(typeof(IValidator)), PartCreationPolicy(CreationPolicy.Shared)]
    public class ContentDocumentValidator : AbstractValidator, IPartImportsSatisfiedNotification
    {
#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied()
#pragma warning restore 1591
        {
            //#if DEBUG
            //            Debugger.Break();
            //#endif
        }

        private readonly ILoggerFacade m_Logger;
        protected readonly IUrakawaSession m_Session;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        ///<param name="session">normally obtained from the MEF composition container, it's a Tobi-specific service</param>
        [ImportingConstructor]
        public ContentDocumentValidator(
            ILoggerFacade logger,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = false, AllowDefault = false)]
            IUrakawaSession session)
        {
            m_Logger = logger;
            m_Session = session;
            m_ValidationItems = new List<ValidationItem>();
            m_DtdRegex = new DtdSharpToRegex();

            m_Logger.Log(@"ContentDocumentValidator initialized", Category.Debug, Priority.Medium);
        }

        public override string Name
        {
            get { return Tobi_Plugin_Validator_ContentDocument_Lang.ContentDocumentValidator_Name; }       // TODO LOCALIZE ContentDocumentValidator_Name
        }

        public override string Description
        {
            get { return Tobi_Plugin_Validator_ContentDocument_Lang.ContentDocumentValidator_Description; }     // TODO LOCALIZE ContentDocumentValidator_Description
        }

        public override bool ShouldRunOnlyOnce
        {
            get { return true; }
        }

        public override IEnumerable<ValidationItem> ValidationItems
        {
            get { return m_ValidationItems; }
        }

        private List<ValidationItem> m_ValidationItems;

        public override bool Validate()
        {
            if (m_DtdRegex.DtdRegexTable == null || m_DtdRegex.DtdRegexTable.Count == 0)
                UseDtd(DTDs.DTDs.DTBOOK_2005_3);

            if (m_Session.DocumentProject != null &&
                m_Session.DocumentProject.Presentations.Count > 0)
            {
                m_ValidationItems = new List<ValidationItem>();

                if (m_DtdRegex == null || m_DtdRegex.DtdRegexTable == null ||
                    m_DtdRegex.DtdRegexTable.Count == 0)
                {
                    ContentDocumentValidationError error = new ContentDocumentValidationError
                                                               {
                                                                   ErrorType = ContentDocumentErrorType.MissingDtd,
                                                                   DtdIdentifier = m_DtdIdentifier
                                                               };
                    m_ValidationItems.Add(error);
                    IsValid = false;
                }
                else
                {
                    IsValid = ValidateNode(m_Session.DocumentProject.Presentations.Get(0).RootNode);
                }
            }
            return IsValid;
        }

        private DTD m_Dtd;
        private DtdSharpToRegex m_DtdRegex;
        private string m_DtdIdentifier;

        private const string m_DtdStoreDirName = "Cached-DTDs";

        public void UseDtd(string dtdIdentifier)
        {
            m_DtdIdentifier = dtdIdentifier;
            string dtdCache = dtdIdentifier + ".cache";

            //check to see if we have a cached version of this file
#if USE_ISOLATED_STORAGE
            Stream stream = null;
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                string[] filenames = store.GetFileNames(dtdCache);
                if (filenames.Length > 0)
                {
                    stream = new IsolatedStorageFileStream(dtdCache, FileMode.Open, FileAccess.Read, FileShare.None, store);
                }
            }
            if (stream != null)
            {
                try
                {
                    m_DtdRegex.ReadFromCache(new StreamReader(stream));
                }
                finally
                {
                    stream.Close();
                }
            }

                // NOTE: we could actually use the same code as below, which gives more control over the subdirectory and doesn't have any size limits:
#else
            string dirpath = Path.Combine(ExternalFilesDataManager.STORAGE_FOLDER_PATH, m_DtdStoreDirName);
            //if (!Directory.Exists(dirpath))
            //{
            //    Directory.CreateDirectory(dirpath);
            //}

            string path = Path.Combine(dirpath, dtdCache);
            if (File.Exists(path))
            {
                Stream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
                try
                {
                    m_DtdRegex.ReadFromCache(new StreamReader(stream));
                }
                finally
                {
                    stream.Close();
                }
            }
#endif //USE_ISOLATED_STORAGE
            else
            {
                //else read the .dtd file
                Stream dtdStream = DTDs.DTDs.Fetch(dtdIdentifier);
                if (dtdStream == null)
                {
                    m_Dtd = null;
                    ContentDocumentValidationError error = new ContentDocumentValidationError
                                                               {
                                                                   ErrorType = ContentDocumentErrorType.MissingDtd,
                                                                   DtdIdentifier = m_DtdIdentifier
                                                               };
                    m_ValidationItems.Add(error);
                    return;
                }

                // NOTE: the Stream is automatically closed by the parser, see Scanner.ReadNextChar()
                DTDParser parser = new DTDParser(new StreamReader(dtdStream));
                m_Dtd = parser.Parse(true);

                m_DtdRegex.ParseDtdIntoHashtable(m_Dtd);

                //cache the dtd and save it as dtdIdenfier + ".cache"

#if USE_ISOLATED_STORAGE

                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    stream = new IsolatedStorageFileStream(dtdCache, FileMode.Create, FileAccess.Write, FileShare.None, store);
                }

                // NOTE: we could actually use the same code as below, which gives more control over the subdirectory and doesn't have any size limits:
#else
                if (!Directory.Exists(dirpath))
                {
                    Directory.CreateDirectory(dirpath);
                }

                Stream stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
#endif //USE_ISOLATED_STORAGE
                var writer = new StreamWriter(stream);
                try
                {
                    m_DtdRegex.WriteToCache(writer);
                }
                finally
                {
                    writer.Flush();
                    writer.Close();
                }
            }
        }

        //recursive function to validate the tree
        public bool ValidateNode(TreeNode node)
        {
            bool result = ValidateNodeContent(node);
            foreach (TreeNode child in node.Children.ContentsAs_YieldEnumerable)
            {
                result = result & ValidateNode(child);
            }
            return result;
        }
        //check a single node
        private bool ValidateNodeContent(TreeNode node)
        {
            if (node.HasXmlProperty)
            {
                string childrenNames = DtdSharpToRegex.GenerateChildNameList(node);
                Regex regex = m_DtdRegex.GetRegex(node);
                ContentDocumentValidationError error;
                if (regex == null)
                {
                    error = new ContentDocumentValidationError
                                                               {
                                                                   Target = node,
                                                                   ErrorType = ContentDocumentErrorType.UndefinedElement
                                                               };
                    m_ValidationItems.Add(error);
                    return false;
                }

                Match match = regex.Match(childrenNames);

                if (match.Success && match.ToString() == childrenNames)
                {
                    return true;
                }

                error = new ContentDocumentValidationError
                                                           {
                                                               Target = node,
                                                               ErrorType = ContentDocumentErrorType.InvalidElementSequence,
                                                               AllowedChildNodes = regex.ToString(),
                                                           };

                //look for more details about this error -- which child element is causing problems?
                RevalidateChildren(regex, childrenNames, error);

                m_ValidationItems.Add(error);
                return false;
            }

            //no XML property for the node: therefore, it is valid.
            return true;
        }

        //child element names must be formatted like "a#b#c#"
        private static void RevalidateChildren(Regex regex, string childrenNames, ContentDocumentValidationError error)
        {
            Match match = regex.Match(childrenNames);

            if (match.ToString() == childrenNames)
            {
                //we can say that the element after the last child element in the sequence
                //is where the problems start
                //and childrenNames is known to start at the beginning of the target node's children
                ArrayList childrenArr = StringToArrayList(childrenNames, '#');
                if (childrenArr.Count < error.Target.Children.Count)
                {
                    error.BeginningOfError = error.Target.Children.Get(childrenArr.Count);
                }
            }
            else
            {
                //test subsets of children -- for a#b#c# test a#b#
                ArrayList childrenArr = StringToArrayList(childrenNames, '#');
                if (childrenArr.Count >= 2)
                {
                    string subchildren = ArrayListToString(childrenArr.GetRange(0, childrenArr.Count - 1), '#');
                    RevalidateChildren(regex, subchildren, error);
                }
                else
                {
                    //there are no smaller subsets to test, so the error could be either with the first child
                    //or just a general error with the overall sequence
                    //better not to be specific if we aren't sures
                    error.BeginningOfError = null;
                }
            }
        }
        private static ArrayList StringToArrayList(string input, char delim)
        {
            ArrayList arr = new ArrayList(input.Split(delim));
            //trim the null item at the end of the array list
            if (string.IsNullOrEmpty((string)arr[arr.Count - 1]))
                arr.RemoveAt(arr.Count - 1);
            return arr;
        }
        private static string ArrayListToString(ArrayList arr, char delim)
        {
            string str = "";
            for (int i = 0; i < arr.Count; i++)
            {
                str += arr[i].ToString();
                str += delim;
            }
            return str;
        }


        //static helper functions
        public static string GetTreeNodeName(TreeNode node)
        {
            string nodeName = GetNearestTreeNodeName(node);
            if (string.IsNullOrEmpty(nodeName)) return "";
            return nodeName;
        }
        public static string GetNearestTreeNodeName(TreeNode node)
        {
            if (node.GetXmlElementQName() == null)
            {
                if (node.Parent != null)
                    return GetNearestTreeNodeName(node.Parent);
                else
                    return "";
            }

            return node.GetXmlElementQName().LocalName;
        }

        public static string GetTreeNodeTextExcerpt(TreeNode node)
        {

            string nodeText = node.GetTextMediaFlattened(false);
            if (nodeText == null) return "";
            if (nodeText.Length > 100)
                nodeText = nodeText.Substring(0, 100);

            nodeText += "...";

            //if this is a mixed content model node with previous sibling(s), add some ellipses before the text too
            if (node.GetXmlElementQName() == null && node.PreviousSibling != null)
                nodeText = "...\n" + nodeText;

            return nodeText;
        }

        
        //list the allowed elements, given a regex representing DTD rules
        //it would be easier to just construct this from DtdSharp objects, but there's a chance
        //that DtdSharp was never used in the scenario where Tobi caches a parsed DTD and uses that instead of
        //parsing with DtdSharp.  our cache contains a series of pairs, each representing
        //an element name and a regex of its allowed content model.
        public static string GetElementsListFromDtdRegex(string regex)
        {
            if (string.IsNullOrEmpty(regex)) return "";

            //string cleaner = regex.Replace("?:", "").Replace("#", "").Replace("((", "( (").Replace("))", ") )").Replace(")?(", ")? (");
            string cleaner = regex.Replace("?:", "").Replace("#", "");

            //this tree structure could also be used to tell the user what the proper sequence(s) should be
            //it's not the most efficient way to only retrieve a unique list of element names
            //however, we are dealing with small datasets, so it's not really an issue
            DtdNode dtdExpressionAsTree = Treeify(cleaner);
            
            //get a list of the dtd items that are elements (not just groupings of other elements)
            List<DtdNode> list = GetAllDtdElements(dtdExpressionAsTree);
            
            //keep track of already-seen items
            Hashtable temp = new Hashtable();

            //make a list of unique element names
            string elementNames = "";
            foreach(DtdNode node in list)
            {
                if (!temp.ContainsKey(node.ElementName))
                {
                    if (!string.IsNullOrEmpty(elementNames))
                        elementNames += ", ";
                    elementNames += node.ElementName;
                    temp[node.ElementName] = true;
                }
            }
            return elementNames;
        }

        private static List<DtdNode> GetAllDtdElements(DtdNode node)
        {
            List<DtdNode> list = new List<DtdNode>();
            if (!string.IsNullOrEmpty(node.ElementName))
            {
                list.Add(node);
            }
            foreach (DtdNode child in node.Children)
            {
                list.AddRange(GetAllDtdElements(child));
            }
            return list;
        }
        private class DtdNode
        {
            public List<DtdNode> Children { get; set; }
            public string ElementName { get; set; }
            public string AdditionalInfo { get; set; }
            public DtdNode Parent { get; set; }
            public string ChildRelationship { get; set; }
            public DtdNode()
            {
                AdditionalInfo = "";
                Children = new List<DtdNode>();
                ChildRelationship = "and";
            }  
      
        }

        private static DtdNode Treeify (string regex)
        {
            List<DtdNode> parentQ = new List<DtdNode>();
            //for each open paren, start a new DtdTreeNode
            string temp = "";
            DtdNode node = null;
            bool first = true;
            DtdNode root = null;
            for (int i = 0; i<regex.Length; i++)
            {
                if (regex[i] == '(')
                {
                    temp = "";
                    node = new DtdNode();
                    if (first) root = node;
                    first = false;
                    if (parentQ.Count > 0)
                    {
                        parentQ[parentQ.Count - 1].Children.Add(node);
                        node.Parent = parentQ[parentQ.Count - 1];
                    }
                    parentQ.Add(node);
                    
                }
                else if (regex[i] == ')')
                {
                    temp = "";
                    parentQ.RemoveAt(parentQ.Count - 1);
                }
                else if (regex[i] == '?' || regex[i] == '+' || regex[i] == '*')
                {
                    node.AdditionalInfo += regex[i];
                    temp = "";
                }
                else if (regex[i] == '|')
                {
                    if (node.Parent != null)
                        node.Parent.ChildRelationship = "or";
                }
                else if (regex[i] == 'P')
                {
                    //look ahead for PCDATA
                    string str = regex.Substring(i, 6);
                    if (str == "PCDATA")
                    {
                        node = new DtdNode();
                        node.ElementName = "PCDATA";
                        if (parentQ.Count > 0)
                        {
                            parentQ[parentQ.Count - 1].Children.Add(node);
                            node.Parent = parentQ[parentQ.Count - 1];
                        }
                        else
                        {
                            if (first) root = node;
                            first = false;
                        }

                        i += 5;
                    }
                }
                else
                {
                    temp += regex[i];
                    node.ElementName = temp;
                }

            }

            return root;
        }

        //returns a 1-child-deep "flat" xml representation
        public static string GetNodeXml(TreeNode node)
        {
            if (node == null) return "";
            string str = GetNodeXml_Deep(node, 0);
            return GetNodeXml_Flat(node);
        }

        private static string GetNodeXml_Flat(TreeNode node)
        {
            string xml = "";
            string nodeName = GetTreeNodeName(node);
            xml = string.Format("<{0}>", nodeName);

            if (node.GetTextMedia() != null)
            {
                if (node.GetTextMedia().Text.Length > 10)
                {
                    xml += node.GetTextMedia().Text.Substring(0, 10);
                    xml += "...";
                }
                else
                {
                    xml += node.GetTextMedia().Text;
                }
            }

            foreach (TreeNode child in node.Children.ContentsAs_YieldEnumerable)
            {
                string childNodeText = GetTreeNodeTextExcerpt(child);
                string childNodeName = GetTreeNodeName(child);
                xml += string.Format("\n\t<{0}>{1}</{0}>", childNodeName, childNodeText);
            }

            xml += string.Format("\n</{0}>", nodeName);
            return xml;
        }

        private static string GetNodeXml_Deep(TreeNode node, int level)
        {
            string xml = "";
            string nodeName = "";
            string indent = new string('\t', level);
            if (node.GetXmlElementQName() != null)
            {
                nodeName = node.GetXmlElementQName().LocalName;
                xml += string.Format("\n{0}<{1}>", indent, nodeName);
            }

            foreach (TreeNode child in node.Children.ContentsAs_YieldEnumerable)
            {
                xml += GetNodeXml_Deep(child, level++);
            }

            if (!string.IsNullOrEmpty(nodeName))
                xml += string.Format("\n{0}</{1}>", indent, nodeName);
            return xml;
        }

    }


}
