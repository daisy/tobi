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
            List<Metadata> list = obj.GetPresentation(0).ListOfMetadata;
            //unhook up the events
            foreach (Metadata metadata in list)
            {
                metadata.NameChanged -= new System.EventHandler<NameChangedEventArgs>(OnNameChanged);
                metadata.ContentChanged -= new System.EventHandler<ContentChangedEventArgs>(OnContentChanged);
            }
            OnProjectLoaded(null);
        }

        private void OnProjectLoaded(Project project)
        {
            Logger.Log("MetadataPaneViewModel.OnProjectLoaded" + (project == null ? "(null)" : ""), Category.Debug, Priority.Medium);

            //var shell = Container.Resolve<IShellPresenter>();
            //shell.DocumentProject

            Project = project;

            project.GetPresentation(0).MetadataAdded += new System.EventHandler<MetadataAddedEventArgs>
                (this.OnMetadataAdded);
            project.GetPresentation(0).MetadataDeleted += new System.EventHandler<MetadataDeletedEventArgs>
                (this.OnMetadataDeleted);
            if (project != null)
            {
                List<Metadata> list = Project.GetPresentation(0).ListOfMetadata;
                //hook up the events
                foreach (Metadata metadata in list)
                {
                    metadata.NameChanged += new System.EventHandler<NameChangedEventArgs>(OnNameChanged);
                    metadata.ContentChanged += new System.EventHandler<ContentChangedEventArgs>(OnContentChanged);
                }
            }
        }

        
        #endregion Initialization

        #region Commands

        public RichDelegateCommand<object> CommandShowMetadataPane { get; private set; }

        private void initializeCommands()
        {
            Logger.Log("MetadataPaneViewModel.initializeCommands", Category.Debug, Priority.Medium);

            var shellPresenter = Container.Resolve<IShellPresenter>();
            //
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

        //public ObservableCollection<Metadata> ObservableMetadatas;
        
        [NotifyDependsOn("Project")]
        public ObservableCollection<Metadata> Metadatas 
        {
            get
            {
                if (Project == null || Project.NumberOfPresentations <= 0)
                {
                    return null;
                }
                //let's waste some memory...
                return new ObservableCollection<Metadata>(Project.GetPresentation(0).ListOfMetadata);
            }
        }

        public void OnMetadataDeleted(object sender, MetadataDeletedEventArgs eventArgs)
        {
            eventArgs.DeletedMetadata.NameChanged -= new System.EventHandler<NameChangedEventArgs>(OnNameChanged);
            eventArgs.DeletedMetadata.ContentChanged -= new System.EventHandler<ContentChangedEventArgs>(OnContentChanged);
        }

        public void OnMetadataAdded(object sender, MetadataAddedEventArgs eventArgs)
        {
            eventArgs.AddedMetadata.NameChanged += new System.EventHandler<NameChangedEventArgs>(OnNameChanged);
            eventArgs.AddedMetadata.ContentChanged += new System.EventHandler<ContentChangedEventArgs>(OnContentChanged);
        }

        void OnContentChanged(object sender, ContentChangedEventArgs e)
        {
            throw new System.NotImplementedException("Dear user: sorry!");   
        }

        void OnNameChanged(object sender, NameChangedEventArgs e)
        {
            throw new System.NotImplementedException("Dear user: sorry!");
        }
    }
}
