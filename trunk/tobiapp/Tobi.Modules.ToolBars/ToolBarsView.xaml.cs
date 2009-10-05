using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Regions;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using Tobi.Modules.MetadataPane;


namespace Tobi.Modules.ToolBars
{
    /// <summary>
    /// Interaction logic for ToolBarsView.xaml
    /// </summary>
    public partial class ToolBarsView : INotifyPropertyChangedEx
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void DispatchPropertyChangedEvent(PropertyChangedEventArgs e)
        {
            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        private PropertyChangedNotifyBase m_PropertyChangeHandler;

        public RichDelegateCommand MagnifyUiIncreaseCommand { get; private set; }
        public RichDelegateCommand MagnifyUiDecreaseCommand { get; private set; }
        public RichDelegateCommand ManageShortcutsCommand { get; private set; }
        public RichDelegateCommand SaveCommand { get; private set; }
        public RichDelegateCommand SaveAsCommand { get; private set; }

        public RichDelegateCommand NewCommand { get; private set; }
        public RichDelegateCommand OpenCommand { get; private set; }

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

        public RichDelegateCommand CommandFocus { get; private set; }

        public RichDelegateCommand CommandShowMetadataPane { get; private set; }

        protected IUnityContainer Container { get; private set; }
        public ILoggerFacade Logger { get; private set; }

        //[ImportingConstructor]
        //public ToolBarsView() //ILoggerFacade logger)
        //{
        //logger.Log(
        //    "ToolBarsView: using logger from the CAG/Prism/CompositeWPF (well, actually: from the Unity Ddependency Injection Container), obtained via MEF",
        //    Category.Info, Priority.Low);
        //}

        public ToolBarsView(IUnityContainer container, ILoggerFacade logger)
        {
            m_PropertyChangeHandler = new PropertyChangedNotifyBase();
            m_PropertyChangeHandler.InitializeDependentProperties(this);

            Container = container;
            Logger = logger;

            // TODO: UGLY ! We need a much better design for sharing commands
            var metadata = Container.Resolve<MetadataPaneViewModel>();
            if (metadata != null)
            {
                CommandShowMetadataPane = metadata.CommandShowMetadataPane;
            }

            var session = Container.Resolve<IUrakawaSession>();
            if (session != null)
            {
                SaveCommand = session.SaveCommand;
                SaveAsCommand = session.SaveAsCommand;
                OpenCommand = session.OpenCommand;
                NewCommand = session.NewCommand;

                UndoCommand = session.UndoCommand;
                RedoCommand = session.RedoCommand;
            }

            var shellPresenter = Container.Resolve<IShellPresenter>();
            if (shellPresenter != null)
            {
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
            //
            CommandFocus = new RichDelegateCommand(
                UserInterfaceStrings.Toolbar_Focus,
                null,
                UserInterfaceStrings.Toolbar_Focus_KEYS,
                null,
                () => FocusHelper.Focus(this, FocusStart),
                () => true);

            if (shellPresenter != null)
            {
                shellPresenter.RegisterRichCommand(CommandFocus);
            }
            //

            InitializeComponent();

            RegionManager.SetRegionManager(this, Container.Resolve<IRegionManager>());
            RegionManager.UpdateRegions();

            var regionManager = Container.Resolve<IRegionManager>();
            IRegion targetRegion = regionManager.Regions[RegionNames.MainToolbar];

            targetRegion.Add(OpenCommand);
            targetRegion.Activate(OpenCommand);

            targetRegion.Add(SaveCommand);
            targetRegion.Activate(SaveCommand);

            targetRegion.Add(UndoCommand);
            targetRegion.Activate(UndoCommand);

            targetRegion.Add(RedoCommand);
            targetRegion.Activate(RedoCommand);

            targetRegion.Add(MagnifyUiDecreaseCommand);
            targetRegion.Activate(MagnifyUiDecreaseCommand);

            targetRegion.Add(MagnifyUiIncreaseCommand);
            targetRegion.Activate(MagnifyUiIncreaseCommand);

            targetRegion.Add(CommandShowMetadataPane);
            targetRegion.Activate(CommandShowMetadataPane);
        }

        // ReSharper disable RedundantDefaultFieldInitializer
        private readonly List<Double> m_IconHeights = new List<double>
                                               {
                                                   20,30,40,50,60,70,80,90,100,150,200,250,300
                                               };
        // ReSharper restore RedundantDefaultFieldInitializer
        public List<Double> IconHeights
        {
            get
            {
                return m_IconHeights;
            }
        }

        public List<Double> IconWidths
        {
            get
            {
                return m_IconHeights;
            }
        }

        private double m_IconHeight = 32;
        public Double IconHeight
        {
            get
            {
                return m_IconHeight;
            }
            set
            {
                if (m_IconHeight == value) return;
                m_IconHeight = value;
                m_PropertyChangeHandler.RaisePropertyChanged(() => IconHeight);
            }
        }

        private double m_IconWidth = 32;
        public Double IconWidth
        {
            get
            {
                return m_IconWidth;
            }
            set
            {
                if (m_IconWidth == value) return;
                m_IconWidth = value;
                m_PropertyChangeHandler.RaisePropertyChanged(() => IconWidth);
            }
        }
    }
}
