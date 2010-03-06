using urakawa.metadata;
using Tobi.Common.Validation;

namespace Tobi.Plugin.Validator.Metadata
{
    public enum MetadataErrorType
    {
        FormatError,
        DuplicateItemError,
        MissingItemError
    }

    public class MetadataValidationError : ValidationItem
    {
        //the error type
        public MetadataErrorType ErrorType { get; set; }

        //the criteria
        public MetadataDefinition Definition { get; set; }

        //helpful hint about formatting
        public string Hint { get; set; }

        public urakawa.metadata.Metadata Target { get; set;}

        public new string Message
        {
            get
            {
                string description;
                switch (ErrorType)
                {
                    case MetadataErrorType.FormatError:
                        description = string.Format(Tobi_Plugin_Validator_Metadata_Lang.DefNameMustBeHint,                               // TODO LOCALIZE DefNameMustBeHint
                                                    Definition.Name.ToLower(), Hint);
                        break;
                    case MetadataErrorType.MissingItemError:
                        description = string.Format(Tobi_Plugin_Validator_Metadata_Lang.Missing, Definition.Name.ToLower());         // TODO LOCALIZE Missing
                        break;
                    case MetadataErrorType.DuplicateItemError:
                        description = string.Format(Tobi_Plugin_Validator_Metadata_Lang.DuplicateNotAllowed, Definition.Name.ToLower());    // TODO LOCALIZE DuplicateNotAllowed
                        break;
                    default:
                        description = string.Format(Tobi_Plugin_Validator_Metadata_Lang.UnspecifiedError, Definition.Name.ToLower());        // TODO LOCALIZE UnspecifiedError
                        break;
                }
                return description;
            }
        }
        public MetadataValidationError(MetadataDefinition definition)
        {
            Definition = definition;
        }
    }

}
