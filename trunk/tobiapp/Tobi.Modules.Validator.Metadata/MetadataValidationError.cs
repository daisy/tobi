using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Presentation.Events;
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

        public override string Message
        {
            get
            {
                string description;
                switch (ErrorType)
                {
                    case MetadataErrorType.FormatError:
                        string name = Target.NameContentAttribute.Name;
                        if (Definition != null && string.IsNullOrEmpty(name))
                            name = Definition.Name;
                        
                        description = string.Format(Tobi_Plugin_Validator_Metadata_Lang.DefNameMustBeHint,                               // TODO LOCALIZE DefNameMustBeHint
                                                    name.ToLower(), Hint);
                        break;
                    case MetadataErrorType.MissingItemError:
                        description = string.Format(Tobi_Plugin_Validator_Metadata_Lang.Missing, Definition.Name.ToLower());         // TODO LOCALIZE Missing
                        break;
                    case MetadataErrorType.DuplicateItemError:
                        description = string.Format(Tobi_Plugin_Validator_Metadata_Lang.DuplicateNotAllowed, Definition.Name.ToLower());    // TODO LOCALIZE DuplicateNotAllowed
                        break;
                    default:
                        string name2 = Definition.Name;
                        if (string.IsNullOrEmpty(name2))
                            name2 = Target.NameContentAttribute.Name;
                        description = string.Format(Tobi_Plugin_Validator_Metadata_Lang.UnspecifiedError, name2.ToLower());        // TODO LOCALIZE UnspecifiedError
                        break;
                }
                return description;
            }
        }
     
        public override string CompleteSummary
        {
            get
            {
                string definition = "";
                if (Definition != null)
                {
                    definition = string.Format("Rules for {0}\n{1}\nMust be a {2}\nIs {3}\n{4}",
                                               Definition.Name,
                                               Definition.Description,
                                               DataTypeToString(Definition.DataType),
                                               OccurrenceToString(Definition),
                                               RepeatableToString(Definition.IsRepeatable));
                    
                    if (Definition.Synonyms != null && Definition.Synonyms.Count > 0)
                    {
                        string synonyms = string.Join(",", Definition.Synonyms.ToArray());
                        definition += string.Format("\nSynonyms: {0}", synonyms);
                    }
                }

                
                if (ErrorType == MetadataErrorType.DuplicateItemError)
                {
                    return string.Format(@"Metadata error: duplicate items detected
This metadata field cannot have more than one instance.
{0}", definition);
                }
                if (ErrorType == MetadataErrorType.FormatError)
                {
                    return string.Format(@"Metadata error: invalid formatting
The value for the item is invalid.
{0} = {1}
Hint: {0} must be {2}
{3}", 
    Target.NameContentAttribute.Name, 
    Target.NameContentAttribute.Value,
    Hint, 
    definition);
                }
                if (ErrorType == MetadataErrorType.MissingItemError)
                {
                    string name = "";
                    if (Definition != null) name = Definition.Name;

                    return string.Format(@"Metadata error: missing a required item
An entry for {0} was not found.
{1}", name, definition);
                }
                //catch-all
                return Message;
            }
        }

        public override void TakeAction()
        {
            m_EventAggregator.GetEvent<LaunchMetadataEditorEvent>().Publish(this);
        }

        private IEventAggregator m_EventAggregator;
        public MetadataValidationError(MetadataDefinition definition, IEventAggregator eventAggregator)
        {
            Definition = definition;
            m_EventAggregator = eventAggregator;
        }

        public static string RepeatableToString(bool value)
        {
            return value ? Tobi_Plugin_Validator_Metadata_Lang.MetadataMayBeRepeated : Tobi_Plugin_Validator_Metadata_Lang.MetadataMayNotBeRepeated; // TODO LOCALIZE MetadataMayBeRepeated, MetadataMayNotBeRepeated
        }
        public static string DataTypeToString(MetadataDataType dataType)
        {
            if (dataType == MetadataDataType.String)
                return "string";
            if (dataType == MetadataDataType.ClockValue)
                return "timestamp";
            if (dataType == MetadataDataType.Double)
                return "double (e.g. 10.56)";
            if (dataType == MetadataDataType.Date)
                return "date";
            if (dataType == MetadataDataType.FileUri)
                return "path to a file";
            if (dataType == MetadataDataType.Integer)
                return "integer";
            if (dataType == MetadataDataType.LanguageCode)
                return "language code";
            if (dataType == MetadataDataType.Number)
                return "number";

            return "";
        }

        public static string OccurrenceToString(MetadataDefinition item)
        {
            if (item.Occurrence == MetadataOccurrence.Required)
                return Tobi_Plugin_Validator_Metadata_Lang.Metadata_Required;          // TODO LOCALIZE Metadata_Required
            if (item.Occurrence == MetadataOccurrence.Recommended)
                return Tobi_Plugin_Validator_Metadata_Lang.Metadata_Recommended;        // TODO LOCALIZE Metadata_Recommended
            return Tobi_Plugin_Validator_Metadata_Lang.Metadata_Optional;               // TODO LOCALIZE Metadata_Optional
        }
    }

    public class LaunchMetadataEditorEvent : CompositePresentationEvent<MetadataValidationError>
    {
        public static ThreadOption THREAD_OPTION = ThreadOption.PublisherThread;
    }
}
