using System;
using System.Collections;
using System.Linq;
using Tobi.Common.MVVM;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using urakawa;
using urakawa.events;
using urakawa.metadata;
using urakawa.commands;
using urakawa.metadata.daisy;

namespace Tobi.Plugin.MetadataPane
{
    // NotifyingMetadataItem is a wrapper around basic urakawa.Metadata
    // it has a MetadataDefinition, it validates itself
    // and it raises PropertyChanged notifications
    public class NotifyingMetadataItem : PropertyChangedNotifyBase
    {
        public MetadataDefinition Definition { get; set;}
        public Metadata UrakawaMetadata { get; private set; }
        public MetadataCollection ParentCollection{get; private set;}
        private bool m_CanEditOrDelete;
        public bool CanEditOrDelete
        {
            get
            {
                m_CanEditOrDelete = ParentCollection.CanEditOrDelete(this);
                return m_CanEditOrDelete;
            }
            set
            {
                m_CanEditOrDelete = value;
            }
        }
        public NotifyingMetadataItem(Metadata metadata, MetadataCollection parentCollection,
            MetadataDefinition definition)
        {
            UrakawaMetadata = metadata;
            UrakawaMetadata.Changed += OnMetadataChanged;
            ParentCollection = parentCollection;
            Definition = definition;
            ParentCollection.Changed += OnParentCollectionChanged;
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
            }
        }
        public bool IsRequired
        {
            get
            {
                if (Definition != null)
                    return Definition.Occurrence == MetadataOccurrence.Required;
               
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

                //the definition changes when the name changes
                Definition = SupportedMetadata_Z39862005.DefinitionSet.GetMetadataDefinition(Name, true);
                //when you change the name, you can't be sure that it's the primary identifier anymore
                IsPrimaryIdentifier = false;
            }
        }

        void OnMetadataChanged(object sender, DataModelChangedEventArgs e)
        {
            //e actually is MetadataEventArgs
            RaisePropertyChanged(() => Name);
            RaisePropertyChanged(() => Content);
            RaisePropertyChanged(() => Definition);
            RaisePropertyChanged(() => IsRequired);
            RaisePropertyChanged(() => IsPrimaryIdentifier);
            RaisePropertyChanged(() => CanEditOrDelete);
        }
        void OnParentCollectionChanged(object sender, EventArgs e)
        {
            RaisePropertyChanged(() => CanEditOrDelete);
        }
        internal void RemoveEvents()
        {
            UrakawaMetadata.Changed -= OnMetadataChanged;
            ParentCollection.Changed -= OnParentCollectionChanged;
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
                    RaisePropertyChanged(() => Metadatas);
                }
            }
        }

        //indicates that the data model changed in some way (add/delete/edit)
        public event EventHandler<EventArgs> Changed;
        protected void NotifyChanged(EventArgs args)
        {
            EventHandler<EventArgs> d = Changed;
            if (d != null) d(this, args);
        }


        public MetadataCollection(IEnumerable<Metadata> metadatas) //, List<MetadataDefinition> definitions)
        {
            m_Metadatas = new ObservableCollection<NotifyingMetadataItem>();
            foreach (Metadata metadata in metadatas)
            {
                addItem(metadata);
            }
        }

        //analyzes the whole collection and decides if the given item can be edited or deleted
        public bool CanEditOrDelete(NotifyingMetadataItem item)
        {
            if (!item.IsRequired) return true;
            if (item.Definition == null) return true;

            int count = Metadatas.Count(meta => meta.Definition == item.Definition);

            if (count > 1) return true;

            return false;
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
                    metadata.UrakawaMetadata.Changed -= new EventHandler<DataModelChangedEventArgs>(UrakawaMetadata_Changed);
                    NotifyChanged(new EventArgs());
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
                SupportedMetadata_Z39862005.DefinitionSet.GetMetadataDefinition(metadata.NameContentAttribute.Name, true);
            //filter out read-only items because they will be filled in by Tobi at export time
            if (!definition.IsReadOnly)
            {
                NotifyingMetadataItem newItem = new NotifyingMetadataItem(metadata, this, definition);
                newItem.BindPropertyChangedToAction(()=> newItem.IsPrimaryIdentifier, 
                    ()=> notifyOfPrimaryIdentifierChange(newItem));
                m_Metadatas.Add(newItem);
                newItem.UrakawaMetadata.Changed += new EventHandler<DataModelChangedEventArgs>(UrakawaMetadata_Changed);
                NotifyChanged(new EventArgs());
            }
        }
        
        //need to track any change in the collection, since it affects the CanEditOrDelete property of NotifyingMetadataItem
        void UrakawaMetadata_Changed(object sender, DataModelChangedEventArgs e)
        {
            NotifyChanged(new EventArgs());
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
            
            return false;
        }
    }
}
