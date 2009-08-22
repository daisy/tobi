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
using urakawa.commands;
using urakawa.command;
using urakawa.undo;

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
                obj => { this.ShowDialog(); },
                obj => { return this.CanShowDialog();});

            shellPresenter.RegisterRichCommand(CommandShowMetadataPane);
        }

        bool CanShowDialog()
        {
            var session = Container.Resolve<IUrakawaSession>();
            return session.DocumentProject != null && session.DocumentProject.Presentations.Count > 0;
        }
        
        void ShowDialog()
        {
            Logger.Log("MetadataPaneViewModel.showMetadata", Category.Debug, Priority.Medium);
            
            var shellPresenter_ = Container.Resolve<IShellPresenter>();
            var windowPopup = new PopupModalWindow(shellPresenter_,
                                                   UserInterfaceStrings.EscapeMnemonic(
                                                       UserInterfaceStrings.ShowMetadata),
                                                   View,
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 700, 500);
            windowPopup.Closed += new System.EventHandler(OnDialogClosed);
            //start a transaction
            var session = Container.Resolve<IUrakawaSession>();
            session.DocumentProject.Presentations.Get(0).UndoRedoManager.StartTransaction
                ("Open metadata editor", "The metadata editor modal dialog is opening.");
   
            windowPopup.ShowModal();
                    
        }
        void OnDialogClosed(object sender, System.EventArgs e)
        {
            ((PopupModalWindow)sender).Closed -= new System.EventHandler(OnDialogClosed);
            //end the transaction
            var session = Container.Resolve<IUrakawaSession>();
            session.DocumentProject.Presentations.Get(0).UndoRedoManager.EndTransaction();
        }

        
        #endregion Commands
        
      
        public ObservableCollection<MetadataValidationError> ValidationErrors
        {
            get
            {
                ObservableCollection<MetadataValidationError> errors =
                    new ObservableCollection<MetadataValidationError>(m_Validator.Errors);
                return errors;
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
                        Presentation presentation = session.DocumentProject.Presentations.Get(0);
            
                        m_Metadatas = new ObservableMetadataCollection
                            (presentation.Metadatas.ContentsAs_ListCopy,
                            SupportedMetadata_Z39862005.MetadataList,
                            presentation);
                        presentation.Metadatas.ObjectAdded += m_Metadatas.OnMetadataAdded;
                        presentation.Metadatas.ObjectRemoved += m_Metadatas.OnMetadataDeleted;
                    }
                }
                return m_Metadatas;
            }
        }

        public void RemoveMetadata(NotifyingMetadataItem metadata)
        {
            var session = Container.Resolve<IUrakawaSession>();
            Presentation presentation = session.DocumentProject.Presentations.Get(0);
            MetadataRemoveCommand cmd = presentation.CommandFactory.CreateMetadataRemoveCommand
                (metadata.UrakawaMetadata, presentation);
            presentation.UndoRedoManager.Execute(cmd);
        }

        public void AddEmptyMetadata()
        {
            Metadata metadata = new Metadata {Name = "", Content = ""};
            var session = Container.Resolve<IUrakawaSession>();
            Presentation presentation = session.DocumentProject.Presentations.Get(0);
            MetadataAddCommand cmd = presentation.CommandFactory.CreateMetadataAddCommand
                (metadata, presentation);
            presentation.UndoRedoManager.Execute(cmd);
        }

        
        /// <summary>
        /// validate all metadata
        /// </summary>
        public bool ValidateMetadata()
        {
            ValidationErrors.Clear();
            var session = Container.Resolve<IUrakawaSession>();

            List<Metadata> metadatas = session.DocumentProject.Presentations.Get(0).Metadatas.ContentsAs_ListCopy;
            bool result = m_Validator.Validate(metadatas);
            RaisePropertyChanged(() => ValidationErrors);
            
            return result;
        }

        
        public string GetViewModelDebugStringForMetaData()
        {
            string data = "";
            
            //iterate through our observable collection
            foreach (NotifyingMetadataItem m in this.Metadatas)
            {
                data += string.Format("{0} = {1}\n", m.Name, m.Content);
            }
            return data;
        }

        public string GetDataModelDebugStringForMetaData()
        {
            string data = "";
            var session = Container.Resolve<IUrakawaSession>();
            List<Metadata> list = session.DocumentProject.Presentations.Get(0).Metadatas.ContentsAs_ListCopy;
            //iterate through the SDK metadata
            foreach (Metadata m in list)
            {
                data += string.Format("{0} = {1}\n", m.Name, m.Content);
            }
            return data;
        }

        private NotifyingMetadataItem m_SelectedMetadata;
        public NotifyingMetadataItem SelectedMetadata
        {
            get
            {
                return m_SelectedMetadata;
            }
            set
            {
                if (m_SelectedMetadata != value)
                {
                    m_SelectedMetadata = value;
                    RaisePropertyChanged(() => SelectedMetadata);
                }
            }
        }
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
    
}
