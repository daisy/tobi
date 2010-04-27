using System;
using Tobi.Common;
using Tobi.Common.Validation;
using urakawa.core;

namespace Tobi.Plugin.Validator.MissingAudio
{
    public static class Tobi_Plugin_Validator_MissingAudio_Lang
    {
        public static string MissingAudioMessage = @"TEMP_DUMMY";
        public static string MissingAudioSummary = @"TEMP_DUMMY";
        public static string MissingAudioValidator_Name = @"TEMP_DUMMY";
        public static string MissingAudioValidator_Description = @"TEMP_DUMMY";
        public static string MissingAudioMessage2 = @"TEMP_DUMMY";
        public static string MissingAudio = @"TEMP_DUMMY";
        public static string ClickToView = @"TEMP_DUMMY";
    }
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
