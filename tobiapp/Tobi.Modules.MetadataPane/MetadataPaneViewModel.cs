using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Events;
using Microsoft.Practices.Unity;
using Tobi.Infrastructure;
using Tobi.Infrastructure.Commanding;
using Tobi.Infrastructure.UI;
using urakawa;
using urakawa.metadata;
using urakawa.metadata.daisy;
using urakawa.events.presentation;



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

            StatusText = "everything is wonderful";

            RefreshDataTemplateSelectors();
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
            Logger.Log("MetadataPaneViewModel.OnProjectLoaded" + (project == null ? "(null)" : ""), 
                Category.Debug, Priority.Medium);
            Project = project;
            ContentTemplateSelectorProperty = new ContentTemplateSelector((MetadataPaneView)View);
            NameTemplateSelectorProperty = new NameTemplateSelector((MetadataPaneView)View);
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
                                                       UserInterfaceStrings.ShowMetadata),
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

        public void RefreshDataTemplateSelectors()
        {
            //ContentTemplateSelectorProperty = new ContentTemplateSelector((MetadataPaneView)View);
            NameTemplateSelectorProperty = new NameTemplateSelector((MetadataPaneView)View);
        }
        private string m_StatusText;
        public string StatusText
        {
            get
            {
                return m_StatusText;
            }
            set
            {
                m_StatusText = value;
                OnPropertyChanged(() => StatusText);
            }
        }

        private ObservableMetadataCollection m_Metadatas;
        
        [NotifyDependsOn("Project")]
        public ObservableMetadataCollection Metadatas 
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
                        m_Metadatas = new ObservableMetadataCollection(Project.GetPresentation(0).ListOfMetadata);
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
            if (metadata != null)
            {
                //metadata.Content = "Fake book about fake things";
                metadata.Name = "dtb:sourceDate";
            }
        }

        public void RemoveMetadata(NotifyingMetadataItem metadata)
        {
            //TODO: warn against or prevent removing required metadata
            Project.GetPresentation(0).DeleteMetadata(metadata.UrakawaMetadata);
        }

        public void AddEmptyMetadata()
        {
            Metadata metadata = new Metadata();
            metadata.Name = "";
            metadata.Content = "";
            Project.GetPresentation(0).AddMetadata(metadata);
        }

       

        public ObservableCollection<string> GetAvailableMetadata()
        {
            List<Metadata> metadatas = Project.GetPresentation(0).ListOfMetadata;
            List<MetadataDefinition> availableMetadata = 
                MetadataAvailability.GetAvailableMetadata(metadatas, SupportedMetadata_Z39862005.MetadataList);

            ObservableCollection<string> list = new ObservableCollection<string>();
            
            foreach (MetadataDefinition metadata in availableMetadata)
            {
                list.Add(metadata.Name);
            }
            return list;

        }

        #region validation

        /// <summary>
        /// validate a single item
        /// </summary>
        /// <param name="metadata"></param>
        public void ValidateMetadata(NotifyingMetadataItem metadata)
        {
            MetadataValidation validation = new MetadataValidation(SupportedMetadata_Z39862005.MetadataList);
            validation.ValidateItem(metadata.UrakawaMetadata);
        }
        /// <summary>
        /// validate all metadata
        /// </summary>
        public void ValidateMetadata()
        {
            List<string> errors = new List<string>();
            List<Metadata> metadatas = Project.GetPresentation(0).ListOfMetadata;

            MetadataValidation validation = new 
                MetadataValidation(SupportedMetadata_Z39862005.MetadataList);
            
            if (validation.Validate(metadatas) == false)
            {
                foreach (MetadataValidationReportItem item in validation.Report)
                {
                    errors.Add(item.Description);
                }
            }

            if (errors.Count > 0)
            {
                StatusText = string.Join("\n", errors.ToArray());
            }
            else
            {
                StatusText = "your metadata is great";
            }
                
        }
#endregion validation

        private NameTemplateSelector m_NameTemplateSelector = null;
        public NameTemplateSelector NameTemplateSelectorProperty
        {
            get
            {
                return m_NameTemplateSelector;
            }
            private set
            {
                m_NameTemplateSelector = value;
                OnPropertyChanged(() => NameTemplateSelectorProperty);
            }
        }

        private ContentTemplateSelector m_ContentTemplateSelector = null;
        public ContentTemplateSelector ContentTemplateSelectorProperty
        {
            get
            {
                return m_ContentTemplateSelector;
            }
            private set
            {
                m_ContentTemplateSelector = value;
                OnPropertyChanged(() => ContentTemplateSelectorProperty);
            }
        }
    }

        
    
    
}
