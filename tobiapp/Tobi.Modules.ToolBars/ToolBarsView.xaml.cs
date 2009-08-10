using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Practices.Composite.Logging;
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

        public ToolBarsView()
        {
            m_PropertyChangeHandler = new PropertyChangedNotifyBase();
            m_PropertyChangeHandler.InitializeDependentProperties(this);
        }

        public RichDelegateCommand<object> MagnifyUiIncreaseCommand { get; private set; }
        public RichDelegateCommand<object> MagnifyUiDecreaseCommand { get; private set; }
        public RichDelegateCommand<object> ManageShortcutsCommand { get; private set; }
        public RichDelegateCommand<object> SaveCommand { get; private set; }
        public RichDelegateCommand<object> SaveAsCommand { get; private set; }

        public RichDelegateCommand<object> NewCommand { get; private set; }
        public RichDelegateCommand<object> OpenCommand { get; private set; }

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

        public RichDelegateCommand<object> CommandFocus { get; private set; }

        public RichDelegateCommand<object> CommandShowMetadataPane { get; private set; }

        protected IUnityContainer Container { get; private set; }
        public ILoggerFacade Logger { get; private set; }

        ///<summary>
        /// Default constructor
        ///</summary>
        public ToolBarsView(IUnityContainer container, ILoggerFacade logger)
        {
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
            CommandFocus = new RichDelegateCommand<object>(
                UserInterfaceStrings.Toolbar_Focus,
                null,
                UserInterfaceStrings.Toolbar_Focus_KEYS,
                null,
                obj => FocusHelper.Focus(this, FocusStart),
                obj => true);

            if (shellPresenter != null)
            {
                shellPresenter.RegisterRichCommand(CommandFocus);
            }
            //

            InitializeComponent();
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
