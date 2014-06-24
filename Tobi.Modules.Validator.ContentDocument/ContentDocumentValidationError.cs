using System;
using Tobi.Common;
using Tobi.Common.Validation;
using urakawa.core;

namespace Tobi.Plugin.Validator.ContentDocument
{  
    public class InvalidElementSequenceValidationError : ValidationItemWithTarget<TreeNode>
    {
        private readonly IUrakawaSession m_UrakawaSession;
        public string AllowedChildNodes { get; set; }
        public InvalidElementSequenceValidationError(IUrakawaSession session)
        {
            m_UrakawaSession = session;
            Severity = ValidationSeverity.Error;
        }

        public override string Message
        {
            get
            {
                return string.Format(Tobi_Plugin_Validator_ContentDocument_Lang.InvalidElementSequenceMessage,
                                     ValidatorUtilities.GetTreeNodeName(Target));
            }
        }

        public override string CompleteSummary
        {
            get
            {
                return string.Format(Tobi_Plugin_Validator_ContentDocument_Lang.InvalidElementSequenceSummary,
                        ValidatorUtilities.GetTreeNodeName(Target),
                        ValidatorUtilities.GetNodeXml_Flat(Target),
                        ContentDocumentValidator.GetElementsListFromDtdRegex(AllowedChildNodes));
            }
        }

        public override void TakeAction()
        {
            m_UrakawaSession.PerformTreeNodeSelection(Target, false, null);
        }

        public override bool CanTakeAction
        {
            get { return true; }
        }
    }
    public class MissingDtdValidationError : ValidationItem
    {
        public string DtdIdentifier { get; set;}

        public override string Message
        {
            get 
            {
                return string.Format(Tobi_Plugin_Validator_ContentDocument_Lang.NoDTDFound, DtdIdentifier);
            }
        }

        public override string CompleteSummary
        {
            get
            {
                return string.Format(Tobi_Plugin_Validator_ContentDocument_Lang.MissingDTDSummary, DtdIdentifier);    
            }
        }

        public override void TakeAction()
        {
            //do nothing
        }

        public override bool CanTakeAction
        {
            get { return false; }
        }
    }
    public class UndefinedElementValidationError : ValidationItemWithTarget<TreeNode>
    {
        private readonly IUrakawaSession m_UrakawaSession;
       
        public UndefinedElementValidationError(IUrakawaSession session)
        {
            m_UrakawaSession = session;
            Severity = ValidationSeverity.Error;
        }

        public override string Message
        {
            get
            {
                return string.Format(Tobi_Plugin_Validator_ContentDocument_Lang.NoElementDefinitionFound, 
                    ValidatorUtilities.GetTreeNodeName(Target));
            }
        }

        public override string CompleteSummary
        {
            get
            {
                return string.Format(Tobi_Plugin_Validator_ContentDocument_Lang.UndefinedElementSummary,
                    ValidatorUtilities.GetNodeXml_Flat(Target));
            }
        }

        public override void TakeAction()
        {
            m_UrakawaSession.PerformTreeNodeSelection(Target, false, null);
        }

        public override bool CanTakeAction
        {
            get { return true; }
        }
    }
}
