﻿using System.Collections.Generic;
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

            m_Logger.Log("MetadataValidator.OnUndoRedoManagerChanged", Category.Debug, Priority.Medium);

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
            //    Debug.Assert(eventt is DoneEventArgs || eventt is TransactionEndedEventArgs);
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
            foreach (var cmd in comp.ChildCommands.ContentsAs_YieldEnumerable)
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
            get { return Tobi_Plugin_Validator_Metadata_Lang.MetadataValidator_Name; }       // TODO LOCALIZE MetadataValidator_Name
        }

        public override string Description
        {
            get { return Tobi_Plugin_Validator_Metadata_Lang.MetadataValidator_Description; }       // TODO LOCALIZE MetadataValidator_Description
        }

        public override bool Validate()
        {
            bool isValid = true;

            if (m_Session.DocumentProject != null)
            {
                List<urakawa.metadata.Metadata> metadatas =
                    m_Session.DocumentProject.Presentations.Get(0).Metadatas.ContentsAs_ListCopy;

                resetToValid();
                isValid = _validate(metadatas);
            }

            return isValid;
        }

        //TODO: un-hardcode this
        public MetadataDefinitionSet MetadataDefinitions =
            SupportedMetadata_Z39862005.DefinitionSet;

        private MetadataDataTypeValidator m_DataTypeValidator;
        private MetadataOccurrenceValidator m_OccurrenceValidator;


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

        
        internal void ReportError(MetadataValidationError error)
        {
            //prevent duplicate errors: check that the items aren't identical
            //and check that the definitions don't match and the error types don't match
            //theoretically, there should only be one error type per definition (e.g. format, duplicate, etc)

            ValidationItem valItem = null;

            foreach (var e in ValidationItems)
            {
                MetadataValidationError err = e as MetadataValidationError;
                if (err == null) continue;

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

                if (sameItem || (sameDef && sameType && error.Target == null))
                {
                    valItem = err;
                    break; // && err.ErrorType != MetadataErrorType.MissingItemError
                }
            }

            if (valItem == null)
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
                        MetadataValidationError err = new MetadataValidationError(metadataDefinition, m_EventAggregator);
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
                        MetadataValidationError err = new MetadataValidationError(metadataDefinition, m_EventAggregator);
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
            MetadataValidationError err = new MetadataValidationError(definition, m_EventAggregator);
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
                MetadataValidationError err = new MetadataValidationError(definition, m_EventAggregator);
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
                MetadataValidationError err = new MetadataValidationError(definition, m_EventAggregator);
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
                MetadataValidationError err = new MetadataValidationError(definition, m_EventAggregator);
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
                MetadataValidationError err = new MetadataValidationError(definition, m_EventAggregator);
                err.ErrorType = MetadataErrorType.FormatError;
                err.Hint = m_NonEmptyHint;
                err.Target = metadata;

                m_ParentValidator.ReportError(err);
                return false;
            }
        }
    }
}