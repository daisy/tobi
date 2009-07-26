using System.Diagnostics;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Regions;
using Microsoft.Practices.Composite.UnityExtensions;
using Microsoft.Practices.Composite.Modularity;
using System.Windows;
using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Modules.AudioPane;
using Tobi.Modules.DocumentPane;
using Tobi.Modules.FileDialog;
using Tobi.Modules.MetadataPane;
using Tobi.Modules.NavigationPane;
using Tobi.Modules.MenuBar;
using Tobi.Modules.ToolBars;
using Tobi.Modules.Urakawa;

namespace Tobi
{
    /// <summary>
    /// The Tobi-specific Bootstrapper
    /// </summary>
    public class Bootstrapper : UnityBootstrapper
    {
        private readonly BitFactoryLoggerAdapter m_Logger;

        public Bootstrapper()
        {
            m_Logger = new BitFactoryLoggerAdapter(); //EntLibLoggerAdapter();
            Debug.WriteLine("Testing redirection of System.Diagnostics.Debug/Trace output to application logger. This message should not appear in release mode.");
        }

        ///<summary>
        /// Overriding the default TRACE logger with our own (available application-wide, through the DI container)
        ///</summary>
        protected override ILoggerFacade LoggerFacade
        {
            get { return m_Logger; }
        }

        ///<summary>
        /// Registration of the Shell View into the container
        ///</summary>
        protected override void ConfigureContainer()
        {
            //Container.RegisterInstance<Dispatcher>(Dispatcher.CurrentDispatcher);

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

            /*
            var name = typeof (UrakawaModule).Name;
            var moduleCatalog = Container.Resolve<IModuleCatalog>();
            foreach (var module in moduleCatalog.Modules)
            {
                if (module.ModuleName == name)
                {
                    var moduleManager = Container.Resolve<IModuleManager>();
                    moduleManager.LoadModule(module.ModuleName);
                }
            }*/
        }

        /// <summary>
        /// Tobi loads its main Modules statically
        /// </summary>
        protected override IModuleCatalog GetModuleCatalog()
        {
            //return new DirectoryModuleCatalog() { ModulePath = @".\Modules" };

            return new ModuleCatalog()
                .AddModule(typeof(FileDialogModule))
                .AddModule(typeof(UrakawaModule))
                .AddModule(typeof(MenuBarModule), "UrakawaModule")
                .AddModule(typeof(NavigationPaneModule), "DocumentPaneModule")
                .AddModule(typeof(DocumentPaneModule))
                .AddModule(typeof(AudioPaneModule))
                .AddModule(typeof(MetadataPaneModule))
                .AddModule(typeof(ToolBarsModule), new string[]{"MetadataPaneModule", "UrakawaModule"}
        ); // TODO: remove this dependency, currently necessary to retrieve the local Command and inject it into a button}

            //.AddModule(typeof (StatusBarModule));
        }

        protected override RegionAdapterMappings ConfigureRegionAdapterMappings()
        {
            var mappings = base.ConfigureRegionAdapterMappings() ?? Container.Resolve<RegionAdapterMappings>();
            //mappings.RegisterMapping(typeof(Menu), Container.Resolve<DynamicItemsControlRegionAdapter>());
            //mappings.RegisterMapping(typeof(MenuItem), Container.Resolve<DynamicItemsControlRegionAdapter>());
            //mappings.RegisterMapping(typeof(ToolBarTray), Container.Resolve<ToolBarTrayRegionAdapter>());
            return mappings;
        }
    }
}