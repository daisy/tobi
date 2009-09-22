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
    public partial class MenuBarView
    {
        protected IUnityContainer Container { get; private set; }
        protected ILoggerFacade Logger { get; private set; }
        protected IEventAggregator EventAggregator { get; private set; }


        public RichDelegateCommand<object> ExitCommand { get; private set; }

        public RichDelegateCommand<object> MagnifyUiIncreaseCommand { get; private set; }
        public RichDelegateCommand<object> MagnifyUiDecreaseCommand { get; private set; }

        public RichDelegateCommand<object> ManageShortcutsCommand { get; private set; }

        public RichDelegateCommand<object> UndoCommand { get; private set; }
        public RichDelegateCommand<object> RedoCommand { get; private set; }

        public RichDelegateCommand<object> CopyCommand { get; private set; }
        public RichDelegateCommand<object> CutCommand { get; private set; }
        public RichDelegateCommand<object> PasteCommand { get; private set; }

        public RichDelegateCommand<object> HelpCommand { get; private set; }
        public RichDelegateCommand<object> PreferencesCommand { get; private set; }
        //public RichDelegateCommand<object> WebHomeCommand { get; private set; }

        //public RichDelegateCommand<object> NavNextCommand { get; private set; }
        //public RichDelegateCommand<object> NavPreviousCommand { get; private set; }

        public RichDelegateCommand<object> ExportCommand { get; private set; }

        public RichDelegateCommand<object> SaveCommand { get; private set; }
        public RichDelegateCommand<object> SaveAsCommand { get; private set; }

        public RichDelegateCommand<object> NewCommand { get; private set; }
        public RichDelegateCommand<object> OpenCommand { get; private set; }
        public RichDelegateCommand<object> CloseCommand { get; private set; }

        public RichDelegateCommand<object> CommandShowMetadataPane { get; private set; }

        //AUDIO commands
        public RichDelegateCommand<object> AudioCommandInsertFile { get; private set; }
        public RichDelegateCommand<object> AudioCommandGotoBegining { get; private set; }
        public RichDelegateCommand<object> AudioCommandGotoEnd { get; private set; }
        public RichDelegateCommand<object> AudioCommandStepBack { get; private set; }
        public RichDelegateCommand<object> AudioCommandStepForward { get; private set; }
        public RichDelegateCommand<object> AudioCommandRewind { get; private set; }
        public RichDelegateCommand<object> AudioCommandFastForward { get; private set; }
        public RichDelegateCommand<object> AudioCommandSelectAll { get; private set; }
        public RichDelegateCommand<object> AudioCommandClearSelection { get; private set; }
        public RichDelegateCommand<object> AudioCommandZoomSelection { get; private set; }
        public RichDelegateCommand<object> AudioCommandZoomFitFull { get; private set; }
        public RichDelegateCommand<object> AudioCommandPlay { get; private set; }
        public RichDelegateCommand<object> AudioCommandPlayPreviewLeft { get; private set; }
        public RichDelegateCommand<object> AudioCommandPlayPreviewRight { get; private set; }
        public RichDelegateCommand<object> AudioCommandPause { get; private set; }
        public RichDelegateCommand<object> AudioCommandStartRecord { get; private set; }
        public RichDelegateCommand<object> AudioCommandStopRecord { get; private set; }
        public RichDelegateCommand<object> AudioCommandStartMonitor { get; private set; }
        public RichDelegateCommand<object> AudioCommandStopMonitor { get; private set; }
        public RichDelegateCommand<object> AudioCommandBeginSelection { get; private set; }
        public RichDelegateCommand<object> AudioCommandEndSelection { get; private set; }
        public RichDelegateCommand<object> AudioCommandSelectNextChunk { get; private set; }
        public RichDelegateCommand<object> AudioCommandSelectPreviousChunk { get; private set; }
        public RichDelegateCommand<object> AudioCommandDeleteAudioSelection { get; private set; }
        //end AUDIO commands

        ///<summary>
        /// Dependency-injected constructor
        ///</summary>
        public MenuBarView(IUnityContainer container, ILoggerFacade logger, IEventAggregator eventAggregator)
        {
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
                NewCommand = session.NewCommand;
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
