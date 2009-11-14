using System.Collections.Generic;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.Validation;
using urakawa.metadata.daisy;
using System.ComponentModel.Composition;

namespace Tobi.Modules.Validator.Metadata
{
    [Export(typeof(IValidator))]
    public class MetadataValidator : IValidator
    {
        [Import(typeof(IUrakawaSession))]
        protected IUrakawaSession m_Session;

        private urakawa.metadata.daisy.MetadataValidator m_Validator =
            new urakawa.metadata.daisy.MetadataValidator(SupportedMetadata_Z39862005.MetadataDefinitions);

        [ImportingConstructor]
        public MetadataValidator(
            [Import(typeof(ILoggerFacade))]
            ILoggerFacade logger)
        {
            logger.Log("Hello world !", Category.Info, Priority.High);
        }

        #region IValidator Members

        public string Name
        {
            get
            {
                return "MetadataValidator";
            }
        }

        public string Description
        {
            get
            {
                return "Validate metadata";
            }
        }

        public bool Validate()
        {
            if (m_Session.DocumentProject != null &&
                m_Session.DocumentProject.Presentations.Count > 0)
            {
                List<urakawa.metadata.Metadata> metadatas =
                    m_Session.DocumentProject.Presentations.Get(0).Metadatas.ContentsAs_ListCopy;
                return m_Validator.Validate(metadatas);
            }

            return true;
        }

        public IEnumerable<ValidationItem> ValidationItems
        {
            get
            {
                foreach (MetadataValidationError err in m_Validator.Errors)
                {
                    var validationItem = new ValidationItem
                                             {
                                                 Message = getErrorText(err),
                                                 Severity = ValidationSeverity.Error,
                                                 Validator = this
                                             };
                    yield return validationItem;
                }
                yield break;
            }
        }
        #endregion

        private string getErrorText(MetadataValidationError error)
        {
            string description = null;
            if (error is MetadataValidationFormatError)
            {
                description = string.Format("{0} must be {1}.",
                    error.Definition.Name.ToLower(),
                    ((MetadataValidationFormatError)error).Hint);
            }
            else if (error is MetadataValidationMissingItemError)
            {
                description = string.Format("Missing {0}", error.Definition.Name.ToLower());
            }
            else if (error is MetadataValidationDuplicateItemError)
            {
                description = string.Format("Duplicate of {0} not allowed.", error.Definition.Name.ToLower());
            }
            else
            {
                description = string.Format("Unspecified error in {0}.", error.Definition.Name.ToLower());
            }
            return description;
        }
    }
}
