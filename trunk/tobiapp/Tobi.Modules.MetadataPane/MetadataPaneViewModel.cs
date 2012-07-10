using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.Validation;
using Tobi.Plugin.Validator.Metadata;
using urakawa;
using urakawa.metadata;
using urakawa.metadata.daisy;
using urakawa.commands;

namespace Tobi.Plugin.MetadataPane
{
    /// <summary>
    /// ViewModel for the MetadataPane
    /// </summary>
    [Export(typeof(MetadataPaneViewModel)), PartCreationPolicy(CreationPolicy.Shared)]
    public class MetadataPaneViewModel : ViewModelBase, IPartImportsSatisfiedNotification
    {
#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied()
#pragma warning restore 1591
        {
            //#if DEBUG
            //            Debugger.Break();
            //#endif

        }

        private readonly IEventAggregator m_EventAggregator;
        private readonly ILoggerFacade m_Logger;

        private readonly IUrakawaSession m_UrakawaSession;

        [ImportingConstructor]
        public MetadataPaneViewModel(
            IEventAggregator eventAggregator,
            ILoggerFacade logger,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession session,
            [Import(typeof(MetadataValidator), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = false)]
            MetadataValidator validator
            )
        {
            m_EventAggregator = eventAggregator;
            m_Logger = logger;

            m_Validator = validator;
            m_UrakawaSession = session;

            m_MetadataCollection = null;

            ValidationItems = new ObservableCollection<ValidationItem>();

            if (m_Validator != null)
            {
                m_Validator.ValidatorStateRefreshed += OnValidatorStateRefreshed;
                resetValidationItems(m_Validator);
            }

            m_EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(OnProjectLoaded, ProjectLoadedEvent.THREAD_OPTION);
            m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(OnProjectUnLoaded, ProjectUnLoadedEvent.THREAD_OPTION);
        }

        public ObservableCollection<ValidationItem> ValidationItems { get; set; }
        private MetadataValidator m_Validator;

        private void resetValidationItems(MetadataValidator metadataValidator)
        {
            ValidationItems.Clear();

            if (metadataValidator.ValidationItems == null) // metadataValidator.IsValid
            {
                return;
            }

            foreach (var validationItem in metadataValidator.ValidationItems)
            {
                //if (!((MetadataValidationError)validationItem).Definition.IsReadOnly)
                ValidationItems.Add(validationItem);
            }

            RaisePropertyChanged(() => ValidationItems);
        }

        private void OnValidatorStateRefreshed(object sender, ValidatorStateRefreshedEventArgs e)
        {
            resetValidationItems((MetadataValidator)e.Validator);
        }


        private void OnProjectUnLoaded(Project obj)
        {
            OnProjectLoaded(null);
        }

        private void OnProjectLoaded(Project project)
        {
            //if (m_UrakawaSession.IsXukSpine)
            //{
            //    return;
            //}

            m_Logger.Log("MetadataPaneViewModel.OnProject(UN)Loaded" + (project == null ? "(null)" : ""),
                Category.Debug, Priority.Medium);

            m_MetadataCollection = null;

            RaisePropertyChanged(() => MetadataCollection);
        }


        //remove metadata entries with empty names
        public void removeEmptyMetadata()
        {
            Presentation presentation = m_UrakawaSession.DocumentProject.Presentations.Get(0);
            List<MetadataRemoveCommand> removalsList = new List<MetadataRemoveCommand>();

            foreach (Metadata m in presentation.Metadatas.ContentsAs_Enumerable)
            {
                if (string.IsNullOrEmpty(m.NameContentAttribute.Name))
                {
                    MetadataRemoveCommand cmd = presentation.CommandFactory.CreateMetadataRemoveCommand(m);
                    removalsList.Add(cmd);
                }
            }
            foreach (MetadataRemoveCommand cmd in removalsList)
            {
                presentation.UndoRedoManager.Execute(cmd);
            }
        }


        private MetadataCollection m_MetadataCollection;
        public MetadataCollection MetadataCollection
        {
            get
            {
                if (m_UrakawaSession.DocumentProject == null)
                {
                    m_MetadataCollection = null;
                }
                else
                {
                    if (m_MetadataCollection == null)
                    {
                        Presentation presentation = m_UrakawaSession.DocumentProject.Presentations.Get(0);

                        m_MetadataCollection = new MetadataCollection(presentation.Metadatas.ContentsAs_Enumerable);
                        //SupportedMetadata_Z39862005.DefinitionSet.Definitions

                        presentation.Metadatas.ObjectAdded += m_MetadataCollection.OnMetadataAdded;
                        presentation.Metadatas.ObjectRemoved += m_MetadataCollection.OnMetadataDeleted;
                    }
                }

                return m_MetadataCollection;
            }
        }

        public void RemoveMetadata(NotifyingMetadataItem metadata)
        {
            Presentation presentation = m_UrakawaSession.DocumentProject.Presentations.Get(0);
            MetadataRemoveCommand cmd = presentation.CommandFactory.CreateMetadataRemoveCommand
                (metadata.UrakawaMetadata);
            presentation.UndoRedoManager.Execute(cmd);
        }

        public void AddEmptyMetadata()
        {
            Presentation presentation = m_UrakawaSession.DocumentProject.Presentations.Get(0);

            Metadata metadata = presentation.MetadataFactory.CreateMetadata();
            metadata.NameContentAttribute = new MetadataAttribute
            {
                Name = "",
                NamespaceUri = "",
                Value = SupportedMetadata_Z39862005.MagicStringEmpty
            };
            MetadataAddCommand cmd = presentation.CommandFactory.CreateMetadataAddCommand
                (metadata);
            presentation.UndoRedoManager.Execute(cmd);
        }

        public string GetViewModelDebugStringForMetaData()
        {
            string data = "";

            //iterate through our observable collection
            foreach (NotifyingMetadataItem m in this.MetadataCollection.Metadatas)
            {
                data += string.Format("{0} = {1}" + Environment.NewLine, m.Name, m.Content);

                foreach (var optAttr in m.UrakawaMetadata.OtherAttributes.ContentsAs_Enumerable)
                {
                    data += string.Format("-- {0} = {1} (NS: {2})" + Environment.NewLine, optAttr.Name, optAttr.Value, optAttr.NamespaceUri);
                }
            }
            return data;
        }

        public string GetDataModelDebugStringForMetaData()
        {
            string data = "";

            foreach (Metadata m in m_UrakawaSession.DocumentProject.Presentations.Get(0).Metadatas.ContentsAs_Enumerable)
            {
                data += string.Format("{0} = {1}" + Environment.NewLine, m.NameContentAttribute.Name, m.NameContentAttribute.Value);

                foreach (var optAttr in m.OtherAttributes.ContentsAs_Enumerable)
                {
                    data += string.Format("-- {0} = {1} (NS: {2})" + Environment.NewLine, optAttr.Name, optAttr.Value, optAttr.NamespaceUri);
                }
            }
            return data;
        }

        /// <summary>
        /// based on the existing metadata, return a list of metadata fields available
        /// for addition
        /// </summary>
        public ObservableCollection<string> AvailableMetadataNames
        {
            get
            {
                ObservableCollection<string> list = new ObservableCollection<string>();

                if (m_UrakawaSession.DocumentProject == null)
                {
                    return list;
                }

                List<MetadataDefinition> availableMetadata = new List<MetadataDefinition>();

                foreach (MetadataDefinition definition in SupportedMetadata_Z39862005.DefinitionSet.Definitions)
                {
                    //string name = definition.Name.ToLower();
                    bool exists = false;
                    foreach (Metadata item in m_UrakawaSession.DocumentProject.Presentations.Get(0).Metadatas.ContentsAs_Enumerable)
                    {
                        if (item.NameContentAttribute.Name.Equals(definition.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                    {
                        availableMetadata.Add(definition);
                    }
                    else
                    {
                        if (definition.IsRepeatable)
                        {
                            availableMetadata.Add(definition);
                        }
                    }
                }

                foreach (MetadataDefinition metadata in availableMetadata)
                {
                    if (!metadata.IsReadOnly)
                    {
                        list.Add(metadata.Name);
                    }
                }
                return list;
            }
        }

        internal void SelectionChanged()
        {
            RaisePropertyChanged(() => AvailableMetadataNames);
        }

        /*
         * SEE ValidationItemSelectedEvent
        //todo: scroll to the selected item
        public void OnValidationErrorSelected(ValidationErrorSelectedEventArgs e)
        {
            
        }
         * */
    }
}
