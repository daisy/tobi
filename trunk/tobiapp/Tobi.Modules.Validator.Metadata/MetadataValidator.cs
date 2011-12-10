using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.Validation;
using System.ComponentModel.Composition;
using urakawa;
using urakawa.command;
using urakawa.commands;
using urakawa.events.undo;
using urakawa.metadata.daisy;
using urakawa.metadata;
using System;

namespace Tobi.Plugin.Validator.Metadata
{
    /// <summary>
    /// The main validator class
    /// </summary>
    [Export(typeof(MetadataValidator)), PartCreationPolicy(CreationPolicy.Shared)]
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
            IEventAggregator eventAggregator,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = false, AllowDefault = false)]
            IUrakawaSession session)
            : base(eventAggregator)
        {
            m_Logger = logger;
            m_Session = session;

            m_DataTypeValidator = new MetadataDataTypeValidator(this, m_EventAggregator);
            m_OccurrenceValidator = new MetadataOccurrenceValidator(this, m_EventAggregator);
            m_ValidationItemTemplate = new MetadataValidationItemTemplate();

            m_Logger.Log(@"MetadataValidator initialized", Category.Debug, Priority.Medium);
        }

        protected override void OnProjectLoaded(Project project)
        {
            base.OnProjectLoaded(project);

            project.Presentations.Get(0).UndoRedoManager.CommandDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandReDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandUnDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.TransactionEnded += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.TransactionCancelled += OnUndoRedoManagerChanged;

            Validate();
        }

        protected override void OnProjectUnLoaded(Project project)
        {
            base.OnProjectUnLoaded(project);

            project.Presentations.Get(0).UndoRedoManager.CommandDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandReDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandUnDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.TransactionEnded -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.TransactionCancelled -= OnUndoRedoManagerChanged;
        }

        private void OnUndoRedoManagerChanged(object sender, UndoRedoManagerEventArgs eventt)
        {
            if (!Dispatcher.CurrentDispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Normal, (Action<object, UndoRedoManagerEventArgs>)OnUndoRedoManagerChanged, sender, eventt);
                return;
            }

            //m_Logger.Log("MetadataValidator.OnUndoRedoManagerChanged", Category.Debug, Priority.Medium);

            if (!(eventt is DoneEventArgs
                           || eventt is UnDoneEventArgs
                           || eventt is ReDoneEventArgs
                           || eventt is TransactionEndedEventArgs
                           || eventt is TransactionCancelledEventArgs
                           ))
            {
                Debug.Fail("This should never happen !!");
                return;
            }

            //if (m_Session.DocumentProject.Presentations.Get(0).UndoRedoManager.IsTransactionActive)
            //{
            //    DebugFix.Assert(eventt is DoneEventArgs || eventt is TransactionEndedEventArgs);
            //    m_Logger.Log("AudioContentValidator.OnUndoRedoManagerChanged (exit: ongoing TRANSACTION...)", Category.Debug, Priority.Medium);
            //    return;
            //}

            //bool done = eventt is DoneEventArgs || eventt is ReDoneEventArgs || eventt is TransactionEndedEventArgs;

            Command cmd = eventt.Command;

            if (cmd is CompositeCommand)
            {
                if (isAllCommandsMetadata((CompositeCommand)cmd))
                {
                    Validate();
                }
            }
            else if (isCommandMetadata(cmd))
            {
                Validate();
            }
        }

        private bool isAllCommandsMetadata(CompositeCommand comp)
        {
            foreach (var cmd in comp.ChildCommands.ContentsAs_Enumerable)
            {
                if (!isCommandMetadata(cmd))
                {
                    return false;
                }
            }
            return true;
        }

        private bool isCommandMetadata(Command cmd)
        {
            return cmd is MetadataAddCommand
                || cmd is MetadataRemoveCommand
                || cmd is MetadataSetContentCommand
                || cmd is MetadataSetIdCommand
                || cmd is MetadataSetNameCommand;
        }

        public override string Name
        {
            get { return Tobi_Plugin_Validator_Metadata_Lang.MetadataValidator_Name; }
        }

        public override string Description
        {
            get { return Tobi_Plugin_Validator_Metadata_Lang.MetadataValidator_Description; }
        }

        public override bool Validate()
        {
            bool isValid = true;

            if (m_Session.DocumentProject != null)
            {
                resetToValid();
                isValid = _validate();
            }

            return isValid;
        }

        //TODO: un-hardcode this
        public MetadataDefinitionSet MetadataDefinitions =
            SupportedMetadata_Z39862005.DefinitionSet;

        private MetadataDataTypeValidator m_DataTypeValidator;
        private MetadataOccurrenceValidator m_OccurrenceValidator;


        private bool _validate() //IEnumerable<urakawa.metadata.Metadata> metadatas)
        {
            bool isValid = true;

            //validate each item by itself
            foreach (urakawa.metadata.Metadata metadata in m_Session.DocumentProject.Presentations.Get(0).Metadatas.ContentsAs_Enumerable)
            {
                if (!_validateItem(metadata))
                    isValid = false;
            }

            isValid = isValid & _validateAsSet(); //metadatas);

            return isValid;
        }


        internal void ReportError(ValidationItem error)
        {
            //prevent duplicate errors: check that the items aren't identical
            //and check that the definitions don't match and the error types don't match
            //theoretically, there should only be one error type per definition (e.g. format, duplicate, etc)

            bool foundDuplicate = false;

            foreach (ValidationItem e in ValidationItems)
            {
                if (e == null) continue;

                bool sameItem = false;
                bool sameDef = sameDef = (e as IMetadataValidationError).Definition == (error as IMetadataValidationError).Definition;
                bool sameType = e.GetType() == error.GetType();

                if (sameType)
                {
                    //does this error's target metadata item already have an error
                    //of this type associated with it?
                    if (e is AbstractMetadataValidationErrorWithTarget &&
                        error is AbstractMetadataValidationErrorWithTarget)
                    {
                        AbstractMetadataValidationErrorWithTarget eWithTarget =
                            e as AbstractMetadataValidationErrorWithTarget;
                        AbstractMetadataValidationErrorWithTarget errorWithTarget =
                            error as AbstractMetadataValidationErrorWithTarget;

                        if (sameType
                            &&
                            eWithTarget.Target == errorWithTarget.Target
                            &&
                            eWithTarget.Target != null)
                        {
                            sameItem = true;
                        }
                    }
                }
                if (error is AbstractMetadataValidationErrorWithTarget && sameItem == true)
                {
                    foundDuplicate = true;
                    break;
                }
                else if (!(error is AbstractMetadataValidationErrorWithTarget) && sameDef && sameType)
                {
                    foundDuplicate = true;
                    break;
                }
            }

            if (!foundDuplicate)
            {
                addValidationItem(error);
            }
        }

        private bool _validateItem(urakawa.metadata.Metadata metadata)
        {
            MetadataDefinition metadataDefinition =
                MetadataDefinitions.GetMetadataDefinition(metadata.NameContentAttribute.Name, true);

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

        private bool _validateAsSet() //IEnumerable<urakawa.metadata.Metadata> metadatas)
        {
            bool isValid = true;
            //make sure all the required items are there
            foreach (MetadataDefinition metadataDefinition in MetadataDefinitions.Definitions)
            {
                if (!metadataDefinition.IsReadOnly
                    && metadataDefinition.Occurrence == MetadataOccurrence.Required)
                {
                    string name = metadataDefinition.Name.ToLower();

                    bool found = false;
                    foreach (urakawa.metadata.Metadata item in m_Session.DocumentProject.Presentations.Get(0).Metadatas.ContentsAs_Enumerable)
                    {
                        if (item.NameContentAttribute.Name.ToLower() == name)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        MetadataMissingItemValidationError err =
                            new MetadataMissingItemValidationError(metadataDefinition, m_EventAggregator);
                        ReportError(err);
                        isValid = false;
                    }
                }
            }

            //make sure repetitions are ok

            List<urakawa.metadata.Metadata> list = new List<urakawa.metadata.Metadata>();
            foreach (urakawa.metadata.Metadata metadata in m_Session.DocumentProject.Presentations.Get(0).Metadatas.ContentsAs_Enumerable)
            {
                MetadataDefinition metadataDefinition =
                    MetadataDefinitions.GetMetadataDefinition(metadata.NameContentAttribute.Name);

                if (metadataDefinition != null && !metadataDefinition.IsRepeatable)
                {
                    string name = metadata.NameContentAttribute.Name.ToLower();
                    list.Clear();
                    foreach (urakawa.metadata.Metadata item in m_Session.DocumentProject.Presentations.Get(0).Metadatas.ContentsAs_Enumerable)
                    {
                        if (item.NameContentAttribute.Name.ToLower() == name)
                        {
                            list.Add(item);
                        }
                    }

                    if (list.Count > 1)
                    {
                        isValid = false;
                        foreach (urakawa.metadata.Metadata item in list)
                        {
                            MetadataDuplicateItemValidationError err =
                                new MetadataDuplicateItemValidationError(item, metadataDefinition, m_EventAggregator);
                            ReportError(err);
                        }
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
        private string m_DateHint = Tobi_Plugin_Validator_Metadata_Lang.DateHint;
        private string m_NumericHint = Tobi_Plugin_Validator_Metadata_Lang.NumericValueHint;
        private readonly IEventAggregator m_EventAggregator;

        public MetadataDataTypeValidator(MetadataValidator parentValidator, IEventAggregator eventAggregator)
        {
            m_ParentValidator = parentValidator;
            m_EventAggregator = eventAggregator;
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
            MetadataFormatValidationError err =
                new MetadataFormatValidationError(metadata, definition, m_EventAggregator);
            err.Hint = m_DateHint;

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
                MetadataFormatValidationError err =
                    new MetadataFormatValidationError(metadata, definition, m_EventAggregator);
                err.Hint = m_NumericHint;

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
                MetadataFormatValidationError err =
                    new MetadataFormatValidationError(metadata, definition, m_EventAggregator);
                err.Hint = m_NumericHint;

                m_ParentValidator.ReportError(err);
                return false;
            }
            return true;
        }
        //works for both double and int
        private bool _validateNumber(urakawa.metadata.Metadata metadata, MetadataDefinition definition)
        {
            bool res = _validateInteger(metadata, definition);
            if (!res) return false;

            res = _validateDouble(metadata, definition);
            if (!res) return false;

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

    //checks for non-empty values
    //we could probably move this into one of the other validators since it's pretty simple what it does
    public class MetadataOccurrenceValidator
    {
        private MetadataValidator m_ParentValidator;
        private string m_NonEmptyHint = Tobi_Plugin_Validator_Metadata_Lang.NonEmptyHint;
        private readonly IEventAggregator m_EventAggregator;

        public MetadataOccurrenceValidator(MetadataValidator parentValidator, IEventAggregator eventAggregator)
        {
            m_ParentValidator = parentValidator;
            m_EventAggregator = eventAggregator;
        }

        public bool Validate(urakawa.metadata.Metadata metadata, MetadataDefinition definition)
        {
            //neither required nor optional fields may be empty
            //check both an empty string and our "magic" string value that is
            //used upon creation of a new metadata item
            if (!string.IsNullOrEmpty(metadata.NameContentAttribute.Value) &&
                    metadata.NameContentAttribute.Value != SupportedMetadata_Z39862005.MagicStringEmpty)
            {
                return true;
            }
            else
            {
                MetadataFormatValidationError err =
                    new MetadataFormatValidationError(metadata, definition, m_EventAggregator);
                err.Hint = m_NonEmptyHint;

                m_ParentValidator.ReportError(err);
                return false;
            }
        }
    }
}
