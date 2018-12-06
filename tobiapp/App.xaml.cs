using System;
using System.Collections;
using System.Configuration;
using System.Deployment.Application;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Test;
using Tobi.Common.UI;

namespace Tobi
{
    public class AppRefProvider : MarshalByRefObject
    {
        private Application app;
        //private Dispatcher dispatcher;
        public AppRefProvider(Application a) //, Dispatcher d)
        {
            app = a;
            //dispatcher = d;
        }

        public void TryOpenFile(string filePath)
        {
            app.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (Action)(() =>
                             {
                                 var shell = app.MainWindow as Shell;
                                 if (shell != null)
                                 {
                                     shell.TryOpenFile(filePath);
                                 }
                             }
                        ));
        }
    }

    /// <summary>
    /// The application delegates to the Composite WPF <see cref="Bootstrapper"/>
    /// </summary>
    // [Serializable()]
    public partial class App
    {
        public App()
        {
            // Apparently fixes memory leaks...
            new HwndSource(new HwndSourceParameters());
        }

        public SplashScreen SplashScreen
        {
            get;
            set;
        }

        public string ParameterOpenFilePath;

        /// <summary>
        /// Application Entry Point.
        /// </summary>
        //[DebuggerNonUserCodeAttribute()]
        [STAThreadAttribute()]
        [LoaderOptimization(LoaderOptimization.SingleDomain)]
        public static void Main()
        {
            string productName = "Daisy_Tobi";
            string tobiAppRefProviderToken = "TobiAppRefProvider";

            bool firstInstance;
            var theMutex = new Mutex(false, "Local\\" + productName, out firstInstance);

            try
            {
                string[] appParamters = null;

                bool clickOnce = ApplicationDeployment.IsNetworkDeployed;
                if (clickOnce)
                {
                    appParamters = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;
                }
                else
                {
                    appParamters = Environment.GetCommandLineArgs();
                }
                bool hasParameters = appParamters != null &&
                                     (clickOnce ? appParamters.Length > 0
                                     : appParamters.Length > 1);

                string filePath = "";

                if (hasParameters)
                {
                    if (clickOnce)
                    {
                        try
                        {
                            filePath = new Uri(appParamters[0]).LocalPath;
                        }
                        catch
                        {
                            filePath = "";
                            MessageBox.Show("CANNOT PARSE URI: " + appParamters[0]);
                        }
                    }
                    else
                    {
                        try
                        {
                            //Tobi.exe /verbose /debuglevel=3
                            var dict = CommandLineDictionary.FromArguments(appParamters.Skip(1), '/', '=');

                            //bool verbose = d.ContainsKey("verbose");
                            //int testId = Int32.Parse(d["debuglevel"]);

                            string open;
                            dict.TryGetValue("open", out open);

                            if (!string.IsNullOrEmpty(open))
                            {
                                filePath = open;
                            }
                        }
                        catch
                        {
                            filePath = appParamters[1];
                        }
                    }
                }

                if (firstInstance)
                {
                    IChannel theChannel = new System.Runtime.Remoting.Channels.Ipc.IpcChannel(productName);
                    try
                    {
                        ChannelServices.RegisterChannel(theChannel, false);

                        var app = new App();
                        app.InitializeComponent();

                        DebugFix.Assert(app == Application.Current);

                        RemotingServices.Marshal(new AppRefProvider(app
                            //, app.Dispatcher
                            ), tobiAppRefProviderToken, typeof(AppRefProvider));

                        if (!string.IsNullOrEmpty(filePath))
                        {
                            app.ParameterOpenFilePath = filePath;
                        }

                        app.Run();
                    }
                    catch (SocketException ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    finally
                    {
                        ChannelServices.UnregisterChannel(theChannel);
                    }
                }
                else
                {
                    if (
#if DEBUG
true ||
#endif //DEBUG
 !string.IsNullOrEmpty(filePath))
                    {
#if DEBUG
                        MessageBox.Show("DEBUG: Tobi instance already running >> " + filePath);
#endif //DEBUG

                        try
                        {
                            AppRefProvider appRefProvider =
                                (AppRefProvider)RemotingServices.Connect(
                                    typeof(AppRefProvider),
                                    "ipc://" + productName + "/" + tobiAppRefProviderToken);

                            appRefProvider.TryOpenFile(filePath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                }
            }
            finally
            {
                theMutex.Close();
            }
        }

        ///<summary>
        /// Called after Main()
        ///</summary>
        ///<param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
#if DEBUG
            //System.Diagnostics.Debugger.Launch();
#endif

#if NET40
            //http://blogs.msdn.com/jgoldb/archive/2010/04/12/what-s-new-for-performance-in-wpf-in-net-4.aspx
            //http://blogs.msdn.com/jgoldb/archive/2007/10/10/performance-improvements-in-wpf-in-net-3-5-3-0-sp1.aspx

            Console.WriteLine(@"Shell WpfSoftwareRender => " + Tobi.Common.Settings.Default.WpfSoftwareRender);

            if (Tobi.Common.Settings.Default.WpfSoftwareRender)
            {
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            }

            //0 => No graphics hardware acceleration available for the application on the device.
            //1 => Partial graphics hardware acceleration available on the video card. This corresponds to a DirectX version that is greater than or equal to 7.0 and less than 9.0.
            //2 => A rendering tier value of 2 means that most of the graphics features of WPF should use hardware acceleration provided the necessary system resources have not been exhausted. This corresponds to a DirectX version that is greater than or equal to 9.0.
            int renderingTier = (RenderCapability.Tier >> 16);

            Console.WriteLine(@"Shell RenderCapability.Tier => " + renderingTier);
#endif //NET40
            //TODO: See Mono.Options for managing command line parameters.

            // Ignore 0 index:
            //Environment.GetCommandLineArgs()
            //or:

            /*
             * public class CommandLineArguments
{
   bool? Verbose { get; set; }
   int? DebugLevel { get; set; }
}

CommandLineArguments a = new CommandLineArguments();
CommandLineParser.ParseArguments(a, args);
             */

            //or:

            /*
             public class RunCommand : Command
{
   bool? Verbose { get; set; }
   int? RunId { get; set; }

   public override void Execute()
   {
      // Implement your "run" execution logic here.
   }
}

Command c = new RunCommand();
CommandLineParser.ParseArguments(c, args); 
c.Execute();
             */
            base.OnStartup(e);
        }

        private void SetCulture(string str)
        {
            DebugFix.Assert(CultureInfo.CurrentCulture.Equals(Thread.CurrentThread.CurrentCulture));
            DebugFix.Assert(CultureInfo.CurrentUICulture.Equals(Thread.CurrentThread.CurrentUICulture));

            //var c1 = CultureInfo.CurrentCulture;
            //var c2 = CultureInfo.CurrentUICulture;
            //DebugFix.Assert(c1.Equals(c2));

            if (str == "en") str = "en-GB";
            if (str == "fr") str = "fr-FR";

            var c3 = CultureInfo.GetCultureInfoByIetfLanguageTag(str);

            var c4 = CultureInfo.GetCultureInfo(str);

            DebugFix.Assert(c3.Equals(c4));

            var c5 = new CultureInfo(str);

            DebugFix.Assert(c3.Equals(c5));

            //if (!c4.IsNeutralCulture)
            //{
            //    Thread.CurrentThread.CurrentCulture = c4;
            //}
            Thread.CurrentThread.CurrentUICulture = c4;

            //Thread.CurrentThread.CurrentCulture = c4;
            //FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            //    new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            //DebugFix.Assert(Thread.CurrentThread.CurrentUICulture.Equals(Thread.CurrentThread.CurrentCulture));
            
            str = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            str = CultureInfo.CurrentCulture.ThreeLetterWindowsLanguageName;
            str = CultureInfo.CurrentCulture.ThreeLetterISOLanguageName;
            str = CultureInfo.CurrentCulture.NativeName;
            str = CultureInfo.CurrentCulture.Name;
            str = CultureInfo.CurrentCulture.IetfLanguageTag;
            str = CultureInfo.CurrentCulture.EnglishName;
            str = CultureInfo.CurrentCulture.DisplayName;

            str = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            str = CultureInfo.CurrentUICulture.ThreeLetterWindowsLanguageName;
            str = CultureInfo.CurrentUICulture.ThreeLetterISOLanguageName;
            str = CultureInfo.CurrentUICulture.NativeName;
            str = CultureInfo.CurrentUICulture.Name;
            str = CultureInfo.CurrentUICulture.IetfLanguageTag;
            str = CultureInfo.CurrentUICulture.EnglishName;
            str = CultureInfo.CurrentUICulture.DisplayName;
        }

        private void HandleConfigurationErrorsException(ConfigurationErrorsException ex)
        {
#if DEBUG
            Debugger.Break();
#endif
            string filename = ex.Filename;
            if (string.IsNullOrEmpty(filename)
                && ex.InnerException != null
                && ex.InnerException is ConfigurationErrorsException)
            {
                filename = ((ConfigurationErrorsException)ex.InnerException).Filename;
            }

            if (filename != null && File.Exists(filename))
            {
                File.Delete(filename);
            }

            string directory = Path.GetDirectoryName(filename);

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
            string settingsPath = Path.GetDirectoryName(config.FilePath);
            Shell.ExecuteShellProcess_(settingsPath);

            DebugFix.Assert(directory == settingsPath);

            Settings.Default.Reset();
            //Settings.Default.Reload();
            //Settings.Default.Upgrade();
        }

        /// <summary>
        /// Called after OnStartup()
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnApplicationStartup(object sender, StartupEventArgs e)
        {
            if (Tobi.Common.Settings.Default.UpgradeSettings)
            {
                if (!ApplicationDeployment.IsNetworkDeployed)
                {
                    // ClickOnce automatically Upgrades.

                    Settings.Default.Upgrade();
                    Tobi.Common.Settings.Default.Upgrade();
                }

                Tobi.Common.Settings.Default.UpgradeSettings = true; // ensure settings aggregator does its job of upgrading the other providers
            }

            try
            {
                SettingsPropertyCollection col1 = Settings.Default.Properties;
                IEnumerator enume1 = col1.GetEnumerator();
                while (enume1.MoveNext())
                {
                    var current = (SettingsProperty)enume1.Current;
                    Console.WriteLine("--- " + current.Name + ":");
                    Console.WriteLine(current.DefaultValue);
                    Console.WriteLine(Settings.Default[current.Name]);
                }
                //Settings.Default.Reload();
            }
            catch (ConfigurationErrorsException ex)
            {
                HandleConfigurationErrorsException(ex);
            }

#if DEBUG
            var str = Tobi_Lang.LangStringKey1;
#endif
            try
            {
#if DEBUG
                SetCulture("fr");
                str = Tobi_Lang.LangStringKey1;

                SetCulture("hi");
                str = Tobi_Lang.LangStringKey1;
#endif
                SetCulture(Settings.Default.Lang);
            }
            catch (ConfigurationErrorsException ex)
            {
                HandleConfigurationErrorsException(ex);
#if DEBUG
                MessageBox.Show(ex.Message);

                Process.GetCurrentProcess().Kill();
                return;
#endif
            }
#if NET40
            catch (CultureNotFoundException ex2)
            {
#if DEBUG
                Debugger.Break();
#endif
                Settings.Default.Lang = "en";
                SetCulture(Settings.Default.Lang);
            }
#else
            catch (ArgumentException ex2)
            {
#if DEBUG
                Debugger.Break();
#endif
                Settings.Default.Lang = "en";
                SetCulture(Settings.Default.Lang);
            }
#endif

#if DEBUG
            str = Tobi_Lang.LangStringKey1;
#endif

            if (false && ApplicationDeployment.IsNetworkDeployed)
            {
                string lang = Thread.CurrentThread.CurrentUICulture.ToString();

                ApplicationDeployment deploy = ApplicationDeployment.CurrentDeployment;
                try
                {
                    if (!deploy.IsFileGroupDownloaded(lang)) //deploy.IsFirstRun
                    {
                        deploy.DownloadFileGroup(lang);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);

                    MessageBox.Show(ex.Message);

                    // Log error. Do not report this error to the user, because a satellite
                    // assembly may not exist if the user's culture and the application's
                    // default culture match.
                }
            }

            //to use on individual forms: this.Language = XmlLanguage.GetLanguage(CultureInfo.CurrentUICulture.IetfLanguageTag);
            FrameworkElement.LanguageProperty.OverrideMetadata(
                  typeof(FrameworkElement),
                  new FrameworkPropertyMetadata(
                     XmlLanguage.GetLanguage(
                     CultureInfo.CurrentUICulture.IetfLanguageTag)));

            Timeline.DesiredFrameRateProperty.OverrideMetadata(
                typeof(Timeline),
                new FrameworkPropertyMetadata { DefaultValue = 20 }
                );

            //AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

            SplashScreen = new SplashScreen(Assembly.GetExecutingAssembly(), "TobiSplashScreen.png");
            SplashScreen.Show(false);

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            runBootstrapper();
        }

        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            return null;

            Assembly parentAssembly = Assembly.GetExecutingAssembly();
            //Application.ResourceAssembly = parentAssembly;

            var name = args.Name.Substring(0, args.Name.IndexOf(',')) + ".dll";
            var resourceName = parentAssembly.GetManifestResourceNames().First(s => s.EndsWith(name));

            using (Stream stream = parentAssembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    var block = new byte[stream.Length];
                    stream.Read(block, 0, block.Length);
                    return Assembly.Load(block);
                }
            }

            return null;
        }

        private static void runBootstrapper()
        {
            AppDomain.CurrentDomain.UnhandledException += appDomainUnhandledException;
            try
            {
                var bootstrapper = new Bootstrapper();
                bootstrapper.Run();

                var app = Application.Current as App;
                if (app != null)
                {
                    var shell = app.MainWindow as Shell;
                    if (shell != null)
                    {
                        shell.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                                     (Action)(() =>
                                shell.TryOpenFile(app.ParameterOpenFilePath)
                                                              ));
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionHandler.Handle(ex, true, null);
            }
        }

        private static void appDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ExceptionHandler.Handle(e.ExceptionObject as Exception, e.IsTerminating, null);
        }

        public static void LoadAndMergeResourceDictionary(string path, string assemblyFullName)
        {
            string dictionaryName = string.Format("/{0};component/{1}", assemblyFullName, path);
            var uri = new Uri(dictionaryName, UriKind.Relative);

            var dictionary = (ResourceDictionary)Application.LoadComponent(uri);

            // Or:
            //var resDictionary = new ResourceDictionary
            //{
            //    Source =
            //        new Uri(
            //        "pack://application:,,,/" + assemblyFullName +
            //        ";Component/" + path)
            //};

            Application.Current.Resources.MergedDictionaries.Add(dictionary);
        }
    }
}