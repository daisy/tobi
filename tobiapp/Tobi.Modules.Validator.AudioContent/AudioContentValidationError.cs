using System.ComponentModel.Composition;
using Tobi.Common;
using Tobi.Common.Validation;
using urakawa.core;

namespace Tobi.Plugin.Validator.AudioContent
{
    public class AudioContentValidationError : ValidationItem
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
                        ValidatorUtilities.GetNodeXml(Target, true));
            }
            
        }

        [Import(typeof (IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false,
            AllowRecomposition = false)] 
        private IUrakawaSession m_UrakawaSession;
       
        //TODO: m_UrakawaSession is null ... am I importing it correctly?
        public override void TakeAction()
        {
            m_UrakawaSession.PerformTreeNodeSelection(Target);
        }

        
        public AudioContentValidationError()
        {
            Severity = ValidationSeverity.Error;
        }
    }
}
