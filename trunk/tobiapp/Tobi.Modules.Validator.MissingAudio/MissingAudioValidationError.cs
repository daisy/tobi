using Tobi.Common;
using Tobi.Common.Validation;
using urakawa.core;

namespace Tobi.Plugin.Validator.MissingAudio
{
    public class MissingAudioValidationError : ValidationItemWithTarget<TreeNode>
    {
        public override string Message
        {
            get { return string.Format(Tobi_Plugin_Validator_MissingAudio_Lang.MissingAudioMessage, ValidatorUtilities.GetTreeNodeName(Target)); }
        }
        public override string CompleteSummary
        {
            get
            {
                return string.Format(Tobi_Plugin_Validator_MissingAudio_Lang.MissingAudioSummary, 
                        ValidatorUtilities.GetTreeNodeName(Target), 
                        ValidatorUtilities.GetNodeXml(Target, true)
                        );
            }
        }

        private IUrakawaSession m_UrakawaSession;
       
        public override void TakeAction()
        {
            m_UrakawaSession.PerformTreeNodeSelection(Target);
        }

        public override bool CanTakeAction
        {
            get { return true; }
        }

        public MissingAudioValidationError(IUrakawaSession session)
        {
            m_UrakawaSession = session;
            Severity = ValidationSeverity.Error;
        }
    }
}
