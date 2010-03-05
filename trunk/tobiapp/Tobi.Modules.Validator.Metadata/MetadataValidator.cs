using System.Collections.Generic;
using System.Windows;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.Validation;
using System.ComponentModel.Composition;
using urakawa.metadata.daisy;
using urakawa.metadata;
using System;

namespace Tobi.Plugin.Validator.Metadata
{   
    /// <summary>
    /// The main validator class
    /// </summary>
    [Export(typeof(IValidator))]
    [Export(typeof(MetadataValidator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class MetadataValidator : AbstractValidator, IPartImportsSatisfiedNotification
    {
#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied()
#pragma warning restore 1591
        {
            //#if DEBUG
            //            Debugger.Break();
            //#endif
        }

        private readonly ILoggerFacade m_Logger;
        protected readonly IUrakawaSession m_Session;
        private ResourceDictionary m_ValidationItemTemplate;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        ///<param name="session">normally obtained from the MEF composition container, it's a Tobi-specific service</param>
        [ImportingConstructor]
        public MetadataValidator(
            ILoggerFacade logger,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = false, AllowDefault = false)]
            IUrakawaSession session)
        {
            m_Logger = logger;
            m_Session = session;

            m_DataTypeValidator = new MetadataDataTypeValidator(this);
            m_OccurrenceValidator = new MetadataOccurrenceValidator(this);
            m_ValidationItems = new List<ValidationItem>();
            m_ValidationItemTemplate = new MetadataValidationItemTemplate();

            m_Logger.Log(@"MetadataValidator initialized", Category.Debug, Priority.Medium);
        }

        public override string Name
        {
            get { return "MetadataValidator";}       // TODO LOCALIZE MetadataValidator
        }

        public override string Description
        {
            get{ return "Validate publication metadata";}       // TODO LOCALIZE ValidatePublicationMetadata
        }

        public override bool Validate()
        {
            bool isValid = true;

            if (m_Session.DocumentProject != null &&
                m_Session.DocumentProject.Presentations.Count > 0)
            {
                List<urakawa.metadata.Metadata> metadatas =
                    m_Session.DocumentProject.Presentations.Get(0).Metadatas.ContentsAs_ListCopy;

                m_ValidationItems = new List<ValidationItem>();
                isValid = _validate(metadatas);
            }

            IsValid = isValid;
            return isValid;
        }

        public override IEnumerable<ValidationItem> ValidationItems
        {
            get { return m_ValidationItems;}
        }


        //TODO: un-hardcode this
        public MetadataDefinitionSet MetadataDefinitions = 
            SupportedMetadata_Z39862005.DefinitionSet;

        private MetadataDataTypeValidator m_DataTypeValidator;
        private MetadataOccurrenceValidator m_OccurrenceValidator;
        private List<ValidationItem> m_ValidationItems;
        
        private bool _validate(List<urakawa.metadata.Metadata> metadatas)
        {
            bool isValid = true;
            
            //validate each item by itself
            foreach (urakawa.metadata.Metadata metadata in metadatas)
            {
                if (!_validateItem(metadata))
                    isValid = false;
            }

            isValid = isValid & _validateAsSet(metadatas);

            return isValid;
        }

        //TODO: probably do not need this function anymore
        //validate a single item (do not look at the entire set - do not look for repetitions)
        public bool Validate(urakawa.metadata.Metadata metadata)
        {
            return _validateItem(metadata);
        }
        internal void ReportError(MetadataValidationError error)
        {
            //prevent duplicate errors: check that the items aren't identical
            //and check that the definitions don't match and the error types don't match
            //theoretically, there should only be one error type per definition (e.g. format, duplicate, etc)

            if (m_ValidationItems.Find
                (
                        delegate(ValidationItem e)
                        {
                            MetadataValidationError err = e as MetadataValidationError;
                            if (err == null) return false;

                            bool sameItem = false;
                            if (err.ErrorType == error.ErrorType)
                            {
                                //does this error's target metadata item already have an error
                                //of this type associated with it?
                                sameItem = err.Target != null && (err.Target == error.Target);
                            }
                            //does this error's type and target metadata definition already exist?
                            bool sameDef = (err.Definition == error.Definition);
                            bool sameType = (err.ErrorType == error.ErrorType);

                            if (sameItem || (sameDef && sameType)) return true; // && err.ErrorType != MetadataErrorType.MissingItemError
                            else return false;
                        }
                ) == null)
            {
                m_ValidationItems.Add(error);
            }
        }
        private bool _validateItem(urakawa.metadata.Metadata metadata)
        {
            MetadataDefinition metadataDefinition = 
                MetadataDefinitions.GetMetadataDefinition(metadata.NameContentAttribute.Name);
			
            if (metadataDefinition == null)
            {
                metadataDefinition = MetadataDefinitions.UnrecognizedItemFallbackDefinition;
            }
            
            //check the occurrence requirement
            bool meetsOccurrenceRequirement = m_OccurrenceValidator.Validate(metadata, metadataDefinition);
            //check the data type
            bool meetsDataTypeRequirement = m_DataTypeValidator.Validate(metadata, metadataDefinition);

            if (!(meetsOccurrenceRequirement & meetsDataTypeRequirement))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool _validateAsSet(List<urakawa.metadata.Metadata> metadatas)
        {
            bool isValid = true;
            //make sure all the required items are there
            foreach (MetadataDefinition metadataDefinition in MetadataDefinitions.Definitions)
            {
                if (!metadataDefinition.IsReadOnly && metadataDefinition.Occurrence == MetadataOccurrence.Required)
                {
                    urakawa.metadata.Metadata metadata = metadatas.Find(
                        item => item.NameContentAttribute.Name.ToLower() ==
                                metadataDefinition.Name.ToLower());

                    if (metadata == null)
                    {
                        MetadataValidationError err = new MetadataValidationError(metadataDefinition);
                        err.ErrorType = MetadataErrorType.MissingItemError;
                        ReportError(err);
                        isValid = false;
                    }
                }
            }

            //make sure repetitions are ok
            foreach (urakawa.metadata.Metadata metadata in metadatas)
            {
                MetadataDefinition metadataDefinition =
                    MetadataDefinitions.GetMetadataDefinition(metadata.NameContentAttribute.Name);

                if (metadataDefinition != null && !metadataDefinition.IsRepeatable)
                {
                    List<urakawa.metadata.Metadata> list = metadatas.FindAll(
                        item => item.NameContentAttribute.Name.ToLower() ==
                                metadata.NameContentAttribute.Name.ToLower());

                    if (list.Count > 1)
                    {
                        MetadataValidationError err = new MetadataValidationError(metadataDefinition);
                        err.ErrorType = MetadataErrorType.DuplicateItemError;
                        ReportError(err);
                        isValid = false;
                    }
                }
            }
            return isValid;
        }
    }

    public class MetadataDataTypeValidator
    {
        private MetadataValidator m_ParentValidator;
        //These hints describe what the data must be formatted as.
        //Complete sentences purposefully left out.
        private const string m_DateHint = "formatted as YYYY-MM-DD, YYYY-MM, or YYYY";             // TODO LOCALIZE Date
        private const string m_NumericHint = "a numeric value";                                    // TODO LOCALIZE NumVal

        public MetadataDataTypeValidator(MetadataValidator parentValidator)
        {
            m_ParentValidator = parentValidator;
        }
        public bool Validate(urakawa.metadata.Metadata metadata, MetadataDefinition definition)
        {
            switch (definition.DataType)
            {
                case MetadataDataType.ClockValue:
                    return _validateClockValue(metadata, definition);
                case MetadataDataType.Date:
                    return _validateDate(metadata, definition);
                case MetadataDataType.FileUri:
                    return _validateFileUri(metadata, definition);
                case MetadataDataType.Integer:
                    return _validateInteger(metadata, definition);
                case MetadataDataType.Double:
                    return _validateDouble(metadata, definition);
                case MetadataDataType.Number:
                    return _validateNumber(metadata, definition);
                case MetadataDataType.LanguageCode:
                    return _validateLanguageCode(metadata, definition);
                case MetadataDataType.String:
                    return _validateString(metadata, definition);
            }
            return true;
        }

        private bool _validateClockValue(urakawa.metadata.Metadata metadata, MetadataDefinition definition)
        {
            return true;
        }
        private bool _validateDate(urakawa.metadata.Metadata metadata, MetadataDefinition definition)
        {
            MetadataValidationError err = new MetadataValidationError(definition);
            err.ErrorType = MetadataErrorType.FormatError;
            err.Hint = m_DateHint;
            err.Target = metadata;

            string date = metadata.NameContentAttribute.Value;
            //Require at least the year field
            //The max length of the entire datestring is 10
            if (date.Length < 4 || date.Length > 10)
            {
                m_ParentValidator.ReportError(err);
                return false;
            }

            string[] dateArray = date.Split('-');

            //the year has to be 4 digits
            if (dateArray[0].Length != 4)
            {
                m_ParentValidator.ReportError(err);
                return false;
            }


            //the year has to be digits
            try
            {
                int year = Convert.ToInt32(dateArray[0]);
            }
            catch
            {
                m_ParentValidator.ReportError(err);
                return false;
            }

            //check for a month value (it's optional)
            if (dateArray.Length >= 2)
            {
                //the month has to be numeric
                int month = 0;
                try
                {
                    month = Convert.ToInt32(dateArray[1]);
                }
                catch
                {
                    m_ParentValidator.ReportError(err);
                    return false;
                }
                //the month has to be in this range
                if (month < 1 || month > 12)
                {
                    m_ParentValidator.ReportError(err);
                    return false;
                }
            }
            //check for a day value (it's optional but only if a month is specified)
            if (dateArray.Length == 3)
            {
                //the day has to be a number
                int day = 0;
                try
                {
                    day = Convert.ToInt32(dateArray[2]);
                }
                catch
                {
                    m_ParentValidator.ReportError(err);
                    return false;
                }
                //it has to be in this range
                if (day < 1 || day > 31)
                {
                    m_ParentValidator.ReportError(err);
                    return false;
                }
            }

            return true;
        }
        private bool _validateFileUri(urakawa.metadata.Metadata metadata, MetadataDefinition definition)
        {
            return true;
        }
        private bool _validateInteger(urakawa.metadata.Metadata metadata, MetadataDefinition definition)
        {
            try
            {
                int x = Convert.ToInt32(metadata.NameContentAttribute.Value);
            }
            catch (Exception)
            {
                MetadataValidationError err = new MetadataValidationError(definition);
                err.ErrorType = MetadataErrorType.FormatError;
                err.Hint = m_NumericHint;
                err.Target = metadata;

                m_ParentValidator.ReportError(err);
                return false;
            }
            return true;
        }
        private bool _validateDouble(urakawa.metadata.Metadata metadata, MetadataDefinition definition)
        {
            try
            {
                double x = Convert.ToDouble(metadata.NameContentAttribute.Value);
            }
            catch (Exception)
            {
                MetadataValidationError err = new MetadataValidationError(definition);
                err.ErrorType = MetadataErrorType.FormatError;
                err.Hint = m_NumericHint;
                err.Target = metadata;

                m_ParentValidator.ReportError(err);
                return false;
            }
            return true;
        }
        //works for both double and int
        private bool _validateNumber(urakawa.metadata.Metadata metadata, MetadataDefinition definition)
        {
            try
            {
                int x = Convert.ToInt32(metadata.NameContentAttribute.Value);
            }
            catch (Exception)
            {
                double x = Convert.ToDouble(metadata.NameContentAttribute.Value);
            }
            catch
            {
                MetadataValidationError err = new MetadataValidationError(definition);
                err.ErrorType = MetadataErrorType.FormatError;
                err.Hint = m_NumericHint;
                err.Target = metadata;

                m_ParentValidator.ReportError(err);
                return false;
            }
            return true;
        }
        private bool _validateLanguageCode(urakawa.metadata.Metadata metadata, MetadataDefinition definition)
        {
            return true;
        }
        private bool _validateString(urakawa.metadata.Metadata metadata, MetadataDefinition definition)
        {
            return true;
        }
    }

    public class MetadataOccurrenceValidator
    {
        private MetadataValidator m_ParentValidator;
        private const string m_NonEmptyHint = "non-empty";                               // TODO LOCALIZE NonEmpty
        public static string MagicStringEmpty { get { return "[EMPTY]";}}
        public MetadataOccurrenceValidator(MetadataValidator parentValidator)
        {
            m_ParentValidator = parentValidator;
        }

        public bool Validate(urakawa.metadata.Metadata metadata, MetadataDefinition definition)
        {
            //neither required nor optional fields may be empty
            //check both an empty string and our "magic" string value that is
            //used upon creation of a new metadata item
            if (!string.IsNullOrEmpty(metadata.NameContentAttribute.Value) && 
                    metadata.NameContentAttribute.Value != MagicStringEmpty)
            {
                return true;
            }
            else
            {
                MetadataValidationError err = new MetadataValidationError(definition);
                err.ErrorType = MetadataErrorType.FormatError;
                err.Hint = m_NonEmptyHint;
                err.Target = metadata;

                m_ParentValidator.ReportError(err);
                return false;
            }
        }
    }
}
