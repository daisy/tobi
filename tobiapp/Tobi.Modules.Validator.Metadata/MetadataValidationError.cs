using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Presentation.Events;
using urakawa.metadata;
using Tobi.Common.Validation;

namespace Tobi.Plugin.Validator.Metadata
{
    public interface IMetadataValidationError
    {
        MetadataDefinition Definition { get; set; }
        string Hint { get; set; }
    }
    
    public abstract class AbstractMetadataValidationError : ValidationItem, IMetadataValidationError
    {
        private IEventAggregator m_EventAggregator;
        public AbstractMetadataValidationError(MetadataDefinition definition, IEventAggregator eventAggregator)
        {
            Definition = definition;
            m_EventAggregator = eventAggregator;
        }
        public override void TakeAction()
        {
            m_EventAggregator.GetEvent<LaunchMetadataEditorEvent>().Publish(this);
        }

        public override bool CanTakeAction
        {
            get { return true;}
        }

        public MetadataDefinition Definition { get; set; }

        public string Hint { get; set; }
    }
    public abstract class AbstractMetadataValidationErrorWithTarget : 
        ValidationItemWithTarget<urakawa.metadata.Metadata>, IMetadataValidationError
    {
        private IEventAggregator m_EventAggregator;
        public AbstractMetadataValidationErrorWithTarget(urakawa.metadata.Metadata target, 
            MetadataDefinition definition, IEventAggregator eventAggregator)
        {
            Target = target;
            Definition = definition;
            m_EventAggregator = eventAggregator;
        }
        public override void TakeAction()
        {
            m_EventAggregator.GetEvent<LaunchMetadataEditorEvent>().Publish(this);
        }

        public override bool CanTakeAction
        {
            get { return true;}
        }
        public MetadataDefinition Definition { get; set;}
        public string Hint { get; set;}
    }

    public class MetadataFormatValidationError : AbstractMetadataValidationErrorWithTarget
    {
        public MetadataFormatValidationError(urakawa.metadata.Metadata target, MetadataDefinition definition, IEventAggregator eventAggregator) 
            : base (target, definition, eventAggregator)
        {
        }
        public override string Message
        {
            get
            {
                string name = Target.NameContentAttribute.Name;
                if (Definition != null && string.IsNullOrEmpty(name))
                    name = Definition.Name;

                return string.Format(Tobi_Plugin_Validator_Metadata_Lang.DefNameMustBeHint,                              
                                            name.ToLower(), Hint);
            }
        }

        public override string CompleteSummary
        {
            get
            {
                string definition = MetadataUtilities.GetDefinitionSummary(Definition);
                return string.Format(Tobi_Plugin_Validator_Metadata_Lang.FormatErrorCompleteSummary,
    Target.NameContentAttribute.Name,
    Target.NameContentAttribute.Value,
    Hint,
    definition);
            }
        }
     }

    public class MetadataMissingItemValidationError : AbstractMetadataValidationError
    {
        public MetadataMissingItemValidationError(MetadataDefinition definition, IEventAggregator eventAggregator) : 
            base(definition, eventAggregator)
        {
        }

        public override string Message
        {
            get
            {
                return string.Format(Tobi_Plugin_Validator_Metadata_Lang.Missing, Definition.Name.ToLower()); 
            }
        }

        public override string CompleteSummary
        {
            get
            {
                string definition = MetadataUtilities.GetDefinitionSummary(Definition);
                string name = "";
                if (Definition != null) name = Definition.Name;

                return string.Format(Tobi_Plugin_Validator_Metadata_Lang.MissingItemErrorCompleteSummary, name, definition);
            }
        }
    }

    public class MetadataDuplicateItemValidationError : AbstractMetadataValidationErrorWithTarget
    {
        public MetadataDuplicateItemValidationError(urakawa.metadata.Metadata target, MetadataDefinition definition, IEventAggregator eventAggregator) : 
            base(target, definition, eventAggregator)
        {
        }

        public override string Message
        {
            get
            {
                return string.Format(Tobi_Plugin_Validator_Metadata_Lang.DuplicateNotAllowed, Definition.Name.ToLower());
            }
        }

        public override string CompleteSummary
        {
            get
            {
                string definition = MetadataUtilities.GetDefinitionSummary(Definition);
                return string.Format(Tobi_Plugin_Validator_Metadata_Lang.DuplicateErrorCompleteSummary, definition);
            }
        }
    }

    public class MetadataUtilities
    {
        public static string RepeatableToString(bool value)
        {
            return value ? Tobi_Plugin_Validator_Metadata_Lang.MetadataMayBeRepeated : Tobi_Plugin_Validator_Metadata_Lang.MetadataMayNotBeRepeated;
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
                return Tobi_Plugin_Validator_Metadata_Lang.Metadata_Required;          
            if (item.Occurrence == MetadataOccurrence.Recommended)
                return Tobi_Plugin_Validator_Metadata_Lang.Metadata_Recommended;        
            return Tobi_Plugin_Validator_Metadata_Lang.Metadata_Optional;               
        }
        public static string GetDefinitionSummary(MetadataDefinition definition)
        {
            string defSummary = string.Format(Tobi_Plugin_Validator_Metadata_Lang.MetadataDefinitionSummary,
                                               definition.Name,
                                               definition.Description,
                                               DataTypeToString(definition.DataType),
                                               OccurrenceToString(definition),
                                               RepeatableToString(definition.IsRepeatable));

            if (definition.Synonyms != null && definition.Synonyms.Count > 0)
            {
                string synonyms = string.Join(",", definition.Synonyms.ToArray());
                defSummary += string.Format(Tobi_Plugin_Validator_Metadata_Lang.MetadataSynonyms, synonyms);
            }
            return defSummary;

        }
    }

    public class LaunchMetadataEditorEvent : CompositePresentationEvent<ValidationItem>
    {
        public static ThreadOption THREAD_OPTION = ThreadOption.PublisherThread;
    }
}
