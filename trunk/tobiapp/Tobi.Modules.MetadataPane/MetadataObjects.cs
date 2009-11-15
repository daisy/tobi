using Tobi.Common.MVVM;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using urakawa;
using urakawa.events;
using urakawa.metadata;
using urakawa.metadata.daisy;
using urakawa.commands;
using Tobi.Common;
using Tobi.Modules.Validator.Metadata;

namespace Tobi.Modules.MetadataPane
{
    // NotifyingMetadataItem is a wrapper around basic urakawa.Metadata
    // it has a MetadataDefinition, it validates itself
    // and it raises PropertyChanged notifications
    public class NotifyingMetadataItem : PropertyChangedNotifyBase
    {
        
        public MetadataDefinition Definition
        {
            get {return SupportedMetadata_Z39862005.DefinitionSet.GetMetadataDefinition(this.Name, true);}
        }
       
        public Metadata UrakawaMetadata { get; private set; }
        public MetadataCollection ParentCollection{get; private set;}
        //copy constructor
        public NotifyingMetadataItem(NotifyingMetadataItem notifyingMetadataItem):
            this(notifyingMetadataItem.UrakawaMetadata, notifyingMetadataItem.ParentCollection)
        {   
        }
        public NotifyingMetadataItem(Metadata metadata, MetadataCollection parentCollection)
        {
            UrakawaMetadata = metadata;
            UrakawaMetadata.Changed += new System.EventHandler<DataModelChangedEventArgs>(OnMetadataChangedChanged);
            ParentCollection = parentCollection;
            Validate();
        }

        private bool m_IsValid;
        public bool IsValid
        {
            get
            {
                return m_IsValid;
            }
            private set
            {
                if (m_IsValid != value)
                {
                    m_IsValid = value;
                    RaisePropertyChanged(() => IsValid);
                }
            }
        }
        public bool Validate()
        {
            ValidationError = null;
            //validate everything to get missing/duplicate errors sorted out
            ParentCollection.Validate();
            IsValid = ParentCollection.Validate(this);
            if (!IsValid)
            {
                foreach (MetadataValidationError error in ParentCollection.ValidationErrors)
                {
                    if (error.ErrorType == MetadataErrorType.FormatError &&
                        error.Target == this.UrakawaMetadata)
                    {
                        ValidationError = error;
                        break;
                    }
                }
            }
            return IsValid;    
        }

        private MetadataValidationError m_ValidationError;
        public MetadataValidationError ValidationError 
        { 
            get
            {
                return m_ValidationError;
            }
            private set
            {
                if (m_ValidationError != value)
                {
                    m_ValidationError = value;
                    RaisePropertyChanged(() => ValidationError);
                }
            }
        }

        ~NotifyingMetadataItem()
        {
            RemoveEvents();
        }
        public string Content
        {
            get
            {
                return UrakawaMetadata.NameContentAttribute.Value;
            }
            set
            {
                if (value == null) return;
                MetadataSetContentCommand cmd = 
                    UrakawaMetadata.Presentation.CommandFactory.CreateMetadataSetContentCommand
                    (UrakawaMetadata, value);
                UrakawaMetadata.Presentation.UndoRedoManager.Execute(cmd);
                Validate();
            }
        }
        public bool IsRequired
        {
            get
            {
                if (Definition != null)
                    return Definition.Occurrence == MetadataOccurrence.Required;
                else
                    return false;
            }
        }

        public string Name
        {
            get
            {
                return UrakawaMetadata.NameContentAttribute.Name;
            }
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                MetadataSetNameCommand cmd =
                    UrakawaMetadata.Presentation.CommandFactory.CreateMetadataSetNameCommand
                    (UrakawaMetadata, value);
                UrakawaMetadata.Presentation.UndoRedoManager.Execute(cmd);

                //when you change the name, you can't be sure that it's the primary identifier anymore
                IsPrimaryIdentifier = false;
                Validate();
            }
        }

        void OnMetadataChangedChanged(object sender, DataModelChangedEventArgs e)
        {
            //e actually is MetadataEventArgs
            RaisePropertyChanged(() => Name);
            RaisePropertyChanged(() => Content);
            RaisePropertyChanged(() => Definition);
            RaisePropertyChanged(() => IsRequired);
            RaisePropertyChanged(() => IsPrimaryIdentifier);
            Validate();
        }

        internal void RemoveEvents()
        {
            UrakawaMetadata.Changed -= new System.EventHandler<DataModelChangedEventArgs>(OnMetadataChangedChanged);
        }

        public bool IsPrimaryIdentifier
        {
            get
            {
                return UrakawaMetadata.IsMarkedAsPrimaryIdentifier;
            }
            set
            {
                if (value == UrakawaMetadata.IsMarkedAsPrimaryIdentifier) return;

                MetadataSetIdCommand cmd =
                    UrakawaMetadata.Presentation.CommandFactory.CreateMetadataSetIdCommand(UrakawaMetadata, value);
                UrakawaMetadata.Presentation.UndoRedoManager.Execute(cmd);

                RaisePropertyChanged(() => IsPrimaryIdentifier);
            }
        }
    }

    public class MetadataCollection : PropertyChangedNotifyBase
    {
        private ObservableCollection<NotifyingMetadataItem> m_Metadatas;
        public ObservableCollection<NotifyingMetadataItem> Metadatas
        {
            get
            {
                return m_Metadatas;
            }
            set
            {
                if (m_Metadatas != value)
                {
                    m_Metadatas = value;
                    Validate();
                    RaisePropertyChanged(() => Metadatas);
                }
            }
        }
        public List<MetadataDefinition> Definitions {get; private set;}
        private MetadataValidator m_Validator;
        
        public MetadataCollection(List<Metadata> metadatas, List<MetadataDefinition> definitions)
        {
            
            m_Validator = new MetadataValidator(null); 
            
            m_Metadatas = new ObservableCollection<NotifyingMetadataItem>();
            foreach (Metadata metadata in metadatas)
            {
                addItem(metadata);
            }
        }

        public bool Validate(NotifyingMetadataItem metadata)
        {
            return Validate(metadata.UrakawaMetadata);
        }
        public bool Validate(Metadata metadata)
        {
            bool result = m_Validator.Validate(metadata);
            RaisePropertyChanged(() => ValidationErrors);
            return result;
        }
        //validate all metadata
        public bool Validate()
        {
            bool result = false;
            if (this.Metadatas.Count > 0)
            {
                Presentation presentation = this.Metadatas[0].UrakawaMetadata.Presentation;
                result = m_Validator.Validate(presentation.Metadatas.ContentsAs_ListCopy);
                RaisePropertyChanged(() => ValidationErrors);
            }
            return result;
        }

        public ObservableCollection<MetadataValidationError> ValidationErrors
        {
            get
            {
                ObservableCollection<MetadataValidationError> errors =
                    new ObservableCollection<MetadataValidationError>(m_Validator.ValidationItems);
                return errors;
            }
        }



        #region sdk-events
        public void OnMetadataDeleted(object sender, ObjectRemovedEventArgs<Metadata> ev)
        {
            foreach (NotifyingMetadataItem metadata in this.Metadatas)
            {
                if (metadata.UrakawaMetadata == ev.m_RemovedObject)
                {
                    //reflect the removal in this observable collection
                    this.Metadatas.Remove(metadata);
                    metadata.RemoveEvents();
                    break;
                }
            }
        }

        public void OnMetadataAdded(object sender, ObjectAddedEventArgs<Metadata> ev)
        {
            //reflect the addition in this observable collection                    
            addItem(ev.m_AddedObject);
        }
        #endregion sdk-events

        // all new item additions end up here
        private void addItem(Metadata metadata)
        {
            MetadataDefinition definition =
                m_Validator.MetadataDefinitions.GetMetadataDefinition(
                    metadata.NameContentAttribute.Name.ToLower(), true);

            //filter out read-only items because they will be filled in by Tobi at export time
            if (definition.IsReadOnly == false)
            {
                NotifyingMetadataItem newItem = new NotifyingMetadataItem(metadata, this);
                newItem.BindPropertyChangedToAction(()=> newItem.IsPrimaryIdentifier, 
                    ()=> notifyOfPrimaryIdentifierChange(newItem));
                m_Metadatas.Add(newItem);
            }

        }
        //when a new metadata object assumes the role of primary identifier,
        //set IsPrimaryIdentifier to false on all other metadata objects
        private void notifyOfPrimaryIdentifierChange(NotifyingMetadataItem item)
        {
            if (item.IsPrimaryIdentifier)
            {
                foreach (NotifyingMetadataItem m in m_Metadatas)
                {
                    if (m != item && m.IsPrimaryIdentifier)
                    {
                        m.IsPrimaryIdentifier = false;
                    }
                }
            }
        }

        //find the item that is wrapping the given metadata object
        public NotifyingMetadataItem Find(Metadata metadata)
        {
            foreach (NotifyingMetadataItem metadataItem in Metadatas)
            {
                if (metadataItem.UrakawaMetadata == metadata)
                    return metadataItem;
            }
            return null;
        }

        public bool IsCandidateForPrimaryIdentifier(NotifyingMetadataItem item)
        {
            if (item == null) return false;
            //if this item has a definition (new items have nothing)
            if (item.Definition == null) return false;

            //it should have a dc:identifier definition even if it is actually one of the synonyms
            if (item.Definition.Name.ToLower() == "dc:identifier")
                return true;
            else
                return false;
        }
    }
}
