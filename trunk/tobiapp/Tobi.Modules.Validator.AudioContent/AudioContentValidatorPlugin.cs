using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;

namespace Tobi.Plugin.Validator.AudioContent
{
    ///<summary>
    /// The validation framework includes a top-level UI to display all publication errors as they are detected.
    ///</summary>
    public sealed class AudioContentValidatorPlugin : AbstractTobiPlugin
    {
        private readonly ILoggerFacade m_Logger;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        [ImportingConstructor]
        public AudioContentValidatorPlugin(
            ILoggerFacade logger
            )
        {
            m_Logger = logger;
        }

        public override void Dispose()
        {
            m_Logger.Log(@"AudioContentValidatorPlugin unloaded", Category.Debug, Priority.Medium);
        }

        public override string Name
        {
            get { return "Audio Content Validator"; }
        }

        public override string Description
        {
            get { return "A validator that shows which text nodes are missing audio content"; }
        }
    }
}
