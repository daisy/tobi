using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Unity;

namespace Tobi
{
    /// <summary>
    /// 'Code behind' for the Shell window
    /// </summary>
    public partial class Shell : IShellView
    {
        protected IUnityContainer Container { get; private set; }

        protected IEventAggregator EventAggregator { get; private set; }

        private bool m_InConstructor = false;

        ///<summary>
        /// 
        ///</summary>
        public Shell(IUnityContainer container, IEventAggregator eventAggregator)
        {
            Container = container;
            EventAggregator = eventAggregator;

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
            }

            //Activate();

            /*
            IconBitmapDecoder ibd = new IconBitmapDecoder(new Uri(
                            @"pack://application:,,/Resources/Tobi.ico",
                            UriKind.RelativeOrAbsolute),
                            BitmapCreateOptions.None, BitmapCacheOption.Default);
            Icon = ibd.Frames[0];
            */
        }

        public String WindowTitle
        {
            get
            {
                return "Tobi [unreleased development version] (" + DateTime.Now + ")";
            }
        }

        protected void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
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
                double actualValue = value;
                if (actualValue > ZoomSlider.Maximum)
                {
                    actualValue = ZoomSlider.Maximum;
                }
                if (actualValue < ZoomSlider.Minimum)
                {
                    actualValue = ZoomSlider.Minimum;
                }
                SetValue(MagnificationLevelProperty, actualValue);
            }
        }

        private static void OnMagnificationLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var shell = d as Shell;
            if (shell == null) return;
            shell.NotifyMagnificationLevel();
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
            return value;
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
                shellPresenter.SetZoomValue(MagnificationLevel);
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


/*
 * 
        public void DecreaseZoom(double? step)
        {
            var zoomer =m_DiContainer.Resolve<IUserInterfaceZoomPresenter>();
            double value = ScaleTransform.ScaleX - (step == null ? (zoomer != null ? zoomer.ZoomStep : 0.5) : (double)step);
            if (zoomer!=null)
            {
                if (value < zoomer.MinimumZoom) value = zoomer.MinimumZoom;
            }

            ScaleTransform.ScaleX = value;
        }

        public void IncreaseZoom(double? step)
        {
            var zoomer = m_DiContainer.Resolve<IUserInterfaceZoomPresenter>();
            double value = ScaleTransform.ScaleX + (step == null ? (zoomer != null ? zoomer.ZoomStep : 0.5) : (double)step);
            if (zoomer != null)
            {
                if (value > zoomer.MaximumZoom) value = zoomer.MaximumZoom;
            }

            ScaleTransform.ScaleX = value;
        }

        public Binding GetZoomBinding()
        {
            return new Binding
            {
                Source = ScaleTransform,
                Path = new PropertyPath("ScaleX"),
                Mode = BindingMode.TwoWay
            };
        }
 * 
 * 
 * 
 */