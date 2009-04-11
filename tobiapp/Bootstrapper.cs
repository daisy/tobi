using System.Windows.Input;
using AvalonDock;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Composite.UnityExtensions;
using Microsoft.Practices.Composite.Modularity;
using System.Windows;
using Microsoft.Practices.Unity;
using Tobi.Modules.AudioPane;
using Tobi.Modules.DocumentPane;
using Tobi.Modules.NavigationPane;
using Tobi.Modules.StatusBar;
using Tobi.Modules.MenuBar;
using Tobi.Modules.ToolBars;
using Tobi.Modules.UserInterfaceZoom;

namespace Tobi
{
    /// <summary>
    /// The Tobi-specific Bootstrapper
    /// </summary>
    public class Bootstrapper : UnityBootstrapper
    {
        private readonly EntLibLoggerAdapter _logger = new EntLibLoggerAdapter();

        ///<summary>
        /// Overriding the default TRACE logger with our own (available application-wide, through the DI container)
        ///</summary>
        protected override ILoggerFacade LoggerFacade
        {
            get { return _logger; }
        }

        ///<summary>
        /// Registration of the Shell View into the container
        ///</summary>
        protected override void ConfigureContainer()
        {
            Container.RegisterType<IShellView, Shell>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IShellPresenter, ShellPresenter>(new ContainerControlledLifetimeManager());
            base.ConfigureContainer();
        }

        /// <summary>
        /// Initialization of the Tobi Shell window
        /// </summary>
        protected override DependencyObject CreateShell()
        {
            var shellPresenter = Container.Resolve<IShellPresenter>();
            var shellView = shellPresenter.View;
            shellView.ShowView();
            return shellView as DependencyObject;
        }

        /// <summary>
        /// Tobi loads its main Modules statically
        /// </summary>
        protected override IModuleCatalog GetModuleCatalog()
        {
            return new ModuleCatalog()
                .AddModule(typeof(MenuBarModule))
                .AddModule(typeof(NavigationPaneModule), "DocumentPaneModule")
                .AddModule(typeof(DocumentPaneModule))
                .AddModule(typeof(AudioPaneModule));

                //.AddModule(typeof(StatusBarModule))
                //.AddModule(typeof(ToolBarsModule))
            
                //.AddModule(typeof(UserInterfaceZoomModule))
        }
    }
}