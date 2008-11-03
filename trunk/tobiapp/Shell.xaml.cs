using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Practices.Unity;
using Tobi.Modules.MenuBar;
using Tobi.Modules.UserInterfaceZoom;

namespace Tobi
{
    /// <summary>
    /// 'Code behind' for the Shell window
    /// </summary>
    public partial class Shell : Window, IShellView
    {
        private readonly IUnityContainer _container;

        ///<summary>
        /// Just calls <c>Window.InitializeComponent()</c>.
        ///</summary>
        public Shell(IUnityContainer container)
        {
            InitializeComponent();
            _container = container;
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
            var shellPresenter = _container.Resolve<IShellPresenter>();
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

        public void DecreaseZoom(double? step)
        {
            var zoomer =_container.Resolve<IUserInterfaceZoomPresenter>();
            double value = ScaleTransform.ScaleX - (step == null ? (zoomer != null ? zoomer.ZoomStep : 0.5) : (double)step);
            if (zoomer!=null)
            {
                if (value < zoomer.MinimumZoom) value = zoomer.MinimumZoom;
            }

            ScaleTransform.ScaleX = value;
        }

        public void IncreaseZoom(double? step)
        {
            var zoomer = _container.Resolve<IUserInterfaceZoomPresenter>();
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

        public Window Window
        {
            get { return this; }
        }
    }
}


/*
public static readonly DependencyProperty ZoomValueProperty =
    DependencyProperty.Register("ZoomValue",
    typeof(double),
    typeof(Shell),
    new PropertyMetadata(new PropertyChangedCallback(OnZoomValueChanged)));

public double ZoomValue
{
    get { return (double)GetValue(ZoomValueProperty); }
    set {
        SetValue(ZoomValueProperty, value);
        ScaleTransform.ScaleX = value;
    }
}

private static void OnZoomValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    var oldZoom = ((Shell)d).ZoomValue;
    var newZoom = (double)e.NewValue;
    if (newZoom != oldZoom)
    {
        ((Shell)d).ZoomValue = newZoom;
    }
}
 */