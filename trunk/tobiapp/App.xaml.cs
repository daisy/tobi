using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Navigation;
using Microsoft.Test;
using Tobi.Infrastructure;
using Tobi.Infrastructure.UI;

namespace Tobi
{
    /// <summary>
    /// The application delegates to the Composite WPF <see cref="Bootstrapper"/>
    /// </summary>
    public partial class App
    {
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (((TextBox)sender).SelectionLength == 0)
                ((TextBox)sender).SelectAll();
        }

        public SplashScreen SplashScreen
        {
            get;
            set;
        }

        ///<summary>
        /// Implements 2 runtimes: DEBUG and RELEASE
        ///</summary>
        ///<param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
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

            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;


            SplashScreen = new SplashScreen("TobiSplashScreen.png");
            SplashScreen.Show(false);

            BitFactoryLoggerAdapter.DeleteLogFile();

            PresentationTraceSources.ResourceDictionarySource.Listeners.Add(new ConsoleTraceListener());
            PresentationTraceSources.ResourceDictionarySource.Switch.Level = SourceLevels.All;

            PresentationTraceSources.DataBindingSource.Listeners.Add(new ConsoleTraceListener());
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;

            PresentationTraceSources.DependencyPropertySource.Listeners.Add(new ConsoleTraceListener());
            PresentationTraceSources.DependencyPropertySource.Switch.Level = SourceLevels.All;

            PresentationTraceSources.DocumentsSource.Listeners.Add(new ConsoleTraceListener());
            PresentationTraceSources.DocumentsSource.Switch.Level = SourceLevels.All;

            PresentationTraceSources.MarkupSource.Listeners.Add(new ConsoleTraceListener());
            PresentationTraceSources.MarkupSource.Switch.Level = SourceLevels.All;

            PresentationTraceSources.NameScopeSource.Listeners.Add(new ConsoleTraceListener());
            PresentationTraceSources.NameScopeSource.Switch.Level = SourceLevels.All;

            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            FrameworkElement.LanguageProperty.OverrideMetadata(
                  typeof(FrameworkElement),
                  new FrameworkPropertyMetadata(
                     XmlLanguage.GetLanguage(
                     CultureInfo.CurrentCulture.IetfLanguageTag)));

            EventManager.RegisterClassHandler(typeof(TextBox),
                UIElement.GotFocusEvent,
                new RoutedEventHandler(TextBox_GotFocus));



#if (FALSE && DEBUG)
            runInDebugMode();
#else
            runInReleaseMode();
#endif
        }

        private Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            Assembly parentAssembly = Assembly.GetExecutingAssembly();

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
                handleException(ex, true);
            }
        }

        private static void appDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            handleException(e.ExceptionObject as Exception, e.IsTerminating);
        }

        public static void handleException(Exception ex, bool doExit)
        {
            if (ex == null)
                return;

            //ExceptionPolicy.HandleException(ex, "Default Policy");
            //MessageBox.Show(UserInterfaceStrings.UnhandledException);
            //TaskDialog.ShowException("Unhandled Exception !", UserInterfaceStrings.UnhandledException, ex);


            var margin = new Thickness(0, 0, 0, 8);

            var panel = new DockPanel { LastChildFill = true };

            string logPath = Directory.GetCurrentDirectory() + @"\Tobi.log";

            var labelMsg = new TextBox
                            {
                                FontWeight = FontWeights.ExtraBlack,
                                Text = UserInterfaceStrings.UnhandledException + String.Format("\n[{0}]", logPath),
                                Margin = margin,
                                HorizontalAlignment = HorizontalAlignment.Stretch,
                                VerticalAlignment = VerticalAlignment.Top,
                                TextWrapping = TextWrapping.Wrap,
                                IsReadOnly = true,
                                BorderBrush = Brushes.Red,
                                BorderThickness = new Thickness(1),
                                SnapsToDevicePixels = true
                            };
            labelMsg.SetValue(DockPanel.DockProperty, Dock.Top);
            panel.Children.Add(labelMsg);

            var labelSummary = new TextBox
            {
                Text = ex.Message,
                Margin = margin,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                TextWrapping = TextWrapping.Wrap,
                IsReadOnly = true,
                Background = SystemColors.ControlBrush,
                BorderBrush = SystemColors.ControlDarkBrush,
                BorderThickness = new Thickness(1),
                SnapsToDevicePixels = true
            };
            labelSummary.SetValue(DockPanel.DockProperty, Dock.Top);
            panel.Children.Add(labelSummary);

            var stackTrace = new TextBox
                                 {
                                     Text = ex.StackTrace,
                                     HorizontalAlignment = HorizontalAlignment.Stretch,
                                     VerticalAlignment = VerticalAlignment.Stretch,
                                     TextWrapping = TextWrapping.Wrap,
                                     IsReadOnly = true,
                                     Background = SystemColors.ControlBrush,
                                     BorderBrush = SystemColors.ControlDarkDarkBrush,
                                     BorderThickness = new Thickness(1),
                                     SnapsToDevicePixels = true
                                 };

            var scroll = new ScrollViewer
                             {
                                 Content = stackTrace,
                                 Margin = margin,
                                 HorizontalAlignment = HorizontalAlignment.Stretch,
                                 VerticalAlignment = VerticalAlignment.Stretch,
                             };
            panel.Children.Add(scroll);

            /*
            var logStr = String.Format("CANNOT OPEN [{0}] !", logPath);

            var logFile = File.Open(logPath, FileMode.Open, FileAccess.Read);
            if (logFile.CanRead)
            {
                logFile.Close();
                //logFile.Read(bytes, int, int)
                logStr = File.ReadAllText(logPath);
            }

            var log = new TextBlock
            {
                Text = logStr,
                Margin = new Thickness(15),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                TextWrapping = TextWrapping.Wrap,
            };

            var scroll2 = new ScrollViewer
            {
                Content = log,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            panel.Children.Add(scroll2);
             * */

            var windowPopup = new PopupModalWindow(Current.MainWindow,
                                                   UserInterfaceStrings.EscapeMnemonic(
                                                       UserInterfaceStrings.Unexpected),
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.Ok,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   false, 500, 300);

            SystemSounds.Exclamation.Play();
            windowPopup.Show();

            if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Ok
                && doExit)
            {
                Environment.Exit(1);
            }
        }

        private void OnApplicationLoadCompleted(object sender, NavigationEventArgs e)
        {
            bool debug = true;
        }

        private void OnApplicationStartup(object sender, StartupEventArgs e)
        {
        }
    }
}