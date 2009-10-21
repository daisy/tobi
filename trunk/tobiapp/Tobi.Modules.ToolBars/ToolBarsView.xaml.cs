using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Controls;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Regions;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;


namespace Tobi.Modules.ToolBars
{
    /// <summary>
    /// Interaction logic for ToolBarsView.xaml
    /// </summary>
    public partial class ToolBarsView : INotifyPropertyChangedEx, IToolBarsView
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

        public RichDelegateCommand CommandFocus { get; private set; }

        protected IUnityContainer Container { get; private set; }
        public ILoggerFacade Logger { get; private set; }
        protected IEventAggregator EventAggregator { get; private set; }

        //[ImportingConstructor]
        //public ToolBarsView() //ILoggerFacade logger)
        //{
        //logger.Log(
        //    "ToolBarsView: using logger from the CAG/Prism/CompositeWPF (well, actually: from the Unity Ddependency Injection Container), obtained via MEF",
        //    Category.Info, Priority.Low);
        //}

        public ToolBarsView(IUnityContainer container, ILoggerFacade logger, IEventAggregator eventAggregator)
        {
            m_PropertyChangeHandler = new PropertyChangedNotifyBase();
            m_PropertyChangeHandler.InitializeDependentProperties(this);

            Container = container;
            Logger = logger;
            EventAggregator = eventAggregator;

            // TODO: UGLY ! We need a much better design for sharing commands
            //var metadata = Container.Resolve<MetadataPaneViewModel>();
            //if (metadata != null)
            //{
            //    CommandShowMetadataPane = metadata.CommandShowMetadataPane;
            //}

            //var session = Container.Resolve<IUrakawaSession>();
            //if (session != null)
            //{
            //    SaveCommand = session.SaveCommand;
            //    SaveAsCommand = session.SaveAsCommand;
            //    OpenCommand = session.OpenCommand;
            //    NewCommand = session.NewCommand;

            //    UndoCommand = session.UndoCommand;
            //    RedoCommand = session.RedoCommand;
            //}

            var shellPresenter = Container.Resolve<IShellPresenter>();

            //if (shellPresenter != null)
            //{
            //    MagnifyUiIncreaseCommand = shellPresenter.MagnifyUiIncreaseCommand;
            //    MagnifyUiDecreaseCommand = shellPresenter.MagnifyUiDecreaseCommand;
            //    ManageShortcutsCommand = shellPresenter.ManageShortcutsCommand;


            //    CopyCommand = shellPresenter.CopyCommand;
            //    CutCommand = shellPresenter.CutCommand;
            //    PasteCommand = shellPresenter.PasteCommand;

            //    HelpCommand = shellPresenter.HelpCommand;
            //    PreferencesCommand = shellPresenter.PreferencesCommand;
            //    //WebHomeCommand = shellPresenter.WebHomeCommand;

            //    //NavNextCommand = shellPresenter.NavNextCommand;
            //    //NavPreviousCommand = shellPresenter.NavPreviousCommand;
            //}
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

            EventAggregator.GetEvent<TypeConstructedEvent>().Publish(GetType());

            //var regionManager = Container.Resolve<IRegionManager>();
            //IRegion targetRegion = regionManager.Regions[RegionNames.MainToolbar];

            //targetRegion.Add(OpenCommand);
            //targetRegion.Activate(OpenCommand);

            //targetRegion.Add(SaveCommand);
            //targetRegion.Activate(SaveCommand);

            //var sep = new Separator();
            //targetRegion.Add(sep);
            //targetRegion.Activate(sep);

            //targetRegion.Add(UndoCommand);
            //targetRegion.Activate(UndoCommand);

            //targetRegion.Add(RedoCommand);
            //targetRegion.Activate(RedoCommand);

            //sep = new Separator();
            //targetRegion.Add(sep);
            //targetRegion.Activate(sep);

            //targetRegion.Add(MagnifyUiDecreaseCommand);
            //targetRegion.Activate(MagnifyUiDecreaseCommand);

            //targetRegion.Add(MagnifyUiIncreaseCommand);
            //targetRegion.Activate(MagnifyUiIncreaseCommand);

            //sep = new Separator();
            //targetRegion.Add(sep);
            //targetRegion.Activate(sep);

            //targetRegion.Add(CommandShowMetadataPane);
            //targetRegion.Activate(CommandShowMetadataPane);

            //sep = new Separator();
            //targetRegion.Add(sep);
            //targetRegion.Activate(sep);

            //targetRegion.Add(ManageShortcutsCommand);
            //targetRegion.Activate(ManageShortcutsCommand);
        }

        public void RemoveToolBarGroup(int uid)
        {
            var regionManager = Container.Resolve<IRegionManager>();
            IRegion targetRegion = regionManager.Regions[RegionNames.MainToolbar];

            var viewsToRemove = new List<object>();

            int count = 0;
            object view;
            while ((view = targetRegion.GetView(uid + "_" + count++)) != null)
            {
                viewsToRemove.Add(view);
            }

            foreach (var obj in viewsToRemove)
            {
                targetRegion.Remove(obj);
            }
        }

        public int AddToolBarGroup(RichDelegateCommand[] commands)
        {
            if (!Dispatcher.CheckAccess())
            {
                Debugger.Break();
            }

            int uid = getNewUid();

            var regionManager = Container.Resolve<IRegionManager>();
            IRegion targetRegion = regionManager.Regions[RegionNames.MainToolbar];

            int count = 0;

            foreach (var command in commands)
            {
                //targetRegion.Add(new ButtonRichCommand(){RichCommand = command}, uid + "_" + count++);
                targetRegion.Add(command, uid + "_" + count++);
                targetRegion.Activate(command);
                //command.IconProvider.IconDrawScale
            }

            var sep = new Separator();
            targetRegion.Add(sep, uid + "_" + count++);
            targetRegion.Activate(sep);

            return uid;
        }

        private int m_Uid;
        private int getNewUid()
        {
            return m_Uid++;
        }


        //public RichDelegateCommand MagnifyUiIncreaseCommand { get; private set; }
        //public RichDelegateCommand MagnifyUiDecreaseCommand { get; private set; }
        //public RichDelegateCommand ManageShortcutsCommand { get; private set; }
        //public RichDelegateCommand SaveCommand { get; private set; }
        //public RichDelegateCommand SaveAsCommand { get; private set; }

        //public RichDelegateCommand NewCommand { get; private set; }
        //public RichDelegateCommand OpenCommand { get; private set; }

        //public RichDelegateCommand UndoCommand { get; private set; }
        //public RichDelegateCommand RedoCommand { get; private set; }

        //public RichDelegateCommand CopyCommand { get; private set; }
        //public RichDelegateCommand CutCommand { get; private set; }
        //public RichDelegateCommand PasteCommand { get; private set; }

        //public RichDelegateCommand HelpCommand { get; private set; }
        //public RichDelegateCommand PreferencesCommand { get; private set; }
        //public RichDelegateCommand WebHomeCommand { get; private set; }

        //public RichDelegateCommand NavNextCommand { get; private set; }
        //public RichDelegateCommand NavPreviousCommand { get; private set; }

        //public RichDelegateCommand CommandShowMetadataPane { get; private set; }


        //private static int count(IViewsCollection collection)
        //{
        //    int count = 0;
        //    foreach (var view in collection)
        //    {
        //        count++;
        //    }
        //    return count;
        //}

        //// ReSharper disable RedundantDefaultFieldInitializer
        //private readonly List<Double> m_IconHeights = new List<double>
        //                                       {
        //                                           20,30,40,50,60,70,80,90,100,150,200,250,300
        //                                       };
        //// ReSharper restore RedundantDefaultFieldInitializer
        //public List<Double> IconHeights
        //{
        //    get
        //    {
        //        return m_IconHeights;
        //    }
        //}

        //public List<Double> IconWidths
        //{
        //    get
        //    {
        //        return m_IconHeights;
        //    }
        //}

        //private double m_IconHeight = 32;
        //public Double IconHeight
        //{
        //    get
        //    {
        //        return m_IconHeight;
        //    }
        //    set
        //    {
        //        if (m_IconHeight == value) return;
        //        m_IconHeight = value;
        //        m_PropertyChangeHandler.RaisePropertyChanged(() => IconHeight);
        //    }
        //}

        //private double m_IconWidth = 32;
        //public Double IconWidth
        //{
        //    get
        //    {
        //        return m_IconWidth;
        //    }
        //    set
        //    {
        //        if (m_IconWidth == value) return;
        //        m_IconWidth = value;
        //        m_PropertyChangeHandler.RaisePropertyChanged(() => IconWidth);
        //    }
        //}
    }
}
