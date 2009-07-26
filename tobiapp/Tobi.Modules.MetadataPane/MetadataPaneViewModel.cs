using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Events;
using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa;
using urakawa.metadata;
using urakawa.metadata.daisy;


namespace Tobi.Modules.MetadataPane
{
    /// <summary>
    /// ViewModel for the MetadataPane
    /// </summary>
    public class MetadataPaneViewModel : ViewModelBase
    {
        #region Construction

        protected IEventAggregator EventAggregator { get; private set; }
        public ILoggerFacade Logger { get; private set; }
        
        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        public MetadataPaneViewModel(IUnityContainer container, IEventAggregator eventAggregator, ILoggerFacade logger) : base(container)
        {
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

            initializeCommands();

            EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(OnProjectLoaded, ThreadOption.UIThread);
            EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(OnProjectUnLoaded, ThreadOption.UIThread);

            StatusText = "everything is wonderful";

            //this call doesn't seem necessary here
            //RefreshDataTemplateSelectors();
       }

        private void OnProjectUnLoaded(Project obj)
        {
            OnProjectLoaded(null);
        }

        private void OnProjectLoaded(Project project)
        {
            Logger.Log("MetadataPaneViewModel.OnProjectLoaded" + (project == null ? "(null)" : ""), 
                Category.Debug, Priority.Medium);

            ContentTemplateSelectorProperty = (project == null ? null : new ContentTemplateSelector((MetadataPaneView)View));
            NameTemplateSelectorProperty = (project == null ? null : new NameTemplateSelector((MetadataPaneView)View));

            OnPropertyChanged(() => Metadatas);
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
                shellPresenter.LoadTangoIcon("accessories-text-editor"),
                obj => showMetadata(), obj => canShowMetadata);

            shellPresenter.RegisterRichCommand(CommandShowMetadataPane);
        }

        private void showMetadata()
        {
            Logger.Log("MetadataPaneViewModel.showMetadata", Category.Debug, Priority.Medium);

            var shellPresenter = Container.Resolve<IShellPresenter>();

            var windowPopup = new PopupModalWindow(shellPresenter,
                                                   UserInterfaceStrings.EscapeMnemonic(
                                                       UserInterfaceStrings.ShowMetadata),
                                                   View,
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 500, 500);
            windowPopup.ShowModal();
        }

        private bool canShowMetadata
        {
            get
            {
                var session = Container.Resolve<IUrakawaSession>();
                return session.DocumentProject != null && session.DocumentProject.Presentations.Count > 0;
            }
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
                return ((MetadataPaneView) View).SelectedMetadataDescription;
                //return m_StatusText;
            }
            set
            {
                m_StatusText = value;
                OnPropertyChanged(() => StatusText);
            }
        }

        private ObservableMetadataCollection m_Metadatas;
        
        public ObservableMetadataCollection Metadatas 
        {
            get
            {
                var session = Container.Resolve<IUrakawaSession>();

                if (session.DocumentProject == null || session.DocumentProject.Presentations.Count <= 0)
                {
                    m_Metadatas = null;
                }
                else
                {
                    if (m_Metadatas == null)
                    {
                        m_Metadatas = new ObservableMetadataCollection(session.DocumentProject.Presentations.Get(0).Metadatas.ContentsAs_ListCopy);
                        session.DocumentProject.Presentations.Get(0).Metadatas.ObjectAdded += m_Metadatas.OnMetadataAdded;
                        session.DocumentProject.Presentations.Get(0).Metadatas.ObjectRemoved += m_Metadatas.OnMetadataDeleted;
                    }
                }
                return m_Metadatas;
            }
        }


        public void CreateFakeData()
        {
            var session = Container.Resolve<IUrakawaSession>();

            List<Metadata> list = session.DocumentProject.Presentations.Get(0).Metadatas.ContentsAs_ListCopy;
            Metadata metadata = list.Find(s => s.Name == "dc:Title");
            if (metadata != null)
            {
                //metadata.Content = "Fake book about fake things";
                metadata.Name = "dtb:sourceDate";
            }
        }

        public void RemoveMetadata(NotifyingMetadataItem metadata)
        {
            var session = Container.Resolve<IUrakawaSession>();

            //TODO: warn against or prevent removing required metadata
            session.DocumentProject.Presentations.Get(0).Metadatas.Remove(metadata.UrakawaMetadata);
        }

        public void AddEmptyMetadata()
        {
            Metadata metadata = new Metadata {Name = "", Content = ""};

            var session = Container.Resolve<IUrakawaSession>();
            ObjectListProvider<Metadata> list = session.DocumentProject.Presentations.Get(0).Metadatas;
            list.Insert(list.Count, metadata);
        }

       

        public ObservableCollection<string> GetAvailableMetadata()
        {
            ObservableCollection<string> list = new ObservableCollection<string>();

            var session = Container.Resolve<IUrakawaSession>();
            if (session.DocumentProject == null)
            {
                return list;
            }

            List<Metadata> metadatas = session.DocumentProject.Presentations.Get(0).Metadatas.ContentsAs_ListCopy;
            List<MetadataDefinition> availableMetadata = 
                MetadataAvailability.GetAvailableMetadata(metadatas, SupportedMetadata_Z39862005.MetadataList);

            
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

            var session = Container.Resolve<IUrakawaSession>();

            List<Metadata> metadatas = session.DocumentProject.Presentations.Get(0).Metadatas.ContentsAs_ListCopy;

            MetadataValidation validation = new 
                MetadataValidation(SupportedMetadata_Z39862005.MetadataList);
            
            if (validation.Validate(metadatas) == false)
            {
                foreach (MetadataValidationReportItem item in validation.Report)
                {

                    string error_desc;
                    if (item.Metadata != null)
                    {
                        error_desc = string.Format("{0}:\n\t{1}={2}",
                                                   item.Description, item.Metadata.Name, item.Metadata.Content);
                    }
                    else
                    {
                        error_desc = item.Description;
                    }   
                    errors.Add(error_desc);
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

        public string GetDebugStringForMetaData()
        {
            string data = "";

            var session = Container.Resolve<IUrakawaSession>();

            foreach (Metadata m in session.DocumentProject.Presentations.Get(0).Metadatas.ContentsAs_ListCopy)
            {
                data += string.Format("{0} = {1}\n", m.Name, m.Content);
            }
            return data;
        }
    }

        
    
    
}
