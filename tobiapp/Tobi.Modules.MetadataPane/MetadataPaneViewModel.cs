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
using System.Windows;

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
        private MetadataValidator m_Validator;
        
        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        public MetadataPaneViewModel(IUnityContainer container, IEventAggregator eventAggregator, ILoggerFacade logger) : base(container)
        {
            EventAggregator = eventAggregator;
            Logger = logger;
            m_Metadatas = null;
            Initialize();
            m_Validator = new MetadataValidator(SupportedMetadata_Z39862005.MetadataList);
            ValidationErrors = new ObservableCollection<string>();
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

       }

        private void OnProjectUnLoaded(Project obj)
        {
            OnProjectLoaded(null);
        }

        private void OnProjectLoaded(Project project)
        {
            Logger.Log("MetadataPaneViewModel.OnProjectLoaded" + (project == null ? "(null)" : ""), 
                Category.Debug, Priority.Medium);

            RaisePropertyChanged(() => Metadatas);
        }

        
        #endregion Initialization

        #region Commands

        public RichDelegateCommand<object> CommandShowMetadataPane { get; private set; }
        
        private void initializeCommands()
        {
            Logger.Log("MetadataPaneViewModel.initializeCommands", Category.Debug, Priority.Medium);

            var shellPresenter = Container.Resolve<IShellPresenter>();
            
            CommandShowMetadataPane = new RichDelegateCommand<object>(
                UserInterfaceStrings.ShowMetadata,
                UserInterfaceStrings.ShowMetadata_,
                UserInterfaceStrings.ShowMetadata_KEYS,
                shellPresenter.LoadTangoIcon("accessories-text-editor"),
                obj =>
                {
                    Logger.Log("MetadataPaneViewModel.showMetadata", Category.Debug, Priority.Medium);

                    var shellPresenter_ = Container.Resolve<IShellPresenter>();

                    var windowPopup = new PopupModalWindow(shellPresenter_,
                                                           UserInterfaceStrings.EscapeMnemonic(
                                                               UserInterfaceStrings.ShowMetadata),
                                                           View,
                                                           PopupModalWindow.DialogButtonsSet.OkCancel,
                                                           PopupModalWindow.DialogButton.Ok,
                                                           true, 500, 500);
                    windowPopup.ShowModal();
                },
                obj =>
                {
                    var session = Container.Resolve<IUrakawaSession>();
                    return session.DocumentProject != null && session.DocumentProject.Presentations.Count > 0;
                });

            shellPresenter.RegisterRichCommand(CommandShowMetadataPane);
        }

        #endregion Commands
        
      
        public ObservableCollection<string> ValidationErrors {get; set;}

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
                        m_Metadatas = new ObservableMetadataCollection
                            (session.DocumentProject.Presentations.Get(0).Metadatas.ContentsAs_ListCopy,
                            SupportedMetadata_Z39862005.MetadataList);
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


        #region validation

        /// <summary>
        /// validate a single item
        /// </summary>
        /// <param name="metadata"></param>
        public void ValidateMetadata(NotifyingMetadataItem metadata)
        {
            if (m_Validator.ValidateItem(metadata.UrakawaMetadata) == false)
            {
                if (m_Validator.Errors.Count > 0)
                {
                    //get the last-recorded error
                    MetadataValidationError metadataError = m_Validator.Errors[m_Validator.Errors.Count - 1];
                    string errorDescription = null;
                    errorDescription = string.Format
                            ("{0}: {1}", metadataError.Definition.Name, metadataError.Description);
                    ValidationErrors.Add(errorDescription);
                    RaisePropertyChanged(() => ValidationErrors);
                }
            }
        }

        /// <summary>
        /// validate all metadata
        /// </summary>
        public void ValidateMetadata()
        {
            ValidationErrors.Clear();
            var session = Container.Resolve<IUrakawaSession>();

            List<Metadata> metadatas = session.DocumentProject.Presentations.Get(0).Metadatas.ContentsAs_ListCopy;
            m_Validator.Validate(metadatas);

            foreach (MetadataValidationError metadataError in m_Validator.Errors)
            {
                string errorDescription = null;
                errorDescription = string.Format
                        ("{0}: {1}", metadataError.Definition.Name, metadataError.Description);
                ValidationErrors.Add(errorDescription);
                RaisePropertyChanged(() => ValidationErrors);
            }
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

        public NotifyingMetadataItem SelectedMetadata { get; set;}
        /// <summary>
        /// based on the existing metadata, return a list of metadata fields available
        /// for addition
        /// </summary>
        public ObservableCollection<string> AvailableMetadata
        {
            get
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
                
                //the available metadata list might not have our selection in it
                //if the selection is meant not to be duplicated
                //we need users to be able to have the current Name as an option
                if (SelectedMetadata != null)
                {
                    NotifyingMetadataItem selection = (NotifyingMetadataItem)SelectedMetadata;
                    if (selection.Name != "")
                    {
                        if (list.Contains(selection.Name) == false)
                            list.Insert(0, selection.Name);
                    }
                }
                return list;
            }
        }
        
    }

    public class MetadataValidationRule : ValidationRule
    {
        
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            MetadataValidator validator = new MetadataValidator(SupportedMetadata_Z39862005.MetadataList);
            BindingGroup bindingGroup = (BindingGroup) value;
            //sometimes the binding group is empty
            if (bindingGroup.Items.Count == 0)
            {
                return ValidationResult.ValidResult;
            }

            NotifyingMetadataItem metadata = bindingGroup.Items[0] as NotifyingMetadataItem;
            bool result = validator.ValidateItem(metadata.UrakawaMetadata);

            if (result)
            {
                return ValidationResult.ValidResult;
            }
            else
            {
                MetadataValidationError item = null;
                if (validator.Errors.Count > 0)
                {
                    item = validator.Errors[validator.Errors.Count - 1];
                }
                return new ValidationResult(false, item.Description);
            }
                
        }
    }
    
}
