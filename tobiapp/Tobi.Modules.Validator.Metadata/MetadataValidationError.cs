using System;
using System.ComponentModel.Composition;
using System.Windows;
using Tobi.Common;
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
                        string name = Definition.Name;
                        if (string.IsNullOrEmpty(name))
                            name = Target.NameContentAttribute.Name;
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
        /*
         * relative to description:
         * <Binding StringFormat="Rules for {0}" Path="Name"/>  <!-- TODO LOCALIZE MetadataDefinition_RulesForName -->
          <Binding StringFormat="Must be a {0}" Path="DataType" Converter="{Metadata:DataTypeToStringConverter}"/>   <!-- TODO LOCALIZE MetadataDefinition_MustBeDatatype -->
          <Binding StringFormat="Is {0}" Converter="{Metadata:OccurrenceDescriptionConverter}"/>  <!-- TODO LOCALIZE MetadataDefinition_Occurrance--> 
          <Binding Path="IsRepeatable" Converter="{Metadata:IsRepeatableToStringConverter}"/>
*/
        public override string CompleteSummary
        {
            get
            {
                if (Definition != null)
                {
                    string definition = string.Format("Rules for {0}:\nMust be a {1}\nIs {2}\n{3}",
                        Definition.Name, 
                        DataTypeToString(Definition.DataType),
                        OccurrenceToString(Definition),
                        RepeatableToString(Definition.IsRepeatable));

                    return string.Format("{0}\n{1}", Message, definition);
                }
                
                return Message;
                
            }
        }

        public override void TakeAction()
        {
            //this message is just for testing
            MessageBox.Show("This should open the metadadata editor");
            //really, what we want is to open the metadata pane:
            //CommandShowMetadataPane.Execute();
            //but first, we need to get to that command
        }

        public MetadataValidationError(MetadataDefinition definition)
        {
            Definition = definition;
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

}
