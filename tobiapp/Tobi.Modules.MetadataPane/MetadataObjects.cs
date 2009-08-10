using Tobi.Common.MVVM;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using urakawa;
using urakawa.metadata;
using urakawa.events.metadata;
using urakawa.metadata.daisy;
using System;

namespace Tobi.Modules.MetadataPane
{   
    public class NotifyingMetadataItem : PropertyChangedNotifyBase
    {
        private Metadata m_Metadata;
        public ObservableMetadataCollection ParentCollection { get; private set; }

        public MetadataDefinition Definition
        {
            get
            {
                return ParentCollection.Definitions.Find(s => s.Name == this.Name);
            }
        }
        
        public Metadata UrakawaMetadata
        {
            get
            {
                return m_Metadata;
            }
        }
        //copy constructor
        public NotifyingMetadataItem(NotifyingMetadataItem notifyingMetadataItem)
        {
            m_Metadata = notifyingMetadataItem.UrakawaMetadata;
            ParentCollection = notifyingMetadataItem.ParentCollection;
            m_Metadata.NameChanged += new System.EventHandler<NameChangedEventArgs>(this.OnNameChanged);
            m_Metadata.ContentChanged += new System.EventHandler<ContentChangedEventArgs>(this.OnContentChanged);
        }
        public NotifyingMetadataItem(Metadata metadata, ObservableMetadataCollection parentCollection)
        {
            m_Metadata = metadata;
            ParentCollection = parentCollection;
            m_Metadata.NameChanged += new System.EventHandler<NameChangedEventArgs>(this.OnNameChanged);
            m_Metadata.ContentChanged += new System.EventHandler<ContentChangedEventArgs>(this.OnContentChanged);
        }
        ~NotifyingMetadataItem()
        {
            RemoveEvents();
        }
        public string Content
        {
            get
            {
                return m_Metadata.Content;
            }
            set
            {
                if (m_Metadata.Content == value) return;

                //we have to protect the Urakawa SDK from null metadata values ... 
                if (value != null)
                {
                    m_Metadata.Content = value;
                    RaisePropertyChanged(() => Content);
                }

            }
        }

        public string Name
        {
            get
            {
                return m_Metadata.Name;
            }
            set
            {
                if (m_Metadata.Name == value) return;
                //we have to protect the Urakawa SDK from null metadata values ... 
                if (value != null)
                {
                    m_Metadata.Name = value;
                    RaisePropertyChanged(() => Name);
                    RaisePropertyChanged(() => Definition);
                    
                }
            }
        }

        void OnContentChanged(object sender, ContentChangedEventArgs e)
        {
            RaisePropertyChanged(() => Content);
        }

        void OnNameChanged(object sender, NameChangedEventArgs e)
        {
            RaisePropertyChanged(() => Name);
            RaisePropertyChanged(() => Definition);
        }

        internal void RemoveEvents()
        {
            m_Metadata.NameChanged -= new System.EventHandler<NameChangedEventArgs>(OnNameChanged);
            m_Metadata.ContentChanged -= new System.EventHandler<ContentChangedEventArgs>(OnContentChanged);
        }
    }


    public class ObservableMetadataCollection : ObservableCollection<NotifyingMetadataItem>
    {
        private List<MetadataDefinition> m_Definitions;
        public List<MetadataDefinition> Definitions 
        { 
            get
            {
                return m_Definitions;
            }
        }
        public ObservableMetadataCollection(List<Metadata> metadatas, List<MetadataDefinition> definitions)
        {
            m_Definitions = definitions;
            foreach (Metadata metadata in metadatas)
            {
                _addItem(metadata);
            }
        }

        #region sdk-events
        public void OnMetadataDeleted(object sender, ObjectRemovedEventArgs<Metadata> ev)
        {
            foreach (NotifyingMetadataItem metadata in this)
            {
                if (metadata.Content == ev.m_RemovedObject.Content &&
                    metadata.Name == ev.m_RemovedObject.Name)
                {
                    this.Remove(metadata);
                    metadata.RemoveEvents();
                    break;
                }
            }
        }

        public void OnMetadataAdded(object sender, ObjectAddedEventArgs<Metadata> ev)
        {
            _addItem(ev.m_AddedObject);
        }
        #endregion sdk-events

        /// <summary>
        /// all new item additions end up here
        /// </summary>
        /// <param name="metadata"></param>
        private void _addItem(Metadata metadata)
        {
            this.Add(new NotifyingMetadataItem(metadata, this));       
        }
    }
}
