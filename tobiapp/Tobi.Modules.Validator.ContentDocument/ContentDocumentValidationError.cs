﻿using Tobi.Common;
using Tobi.Common.Validation;
using urakawa.core;

namespace Tobi.Plugin.Validator.ContentDocument
{
    public enum ContentDocumentErrorType
    {
        UndefinedElement,
        InvalidElementSequence,
        MissingDtd
    }
    
    //expectation of error types and available data
    //UndefinedElement => supply Target
    //InvalidElementSequence => supply Target, AllowedChildNodes, optionally BeginningOfError
    //MissingDtd => supply DtdIdentifier
    public class ContentDocumentValidationError : ValidationItem
    {
        public ContentDocumentErrorType ErrorType { get; set; }
        public string AllowedChildNodes { get; set; }
        public TreeNode Target { get; set;}
        public TreeNode BeginningOfError { get; set; }
        public string DtdIdentifier { get; set; }

                public override string Message
        {
            get
            {
                string targetNodeName = "";
                
                if (Target != null)
                {
                    targetNodeName = ValidatorUtilities.GetTreeNodeName(Target);
                }
                
                if (ErrorType == ContentDocumentErrorType.InvalidElementSequence)
                {
                    return string.Format(Tobi_Plugin_Validator_ContentDocument_Lang.InvalidSequence, targetNodeName);           // TODO LOCALIZE Key already added InvalidSequence
                }
                if (ErrorType == ContentDocumentErrorType.MissingDtd)
                {
                    return string.Format(Tobi_Plugin_Validator_ContentDocument_Lang.NoDTDFound, DtdIdentifier);                // TODO LOCALIZE Key already added NoDTDFound
                }
                if (ErrorType == ContentDocumentErrorType.UndefinedElement)
                {
                    return string.Format(Tobi_Plugin_Validator_ContentDocument_Lang.NoElementDefinitionFound, targetNodeName);      // TODO LOCALIZE Key already added NoElementDefinitionFound
                }
                return Tobi_Plugin_Validator_ContentDocument_Lang.UnspecifiedError;                                                           // TODO LOCALIZE Key already added UnspecifiedError
            }
        }
        public override string CompleteSummary
        {
            get
            {
                if (ErrorType == ContentDocumentErrorType.InvalidElementSequence)
                {
                    //return message plus target node snippet plus dtd definition snippet
                    return string.Format("{0}\n{1}\n{2}\n{3}", 
                        Message, 
                        ValidatorUtilities.GetNodeXml(Target, true),
                        "The allowed child elements are:",                        
                        ContentDocumentValidator.GetElementsListFromDtdRegex(AllowedChildNodes));
                }
                if (ErrorType == ContentDocumentErrorType.MissingDtd)
                {
                    return string.Format("DTD resource not found for {0}", DtdIdentifier);    
                }
                if (ErrorType == ContentDocumentErrorType.UndefinedElement)
                {
                    return string.Format("Element definition not found for <{0}>",
                                         ValidatorUtilities.GetTreeNodeName(Target)
                                         );
                }

                return "";
            }
            
        }

        public override void TakeAction()
        {
            m_UrakawaSession.PerformTreeNodeSelection(Target);
        }

        private IUrakawaSession m_UrakawaSession;
       
        public ContentDocumentValidationError(IUrakawaSession session)
        {
            m_UrakawaSession = session;
            Severity = ValidationSeverity.Error;
        }
    }
}
