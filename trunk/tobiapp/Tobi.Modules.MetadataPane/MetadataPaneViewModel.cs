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
            OnProjectLoaded(null);
        }

        private void OnProjectLoaded(Project project)
        {
            Logger.Log("MetadataPaneViewModel.OnProjectLoaded" + (project == null ? "(null)" : ""), Category.Debug, Priority.Medium);

            //var shell = Container.Resolve<IShellPresenter>();
            //shell.DocumentProject

            Project = project;
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
                obj => showMetadata(), obj => true);

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
                                                   true, 500, 600);
            windowPopup.Show();
        }

        #endregion Commands

        [NotifyDependsOn("Project")]
        public List<Metadata> Metadatas
        {
            get
            {
                if (Project == null)
                {
                    return null;
                }

                return Project.GetPresentation(0).ListOfMetadata;
            }
        }
    }
}
