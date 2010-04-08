using Tobi.Common;
using Tobi.Common.Validation;
using urakawa.core;

namespace Tobi.Plugin.Validator.MissingAudio
{
    public class MissingAudioValidationError : ValidationItem
    {
        public TreeNode Target { get; set;}
        
        public override string Message
        {
            get { return "node is missing audio"; }
        }
        public override string CompleteSummary
        {
            get
            {
                return string.Format("{0}\n{1}", 
                        Message, 
                        ValidatorUtilities.GetNodeXml(Target, true)
                        );
            }
            
        }

        private IUrakawaSession m_UrakawaSession;
       
        public override void TakeAction()
        {
            m_UrakawaSession.PerformTreeNodeSelection(Target);
        }

        public MissingAudioValidationError(IUrakawaSession session)
        {
            m_UrakawaSession = session;
            Severity = ValidationSeverity.Error;
        }
    }
}
