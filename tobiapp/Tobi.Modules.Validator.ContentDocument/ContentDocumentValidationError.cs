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

        //todo: is there a problem overriding the base property Message {get; set;} with this new Message {get;} (no set method) ?
        public new string Message
        {
            get
            {
                string targetNodeName = "";
                string problemChildNodeName = "";
                string targetText = "";
                string problemChildText = "";

                if (Target != null)
                {
                    targetText = Target.GetTextMediaFlattened();
                    if (Target.GetXmlElementQName() != null)
                        targetNodeName = Target.GetXmlElementQName().LocalName;
                }
                if (BeginningOfError != null && BeginningOfError.GetXmlElementQName() != null)
                    problemChildNodeName = BeginningOfError.GetXmlElementQName().LocalName;

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
        public ContentDocumentValidationError()
        {
            Severity = ValidationSeverity.Error;
        }
    }
}
