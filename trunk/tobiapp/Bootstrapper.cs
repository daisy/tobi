using System;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using MefContrib.Integration.Unity;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Regions;
using Microsoft.Practices.Composite.UnityExtensions;
using Microsoft.Practices.Composite.Modularity;
using System.Windows;
using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Common.UI;
using Tobi.Plugin.AudioPane;
using Tobi.Plugin.DocumentPane;
using Tobi.Plugin.MenuBar;
using Tobi.Plugin.MetadataPane;
using Tobi.Plugin.NavigationPane;
using Tobi.Plugin.Settings;
using Tobi.Plugin.StructureTrailPane;
using Tobi.Plugin.ToolBars;
using Tobi.Plugin.Urakawa;
using Tobi.Plugin.Validator;
using Tobi.Plugin.Validator.ContentDocument;
using Tobi.Plugin.Validator.Metadata;
using urakawa.ExternalFiles;

namespace Tobi
{
    ///<summary>
    /// Empty CAG module (Prism/CompositeWPF/Composite Application Guidance)
    ///</summary>
    public class DummyModule : IModule
    {
        public void Initialize()
        {
        }
    }

    /// <summary>
    /// The Tobi application bootstrapper:
    /// overrides the logger,
    /// initializes the MEF composition container
    /// as well as the Unity dependency injection container,
    /// and binds their respective catalogs (bi-directionally).
    /// Then displays the shell window,
    /// and loads the available plugins,
    /// which fulfills contracts for extension points
    /// and results in the application loading entirely
    /// (dependencies being resolved automatically by the container)
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
            m_Logger = new TobiLoggerFacade();

            Console.WriteLine(@"CONSOLE -- Testing redirection from System.Console.WriteLine() to the application logger.");
            Debug.WriteLine(@"DEBUG -- Testing redirection from System.Diagnostics.Debug.WriteLine() to the application logger. This message should not appear in RELEASE mode (only when DEBUG flag is set).");
            Trace.WriteLine(@"TRACE -- Testing redirection from System.Diagnostics.Trace.WriteLine() to the application logger. This message should not appear in RELEASE mode (and in DEBUG mode only when TRACE flag is set).");

            m_Logger.Log(@"[" + DateTime.Now + @"]", Category.Info, Priority.High);
            m_Logger.Log(@"[" + Tobi_Lang.LOG_FILE_PATH + @"]", Category.Info, Priority.High);
            m_Logger.Log(@"[Tobi version: " + Tobi_Lang.APP_VERSION + @"]", Category.Info, Priority.High);

            m_Logger.Log(@"[Tobi app data folder: " + ExternalFilesDataManager.STORAGE_FOLDER_PATH + @"]", Category.Info, Priority.High);
            m_Logger.Log(@"[Tobi exe/log folder: " + Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"]", Category.Info, Priority.High);

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
            m_Logger.Log(@"[Tobi user config folder: " + Path.GetDirectoryName(config.FilePath) + @"]", Category.Info, Priority.High);
        }

        /// <summary>
        /// We implement ILoggerFacade using a custom BitFactory logger
        /// </summary>
        private readonly TobiLoggerFacade m_Logger;

        ///<summary>
        /// We override the default logger (singleton) supplied through the Unity Dependency Injection container
        ///</summary>
        protected override ILoggerFacade LoggerFacade
        {
            get { return m_Logger; }
        }

        /// <summary>
        /// We keep a local copy of the MEF container (and we actually register it into the Unity DI container).
        /// Although the MEF attributed composition model (import/export) mean that we don't really need a reference to the container,
        /// we may need to access the composition container to setup catalogs, etc.
        /// </summary>
        public CompositionContainer MefContainer { get; private set; }

        protected override IUnityContainer CreateContainer()
        {
            LoggerFacade.Log(@"Creating the Unity Dependency Injection container and binding (bi-directionally) with the default MEF composition catalog (i.e. Tobi.exe, for the empty shell window)", Category.Debug, Priority.Low);

            // MEF will scan Tobi.exe only to start with, so that the shell window doesn't attempt to load dependencies immediately but in a differed manner
            // (through a container re-composition when we add further MEF discovery catalogs)
            var aggregateCatalog = new AggregateCatalog(new ComposablePartCatalog[]
            {
                      new AssemblyCatalog(Assembly.GetExecutingAssembly()),
                      //new AssemblyCatalog(this.GetType().Assembly),
                      //new AssemblyCatalog(typeof(Shell).Assembly),

                      //new TypeCatalog(typeof(typeX))
                      //new TypeCatalog(typeof(type1), typeof(type2), ...)
            });

            // This instance, once returned to this method, will be available as the "Container" class member (property),
            // and it will be registered within the Unity dependency injection container itself.
            var unityContainer = new UnityContainer();

            // Bidirectional binding between MEF and Unity.
            // Remark: calls MEF-Compose, which triggers application Parts scanning
            // (building the graph of dependencies, and pruning branches that are rejected),
            // but the OnImportsSatisfied callbacks will only be activated at instanciation time.
            MefContainer = unityContainer.RegisterCatalog(aggregateCatalog);

            return unityContainer;
        }

        protected override void ConfigureContainer()
        {
            // We make the MEF composition container available through the Unity Dependency Injection container
            // (just in case we need a reference to MEF in order to manipulate catalogs, exports, etc. from a plugin)
            Container.RegisterInstance(typeof(CompositionContainer), MefContainer);
            RegisterTypeIfMissing(typeof(IRegionNamedViewRegistry), typeof(RegionNamedViewRegistry), true);

            //Container.RegisterInstance(Dispatcher.CurrentDispatcher);

            base.ConfigureContainer();
        }

        /// <summary>
        /// We return a dummy empty CAG module catalog,
        /// as we use MEF to dynamically discover and compose parts.
        /// MEF is a much more efficient way of resolving dependencies.
        /// </summary>
        /// <returns></returns>
        protected override IModuleCatalog GetModuleCatalog()
        {
            //return new DirectoryModuleCatalog() { ModulePath = @".\Modules" };

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

        protected override IRegionBehaviorFactory ConfigureDefaultRegionBehaviors()
        {
            var defaultRegionBehaviorTypesDictionary = base.ConfigureDefaultRegionBehaviors();

            if (defaultRegionBehaviorTypesDictionary != null)
            {
                defaultRegionBehaviorTypesDictionary.AddIfMissing(AutoPopulateRegionBehaviorNamedViews.BehaviorKey, typeof(AutoPopulateRegionBehaviorNamedViews));
                defaultRegionBehaviorTypesDictionary.AddIfMissing(ClearChildViewsRegionBehavior.BehaviorKey, typeof(ClearChildViewsRegionBehavior));
            }

            return defaultRegionBehaviorTypesDictionary;
        }

        protected override RegionAdapterMappings ConfigureRegionAdapterMappings()
        {
            RegionAdapterMappings regionAdapterMappings = Container.TryResolve<RegionAdapterMappings>();
            if (regionAdapterMappings != null)
            {
#if SILVERLIGHT
                regionAdapterMappings.RegisterMapping(typeof(TabControl), this.Container.Resolve<TabControlRegionAdapter>());
#endif
                regionAdapterMappings.RegisterMapping(typeof(Selector), this.Container.Resolve<SelectorRegionAdapter>());
                regionAdapterMappings.RegisterMapping(typeof(ItemsControl), this.Container.Resolve<PreferredPositionItemsControlRegionAdapter>());
                regionAdapterMappings.RegisterMapping(typeof(ContentControl), this.Container.Resolve<ContentControlRegionAdapter>());
            }

            return regionAdapterMappings;
        
            //var mappings = base.ConfigureRegionAdapterMappings();
            //if (mappings != null)
            //{
            //    //mappings.RegisterMapping(typeof(Menu), Container.Resolve<DynamicItemsControlRegionAdapter>());
            //    //mappings.RegisterMapping(typeof(MenuItem), Container.Resolve<DynamicItemsControlRegionAdapter>());
            //    //mappings.RegisterMapping(typeof(ToolBarTray), Container.Resolve<ToolBarTrayRegionAdapter>());
            //}
            //return mappings;
        }

        /// <summary>
        /// We display Tobi's main window (the "shell")
        /// Remark: CAG regions defines extension points for views,
        /// which get loaded as ITobiModule instances get created.
        /// </summary>
        protected override DependencyObject CreateShell()
        {
            //var shellView = Container.Resolve<IShellView>();
            var shellView = MefContainer.GetExportedValue<IShellView>();
            shellView.Show();

            return shellView as DependencyObject;
        }

        /// <summary>
        /// The shell is loaded at this stage, so we load the rest of the application
        /// by discovering plugins and the extension points they fulfill,
        /// and by resolving instances through the MEF dependency graph. 
        /// </summary>
        protected override void InitializeModules()
        {
            // Does nothing, as we do not use any real CAG IModule (only the dummy empty one).
            base.InitializeModules();

            // MEF will fetch DLL assemblies adjacent to the Tobi.exe executable
            // We could add support for a special "plugin" folder,
            // using something like: + Path.DirectorySeparatorChar + "addons";
            string mefDir = AppDomain.CurrentDomain.BaseDirectory;

            // TODO: save the catalog in the MEF container,
            // so we can call dirCatalog.Refresh(); when needed (which triggers re-composition)
            var dirCatalog = new DirectoryCatalog(mefDir, @"Tobi.Plugin.*.dll");

            // TODO: deactivated for debugging only ! (to avoid scanning DLLs other than the below explicit ones)
            //Container.RegisterCatalog(dirCatalog);

            // We could add MEF sub-directories too
            //var directories = Directory.GetDirectories(mefDir, "*.*", SearchOption.AllDirectories);
            //foreach (var directory in directories)
            //{
            //    aggregateCatalog.Catalogs.Add(new DirectoryCatalog(directory));
            //}

            //MessageBox.Show(@"Just before resolving Urakawa session (take a look at the window title 'waiting...')");

            // NOTE: we're loading assemblies manually so that they get picked-up by ClickOnce deployment.
            Container.RegisterCatalog(new AggregateCatalog(new ComposablePartCatalog[]
            {
                new AssemblyCatalog(Assembly.GetAssembly(typeof(AbstractTobiPlugin))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(AudioPanePlugin))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(UrakawaPlugin))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(ValidatorPlugin))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(MetadataValidatorPlugin))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(ContentDocumentValidatorPlugin))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(MetadataPanePlugin))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(StructureTrailPanePlugin))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(DocumentPanePlugin))),
                //new AssemblyCatalog(Assembly.GetAssembly(typeof(HeadingNavigationPlugin))), // in the same assembly as the main Navigation Plugin, so not needed
                //new AssemblyCatalog(Assembly.GetAssembly(typeof(PageNavigationPlugin))), // in the same assembly as the main Navigation Plugin, so not needed
                new AssemblyCatalog(Assembly.GetAssembly(typeof(NavigationPanePlugin)))
            }));

            //MessageBox.Show(@"Urakawa session should now be resolved (window title changed)");

            var tobiModules = MefContainer.GetExportedValues<ITobiPlugin>();
            foreach (var tobiModule in tobiModules)
            {
                Debug.Assert(!string.IsNullOrEmpty(tobiModule.Name) && !string.IsNullOrEmpty(tobiModule.Description));
                LoggerFacade.Log(@"Loaded plugin: [[" + tobiModule.Name + @"]] [[" + tobiModule.Version + @"]] [[" + tobiModule.Description + @"]]", Category.Debug, Priority.Low);
            }

            //MessageBox.Show(@"Urakawa module is loaded but it 'waits' for a toolbar to push its commands: press ok to get the toolbar view to load (but not to display yet)");

            // This artificially emulates the dynamic loading of the Toolbar and Menubar plugina:
            // the container gets composed again and the modules dependent on the toolbar/menubar gets satisified
            Container.RegisterCatalog(new AggregateCatalog(new ComposablePartCatalog[]
            {
                new AssemblyCatalog(Assembly.GetAssembly(typeof(ToolBarsPlugin))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(MenuBarPlugin))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(SettingsPlugin)))
            }));


            //MessageBox.Show(@"After pressing ok the toolbar module will load to integrate the view into the window");


            var tobiModulesAFTER = MefContainer.GetExportedValues<ITobiPlugin>();
            foreach (var tobiModuleAFTER in tobiModulesAFTER)
            {
                Debug.Assert(!string.IsNullOrEmpty(tobiModuleAFTER.Name) && !string.IsNullOrEmpty(tobiModuleAFTER.Description));
                LoggerFacade.Log(@"Loaded plugins: [[" + tobiModuleAFTER.Name + @"]] [[" + tobiModuleAFTER.Description + @"]]", Category.Debug, Priority.Low);
            }

            // In ClickOnce application manifest:
            //<fileAssociation xmlns="urn:schemas-microsoft-com:clickonce.v1" extension=".text" description="Text  Document (ClickOnce)" progid="Text.Document" defaultIcon="text.ico" />
            //
            // Arguments for ClickOnce-actived application are passed here (including file path due to file extension association):
            //string[] args = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;
            //if (args != null && args.Length != 0)
            //{
            //    try
            //    {
            //        Uri uri = new Uri(args[0]);
            //        if (!uri.IsFile)
            //            throw new UriFormatException("The URI " + uri + " is not a file.");

            //        OpenFile(uri.AbsolutePath);
            //    }
            //    catch (UriFormatException)
            //    {
            //        MessageBox.Show("Invalid file specified.", Program.Name);
            //    }
            //}

            // The code below is totally obsolete, as we are not using CAG modules.
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