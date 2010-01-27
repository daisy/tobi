using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.Validation;

namespace Tobi.Plugin.Validator.Metadata
{
    ///<summary>
    /// The validation framework includes a top-level UI to display all publication errors as they are detected.
    ///</summary>
    public sealed class MetadataValidatorPlugin : AbstractTobiPlugin
    {
        //private readonly MetadataValidator m_Validator;

        private readonly ILoggerFacade m_Logger;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        [ImportingConstructor]
        public MetadataValidatorPlugin(
            ILoggerFacade logger
            //[Import(typeof(MetadataValidator), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            //MetadataValidator validator
            )
        {
            m_Logger = logger;
            //m_Validator = validator;

            //m_Logger.Log(@"MetadataValidator loaded", Category.Debug, Priority.Medium);
        }

        public override void Dispose()
        {
            m_Logger.Log(@"MetadataValidator unloaded", Category.Debug, Priority.Medium);
        }

        public override string Name
        {
            get { return @"Metadata Validator."; }
        }

        public override string Description
        {
            get { return @"A validator that specialized in metadata."; }
        }
    }
}
