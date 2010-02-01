using Tobi.Common.Validation;
using urakawa.core;

namespace Tobi.Plugin.Validator.ContentDocument
{
    public enum ContentDocumentErrorType
    {
        UndefinedElement,
        ElementMisuse,
        MissingDtd
    }

    public class ContentDocumentValidationError : ValidationItem
    {
        public ContentDocumentErrorType ErrorType { get; set; }
        public string AllowedChildNodes { get; set; }
        public TreeNode Target { get; set;}

        public ContentDocumentValidationError()
        {
            Severity = ValidationSeverity.Error;
        }

        public ContentDocumentValidationError(TreeNode target, ContentDocumentErrorType errorType, 
            string allowedChildNodes, string message)
        {
            Target = target;
            ErrorType = errorType;
            AllowedChildNodes = allowedChildNodes;
            Message = message;

            Severity = ValidationSeverity.Error;
        }
       
    }
}
