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
using System.Windows.Controls;
using System.Windows.Data;

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
        private MetadataValidation m_Validator;

        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        public MetadataPaneViewModel(IUnityContainer container, IEventAggregator eventAggregator, ILoggerFacade logger) : base(container)
        {
            EventAggregator = eventAggregator;
            Logger = logger;
            m_Metadatas = null;
            Initialize();
            m_Validator = new MetadataValidation(SupportedMetadata_Z39862005.MetadataList);
            m_ValidationErrors = new ObservableCollection<string>();
            m_Validator.ValidationErrorEvent += new MetadataValidation.ValidationError(OnValidationError);
        }
        ~MetadataPaneViewModel()
        {
            m_Validator.ValidationErrorEvent -= new MetadataValidation.ValidationError(OnValidationError);
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
       }

        private void OnProjectUnLoaded(Project obj)
        {
            OnProjectLoaded(null);
        }

        private void OnProjectLoaded(Project project)
        {
            Logger.Log("MetadataPaneViewModel.OnProjectLoaded" + (project == null ? "(null)" : ""), 
                Category.Debug, Priority.Medium);

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
        
      
        private string m_StatusText;
        public string StatusText
        {
            get
            {
                if (ValidationErrors.Count > 0)
                    return "Invalid metadata";
                else if (((MetadataPaneView)View).SelectedMetadata != null)
                    return ((MetadataPaneView)View).SelectedMetadataDescription;
                else
                    return "Ready";
            }
            set
            {
                m_StatusText = value;
                OnPropertyChanged(() => StatusText);
            }
        }
        
        
        private ObservableCollection<string> m_ValidationErrors;
        public ObservableCollection<string> ValidationErrors
        {
            get
            {
                return m_ValidationErrors;
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
            m_Validator.ValidateItem(metadata.UrakawaMetadata);
        }

        /// <summary>
        /// validate all metadata
        /// </summary>
        public void ValidateMetadata()
        {
            m_ValidationErrors.Clear();
            var session = Container.Resolve<IUrakawaSession>();

            List<Metadata> metadatas = session.DocumentProject.Presentations.Get(0).Metadatas.ContentsAs_ListCopy;

            m_Validator.Validate(metadatas);
        }
        public void OnValidationError(MetadataValidationReportItem item)
        {
            string errorDescription = null;
            if (item.Metadata != null)
                errorDescription = string.Format("{0}: {1}", item.Metadata.Name, item.Description);
            else
                errorDescription = string.Format("{0}", item.Description);
            m_ValidationErrors.Add(errorDescription);
            OnPropertyChanged(() => ValidationErrors);
        }
#endregion validation

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

    public class MetadataValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            BindingGroup bindingGroup = (BindingGroup)value;
            NotifyingMetadataItem metadata = bindingGroup.Items[0] as NotifyingMetadataItem;
            MetadataValidation validator = new MetadataValidation(SupportedMetadata_Z39862005.MetadataList);
            bool result = validator.ValidateItem(metadata.UrakawaMetadata);

            if (result)
            {
                return ValidationResult.ValidResult;
            }
            else
            {
                MetadataValidationReportItem item = null;
                if (validator.Report.Count > 0)
                {
                    item = validator.Report[validator.Report.Count - 1];
                }
                return new ValidationResult(false, item.Description);
            }
                
        }
    }


        
    
    
}
