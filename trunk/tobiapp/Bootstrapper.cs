using System;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Configuration;
using System.Deployment.Application;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using AudioLib;
using MefContrib.Integration.Unity;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Regions;
using Microsoft.Practices.Composite.UnityExtensions;
using Microsoft.Practices.Composite.Modularity;
using System.Windows;
using Microsoft.Practices.Unity;
using MSjogren.Samples.ShellLink;
using Tobi.Common;
using Tobi.Common.UI;
using Tobi.Plugin.AudioPane;
using Tobi.Plugin.Descriptions;
using Tobi.Plugin.DocumentPane;
using Tobi.Plugin.MenuBar;
using Tobi.Plugin.MetadataPane;
using Tobi.Plugin.NavigationPane;
using Tobi.Plugin.Settings;
using Tobi.Plugin.StructureTrailPane;
using Tobi.Plugin.ToolBars;
using Tobi.Plugin.Urakawa;
using Tobi.Plugin.Validator;
using Tobi.Plugin.Validator.MissingAudio;
using Tobi.Plugin.Validator.ContentDocument;
using Tobi.Plugin.Validator.Metadata;
using urakawa.ExternalFiles;
using System.Threading;

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
        private string askUserId(IShellView shellView, string title, string message, string info)
        {
            m_Logger.Log("Bootstrapper.askUserId", Category.Debug, Priority.Medium);

            var label = new TextBlock // TextBoxReadOnlyCaretVisible
            {
                //FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],

                //BorderThickness = new Thickness(1),
                //Padding = new Thickness(6),

                //TextReadOnly = message,
                Text = message,
                FontWeight = FontWeights.Bold,

                Margin = new Thickness(8, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap
            };

            var input = new TextBox()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Text = ""
            };

            //var iconProvider = new ScalableGreyableImageProvider(
            //    m_ShellView.LoadTangoIcon("help-browser"),
            //    m_ShellView.MagnificationLevel);

            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            //panel.Margin = new Thickness(8, 8, 8, 0);

            //panel.Children.Add(iconProvider.IconLarge);
            panel.Children.Add(label);
            panel.Children.Add(input);


            var checkBox = new CheckBox
            {
                FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],
                IsThreeState = false,
                IsChecked = Settings.Default.UserId_DoNotAskAgain,
                VerticalAlignment = VerticalAlignment.Center,
                Content = Tobi_Common_Lang.DoNotShowMessageAgain,
                Margin = new Thickness(0, 16, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            panel.Children.Add(checkBox);



            var details = new TextBoxReadOnlyCaretVisible
            {
                FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],

                BorderThickness = new Thickness(1),
                Padding = new Thickness(6),
                TextReadOnly = info
            };

            var windowPopup = new PopupModalWindow(shellView, // m_ShellView
                                                   title,
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.OkCancel, // PopupModalWindow.DialogButtonsSet.YesNo
                                                   PopupModalWindow.DialogButton.Ok, //PopupModalWindow.DialogButton.Yes
                                                   true, 425, 190, details, 70, null);

            windowPopup.ShowModal();

            Settings.Default.UserId_DoNotAskAgain = checkBox.IsChecked.Value;

            if (PopupModalWindow.IsButtonOkYesApply(windowPopup.ClickedDialogButton))
            {
                return input.Text;
            }

            return null;
        }

        public void usageReport(IShellView shellView)
        {
            string defaultSettingValue = (string)Settings.Default.Properties["UserId"].DefaultValue;
            string settingValue = Tobi.Settings.Default.UserId;
            if (!String.IsNullOrEmpty(settingValue))
            {
                settingValue = settingValue.Trim();
            }

            if (String.IsNullOrEmpty(settingValue) || settingValue.Equals(defaultSettingValue))
            {
                String userid = Settings.Default.UserId_DoNotAskAgain ? "" : askUserId(shellView, Tobi_Lang.UserId_title, Tobi_Lang.UserId_message, Tobi_Lang.UserId_details);
                if (!String.IsNullOrEmpty(userid))
                {
                    userid = userid.Trim();
                }
                if (String.IsNullOrEmpty(userid))
                {
                    Tobi.Settings.Default.UserId = defaultSettingValue;
                    settingValue = "";
                }
                else
                {
                    Tobi.Settings.Default.UserId = userid;
                    settingValue = userid;
                }
            }

            //http://blogs.msdn.com/yangxind/archive/2006/11/09/don-t-use-net-system-uri-unescapedatastring-in-url-decoding.aspx

            string url = ApplicationConstants.TOBI_ANON_USAGE_URI;

            url += "?version=" + Uri.EscapeDataString(ApplicationConstants.APP_VERSION);
            url += "&clickonce=" + (ApplicationDeployment.IsNetworkDeployed ? "true" :
                (Debugger.IsAttached ? "VS" : "false"));
            url += "&datetime=" + Uri.EscapeDataString(DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss_K", CultureInfo.InvariantCulture));
            url += "&datetimeutc=" + Uri.EscapeDataString(DateTime.UtcNow.ToString("yyyy-MM-dd_HH:mm:ss_K", CultureInfo.InvariantCulture));
            url += "&os=" + Uri.EscapeDataString(ApplicationConstants.OS_INFORMATION);
            url += "&lang=" + Thread.CurrentThread.CurrentUICulture;
            url += "&userid=" + Uri.EscapeDataString(settingValue);

            // THIS BREAKS PRIVACY, so we don't 
            //string ipAddress = "";
            //IPHostEntry ipHostEntry = Dns.GetHostEntry(Dns.GetHostName());
            //IPAddress[] ipAddresses = ipHostEntry.AddressList;
            //for (int i = 0; i < ipAddresses.Length; i++)
            //{
            //    if (ipAddresses[i].AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
            //    {
            //        if (!IPAddress.IsLoopback(ipAddresses[i]))
            //        {
            //            ipAddress += string.Format("_{0}", ipAddresses[i]);
            //        }
            //    }
            //    else
            //    {
            //        ipAddress += string.Format("__{0}", ipAddresses[i]);
            //    }
            //}
            //if (!string.IsNullOrEmpty(ipAddress))
            //{
            //    url += "&localip=" + Uri.EscapeDataString(ipAddress);
            //}

            if (Settings.Default.EnableAnonymousUsageReport)
            {
                var webClient = new WebClient { UseDefaultCredentials = true };
                StreamReader streamReader = null;
                try
                {
                    streamReader = new StreamReader(webClient.OpenRead(url), Encoding.UTF8);
                    string str = streamReader.ReadToEnd();
                    m_Logger.Log(str, Category.Info, Priority.High);
                }
                catch (Exception ex)
                {
                    m_Logger.Log(@"Can't query Tobi anonymous usage logger !", Category.Exception, Priority.High);
                    ExceptionHandler.LogException(ex);
                    //#if DEBUG
                    //                Debugger.Break();
                    //#endif // DEBUG
                }
                finally
                {
                    if (streamReader != null)
                        streamReader.Close();
                }
            }
            else
            {
                m_Logger.Log(@"Tobi anonymous usage logger has been DISABLED by user.", Category.Warn, Priority.High);
            }
        }

        /// <summary>
        /// We create our own logger from this constructor
        /// </summary>
        public Bootstrapper()
        {
            m_Logger = new TobiLoggerFacade();

            Console.WriteLine(@"CONSOLE -- Testing redirection from System.Console.WriteLine() to the application logger.");
            Debug.WriteLine(@"DEBUG -- Testing redirection from System.Diagnostics.Debug.WriteLine() to the application logger. This message should not appear in RELEASE mode (only when DEBUG flag is set).");
            Trace.WriteLine(@"TRACE -- Testing redirection from System.Diagnostics.Trace.WriteLine() to the application logger. This message should not appear in RELEASE mode (and in DEBUG mode only when TRACE flag is set).");

            //System.Globalization.CultureInfo.ClearCachedData();
            TimeZoneInfo.ClearCachedData();

            m_Logger.Log(@"[" + DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss_K", CultureInfo.InvariantCulture) + @"]", Category.Info, Priority.High);
            m_Logger.Log(@"[" + DateTime.UtcNow.ToString("yyyy-MM-dd_HH:mm:ss_K", CultureInfo.InvariantCulture) + @"]", Category.Info, Priority.High);
            m_Logger.Log(@"[" + ApplicationConstants.LOG_FILE_PATH + @"]", Category.Info, Priority.High);
            m_Logger.Log(@"[Tobi version: " + ApplicationConstants.APP_VERSION + @"]", Category.Info, Priority.High);
            m_Logger.Log(@"[OS version: " + ApplicationConstants.OS_INFORMATION + @"]", Category.Info, Priority.High);
            m_Logger.Log(@"[ClickOnce: " + (ApplicationDeployment.IsNetworkDeployed ? "yes" : "no") + @"]", Category.Info, Priority.High);

            //0 => No graphics hardware acceleration available for the application on the device.
            //1 => Partial graphics hardware acceleration available on the video card. This corresponds to a DirectX version that is greater than or equal to 7.0 and less than 9.0.
            //2 => A rendering tier value of 2 means that most of the graphics features of WPF should use hardware acceleration provided the necessary system resources have not been exhausted. This corresponds to a DirectX version that is greater than or equal to 9.0.
            int renderingTier = (RenderCapability.Tier >> 16);
            m_Logger.Log(@"[Bootstrapper RenderCapability.Tier: " + renderingTier + @"]", Category.Info, Priority.High);

            string appFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            m_Logger.Log(@"[Tobi app data and log folder: " + ExternalFilesDataManager.STORAGE_FOLDER_PATH + @"]", Category.Info, Priority.High);
            m_Logger.Log(@"[Tobi exe folder: " + appFolder + @"]", Category.Info, Priority.High);

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
            m_Logger.Log(@"[Tobi user config folder: " + Path.GetDirectoryName(config.FilePath) + @"]", Category.Info, Priority.High);

//#if ENABLE_LOG_DESKTOP_SHORTCUT

            string logPath = ApplicationConstants.LOG_FILE_PATH; //Path.Combine(appFolder, ApplicationConstants.LOG_FILE_NAME);
            string iconPath = Path.Combine(appFolder, "Shortcut.ico");
            string shortcutToLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
#if NET40
, "Tobi log (.NET4).lnk"
#else
, "Tobi log (.NET3).lnk"
#endif
);

//#if !DEBUG
            if (File.Exists(shortcutToLogPath))
            {
                try
                {
                    File.Delete(shortcutToLogPath);
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debugger.Break();
#endif
                }
            }
            try
            {
                using (var shortcut = new ShellShortcut(shortcutToLogPath))
                {
                    shortcut.Arguments = "";
                    shortcut.Description = "Shortcut to the Tobi log file";
                    //shortcut.IconPath = iconPath;
                    shortcut.Path = logPath;
                    shortcut.WorkingDirectory = appFolder;
                    shortcut.Save();
                }
            }
            catch (Exception ex)
            {
                m_Logger.Log(@"Can't create Tobi desktop shortcut!", Category.Exception, Priority.High);
                ExceptionHandler.LogException(ex);
#if DEBUG
                Debugger.Break();
#endif
            }
//#endif // DEBUG

//#endif //ENABLE_LOG_DESKTOP_SHORTCUT

            foreach (Assembly item in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (item.GlobalAssemblyCache)
                {
                    if (!string.IsNullOrEmpty(item.FullName)
                        && item.FullName.Contains("mscorlib"))
                    {
                        Console.WriteLine(item.FullName);
                        Console.WriteLine(item.Location);
                        Console.WriteLine(item.ImageRuntimeVersion);
                        //Console.WriteLine(item.GetName());
                        //Console.WriteLine(item.CodeBase);
                    }
                }
            }


            if (true || ApplicationDeployment.IsNetworkDeployed)
            {
                if (AppDomain.CurrentDomain.SetupInformation.ActivationArguments != null
                    && AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData != null
                    && AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData.Length > 0)
                {
                    string path = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData[0];
                    Console.WriteLine(@"APP PARAMETER: " + path);
                }
            }


            //    WshShellClass wsh = new WshShellClass();
            //            IWshRuntimeLibrary.IWshShortcut shortcut = wsh.CreateShortcut(
            //                Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\shorcut.lnk") as IWshRuntimeLibrary.IWshShortcut;
            //            shortcut.Arguments = "c:\\app\\settings1.xml";
            //            shortcut.TargetPath = "c:\\app\\myftp.exe";
            //            // not sure about what this is for
            //            shortcut.WindowStyle = 1; 
            //            shortcut.Description = "my shortcut description";
            //            shortcut.WorkingDirectory = "c:\\app";
            //            shortcut.IconLocation = "specify icon location";
            //            shortcut.Save();




            //private void appShortcutToDesktop(string linkName)
            //{
            //    string deskDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            //    using (StreamWriter writer = new StreamWriter(deskDir + "\\" + linkName + ".url"))
            //    {
            //        string app = System.Reflection.Assembly.GetExecutingAssembly().Location;
            //        writer.WriteLine("[InternetShortcut]");
            //        writer.WriteLine("URL=file:///" + app);
            //        writer.WriteLine("IconIndex=0");
            //        string icon = app.Replace('\\', '/');
            //        writer.WriteLine("IconFile=" + icon);
            //        writer.Flush();
            //    }
            //}





            //Set Shell = CreateObject("WScript.Shell")
            //DesktopPath = Shell.SpecialFolders("Desktop")
            //Set link = Shell.CreateShortcut(DesktopPath & "\test.lnk")
            //link.Arguments = "1 2 3"
            //link.Description = "test shortcut"
            //link.HotKey = "CTRL+ALT+SHIFT+X"
            //link.IconLocation = "app.exe,1"
            //link.TargetPath = "c:\blah\app.exe"
            //link.WindowStyle = 3
            //link.WorkingDirectory = "c:\blah"
            //link.Save



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

            // MEF will scan Tobi.exe only to start with,
            // so that the shell window doesn't attempt to load dependencies immediately but in a differed manner
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
            MefContainer = unityContainer.RegisterFallbackCatalog(aggregateCatalog);

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
                regionAdapterMappings.RegisterMapping(typeof(LazyKeepAliveTabControl), this.Container.Resolve<PreferredPositionSelectorRegionAdapter>());
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

            var shell = Container.TryResolve<IShellView>();
            DebugFix.Assert(shell != null);

            // MEF will fetch DLL assemblies adjacent to the Tobi.exe executable
            // We could add support for a special "plugin" folder,
            // using something like: + Path.DirectorySeparatorChar + "addons";
            string mefDir = AppDomain.CurrentDomain.BaseDirectory;


            // sowe can call dirCatalog.Refresh(); when needed (which triggers re-composition)
            //var dirCatalog = new DirectoryCatalog(mefDir, @"Tobi.Plugin.*.dll");
            //Container.RegisterCatalog(dirCatalog);

            // Any MEF part exported from this directory will take precedence over the built-in Tobi ones.
            // As a result, a single Import (as opposed to ImportMany) that has several realizations
            // (i.e. the default/fallback Tobi implementation and the custom Extension)
            // will not trigger an exception (as it is normally the case), instead the Extension will be returned, and the built-in feature ignored.
            var directories = Directory.GetDirectories(mefDir, @"Extensions", SearchOption.TopDirectoryOnly); // @"*.*"
            foreach (var directory in directories)
            {
                try
                {
                    // We use the direct loading below to avoid UNC network drives issues.
                    //Container.RegisterCatalog(new DirectoryCatalog(directory));

                    var extensionsAggregateCatalog = new AggregateCatalog();
                    foreach (string file in Directory.GetFiles(directory, "*.dll"))
                    {
                        var assembly = Assembly.LoadFrom(file);
                        extensionsAggregateCatalog.Catalogs.Add(new AssemblyCatalog(assembly));
                    }
                    Container.RegisterCatalog(extensionsAggregateCatalog);
                }
                catch (Exception ex)
                {
                    ExceptionHandler.Handle(ex, false, shell);
                }
            }
            //var dirCatalog = new DirectoryCatalog(mefDir, @"Tobi.Plugin.MenuBarDebug*.dll");
            //Container.RegisterCatalog(dirCatalog);


            var tobiModulesExtensions = MefContainer.GetExportedValues<ITobiPlugin>();
            foreach (var tobiModuleEXT in tobiModulesExtensions)
            {
                DebugFix.Assert(!string.IsNullOrEmpty(tobiModuleEXT.Name) && !string.IsNullOrEmpty(tobiModuleEXT.Description));
                LoggerFacade.Log(@"Loaded extension plugin: [[" + tobiModuleEXT.Name + @"]] [[" + tobiModuleEXT.Description + @"]]", Category.Debug, Priority.Low);
            }

            // NOTE: we're loading assemblies manually so that they get picked-up by ClickOnce deployment (and it means we can control the order of registration).

            try
            {
                Container.RegisterFallbackCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(UrakawaPlugin))));
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, false, shell);
            }
            try
            {
                Container.RegisterFallbackCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(AbstractTobiPlugin))));
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, false, shell);
            }
            try
            {
                Container.RegisterFallbackCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(AudioPanePlugin))));
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, false, shell);
            }
            try
            {
                Container.RegisterFallbackCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(ValidatorPlugin))));
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, false, shell);
            }
            try
            {
                Container.RegisterFallbackCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(MetadataValidatorPlugin))));
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, false, shell);
            }
            try
            {
                Container.RegisterFallbackCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(ContentDocumentValidatorPlugin))));
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, false, shell);
            }
            try
            {
                Container.RegisterFallbackCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(MissingAudioValidatorPlugin))));
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, false, shell);
            }
            try
            {
                Container.RegisterFallbackCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(MetadataPanePlugin))));
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, false, shell);
            }
            try
            {
                Container.RegisterFallbackCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(StructureTrailPanePlugin))));
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, false, shell);
            }
            try
            {
                Container.RegisterFallbackCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(DocumentPanePlugin))));
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, false, shell);
            }
            try
            {
                Container.RegisterFallbackCatalog(new AggregateCatalog(new ComposablePartCatalog[]
                {
                    //new AssemblyCatalog(Assembly.GetAssembly(typeof(HeadingNavigationPlugin))), // in the same assembly as the main Navigation Plugin, so not needed
                    //new AssemblyCatalog(Assembly.GetAssembly(typeof(PageNavigationPlugin))), // in the same assembly as the main Navigation Plugin, so not needed
                    //new AssemblyCatalog(Assembly.GetAssembly(typeof(MarkersNavigationPlugin))), // in the same assembly as the main Navigation Plugin, so not needed
                    new AssemblyCatalog(Assembly.GetAssembly(typeof(NavigationPanePlugin)))
                }));
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, false, shell);
            }
            try
            {
                //new AssemblyCatalog(Assembly.GetAssembly(typeof(DescriptionsNavigationPlugin))), // in the same assembly as the main Navigation Plugin, so not needed
                Container.RegisterFallbackCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(DescriptionsPlugin))));
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, false, shell);
            }


            //MessageBox.Show(@"Urakawa session should now be resolved (window title changed)");

            var tobiModules = MefContainer.GetExportedValues<ITobiPlugin>();
            foreach (var tobiModule in tobiModules)
            {
                DebugFix.Assert(!string.IsNullOrEmpty(tobiModule.Name) && !string.IsNullOrEmpty(tobiModule.Description));
                LoggerFacade.Log(
                    @"Loaded plugin: [[" + tobiModule.Name + @"]] [[" + tobiModule.Version + @"]] [[" +
                    tobiModule.Description + @"]]", Category.Debug, Priority.Low);
            }

            //MessageBox.Show(@"Urakawa module is loaded but it 'waits' for a toolbar to push its commands: press ok to get the toolbar view to load (but not to display yet)");

            // This artificially emulates the dynamic loading of the Toolbar and Menubar plugin:
            // the container gets composed again and the modules dependent on the toolbar/menubar gets satisified
            try
            {
                Container.RegisterFallbackCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(ToolBarsPlugin))));
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, false, shell);
            }
            try
            {
                Container.RegisterFallbackCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(MenuBarPlugin))));
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, false, shell);
            }
            try
            {
                Container.RegisterFallbackCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(SettingsPlugin))));
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, false, shell);
            }

            //MessageBox.Show(@"After pressing ok the toolbar module will load to integrate the view into the window");


            var tobiModulesAFTER = MefContainer.GetExportedValues<ITobiPlugin>();
            foreach (var tobiModuleAFTER in tobiModulesAFTER)
            {
                DebugFix.Assert(!string.IsNullOrEmpty(tobiModuleAFTER.Name) && !string.IsNullOrEmpty(tobiModuleAFTER.Description));
                LoggerFacade.Log(@"Loaded plugin AFTER: [[" + tobiModuleAFTER.Name + @"]] [[" + tobiModuleAFTER.Description + @"]]", Category.Debug, Priority.Low);
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



            usageReport(shell);
        }
    }
}