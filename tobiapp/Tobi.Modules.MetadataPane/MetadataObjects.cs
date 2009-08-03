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
            m_Metadata.NameChanged += new System.EventHandler<NameChangedEventArgs>(this.OnNameChanged);
            m_Metadata.ContentChanged += new System.EventHandler<ContentChangedEventArgs>(this.OnContentChanged);
        }
        public NotifyingMetadataItem(Metadata metadata)
        {
            m_Metadata = metadata;
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
                    OnPropertyChanged(() => Content);
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
                    OnPropertyChanged(() => Name);
                    
                }
            }
        }

        void OnContentChanged(object sender, ContentChangedEventArgs e)
        {
            OnPropertyChanged(() => Content);
        }

        void OnNameChanged(object sender, NameChangedEventArgs e)
        {
            OnPropertyChanged(() => Name);
        }

        internal void RemoveEvents()
        {
            m_Metadata.NameChanged -= new System.EventHandler<NameChangedEventArgs>(OnNameChanged);
            m_Metadata.ContentChanged -= new System.EventHandler<ContentChangedEventArgs>(OnContentChanged);
        }
    }


    public class ObservableMetadataCollection : ObservableCollection<NotifyingMetadataItem>
    {
        public ObservableMetadataCollection(List<Metadata> metadatas)
        {
            foreach (Metadata metadata in metadatas)
            {
                this.Add(new NotifyingMetadataItem(metadata));
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
            this.Add(new NotifyingMetadataItem(ev.m_AddedObject));
        }
        #endregion sdk-events

    }
}
