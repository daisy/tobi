using System;
using System.ComponentModel.Composition;
using System.Deployment.Application;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Test;
using Tobi.Common;
using Tobi.Common.UI;

namespace Tobi
{
    /// <summary>
    /// The application delegates to the Composite WPF <see cref="Bootstrapper"/>
    /// </summary>
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

        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [STAThreadAttribute()]
        [DebuggerNonUserCodeAttribute()]
        [LoaderOptimization(LoaderOptimization.SingleDomain)]
        public static void Main()
        {
            var app = new App();
            app.InitializeComponent();
            app.Run();
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

            //TODO: See Mono.Options for managing command line parameters.

            // Ignore 0 index:
            //Environment.GetCommandLineArgs()

            //Tobi.exe -verbose -debuglevel:3
            var d = CommandLineDictionary.FromArguments(e.Args, '-', ':');

            //bool verbose = d.ContainsKey("verbose");
            //int testId = Int32.Parse(d["debuglevel"]);

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
            Debug.Assert(CultureInfo.CurrentCulture.Equals(Thread.CurrentThread.CurrentCulture));
            Debug.Assert(CultureInfo.CurrentUICulture.Equals(Thread.CurrentThread.CurrentUICulture));

            //var c1 = CultureInfo.CurrentCulture;
            //var c2 = CultureInfo.CurrentUICulture;
            //Debug.Assert(c1.Equals(c2));

            if (str == "en") str = "en-GB";
            if (str == "fr") str = "fr-FR";

            var c3 = CultureInfo.GetCultureInfoByIetfLanguageTag(str);

            var c4 = CultureInfo.GetCultureInfo(str);

            Debug.Assert(c3.Equals(c4));

            var c5 = new CultureInfo(str);

            Debug.Assert(c3.Equals(c5));

            //if (!c4.IsNeutralCulture)
            //{
            //    Thread.CurrentThread.CurrentCulture = c4;
            //}
            Thread.CurrentThread.CurrentUICulture = c4;

            //Debug.Assert(Thread.CurrentThread.CurrentUICulture.Equals(Thread.CurrentThread.CurrentCulture));

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

        /// <summary>
        /// Called after OnStartup()
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnApplicationStartup(object sender, StartupEventArgs e)
        {
#if DEBUG
            var str = Tobi_Lang.LangStringKey1;

            SetCulture("fr");
            str = Tobi_Lang.LangStringKey1;

            SetCulture("hi");
            str = Tobi_Lang.LangStringKey1;

            SetCulture(Settings.Default.Lang);
            str = Tobi_Lang.LangStringKey1;
#else //DEBUG
            SetCulture(Settings.Default.Lang);
#endif //DEBUG

            //to use on individual forms: this.Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);
            FrameworkElement.LanguageProperty.OverrideMetadata(
                  typeof(FrameworkElement),
                  new FrameworkPropertyMetadata(
                     XmlLanguage.GetLanguage(
                     CultureInfo.CurrentCulture.IetfLanguageTag)));

            Timeline.DesiredFrameRateProperty.OverrideMetadata(
                typeof(Timeline),
                new FrameworkPropertyMetadata { DefaultValue = 20 }
                );

            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

            SplashScreen = new SplashScreen(Assembly.GetExecutingAssembly(), "TobiSplashScreen.png");
            SplashScreen.Show(false);

            ShutdownMode = ShutdownMode.OnMainWindowClose;


#if (FALSE && DEBUG && VSTUDIO) // We want the release-mode exception capture dialog, even when debugging in Visual Studio
            runInDebugMode();
#else
            runInReleaseMode();
#endif
        }

        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
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

        private static void runInDebugMode()
        {
            var bootstrapper = new Bootstrapper();
            bootstrapper.Run();
        }

        private static void runInReleaseMode()
        {
            AppDomain.CurrentDomain.UnhandledException += appDomainUnhandledException;
            try
            {
                var bootstrapper = new Bootstrapper();
                bootstrapper.Run();
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