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
            m_MetadataCollection = null;
            Initialize();
            m_Validator = new MetadataValidator(SupportedMetadata_Z39862005.MetadataDefinitions);
        }
        
        #endregion Construction

        #region Initialization

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

            RaisePropertyChanged(() => MetadataCollection);
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
                obj => ShowDialog(),
                obj => CanShowDialog());

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
                                                   Container.Resolve<IMetadataPaneView>(),
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 700, 400);
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
        
      
        private MetadataCollection m_MetadataCollection;   
        public MetadataCollection MetadataCollection 
        {
            get
            {
                var session = Container.Resolve<IUrakawaSession>();

                if (session.DocumentProject == null || session.DocumentProject.Presentations.Count <= 0)
                {
                    m_MetadataCollection = null;
                }
                else
                {
                    if (m_MetadataCollection == null)
                    {
                        Presentation presentation = session.DocumentProject.Presentations.Get(0);
            
                        m_MetadataCollection = new MetadataCollection
                            (presentation.Metadatas.ContentsAs_ListCopy,
                            SupportedMetadata_Z39862005.MetadataDefinitions);
                        presentation.Metadatas.ObjectAdded += m_MetadataCollection.OnMetadataAdded;
                        presentation.Metadatas.ObjectRemoved += m_MetadataCollection.OnMetadataDeleted;
                    }
                }
                return m_MetadataCollection;
            }
        }
        
        public void RemoveMetadata(NotifyingMetadataItem metadata)
        {
            var session = Container.Resolve<IUrakawaSession>();
            Presentation presentation = session.DocumentProject.Presentations.Get(0);
            MetadataRemoveCommand cmd = presentation.CommandFactory.CreateMetadataRemoveCommand
                (metadata.UrakawaMetadata);
            presentation.UndoRedoManager.Execute(cmd);
        }

        public void AddEmptyMetadata()
        {
            var session = Container.Resolve<IUrakawaSession>();
            Presentation presentation = session.DocumentProject.Presentations.Get(0);
            
            Metadata metadata = presentation.MetadataFactory.CreateMetadata();
            metadata.NameContentAttribute = new MetadataAttribute { Name = "", NamespaceUri = "", Value = "" };
            MetadataAddCommand cmd = presentation.CommandFactory.CreateMetadataAddCommand
                (metadata);
            presentation.UndoRedoManager.Execute(cmd);
        }

        
        /// <summary>
        /// validate all metadata
        /// </summary>
        public bool ValidateMetadata()
        {
            return MetadataCollection.Validate();
           
        }

        
        public string GetViewModelDebugStringForMetaData()
        {
            string data = "";
            
            //iterate through our observable collection
            foreach (NotifyingMetadataItem m in this.MetadataCollection.Metadatas)
            {
                data += string.Format("{0} = {1}\n", m.Name, m.Content);

                foreach (var optAttr in m.UrakawaMetadata.OtherAttributes.ContentsAs_YieldEnumerable)
                {
                    data += string.Format("-- {0} = {1} (NS: {2})\n", optAttr.Name, optAttr.Value, optAttr.NamespaceUri);
                }
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
                data += string.Format("{0} = {1}\n", m.NameContentAttribute.Name, m.NameContentAttribute.Value);

                foreach (var optAttr in m.OtherAttributes.ContentsAs_YieldEnumerable)
                {
                    data += string.Format("-- {0} = {1} (NS: {2})\n", optAttr.Name, optAttr.Value, optAttr.NamespaceUri);
                }
            }
            return data;
        }

        /// <summary>
        /// based on the existing metadata, return a list of metadata fields available
        /// for addition
        /// </summary>
        public ObservableCollection<string> AvailableMetadataNames
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
                    MetadataAvailability.GetAvailableMetadata(metadatas, SupportedMetadata_Z39862005.MetadataDefinitions);


                foreach (MetadataDefinition metadata in availableMetadata)
                {
                    if (!metadata.IsReadOnly) 
                        list.Add(metadata.Name.ToLower());
                }
             
                return list;
            }
        }

        internal void SelectionChanged()
        {
            RaisePropertyChanged(() => AvailableMetadataNames);
        }
    }
}
