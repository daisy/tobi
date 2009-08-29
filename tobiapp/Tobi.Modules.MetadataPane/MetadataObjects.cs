using Tobi.Common.MVVM;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using urakawa;
using urakawa.events;
using urakawa.metadata;
using urakawa.events.metadata;
using urakawa.metadata.daisy;
using urakawa.commands;

namespace Tobi.Modules.MetadataPane
{
    // NotifyingMetadataItem is a wrapper around basic urakawa.Metadata
    // it has a MetadataDefinition, it validates itself
    // and it raises PropertyChanged notifications
    public class NotifyingMetadataItem : PropertyChangedNotifyBase
    {
        
        public MetadataDefinition Definition
        {
            get {return SupportedMetadata_Z39862005.GetMetadataDefinition(this.Name, true);}
        }

        public Metadata UrakawaMetadata { get; private set; }
        public ObservableMetadataCollection ParentCollection{get; private set;}
        //copy constructor
        public NotifyingMetadataItem(NotifyingMetadataItem notifyingMetadataItem):
            this(notifyingMetadataItem.UrakawaMetadata, notifyingMetadataItem.ParentCollection)
        {   
        }
        public NotifyingMetadataItem(Metadata metadata, ObservableMetadataCollection parentCollection)
        {
            UrakawaMetadata = metadata;
            UrakawaMetadata.Changed += new System.EventHandler<DataModelChangedEventArgs>(OnMetadataChangedChanged);
            //UrakawaMetadata.NameChanged += new System.EventHandler<NameChangedEventArgs>(this.OnNameChanged);
            //UrakawaMetadata.ContentChanged += new System.EventHandler<ContentChangedEventArgs>(this.OnContentChanged);
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
            MetadataValidator validator = new MetadataValidator(SupportedMetadata_Z39862005.MetadataList);
            IsValid = validator.ValidateItem(UrakawaMetadata);
            if (IsValid == false && validator.Errors.Count > 0)
            {
                MetadataValidationError error = validator.Errors[validator.Errors.Count - 1];
                System.Diagnostics.Debug.Assert(error is MetadataValidationFormatError);
                ValidationError = (MetadataValidationFormatError) error;
            }
            else
            {
                ValidationError = null;
            }
            return IsValid;    
        }

        private MetadataValidationFormatError m_ValidationError;
        public MetadataValidationFormatError ValidationError 
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
                    ParentCollection.UrakawaPresentation.CommandFactory.CreateMetadataSetContentCommand
                    (UrakawaMetadata, value);
                ParentCollection.UrakawaPresentation.UndoRedoManager.Execute(cmd);
            }
        }

        public string Name
        {
            get
            {
                return UrakawaMetadata.NameContentAttribute.LocalName;
            }
            set
            {
                if (value == null) return;
                MetadataSetNameCommand cmd =
                    ParentCollection.UrakawaPresentation.CommandFactory.CreateMetadataSetNameCommand
                    (UrakawaMetadata, value);
                ParentCollection.UrakawaPresentation.UndoRedoManager.Execute(cmd);
            }
        }

        //void OnContentChanged(object sender, ContentChangedEventArgs e)
        //{
        //    RaisePropertyChanged(() => Content);
        //}

        //void OnNameChanged(object sender, NameChangedEventArgs e)
        //{
        //    RaisePropertyChanged(() => Name);
        //    RaisePropertyChanged(() => Definition);
        //}

        void OnMetadataChangedChanged(object sender, DataModelChangedEventArgs e)
        {
            //e actually is MetadataEventArgs
            RaisePropertyChanged(() => Name);
            RaisePropertyChanged(() => Content);
            RaisePropertyChanged(() => Definition);
        }

        internal void RemoveEvents()
        {
            UrakawaMetadata.Changed -= new System.EventHandler<DataModelChangedEventArgs>(OnMetadataChangedChanged);

            //UrakawaMetadata.NameChanged -= new System.EventHandler<NameChangedEventArgs>(OnNameChanged);
            //UrakawaMetadata.ContentChanged -= new System.EventHandler<ContentChangedEventArgs>(OnContentChanged);
        }
    }


    public class ObservableMetadataCollection : ObservableCollection<NotifyingMetadataItem>
    {
        public List<MetadataDefinition> Definitions {get; private set;}
        public Presentation UrakawaPresentation { get; private set; }

        //the Presentation is used by the NotifyingMetadataItem objects so they can use the UndoRedo manager.
        public ObservableMetadataCollection(List<Metadata> metadatas, List<MetadataDefinition> definitions,
            Presentation presentation)
        {
            Definitions = definitions;
            UrakawaPresentation = presentation;
            //build this observable collection from the source list of urakawa.Metadata objects
            foreach (Metadata metadata in metadatas)
            {
                addItem(metadata);
            }
        }

        #region sdk-events
        public void OnMetadataDeleted(object sender, ObjectRemovedEventArgs<Metadata> ev)
        {
            foreach (NotifyingMetadataItem metadata in this)
            {
                if (metadata.UrakawaMetadata == ev.m_RemovedObject)
                {
                    //reflect the removal in this observable collection
                    this.Remove(metadata);
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
        // it was useful during testing when more than one action was taken during an add, 
        // but perhaps now is redundant? will let it stay in for now.
        private void addItem(Metadata metadata)
        {
            this.Add(new NotifyingMetadataItem(metadata, this));
        }
    }
}
