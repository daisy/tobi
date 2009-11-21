using System;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Reflection;
using MefContrib.Integration.Unity;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Regions;
using Microsoft.Practices.Composite.UnityExtensions;
using Microsoft.Practices.Composite.Modularity;
using System.Windows;
using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Modules.ToolBars;
using Tobi.Modules.Urakawa;

namespace Tobi
{
    public class DummyModule : IModule{
        public void Initialize()
        {
            int debug = 1;
        }
    }

    /// <summary>
    /// The Tobi application bootstrapper:
    /// overrides the logger,
    /// defines the MEF and Unity catalogs
    /// and binds them together (so they can exchange instances transparently) 
    /// displays the shell.
    /// 
    /// Reminder of the call sequence:
    /// CreateContainer
    /// ConfigureContainer
    /// GetModuleCatalog
    /// ConfigureRegionAdapterMappings
    /// CreateShell
    /// InitializeModules
    /// </summary>
    public class Bootstrapper : UnityBootstrapper
    {
        /// <summary>
        /// We create our own logger from this constructor
        /// </summary>
        public Bootstrapper()
        {
            m_Logger = new BitFactoryLoggerAdapter();

            //LoggerFacade.Log(@"blabla", Category.Debug, Priority.Low);

            Console.WriteLine(@"Testing redirection of System.Console.WriteLine() to the application logger.");
            Debug.WriteLine(@"Testing redirection of System.Diagnostics.Debug.WriteLine() to the application logger. This message should not appear in RELEASE mode.");
        }

        /// <summary>
        /// We implement ILoggerFacade using a custom BitFactory logger
        /// </summary>
        private readonly BitFactoryLoggerAdapter m_Logger;

        ///<summary>
        /// We override the default logger (singleton) supplied through the Unity Dependency Injection container
        ///</summary>
        protected override ILoggerFacade LoggerFacade
        {
            get { return m_Logger; }
        }

        /// <summary>
        /// We keep a local copy of the MEF container,
        /// although the MEF attributed composition model mean that we don't really need it.
        /// (we can use the Unity Dependency Injection container instead)
        /// </summary>
        public CompositionContainer MefContainer { get; private set; }

        protected override IUnityContainer CreateContainer()
        {
            LoggerFacade.Log(@"Creating the bidirectionally-bound catalogs for MEF and the Unity Dependency Injection container", Category.Debug, Priority.Low);

            // MEF will scan Tobi.exe and any DLL assemblies which name starts with "Tobi"
            var aggregateCatalog = new AggregateCatalog(new ComposablePartCatalog[]
            {
                      new AssemblyCatalog(Assembly.GetExecutingAssembly()),

                      //new TypeCatalog(typeof(MenuBarView))
                      //new TypeCatalog(typeof(type1), typeof(type2), ...)
            });

            // We could add MEF sub-directories too
            //var directories = Directory.GetDirectories(mefDir, "*.*", SearchOption.AllDirectories);
            //foreach (var directory in directories)
            //{
            //    aggregateCatalog.Catalogs.Add(new DirectoryCatalog(directory));
            //}

            // Remark: this instance, once returned to this method, is available through the "Container" class member
            // (and registered in the Unity dependency injection container itself).
            var unityContainer = new UnityContainer();

            // bidirectional binding between MEF and Unity.
            // Remark: calls MEF-Compose, which triggers application Parts scanning,
            // but the OnImportsSatisfied callbacks will only be activated at instanciation time.
            MefContainer = unityContainer.RegisterCatalog(aggregateCatalog);

            return unityContainer;
        }

        /// <summary>
        /// We explicitly register type into the Unity Dependency Injection container,
        /// such as the Shell View and Presenter (so we guarantee they are available as soon as possible to consumers)
        /// </summary>
        protected override void ConfigureContainer()
        {
            // We make the MEF composition container available through the Unity Dependency Injection container
            Container.RegisterInstance(typeof(CompositionContainer), MefContainer);

            //Container.RegisterInstance(Dispatcher.CurrentDispatcher);

            base.ConfigureContainer();
        }

        /// <summary>
        /// We build a statically-predefined set of inter-dependent application parts to be loaded at startup.
        /// Remark: MEF may load parts in a more dynamic way.
        /// </summary>
        /// <returns></returns>
        protected override IModuleCatalog GetModuleCatalog()
        {
            // We could scan a directory instead of hard-coding types,
            // CAG would then find all implementation of IModule.
            // However we use MEF for such purpose, as it is more suitable for dynamic discovery.
            //return new DirectoryModuleCatalog() { ModulePath = @".\Modules" };

            // We let MEF pull the ITobiModules from the Tobi*.dll assemblies in the application directory.
            // This way, we don't have to statically declare dependencies (MEF resolves its own graph of application Parts).
            return new ModuleCatalog().AddModule(typeof(DummyModule))

                //.AddModule(typeof(UrakawaModule))
                //.AddModule(typeof (StatusBarModule))
                //.AddModule(typeof(FileDialogModule))
                //.AddModule(typeof(ToolBarsModule), new string[]{@"MetadataPaneModule", @"UrakawaModule"})
                //.AddModule(typeof(MenuBarModule), new[] { @"AudioPaneModule" }) //@"UrakawaModule", 
                //.AddModule(typeof(NavigationPaneModule), @"DocumentPaneModule")
                //.AddModule(typeof(HeadingPaneModule), @"NavigationPaneModule")
                //.AddModule(typeof(PagePaneModule), @"NavigationPaneModule")
                //.AddModule(typeof(ToolBarsModule))
                //.AddModule(typeof(DocumentPaneModule))
                //.AddModule(typeof(AudioPaneModule))
                //.AddModule(typeof(MetadataPaneModule), @"ToolBarsModule")
                //.AddModule(typeof(ValidatorModule), @"ToolBarsModule")
                ;
        }

        /// <summary>
        /// We don't actually perform any extra configuration here.
        /// </summary>
        protected override RegionAdapterMappings ConfigureRegionAdapterMappings()
        {
            var mappings = base.ConfigureRegionAdapterMappings();
            //if (mappings != null)
            //{
            //    mappings.RegisterMapping(typeof(Menu), Container.Resolve<DynamicItemsControlRegionAdapter>());
            //    mappings.RegisterMapping(typeof(MenuItem), Container.Resolve<DynamicItemsControlRegionAdapter>());
            //    mappings.RegisterMapping(typeof(ToolBarTray), Container.Resolve<ToolBarTrayRegionAdapter>());
            //}
            return mappings;
        }

        /// <summary>
        /// We display Tobi's main window (the "shell")
        /// Remark: CAG regions defines extension points for views, which can registered as Modules get initialized.
        /// </summary>
        protected override DependencyObject CreateShell()
        {
            //var shellView = Container.Resolve<IShellView>();
            var shellView = MefContainer.GetExportedValue<IShellView>();
            shellView.Show();

            return shellView as DependencyObject;
        }

        /// <summary>
        /// We don't actually perform any extra initialization here.
        /// </summary>
        protected override void InitializeModules()
        {
            // Does nothing, as we do not have CAG IModules (instead we use MEF, see below)
            base.InitializeModules();

            // MEF will fetch DLL assemblies adjacent to the Tobi.exe executable
            // We could add support for a special "plugin" folder, using something like: + Path.DirectorySeparatorChar + "addons";
            string mefDir = AppDomain.CurrentDomain.BaseDirectory;

            // TODO: save the catalog in the MEF container, so we can call dirCatalog.Refresh(); when needed (which triggers re-composition)
            var dirCatalog = new DirectoryCatalog(mefDir, @"Tobi.Modules.*.dll");
            
            // TODO: deactivated for debugging only ! (to avoid scanning DLLs other than the below explicit ones)
            //Container.RegisterCatalog(dirCatalog);

            MessageBox.Show(@"Just before resolving Urakawa session (take a look at the window title 'waiting...')");

            // TODO: for debugging only: we're loading selectively to avoid interference (and weird part dependencies)
            Container.RegisterCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(UrakawaModule))));

            MessageBox.Show(@"Urakawa session should now be resolved (window title changed)");

            var tobiModules = MefContainer.GetExportedValues<ITobiModule>();
            foreach (var tobiModule in tobiModules)
            {
                LoggerFacade.Log(@"Loaded plugins: [[" + tobiModule.Name + @"]] [[" + tobiModule.Description + @"]]", Category.Debug, Priority.Low);
            }

            MessageBox.Show(@"Urakawa module is loaded but it 'waits' for a toolbar to push its commands: press ok to get the toolbar view to load (but not to display yet)");

            // This artificially emulates the dynamic loading of the Toolbar plugin:
            // the container gets composed again and the modules dependent on the toolbar gets satisified
            Container.RegisterCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(ToolBarsModule))));


            MessageBox.Show(@"After pressing ok the toolbar module will load to integrate the view into the window");


            var tobiModulesAFTER = MefContainer.GetExportedValues<ITobiModule>();
            foreach (var tobiModuleAFTER in tobiModulesAFTER)
            {
                LoggerFacade.Log(@"Loaded plugins: [[" + tobiModuleAFTER.Name + @"]] [[" + tobiModuleAFTER.Description + @"]]", Category.Debug, Priority.Low);
            }

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
    }
}