using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;

namespace Tobi.Plugin.Validator.MissingAudio
{
    ///<summary>
    /// The validation framework includes a top-level UI to display all publication errors as they are detected.
    ///</summary>
    public sealed class MissingAudioValidatorPlugin : AbstractTobiPlugin
    {
        private readonly ILoggerFacade m_Logger;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        [ImportingConstructor]
        public MissingAudioValidatorPlugin(
            ILoggerFacade logger
            )
        {
            m_Logger = logger;
        }

        public override void Dispose()
        {
            m_Logger.Log(@"MissingAudioValidatorPlugin unloaded", Category.Debug, Priority.Medium);
        }

        public override string Name
        {
            get { return "Missing Audio Validator"; }
        }

        public override string Description
        {
            get { return "Find text missing audio content"; }
        }
    }
}
