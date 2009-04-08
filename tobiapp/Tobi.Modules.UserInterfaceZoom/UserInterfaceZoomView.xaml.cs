using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Unity;
using Tobi.Infrastructure;

namespace Tobi.Modules.UserInterfaceZoom
{
    /// <summary>
    /// The View implementation (aka XAML 'code-behind')
    /// </summary>
    public partial class UserInterfaceZoomView : IUserInterfaceZoomView
    {
        ///<summary>
        /// The DI container, injected in the constructor by the DI container himself !
        ///</summary>
        protected IUnityContainer Container { get; private set; }

        ///<summary>
        /// The application Logger, injected in the constructor by the DI container.
        ///</summary>
        protected ILoggerFacade Logger { get; private set; }

        ///<summary>
        /// The shell view, injected in the constructor by the DI container.
        ///</summary>
        protected IShellView ShellView { get; private set; }

        ///<summary>
        /// Registers the data binding between the slider control and the user-interface (shell) zoom value
        ///</summary>
        public UserInterfaceZoomView(IShellView shellview, IUnityContainer container, ILoggerFacade logger)
        {
            Logger = logger;
            Container = container;
            ShellView = shellview;

            InitializeComponent();

            //ZoomSlider.SetBinding(RangeBase.ValueProperty, shellview.GetZoomBinding());

            /*BindingOperations.SetBinding(
            Shell.ScaleTransform,
           ScaleTransform.ScaleXProperty,
            zoomBinding
            );
            Shell.ScaleTransform.DataContext = presenter;
             */
        }

        public UserInterfaceZoomPresenter Model
        {
            get { return DataContext as UserInterfaceZoomPresenter; }
            set { DataContext = value; }
        }

        public void FocusControl()
        {
            FocusManager.SetFocusedElement(ShellView.Window, ZoomSlider);
        }

        public string RegionName
        {
            get {
                return "";
                //RegionNames.UserInterfaceZoom;
            }
        }
    }
}
