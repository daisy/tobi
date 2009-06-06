using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Events;
using Microsoft.Practices.Unity;
using Tobi.Infrastructure;
using Tobi.Infrastructure.Commanding;
using Tobi.Infrastructure.UI;
using urakawa;
using urakawa.metadata;
using urakawa.events.presentation;
using urakawa.events.metadata;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Tobi.Modules.MetadataPane
{
    /// <summary>
    /// ViewModel for the MetadataPane
    /// </summary>
    public class MetadataPaneViewModel : ViewModelBase
    {
        #region Construction

        protected IUnityContainer Container { get; private set; }
        protected IEventAggregator EventAggregator { get; private set; }
        public ILoggerFacade Logger { get; private set; }
        
        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        public MetadataPaneViewModel(IUnityContainer container, IEventAggregator eventAggregator, ILoggerFacade logger)
        {
            Container = container;
            EventAggregator = eventAggregator;
            Logger = logger;
            m_Metadatas = null;
            Initialize();
        }

        #endregion Construction

        #region Initialization

        protected IMetadataPaneView View { get; private set; }
        public void SetView(IMetadataPaneView view)
        {
            View = view;
        }

        protected void Initialize()
        {
            Logger.Log("MetadataPaneViewModel.Initialize", Category.Debug, Priority.Medium);

            m_Project = null;

            initializeCommands();

            EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(OnProjectLoaded, ThreadOption.UIThread);
            EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(OnProjectUnLoaded, ThreadOption.UIThread);
        }

        private Project m_Project;
        public Project Project
        {
            get { return m_Project; }
            set
            {
                if (m_Project == value) return;
                m_Project = value;
                OnPropertyChanged(() => Project);
            }
        }

        private void OnProjectUnLoaded(Project obj)
        {
            OnProjectLoaded(null);
        }

        private void OnProjectLoaded(Project project)
        {
            Logger.Log("MetadataPaneViewModel.OnProjectLoaded" + (project == null ? "(null)" : ""), Category.Debug, Priority.Medium);
            Project = project;
        }

        
        #endregion Initialization

        #region Commands

        public RichDelegateCommand<object> CommandShowMetadataPane { get; private set; }

        private void initializeCommands()
        {
            Logger.Log("MetadataPaneViewModel.initializeCommands", Category.Debug, Priority.Medium);

            var shellPresenter = Container.Resolve<IShellPresenter>();
            
            CommandShowMetadataPane = new RichDelegateCommand<object>(UserInterfaceStrings.ShowMetadata,
                UserInterfaceStrings.ShowMetadata_,
                UserInterfaceStrings.ShowMetadata_KEYS,
                (VisualBrush)Application.Current.FindResource("accessories-text-editor"),
                obj => showMetadata(), obj => canShowMetadata);

            shellPresenter.RegisterRichCommand(CommandShowMetadataPane);
        }

        private void showMetadata()
        {
            Logger.Log("MetadataPaneViewModel.showMetadata", Category.Debug, Priority.Medium);

            var shellPresenter = Container.Resolve<IShellPresenter>();
            var window = shellPresenter.View as Window;

            var windowPopup = new PopupModalWindow(window ?? Application.Current.MainWindow,
                                                   UserInterfaceStrings.EscapeMnemonic(
                                                       UserInterfaceStrings.ShowMetadata_),
                                                   View,
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 500, 500);
            windowPopup.Show();
        }

        [NotifyDependsOn("Project")]
        private bool canShowMetadata
        {
            get { return Project != null && Project.NumberOfPresentations > 0; }
        }

        #endregion Commands

        private ObservableMetadata m_Metadatas;
        
        [NotifyDependsOn("Project")]
        public ObservableMetadata Metadatas 
        {
            get
            {
                if (Project == null || Project.NumberOfPresentations <= 0)
                {
                    m_Metadatas = null;
                }
                else
                {
                    if (m_Metadatas == null)
                    {
                        m_Metadatas = new ObservableMetadata(Project.GetPresentation(0).ListOfMetadata);
                        Project.GetPresentation(0).MetadataAdded += new System.EventHandler<MetadataAddedEventArgs>
                            (m_Metadatas.OnMetadataAdded);
                        Project.GetPresentation(0).MetadataDeleted += new System.EventHandler<MetadataDeletedEventArgs>
                            (m_Metadatas.OnMetadataDeleted);
                    }
                }
                return m_Metadatas;
            }
        }
        public void CreateFakeData()
        {
            List<Metadata> list = Project.GetPresentation(0).ListOfMetadata;
            Metadata metadata = list.Find(s => s.Name == "dc:Title");
            metadata.Content = "Fake book about fake things";
            
        }

        internal void RemoveMetadata(NotifyingMetadata metadata)
        {
            //TODO: warn against removing required metadata
            Project.GetPresentation(0).DeleteMetadata(metadata.UrakawaMetadata);
        }
    }

    public class NotifyingMetadata : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                try
                {
                    handler(this, e);
                }
                catch (InvalidOperationException ex)
                {
                    //swallow (some strange framework-raised first-chance exception)
                }
            }
        }

        private Metadata m_Metadata;
        public Metadata UrakawaMetadata
        {
            get
            {
                return m_Metadata;
            }
        }
        public NotifyingMetadata(Metadata metadata)
        {
            m_Metadata = metadata;
            m_Metadata.NameChanged += new System.EventHandler<NameChangedEventArgs>(this.OnNameChanged);
            m_Metadata.ContentChanged += new System.EventHandler<ContentChangedEventArgs>(this.OnContentChanged);
        }
        ~NotifyingMetadata()
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
                m_Metadata.Content = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Content"));
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
                m_Metadata.Name = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Name"));
            }
        }

        void OnContentChanged(object sender, ContentChangedEventArgs e)
        {
           OnPropertyChanged(new PropertyChangedEventArgs("Content"));
        }

        void OnNameChanged(object sender, NameChangedEventArgs e)
        {
            OnPropertyChanged(new PropertyChangedEventArgs("Name"));
        }

        internal void RemoveEvents()
        {
            m_Metadata.NameChanged -= new System.EventHandler<NameChangedEventArgs>(OnNameChanged);
            m_Metadata.ContentChanged -= new System.EventHandler<ContentChangedEventArgs>(OnContentChanged);
        }
    }

    public class ObservableMetadata : ObservableCollection<NotifyingMetadata>
    {
        public ObservableMetadata(List<Metadata> metadatas)
        {
            foreach (Metadata metadata in metadatas)
            {
                this.Add(new NotifyingMetadata(metadata));
            }
        }
        #region sdk-events
        public void OnMetadataDeleted(object sender, MetadataDeletedEventArgs eventArgs)
        {
            foreach (NotifyingMetadata metadata in this)
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
            this.Add(new NotifyingMetadata(eventArgs.AddedMetadata));
        }
        #endregion sdk-events

    }
}
