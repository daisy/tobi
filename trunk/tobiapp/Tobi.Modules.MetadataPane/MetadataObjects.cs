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
       
        public MetadataDefinition Definition
        {
            get
            {
                return SupportedMetadata_Z39862005.GetMetadataDefinition(this.Name, true);
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
        public NotifyingMetadataItem(NotifyingMetadataItem notifyingMetadataItem):
            this(notifyingMetadataItem.UrakawaMetadata)
        {   
        }
        public NotifyingMetadataItem(Metadata metadata)
        {
            m_Metadata = metadata;
            m_Metadata.NameChanged += new System.EventHandler<NameChangedEventArgs>(this.OnNameChanged);
            m_Metadata.ContentChanged += new System.EventHandler<ContentChangedEventArgs>(this.OnContentChanged);
            Validate();
        }

        private bool m_IsValid;
        public bool IsValid
        {
            get
            {
                return m_IsValid;
            } 
            set
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
            this.Add(new NotifyingMetadataItem(metadata));       
        }
    }
}
