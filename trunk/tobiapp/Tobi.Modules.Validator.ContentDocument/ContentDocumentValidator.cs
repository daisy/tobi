using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text.RegularExpressions;
using DtdSharp;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.Validation;
using urakawa.core;
using UrakawaDomValidation;
using urakawa.daisy;

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

        public override bool Validate()
        {
            bool isValid = true;

            if (m_Session.DocumentProject != null &&
                m_Session.DocumentProject.Presentations.Count > 0)
            {   
                m_ValidationItems = new List<ValidationItem>();
                isValid = _validate();
            }

            if (!isValid)
            {
                //TODO: send an event that there are new validation errors
            }

            IsValid = isValid;
            return isValid;
        }

        public override IEnumerable<ValidationItem> ValidationItems
        {
            get { return m_ValidationItems;}
        }

        private List<ValidationItem> m_ValidationItems;
        
        private bool _validate()
        {
            //TODO: report error if DTD not set
            return Validate(m_Session.DocumentProject.Presentations.Get(0).RootNode);
        }

        private DTD m_Dtd;
        private Hashtable m_DtdElementRegex;
        public void UseDtd(StreamReader dtd)
        {
            DTDParser parser = new DTDParser(dtd);
            m_Dtd = parser.Parse(true);
            ParseDtdIntoHashmap();
        }
        public void UseCachedDtd(StreamReader dtd)
        {
            DtdCache.ReadFromCache(dtd);
        }
        public void SaveCachedDtd(StreamWriter dtd)
        {
            DtdCache.WriteToCache(m_DtdElementRegex, dtd);
        }
        //utility function
        public string GetRegex(TreeNode node)
        {
            Regex regex = (Regex)m_DtdElementRegex[node.GetXmlElementQName().LocalName];
            return regex != null ? regex.ToString() : "";
        }

        //take a DtdSharp data structure and create a hashmap where 
        //key: element name
        //value: regex representing the allowed children
        private void ParseDtdIntoHashmap()
        {
            m_DtdElementRegex = new Hashtable();

            foreach (DictionaryEntry entry in m_Dtd.Elements)
            {
                DTDElement dtdElement = (DTDElement)entry.Value;
                string regexStr = GenerateRegexForAllowedChildren(dtdElement.Content);
                Regex regex = new Regex(regexStr);
                m_DtdElementRegex.Add(dtdElement.Name, regex);
            }
        }

        private static string GenerateRegexForAllowedChildren(DTDItem dtdItem)
        {
            string regexStr = "";

            if (dtdItem is DTDAny)
            {
                regexStr += "";// "Any";
            }
            else if (dtdItem is DTDEmpty)
            {
                regexStr += "";
            }
            else if (dtdItem is DTDName)
            {
                regexStr += "(?:" + ((DTDName)dtdItem).Value + "#)";
            }
            else if (dtdItem is DTDChoice)
            {
                List<DTDItem> items = ((DTDChoice)dtdItem).Items;
                if (items.Count > 1) regexStr += "(?:";

                bool isFirst = true;
                foreach (DTDItem item in items)
                {
                    if (!isFirst) regexStr += "|";
                    isFirst = false;
                    regexStr += GenerateRegexForAllowedChildren(item);
                }
                if (items.Count > 1) regexStr += ")";
            }
            else if (dtdItem is DTDSequence)
            {
                List<DTDItem> items = ((DTDSequence)dtdItem).Items;
                if (items.Count > 1) regexStr += "(?:";

                bool isFirst = true;
                foreach (DTDItem item in items)
                {
                    if (!isFirst) regexStr += "";
                    regexStr += GenerateRegexForAllowedChildren(item);
                    isFirst = false;
                }
                if (items.Count > 1) regexStr += ")";
            }
            else if (dtdItem is DTDMixed)
            {
                List<DTDItem> items = ((DTDMixed)dtdItem).Items;
                if (items.Count > 1) regexStr += "(?:";

                bool isFirst = true;
                foreach (DTDItem item in items)
                {
                    if (!isFirst) regexStr += "|";
                    regexStr += GenerateRegexForAllowedChildren(item);
                    isFirst = false;
                }
                if (items.Count > 1) regexStr += ")";
            }
            else if (dtdItem is DTDPCData)
            {
                regexStr += "#PCDATA";
            }
            else
            {
                regexStr += "**UNKNOWN**";
            }
            if (dtdItem.Cardinal == DTDCardinal.ZEROONE)
            {
                regexStr += "?";
            }
            else if (dtdItem.Cardinal == DTDCardinal.ZEROMANY)
            {
                regexStr += "*";
            }
            else if (dtdItem.Cardinal == DTDCardinal.ONEMANY)
            {
                regexStr += "+";
            }
            return regexStr;
        }

        //the recursive function
        public bool Validate(TreeNode node)
        {
            bool result = ValidateNodeContent(node);
            foreach (TreeNode child in node.Children.ContentsAs_ListAsReadOnly)
            {
                result = result & Validate(child);
            }
            return result;
        }
        //check a single node
        private bool ValidateNodeContent(TreeNode node)
        {
            if (node.HasXmlProperty)
            {
                string childrenNames = GetChildrenNames(node);
                Regex regExp = (Regex) m_DtdElementRegex[node.GetXmlElementQName().LocalName];
                if (regExp == null)
                {
                    string msg = string.Format("Definition for {0} not found", node.GetXmlElementQName().LocalName);
                    ContentDocumentValidationError error = new ContentDocumentValidationError
                                                               {
                                                                   Target = node,
                                                                   ErrorType = ContentDocumentErrorType.UndefinedElement,
                                                                   Message = msg
                                                               };
                    m_ValidationItems.Add(error);
                    return false;
                }

                Match match = regExp.Match(childrenNames);

                if (match.Success && match.ToString() == childrenNames)
                {
                    return true;
                }
                else
                {
                    ContentDocumentValidationError error = new ContentDocumentValidationError
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
            }
            else //no XML property
            {
                return true;
            }
        }

        private static string GetChildrenNames(TreeNode node)
        {
            string names = "";

            if (node.GetTextMedia() != null)
            {
                names += "#PCDATA";
            }
            foreach (TreeNode child in node.Children.ContentsAs_ListAsReadOnly)
            {
                if (child.HasXmlProperty)
                {
                    names += child.GetXmlElementQName().LocalName + "#";
                }
            }
            return names;
        }
        
    }
}
