using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text.RegularExpressions;
using DtdSharp;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.Validation;
using urakawa.core;


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
            get { return "Content Document Validator";}
        }

        public override string Description
        {
            get{ return "A validator that uses data from a DTD to validate elements in a document tree.";}
        }
        public override IEnumerable<ValidationItem> ValidationItems
        {
            get { return m_ValidationItems; }
        }

        private List<ValidationItem> m_ValidationItems;
        
        public override bool Validate()
        {
            //TODO: un-hardcode this
            string dtdIdentifier = "DTDs.Resources.dtbook-2005-3.dtd";
            UseDtd(dtdIdentifier);

            if (m_Session.DocumentProject != null &&
                m_Session.DocumentProject.Presentations.Count > 0)
            {   
                m_ValidationItems = new List<ValidationItem>();

                if (m_DtdRegex == null)
                {
                    ContentDocumentValidationError error = new ContentDocumentValidationError
                                                               {
                                                                   ErrorType = ContentDocumentErrorType.MissingDtd,
                                                                   Message = "DTD not assigned."
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

        public void UseDtd(string dtdIdentifier)
        {
            string dtdCache = dtdIdentifier + ".cache";
            //check to see if we have a cached version of this file
            if (ExistsInIsolatedStorage(dtdCache))
            {
                IsolatedStorageFileStream isoStream = GetIsolatedStorageFileStream(dtdCache);
                m_DtdRegex.ReadFromCache(new StreamReader(isoStream));
            }
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
                                                                   Message =
                                                                       string.Format("{0} not found.", dtdIdentifier)
                                                               };
                    m_ValidationItems.Add(error);
                    return;
                }
                
                DTDParser parser = new DTDParser(new StreamReader(dtdStream));
                m_Dtd = parser.Parse(true);
                m_DtdRegex.ParseDtdIntoHashtable(m_Dtd);

                //cache the dtd and save it as dtdIdenfier + ".cache"
                IsolatedStorageFileStream isoStream = GetIsolatedStorageFileStream(dtdCache);
                StreamWriter writer = new StreamWriter(isoStream);
                m_DtdRegex.WriteToCache(writer);
            }
            
        }

        private static bool ExistsInIsolatedStorage(string filename)
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                string[] filenames = store.GetFileNames(filename);
                return filenames.Length > 0;
            }
        }

        private static IsolatedStorageFileStream GetIsolatedStorageFileStream(string filename)
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var stream = new IsolatedStorageFileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, store);
                return stream;
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
                    string msg = string.Format("Definition for {0} not found", node.GetXmlElementQName().LocalName);
                    error = new ContentDocumentValidationError
                                                               {
                                                                   Target = node,
                                                                   ErrorType = ContentDocumentErrorType.UndefinedElement,
                                                                   Message = msg
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
                                                               ErrorType = ContentDocumentErrorType.ElementMisuse,
                                                               AllowedChildNodes = childrenNames,
                                                               Message =
                                                                   "Unexpected child node or missing child node"
                                                           };
                m_ValidationItems.Add(error);
                return false;
            }

            //no XML property for the node: therefore, it is valid.
            return true;
        }
    }
}
