using System;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Threading;
using MefContrib.Integration.Unity;
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

        public CompositionContainer MefContainer { get; private set; }

        protected override IUnityContainer CreateContainer()
        {
            LoggerFacade.Log("Binding the catalogs from MEF and the Unity Dependency Injection container", Category.Debug, Priority.Low);

            var unityContainer = new UnityContainer();

            string mefDir = AppDomain.CurrentDomain.BaseDirectory; // +Path.DirectorySeparatorChar + "MEF";

            var aggregateCatalog = new AggregateCatalog(new ComposablePartCatalog[]
            {
                      new AssemblyCatalog(Assembly.GetExecutingAssembly()),
                      new DirectoryCatalog(mefDir, "Tobi*.dll")
                      //new TypeCatalog(typeof(MenuBarView))
            }); // MEF catalog

            //var directories = Directory.GetDirectories(mefDir, "*.*", SearchOption.AllDirectories);
            //foreach (var directory in directories)
            //{
            //    aggregateCatalog.Catalogs.Add(new DirectoryCatalog(directory));
            //}

            // bidirectional binding between MEF and Unity, calls Compose !
            MefContainer = unityContainer.RegisterCatalog(aggregateCatalog);

            return unityContainer;
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

            //var name = typeof (UrakawaModule).Name;
            //var moduleCatalog = Container.Resolve<IModuleCatalog>();
            //foreach (var module in moduleCatalog.Modules)
            //{
            //    if (module.ModuleName == name)
            //    {
            //        var moduleManager = Container.Resolve<IModuleManager>();
            //        moduleManager.LoadModule(module.ModuleName);
            //    }
            //}
        }

        protected override void ConfigureContainer()
        {
            //Container.RegisterInstance(Dispatcher.CurrentDispatcher);

            Container.RegisterType<IShellView, Shell>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IShellPresenter, ShellPresenter>(new ContainerControlledLifetimeManager());
            //Container.RegisterType<IUrakawaSession, UrakawaSession>(new ContainerControlledLifetimeManager());

            base.ConfigureContainer();
        }

        protected override IModuleCatalog GetModuleCatalog()
        {
            //return new DirectoryModuleCatalog() { ModulePath = @".\Modules" };

            return new ModuleCatalog()
                .AddModule(typeof(FileDialogModule))
                .AddModule(typeof(UrakawaModule))
                //.AddModule(typeof(ToolBarsModule), new string[]{"MetadataPaneModule", "UrakawaModule"})
                .AddModule(typeof(MenuBarModule), new[] { "MetadataPaneModule", "UrakawaModule" })
                .AddModule(typeof(NavigationPaneModule), "DocumentPaneModule")
                .AddModule(typeof(HeadingPaneModule), "NavigationPaneModule")
                .AddModule(typeof(PagePaneModule), "NavigationPaneModule")
                .AddModule(typeof(ToolBarsModule))
                .AddModule(typeof(DocumentPaneModule))
                .AddModule(typeof(AudioPaneModule))
                .AddModule(typeof(MetadataPaneModule))
                ;

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