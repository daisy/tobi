using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.UnityExtensions;
using Microsoft.Practices.Composite.Modularity;
using System.Windows;
using Microsoft.Practices.Unity;
using Tobi.Modules.AudioPane;
using Tobi.Modules.DocumentPane;
using Tobi.Modules.NavigationPane;
using Tobi.Modules.MenuBar;
using Tobi.Modules.ToolBars;

namespace Tobi
{
    /// <summary>
    /// The Tobi-specific Bootstrapper
    /// </summary>
    public class Bootstrapper : UnityBootstrapper
    {
        private readonly BitFactoryLoggerAdapter _logger = new BitFactoryLoggerAdapter(); //EntLibLoggerAdapter();

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

        protected override void InitializeModules()
        {
            base.InitializeModules();

            var moduleCatalog = Container.Resolve<IModuleCatalog>();
            foreach (var module in moduleCatalog.Modules)
            {

                if (module.ModuleName == "MyModuleName")
                {
                    var moduleManager = Container.Resolve<IModuleManager>();
                    moduleManager.LoadModule(module.ModuleName);
                }
            }
        }

        /// <summary>
        /// Tobi loads its main Modules statically
        /// </summary>
        protected override IModuleCatalog GetModuleCatalog()
        {
            //return new DirectoryModuleCatalog() { ModulePath = @".\Modules" };

            return new ModuleCatalog()
                .AddModule(typeof (MenuBarModule))
                .AddModule(typeof (NavigationPaneModule), "DocumentPaneModule")
                .AddModule(typeof (DocumentPaneModule))
                .AddModule(typeof (AudioPaneModule))
                .AddModule(typeof (ToolBarsModule));

                //.AddModule(typeof (StatusBarModule));
            
                //.AddModule(typeof(UserInterfaceZoomModule))
        }
    }
}