using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Unity;
using Microsoft.Win32;
using Tobi.Common;
using Tobi.Common.MVVM;

namespace Tobi
{
    /// <summary>
    /// 'Code behind' for the Shell window
    /// </summary>
    public partial class Shell : IShellView
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

        protected IUnityContainer Container { get; private set; }

        protected IEventAggregator EventAggregator { get; private set; }

        private bool m_InConstructor = false;

        private PropertyChangedNotifyBase m_PropertyChangeHandler;


        ///<summary>
        /// 
        ///</summary>
        public Shell(IUnityContainer container, IEventAggregator eventAggregator)
        {
            Container = container;
            EventAggregator = eventAggregator;

            m_PropertyChangeHandler = new PropertyChangedNotifyBase();
            m_PropertyChangeHandler.InitializeDependentProperties(this);

            m_InConstructor = true;

            InitializeComponent();

            m_InConstructor = false;

            //IRegionManager regionManager = Container.Resolve<IRegionManager>();
            //string regionName = "AvalonDockRegion_1";
            //regionManager.Regions.Add(new AvalonDockRegion() { Name = regionName });
            //((AvalonDockRegion)regionManager.Regions[regionName]).Bind(DocumentContent2);
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            SystemEvents.DisplaySettingsChanged += OnSystemEventsDisplaySettingsChanged;

            try
            {
                Uri iconUri = new Uri("pack://application:,,,/" + GetType().Assembly.GetName().Name
                                        + ";component/Tobi.ico", UriKind.Absolute);
                //Uri iconUri = new Uri("Tobi.ico", UriKind.RelativeOrAbsolute);
                Icon = BitmapFrame.Create(iconUri);
            }
            finally
            {
                //ignore
            }

            App app = Application.Current as App;
            if (app != null)
            {
                app.SplashScreen.Close(TimeSpan.FromSeconds(0.5));
                //app.Dispatcher.BeginInvoke((Action)(() => app.SplashScreen.Close(TimeSpan.Zero)), DispatcherPriority.Loaded);
            }

            var session = Container.Resolve<IUrakawaSession>();
            session.BindPropertyChangedToAction(() => session.DocumentFilePath,
                () => m_PropertyChangeHandler.RaisePropertyChanged(() => WindowTitle));
            session.BindPropertyChangedToAction(() => session.IsDirty,
                () => m_PropertyChangeHandler.RaisePropertyChanged(() => WindowTitle));


            //Activate();

            /*
            IconBitmapDecoder ibd = new IconBitmapDecoder(new Uri(
                            @"pack://application:,,/Resources/Tobi.ico",
                            UriKind.RelativeOrAbsolute),
                            BitmapCreateOptions.None, BitmapCacheOption.Default);
            Icon = ibd.Frames[0];
            */
        }

        private void OnSystemEventsDisplaySettingsChanged(object sender, EventArgs e)
        {
            // update DPI-dependent stuff
        }

        public String WindowTitle
        {
            get
            {
                IUrakawaSession session;
                try
                {
                    session = Container.Resolve<IUrakawaSession>();
                }
                catch (ResolutionFailedException)
                {
                    return "Tobi - Initializing...";
                }
                return "Tobi " + (session.IsDirty ? "* " : "") + "[" + (session.DocumentProject == null ? "no document" : session.DocumentFilePath) + "]";
            }
        }

        protected void OnClosing(object sender, CancelEventArgs e)
        {
            /*
            e.Cancel = true;
            // Workaround for not being able to hide a window during closing.
            // This behavior was needed in WPF to ensure consistent window visiblity state
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, (DispatcherOperationCallback)delegate(object o)
            {
                Hide();
                return null;
            }, null);
             */
            var shellPresenter = Container.Resolve<IShellPresenter>();
            bool leaving = shellPresenter.OnShellWindowClosing();
            if (!leaving) e.Cancel = true;
        }

        ///<summary>
        /// Shows the main shell window
        ///</summary>
        public void ShowView()
        {
            Show();
        }

        public Window Window
        {
            get { return this; }
        }

        private bool m_SplitterDrag = false;

        public bool SplitterDrag
        {
            get
            {
                return m_SplitterDrag;
            }
        }

        private void OnSplitterDragCompleted(object sender, DragCompletedEventArgs e)
        {
            m_SplitterDrag = false;
        }

        private void OnSplitterDragStarted(object sender, DragStartedEventArgs e)
        {
            m_SplitterDrag = true;
        }

        public static readonly DependencyProperty MagnificationLevelProperty =
            DependencyProperty.Register("MagnificationLevel",
            typeof(double),
            typeof(Shell),
            new PropertyMetadata(1.0, OnMagnificationLevelChanged, OnMagnificationLevelCoerce));

        public double MagnificationLevel
        {
            get { return (double)GetValue(MagnificationLevelProperty); }
            set
            {
                // The value will be coerced after this call !
                SetValue(MagnificationLevelProperty, value);
            }
        }


        private static object OnMagnificationLevelCoerce(DependencyObject d, object basevalue)
        {
            var shell = d as Shell;
            if (shell == null) return 1.0;

            var value = (Double)basevalue;
            if (value > shell.ZoomSlider.Maximum)
            {
                value = shell.ZoomSlider.Maximum;
            }
            if (value < shell.ZoomSlider.Minimum)
            {
                value = shell.ZoomSlider.Minimum;
            }

            Application.Current.Resources["MagnificationLevel"] = value;

            return value;
        }

        private static void OnMagnificationLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var shell = d as Shell;
            if (shell == null) return;
            shell.NotifyMagnificationLevel();
        }

        private void NotifyMagnificationLevel()
        {
            if (m_InConstructor)
            {
                return;
            }

            var shellPresenter = Container.Resolve<IShellPresenter>();
            if (shellPresenter != null)
            {
                shellPresenter.OnMagnificationLevelChanged(MagnificationLevel);
            }
            /*
            foreach(InputBinding ib in InputBindings)
            {
                var command = ib.Command as RichDelegateCommand<object>;
                if (command != null)
                {
                    command.IconDrawScale = e.NewValue;
                }
            }
             */
        }
    }
}
