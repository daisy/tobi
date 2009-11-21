using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Events;
using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.Validation;
using Tobi.Plugin.Validator.Metadata;
using urakawa;
using urakawa.metadata;
using urakawa.metadata.daisy;
using urakawa.commands;

namespace Tobi.Plugin.MetadataPane
{
    /// <summary>
    /// ViewModel for the MetadataPane
    /// </summary>
    [Export(typeof(MetadataPaneViewModel)), PartCreationPolicy(CreationPolicy.Shared)]
    public class MetadataPaneViewModel : ViewModelBase, IPartImportsSatisfiedNotification
    {
        public void OnImportsSatisfied()
        {
            //#if DEBUG
            //            Debugger.Break();
            //#endif

            foreach (var validator in Validators)
            {
                if (validator is MetadataValidator)
                {
                    m_LocalValidator = (MetadataValidator)validator;
                    m_LocalValidator.ValidatorStateRefreshed += OnValidatorStateRefreshed;
                    break;
                }
            }

            if (m_LocalValidator != null)
            {
                resetValidationItems(m_LocalValidator);
            }
        }

        [ImportingConstructor]
        public MetadataPaneViewModel(
            IUnityContainer container,
            IEventAggregator eventAggregator,
            ILoggerFacade logger,

            [ImportMany(typeof(IValidator), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = false)]
            IEnumerable<IValidator> validators
            )
            : base(container)
        {
            EventAggregator = eventAggregator;
            Logger = logger;

            Validators = validators;

            m_MetadataCollection = null;

            EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(OnProjectLoaded, ThreadOption.UIThread);
            EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(OnProjectUnLoaded, ThreadOption.UIThread);

            ValidationItems = new ObservableCollection<ValidationItem>();
        }

        #region Construction

        protected IEventAggregator EventAggregator { get; private set; }
        public ILoggerFacade Logger { get; private set; }

        public ObservableCollection<ValidationItem> ValidationItems { get; set; }

        public readonly IEnumerable<IValidator> Validators;

        private MetadataValidator m_LocalValidator;

        private void resetValidationItems(MetadataValidator metadataValidator)
        {
            ValidationItems.Clear();

            if (metadataValidator.ValidationItems == null) // metadataValidator.IsValid
            {
                return;
            }

            foreach (var validationItem in metadataValidator.ValidationItems)
            {
                //if (!((MetadataValidationError)validationItem).Definition.IsReadOnly)
                ValidationItems.Add(validationItem);
            }
        }

        private void OnValidatorStateRefreshed(object sender, ValidatorStateRefreshedEventArgs e)
        {
            resetValidationItems((MetadataValidator)e.Validator);
        }

        #endregion Construction

        private void OnProjectUnLoaded(Project obj)
        {
            OnProjectLoaded(null);
        }

        private void OnProjectLoaded(Project project)
        {
            Logger.Log("MetadataPaneViewModel.OnProject(UN)Loaded" + (project == null ? "(null)" : ""),
                Category.Debug, Priority.Medium);

            if (project == null)
            {
                m_MetadataCollection = null;
            }

            RaisePropertyChanged(() => MetadataCollection);
        }


        #region Commands


        //remove metadata entries with empty names
        public void removeEmptyMetadata()
        {
            var session = Container.Resolve<IUrakawaSession>();
            Presentation presentation = session.DocumentProject.Presentations.Get(0);
            List<MetadataRemoveCommand> removalsList = new List<MetadataRemoveCommand>();

            foreach (Metadata m in presentation.Metadatas.ContentsAs_YieldEnumerable)
            {
                if (string.IsNullOrEmpty(m.NameContentAttribute.Name))
                {
                    MetadataRemoveCommand cmd = presentation.CommandFactory.CreateMetadataRemoveCommand(m);
                    removalsList.Add(cmd);
                }
            }
            foreach (MetadataRemoveCommand cmd in removalsList)
            {
                presentation.UndoRedoManager.Execute(cmd);
            }
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
                            SupportedMetadata_Z39862005.DefinitionSet.Definitions);

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
                    MetadataAvailability.GetAvailableMetadata(metadatas, SupportedMetadata_Z39862005.DefinitionSet.Definitions);


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

            //TODO: what's up here ?
            //RaisePropertyChanged(() => MetadataCollection.Metadatas);
        }

        /*
         * SEE ValidationItemSelectedEvent
        //todo: scroll to the selected item
        public void OnValidationErrorSelected(ValidationErrorSelectedEventArgs e)
        {
            
        }
         * */
    }
}
