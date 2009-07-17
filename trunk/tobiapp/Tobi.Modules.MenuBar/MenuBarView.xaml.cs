using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Unity;
using Tobi.Infrastructure;
using Tobi.Infrastructure.Commanding;
using Tobi.Modules.MetadataPane;

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
        public RichDelegateCommand<object> WebHomeCommand { get; private set; }

        public RichDelegateCommand<object> NavNextCommand { get; private set; }
        public RichDelegateCommand<object> NavPreviousCommand { get; private set; }

        public RichDelegateCommand<object> SaveCommand { get; private set; }
        public RichDelegateCommand<object> SaveAsCommand { get; private set; }

        public RichDelegateCommand<object> NewCommand { get; private set; }
        public RichDelegateCommand<object> OpenCommand { get; private set; }
        public RichDelegateCommand<object> CloseCommand { get; private set; }

        public RichDelegateCommand<object> CommandShowMetadataPane { get; private set; }

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
                WebHomeCommand = shellPresenter.WebHomeCommand;

                NavNextCommand = shellPresenter.NavNextCommand;
                NavPreviousCommand = shellPresenter.NavPreviousCommand;
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
