using Tobi.Common;
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
                    return string.Format(@"Invalid Element Sequence
Element <{0}> contains an invalid sequence of child elements.
{1}
The following are permitted as children for <{0}>:
{2}", 
                        ValidatorUtilities.GetTreeNodeName(Target), 
                        ValidatorUtilities.GetNodeXml(Target, true),              
                        ContentDocumentValidator.GetElementsListFromDtdRegex(AllowedChildNodes));
                }
                if (ErrorType == ContentDocumentErrorType.MissingDtd)
                {
                    return string.Format(@"Missing DTD
Tobi could not locate a DTD, so it cannot validate the document.
The DTD identifier associated with this document is:
{0}", DtdIdentifier);    
                }
                if (ErrorType == ContentDocumentErrorType.UndefinedElement)
                {
                    return string.Format(@"Undefined element
An element definition was not found for:
{0}",
    ValidatorUtilities.GetNodeXml(Target, true));
                }

                //catch-all
                return Message;
            }
            
        }

        public override void TakeAction()
        {
            if (ErrorType != ContentDocumentErrorType.MissingDtd)
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
