using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DtdSharp;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.Validation;
using urakawa;
using urakawa.core;
using urakawa.ExternalFiles;
using urakawa.data;

#if USE_ISOLATED_STORAGE
using System.IO.IsolatedStorage;
#endif //USE_ISOLATED_STORAGE

namespace Tobi.Plugin.Validator.ContentDocument
{
    /// <summary>
    /// The main validator class
    /// </summary>
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
            IEventAggregator eventAggregator,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = false, AllowDefault = false)]
            IUrakawaSession session)
            : base(eventAggregator)
        {
            m_Logger = logger;
            m_Session = session;

            m_DtdRegex = new DtdSharpToRegex();

            m_Logger.Log(@"ContentDocumentValidator initialized", Category.Debug, Priority.Medium);
        }

        protected override void OnProjectLoaded(Project project)
        {
            base.OnProjectLoaded(project);


            //ThreadPool.QueueUserWorkItem(
            //    delegate(Object o) // or: (foo) => {} (LAMBDA)
            //        { }, obj);
            Validate();
        }

        protected override void OnProjectUnLoaded(Project project)
        {
            base.OnProjectUnLoaded(project);
        }

        public override string Name
        {
            get { return Tobi_Plugin_Validator_ContentDocument_Lang.ContentDocumentValidator_Name; }
        }

        public override string Description
        {
            get { return Tobi_Plugin_Validator_ContentDocument_Lang.ContentDocumentValidator_Description; }
        }

        private void loadDTD(string dtdIdentifier)
        {
            DTD dtd = UseDtd(dtdIdentifier);
            if (dtd == null)
            {
                return;
            }

            foreach (DictionaryEntry entry in dtd.Entities)
            {
                DTDEntity dtdEntity = (DTDEntity)entry.Value;

                if (dtdEntity.ExternalId == null)
                {
                    continue;
                }

                string system = dtdEntity.ExternalId.System;
                if (dtdEntity.ExternalId is DTDPublic)
                {
                    string pub = ((DTDPublic)dtdEntity.ExternalId).Pub;
                    if (!string.IsNullOrEmpty(pub))
                    {
                        system = pub.Replace(" ", "%20");
                    }
                }

                foreach (String key in DTDs.DTDs.ENTITIES_MAPPING.Keys)
                {
                    if (system.Contains(key))
                    {
                        loadDTD(DTDs.DTDs.ENTITIES_MAPPING[key]);
                    }
                }
            }
        }

        public override bool Validate()
        {
            //#if DEBUG
            //            m_DtdRegex.Reset();
            //#endif // DEBUG

            if (m_DtdRegex.DtdRegexTable == null || m_DtdRegex.DtdRegexTable.Count == 0)
            {
                m_DtdIdentifier = DTDs.DTDs.DTBOOK_2005_3_MATHML;
                loadDTD(m_DtdIdentifier);

                string dtdCache = m_DtdIdentifier + ".cache";
                string dirpath = Path.Combine(ExternalFilesDataManager.STORAGE_FOLDER_PATH, m_DtdStoreDirName);
                string path = Path.Combine(dirpath, dtdCache);

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
                    FileDataProvider.CreateDirectory(dirpath);
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

            if (m_Session.DocumentProject != null)
            {
                resetToValid();

                if (m_DtdRegex == null || m_DtdRegex.DtdRegexTable == null || m_DtdRegex.DtdRegexTable.Count == 0)
                {
                    MissingDtdValidationError error = new MissingDtdValidationError()
                                                               {
                                                                   DtdIdentifier = m_DtdIdentifier
                                                               };
                    addValidationItem(error);
                }
                else
                {
                    var strBuilder = new StringBuilder();
                    ValidateNode(strBuilder, m_Session.DocumentProject.Presentations.Get(0).RootNode);
                }
            }
            return IsValid;
        }

        private DtdSharpToRegex m_DtdRegex;
        private string m_DtdIdentifier;

        private const string m_DtdStoreDirName = "Cached-DTDs";

        public DTD UseDtd(string dtdIdentifier)
        {
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
                Stream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                try
                {
                    m_DtdRegex.ReadFromCache(new StreamReader(stream));
                }
                finally
                {
                    stream.Close();
                }

                return null;
            }
#endif //USE_ISOLATED_STORAGE
            else
            {
                //else read the .dtd file
                Stream dtdStream = DTDs.DTDs.Fetch(dtdIdentifier);
                if (dtdStream == null)
                {
                    //DebugFix.Assert(false);
#if DEBUG
                    Debugger.Break();
#endif // DEBUG
                    MissingDtdValidationError error = new MissingDtdValidationError()
                                                               {
                                                                   DtdIdentifier = m_DtdIdentifier
                                                               };
                    addValidationItem(error);
                    return null;
                }

                // NOTE: the Stream is automatically closed by the parser, see Scanner.ReadNextChar()
                DTDParser parser = new DTDParser(new StreamReader(dtdStream));
                DTD dtd = parser.Parse(true);

                m_DtdRegex.ParseDtdIntoHashtable(dtd);

                return dtd;
            }
        }

        //recursive function to validate the tree
        public bool ValidateNode(StringBuilder strBuilder, TreeNode node)
        {
            bool result = ValidateNodeContent(strBuilder, node);
            foreach (TreeNode child in node.Children.ContentsAs_Enumerable)
            {
                bool res = ValidateNode(strBuilder, child);
                result = result && res;
            }
            return result;
        }

        //check a single node
        private bool ValidateNodeContent(StringBuilder strBuilder, TreeNode node)
        {
            if (node.HasXmlProperty)
            {
                string childrenNames = DtdSharpToRegex.GenerateChildNameList(strBuilder, node);
                Regex regex = m_DtdRegex.GetRegex(strBuilder, node);
                if (regex == null)
                {
                    var error1 = new UndefinedElementValidationError(m_Session)
                                                               {
                                                                   Target = node
                                                               };
                    addValidationItem(error1);
                    return false;
                }

                Match match = regex.Match(childrenNames);

                string matchStr = match.ToString();
                if (match.Success && matchStr == childrenNames)
                {
                    return true;
                }

                var error2 = new InvalidElementSequenceValidationError(m_Session)
                                                           {
                                                               Target = node,
                                                               AllowedChildNodes = regex.ToString(),
                                                           };

                //look for more details about this error -- which child element is causing problems?
                //RevalidateChildren(regex, childrenNames, error);

                addValidationItem(error2);
                return false;
            }

            //no XML property for the node: therefore, it is valid.
            return true;
        }

        //child element names must be formatted like "a#b#c#"
        /*    private static void RevalidateChildren(Regex regex, string childrenNames, ContentDocumentValidationError error)
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
                        //error.BeginningOfError = error.Target.Children.Get(childrenArr.Count);
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
                        //error.BeginningOfError = null;
                    }
                }
            }
            */
        //private static ArrayList StringToArrayList(string input, char delim)
        //{
        //    ArrayList arr = new ArrayList(input.Split(delim));
        //    //trim the null item at the end of the array list
        //    if (string.IsNullOrEmpty((string)arr[arr.Count - 1]))
        //        arr.RemoveAt(arr.Count - 1);
        //    return arr;
        //}
        //private static string ArrayListToString(ArrayList arr, char delim)
        //{
        //    string str = "";
        //    for (int i = 0; i < arr.Count; i++)
        //    {
        //        str += arr[i].ToString();
        //        str += delim;
        //    }
        //    return str;
        //}

        //list the allowed elements, given a regex representing DTD rules
        //it would be easier to just construct this from DtdSharp objects, but there's a chance
        //that DtdSharp was never used in the scenario where Tobi caches a parsed DTD and uses that instead of
        //parsing with DtdSharp.  our cache contains a series of pairs, each representing
        //an element name and a regex of its allowed content model.
        public static string GetElementsListFromDtdRegex(string regex)
        {
            if (string.IsNullOrEmpty(regex)) return "";

            //string cleaner = regex.Replace("?:", "").Replace(DELIMITER, "").Replace("((", "( (").Replace("))", ") )").Replace(")?(", ")? (");
            string cleaner = regex.Replace("?:", "").Replace("" + DtdSharpToRegex.DELIMITER, "");

            //this tree structure could also be used to tell the user what the proper sequence(s) should be
            //it's not the most efficient way to only retrieve a unique list of element names
            //however, we are dealing with small datasets, so it's not really an issue
            DtdNode dtdExpressionAsTree = Treeify(cleaner);

            //get a list of the dtd items that are elements (not just groupings of other elements)
            List<DtdNode> list = GetAllDtdElements(dtdExpressionAsTree);

            //keep track of already-seen items
            var alreadySeen = new List<string>();

            //make a list of unique element names
            var strBuilder = new StringBuilder();
            bool first = true;
            foreach (DtdNode node in list)
            {
                if (!alreadySeen.Contains(node.ElementName))
                {
                    if (!first)
                    {
                        strBuilder.Append(", ");
                    }
                    first = false;

                    strBuilder.Append(node.ElementName);
                    
                    alreadySeen.Add(node.ElementName);
                }
            }

            return strBuilder.ToString();
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

        private static DtdNode Treeify(string regex)
        {
            List<DtdNode> parentQ = new List<DtdNode>();
            //for each open paren, start a new DtdTreeNode
            string temp = "";
            DtdNode node = null;
            bool first = true;
            DtdNode root = null;
            for (int i = 0; i < regex.Length; i++)
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
                else if (regex[i] == DtdSharpToRegex.PCDATA[0])
                {
                    //look ahead for PCDATA
                    string str = regex.Substring(i, DtdSharpToRegex.PCDATA.Length);
                    if (str == DtdSharpToRegex.PCDATA)
                    {
                        node = new DtdNode();
                        node.ElementName = "TEXT";
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

                        i += (DtdSharpToRegex.PCDATA.Length - 1);
                    }
                }
                else
                {
                    temp += regex[i];
                    node.ElementName = temp.Replace(DtdSharpToRegex.NAMESPACE_PREFIX_SEPARATOR, ':');
                }

            }

            return root;
        }
    }


}
