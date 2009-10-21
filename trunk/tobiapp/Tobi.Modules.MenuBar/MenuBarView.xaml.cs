using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using Tobi.Modules.MetadataPane;
using Tobi.Modules.AudioPane;

namespace Tobi.Modules.MenuBar
{
    /// <summary>
    /// Interaction logic for MenuBarView.xaml
    /// </summary>
    // [Export]
    public partial class MenuBarView // : IPartImportsSatisfiedNotification
    {
        //public void OnImportsSatisfied()
        //{
        //    Debugger.Break();
        //}

        //[Import(typeof(ILoggerFacade), AllowRecomposition = true, AllowDefault = false)]
        //protected ILoggerFacade LoggerFromMEF { get; set; }

        //[Import]
        //protected Lazy<ILoggerFacade> LoggerFromMEF { get; set; }

        //[ImportMany(typeof(ILoggerFacade), AllowRecomposition = true)]
        //public IEnumerable<ILoggerFacade> LoggersFromMEF { get; set; }


        public AudioPaneViewModel AudioPaneViewModel
        {
            get
            {
                var viewModel = Container.Resolve<AudioPaneViewModel>();

                return viewModel;
            }
        }

        public IInputBindingManager InputBindingManager
        {
            get
            {
                var shellPresenter = Container.Resolve<IShellPresenter>();

                return shellPresenter;
            }
        }

        protected IUnityContainer Container { get; private set; }
        protected IEventAggregator EventAggregator { get; private set; }
        protected ILoggerFacade Logger { get; private set; }

        public RichDelegateCommand ExitCommand { get; private set; }

        public RichDelegateCommand MagnifyUiIncreaseCommand { get; private set; }
        public RichDelegateCommand MagnifyUiDecreaseCommand { get; private set; }

        public RichDelegateCommand ManageShortcutsCommand { get; private set; }

        public RichDelegateCommand UndoCommand { get; private set; }
        public RichDelegateCommand RedoCommand { get; private set; }

        public RichDelegateCommand CopyCommand { get; private set; }
        public RichDelegateCommand CutCommand { get; private set; }
        public RichDelegateCommand PasteCommand { get; private set; }

        public RichDelegateCommand HelpCommand { get; private set; }
        public RichDelegateCommand PreferencesCommand { get; private set; }
        //public RichDelegateCommand WebHomeCommand { get; private set; }

        //public RichDelegateCommand NavNextCommand { get; private set; }
        //public RichDelegateCommand NavPreviousCommand { get; private set; }

        public RichDelegateCommand ExportCommand { get; private set; }

        public RichDelegateCommand SaveCommand { get; private set; }
        public RichDelegateCommand SaveAsCommand { get; private set; }

        //public RichDelegateCommand NewCommand { get; private set; }
        public RichDelegateCommand OpenCommand { get; private set; }
        public RichDelegateCommand CloseCommand { get; private set; }

        public RichDelegateCommand CommandShowMetadataPane { get; private set; }
        
        public RichDelegateCommand AudioCommandInsertFile { get; private set; }
        public RichDelegateCommand AudioCommandGotoBegining { get; private set; }
        public RichDelegateCommand AudioCommandGotoEnd { get; private set; }
        public RichDelegateCommand AudioCommandStepBack { get; private set; }
        public RichDelegateCommand AudioCommandStepForward { get; private set; }
        public RichDelegateCommand AudioCommandRewind { get; private set; }
        public RichDelegateCommand AudioCommandFastForward { get; private set; }
        public RichDelegateCommand AudioCommandSelectAll { get; private set; }
        public RichDelegateCommand AudioCommandClearSelection { get; private set; }
        public RichDelegateCommand AudioCommandZoomSelection { get; private set; }
        public RichDelegateCommand AudioCommandZoomFitFull { get; private set; }
        public RichDelegateCommand AudioCommandPlay { get; private set; }
        public RichDelegateCommand AudioCommandPlayPreviewLeft { get; private set; }
        public RichDelegateCommand AudioCommandPlayPreviewRight { get; private set; }
        public RichDelegateCommand AudioCommandPause { get; private set; }
        public RichDelegateCommand AudioCommandStartRecord { get; private set; }
        public RichDelegateCommand AudioCommandStopRecord { get; private set; }
        public RichDelegateCommand AudioCommandStartMonitor { get; private set; }
        public RichDelegateCommand AudioCommandStopMonitor { get; private set; }
        public RichDelegateCommand AudioCommandBeginSelection { get; private set; }
        public RichDelegateCommand AudioCommandEndSelection { get; private set; }
        public RichDelegateCommand AudioCommandSelectNextChunk { get; private set; }
        public RichDelegateCommand AudioCommandSelectPreviousChunk { get; private set; }
        public RichDelegateCommand AudioCommandDeleteAudioSelection { get; private set; }
        


        ///<summary>
        /// Dependency-injected constructor
        ///</summary>
        //[ImportingConstructor]
        public MenuBarView(IUnityContainer container, ILoggerFacade logger, IEventAggregator eventAggregator)
        {
            //LoggerFromMEF.Value.Log(
            //    "MenuBarView: using logger from the CAG/Prism/CompositeWPF (well, actually: from the Unity Ddependency Injection Container), obtained via MEF",
            //    Category.Info, Priority.Low);

            //foreach (ILoggerFacade log in LoggersFromMEF)
            //{
            //    log.Log(
            //        "MenuBarView: using logger from the CAG/Prism/CompositeWPF (well, actually: from the Unity Ddependency Injection Container), obtained via MEF",
            //        Category.Info, Priority.Low);
            //}

            Container = container;
            Logger = logger;
            EventAggregator = eventAggregator;

            Logger.Log("MenuBarView.ctor", Category.Debug, Priority.Medium);

            var session = Container.Resolve<IUrakawaSession>();
            if (session != null)
            {
                SaveCommand = session.SaveCommand;
                ExportCommand = session.ExportCommand;
                SaveAsCommand = session.SaveAsCommand;
                OpenCommand = session.OpenCommand;
                //NewCommand = session.NewCommand;
                CloseCommand = session.CloseCommand;

                UndoCommand = session.UndoCommand;
                RedoCommand = session.RedoCommand;

            }

            var metadata = Container.Resolve<MetadataPaneViewModel>();
            if (metadata != null)
            {
                CommandShowMetadataPane = metadata.CommandShowMetadataPane;
            }

            var shellPresenter = Container.Resolve<IShellPresenter>();
            if (shellPresenter != null)
            {
                ExitCommand = shellPresenter.ExitCommand;

                MagnifyUiIncreaseCommand = shellPresenter.MagnifyUiIncreaseCommand;
                MagnifyUiDecreaseCommand = shellPresenter.MagnifyUiDecreaseCommand;
                ManageShortcutsCommand = shellPresenter.ManageShortcutsCommand;
               
                CopyCommand = shellPresenter.CopyCommand;
                CutCommand = shellPresenter.CutCommand;
                PasteCommand = shellPresenter.PasteCommand;

                HelpCommand = shellPresenter.HelpCommand;
                PreferencesCommand = shellPresenter.PreferencesCommand;
                //WebHomeCommand = shellPresenter.WebHomeCommand;

                //NavNextCommand = shellPresenter.NavNextCommand;
                //NavPreviousCommand = shellPresenter.NavPreviousCommand;
            }

            var audioModule = Container.Resolve<AudioPaneViewModel>();
            if (audioModule != null)
            {

                AudioCommandInsertFile = audioModule.CommandInsertFile;
                AudioCommandGotoBegining = audioModule.CommandGotoBegining;
                AudioCommandGotoEnd = audioModule.CommandGotoEnd;
                AudioCommandStepBack = audioModule.CommandStepBack;
                AudioCommandStepForward = audioModule.CommandStepForward;
                AudioCommandRewind = audioModule.CommandRewind;
                AudioCommandFastForward = audioModule.CommandFastForward;
                AudioCommandSelectAll = audioModule.CommandSelectAll;
                AudioCommandClearSelection = audioModule.CommandClearSelection;
                AudioCommandZoomSelection = audioModule.CommandZoomSelection;
                AudioCommandZoomFitFull = audioModule.CommandZoomFitFull;
                AudioCommandPlay = audioModule.CommandPlay;
                AudioCommandPlayPreviewLeft = audioModule.CommandPlayPreviewLeft;
                AudioCommandPlayPreviewRight = audioModule.CommandPlayPreviewRight;
                AudioCommandPause = audioModule.CommandPause;
                AudioCommandStartRecord = audioModule.CommandStartRecord;
                AudioCommandStopRecord = audioModule.CommandStopRecord;
                AudioCommandStartMonitor = audioModule.CommandStartMonitor;
                AudioCommandStopMonitor = audioModule.CommandStopMonitor;
                AudioCommandBeginSelection = audioModule.CommandBeginSelection;
                AudioCommandEndSelection = audioModule.CommandEndSelection;
                AudioCommandSelectNextChunk = audioModule.CommandSelectNextChunk;
                AudioCommandSelectPreviousChunk = audioModule.CommandSelectPreviousChunk;
                AudioCommandDeleteAudioSelection = audioModule.CommandDeleteAudioSelection;
            }

            InitializeComponent();
        }

        public void EnsureViewMenuCheckState(string regionName, bool visible)
        {
            //TODO make this generic using a mapping between RegionName and an actual menu trigger check box thing
            //if (ZoomMenuItem.IsChecked != visible) ZoomMenuItem.IsChecked = visible;
        }
    }
}
