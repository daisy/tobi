using Tobi.Infrastructure;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using urakawa.metadata;
using urakawa.events.metadata;
using urakawa.events.presentation;
using urakawa.metadata.daisy;
using System;

namespace Tobi.Modules.MetadataPane
{
    public class NotifyingMetadataItem : PropertyChangedNotifyBase, IDataErrorInfo
    {
        private Metadata m_Metadata;
        public Metadata UrakawaMetadata
        {
            get
            {
                return m_Metadata;
            }
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
                    //we need the content to appear "changed" as well
                    //because changing the name changes the context under which the
                    //content is evaluated
                    OnPropertyChanged(() => Content);
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
            //we need the content to appear "changed" as well
            //because changing the name changes the context under which the
            //content is evaluated
            OnPropertyChanged(() => Content);
        }

        internal void RemoveEvents()
        {
            m_Metadata.NameChanged -= new System.EventHandler<NameChangedEventArgs>(OnNameChanged);
            m_Metadata.ContentChanged -= new System.EventHandler<ContentChangedEventArgs>(OnContentChanged);
        }




        #region IDataErrorInfo Members

        public string Error
        {
            get { throw new NotImplementedException(); }
        }

        public string this[string columnName]
        {
            get
            {
                string result = null;
                if (columnName == "Content" || columnName == "Name")
                {
                    MetadataValidation validator = new MetadataValidation(SupportedMetadata_Z39862005.MetadataList);
                    if (validator.ValidateItem(UrakawaMetadata) == false)
                    {
                        if (validator.Report.Count > 0)
                            result = validator.Report[0].Description;
                    }
                }

                return result;
            }
        }

        #endregion
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
        public void OnMetadataDeleted(object sender, MetadataDeletedEventArgs eventArgs)
        {
            foreach (NotifyingMetadataItem metadata in this)
            {
                if (metadata.Content == eventArgs.DeletedMetadata.Content &&
                    metadata.Name == eventArgs.DeletedMetadata.Name)
                {
                    this.Remove(metadata);
                    metadata.RemoveEvents();
                    break;
                }
            }
        }

        public void OnMetadataAdded(object sender, MetadataAddedEventArgs eventArgs)
        {
            this.Add(new NotifyingMetadataItem(eventArgs.AddedMetadata));
        }
        #endregion sdk-events

    }
}
