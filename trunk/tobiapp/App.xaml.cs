using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Navigation;
using Microsoft.Practices.Composite.Logging;
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
        public class FocusOutlineAdorner : Adorner
        {
            private Pen m_pen;
            private Rect m_rectRect;

            public FocusOutlineAdorner(UIElement adornedElement)
                : base(adornedElement)
            {
                m_pen = new Pen(Brushes.Red, 1);
                m_pen.Freeze();
                m_rectRect = new Rect(0, 0, 1, 1);
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                var fe = AdornedElement as FrameworkElement;
                if (fe == null)
                {
                    return;
                }
                m_rectRect.Width = fe.ActualWidth;
                m_rectRect.Height = fe.ActualHeight;

                drawingContext.DrawRectangle(null, m_pen, m_rectRect);
            }
        }

        private void UIElement_LostKeyboardFocus(object sender, RoutedEventArgs e)
        {
            var ui = ((UIElement)sender);

            var oldALayer = AdornerLayer.GetAdornerLayer(ui);
            if (oldALayer == null)
            {
                return;
            }
            Adorner[] adorners = oldALayer.GetAdorners(ui);
            if (adorners == null)
            {
                return;
            }
            foreach (Adorner adorner in adorners)
            {
                if (adorner is FocusOutlineAdorner)
                {
                    oldALayer.Remove(adorner);
                }
            }
        }

        private void UIElement_GotKeyboardFocus(object sender, RoutedEventArgs e)
        {
            var ui = ((UIElement)sender);

            var aLayer = AdornerLayer.GetAdornerLayer(ui);
            if (aLayer == null)
            {
                return;
            }
            var theAdorner = new FocusOutlineAdorner(ui);
            aLayer.Add(theAdorner);
            theAdorner.InvalidateVisual();
        }

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
        { // Ignore 0 index:
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

        /// <summary>
        /// Called after OnStartup()
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnApplicationStartup(object sender, StartupEventArgs e)
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

            SplashScreen = new SplashScreen("TobiSplashScreen.png");
            SplashScreen.Show(false);

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

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            FrameworkElement.LanguageProperty.OverrideMetadata(
                  typeof(FrameworkElement),
                  new FrameworkPropertyMetadata(
                     XmlLanguage.GetLanguage(
                     CultureInfo.CurrentCulture.IetfLanguageTag)));

            EventManager.RegisterClassHandler(typeof(TextBox),
                UIElement.GotFocusEvent,
                new RoutedEventHandler(TextBox_GotFocus));

            /*
            EventManager.RegisterClassHandler(typeof(UIElement),
                UIElement.GotKeyboardFocusEvent,
                new RoutedEventHandler(UIElement_GotKeyboardFocus));

            EventManager.RegisterClassHandler(typeof(UIElement),
                UIElement.LostKeyboardFocusEvent,
                new RoutedEventHandler(UIElement_LostKeyboardFocus));
             * */

            BitFactoryLoggerAdapter.DeleteLogFile();

#if (FALSE && DEBUG)
            runInDebugMode();
#else
            runInReleaseMode();
#endif
        }

        private Assembly ResolveAssembly(object sender, ResolveEventArgs args)
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
                handleException(ex, true);
            }
        }

        private static void appDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            handleException(e.ExceptionObject as Exception, e.IsTerminating);
        }

        public static ILoggerFacade LOGGER = null;

        public static void logException(Exception ex)
        {
            if (LOGGER == null) return;

            if (ex.Message != null)
            {
                LOGGER.Log(ex.Message, Category.Exception, Priority.High);
            }
            if (ex.StackTrace != null)
            {
                LOGGER.Log(ex.StackTrace, Category.Exception, Priority.High);
            }

            if (ex.InnerException != null)
            {
                logException(ex.InnerException);
            }
        }

        public static void handleException(Exception ex, bool doExit)
        {
            if (ex == null)
                return;

            logException(ex);

            var margin = new Thickness(0, 0, 0, 8);

            //var panel = new DockPanel { LastChildFill = true };
            var panel = new StackPanel { Orientation = Orientation.Vertical };

            var labelSummary = new TextBox
            {
                Text = ex.Message,
                Margin = margin,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                TextWrapping = TextWrapping.Wrap,
                IsReadOnly = true,
                Background = SystemColors.ControlLightLightBrush,
                BorderBrush = SystemColors.ControlDarkBrush,
                BorderThickness = new Thickness(1),
                SnapsToDevicePixels = true,
                Padding = new Thickness(5)
            };
            //labelSummary.SetValue(DockPanel.DockProperty, Dock.Top);
            panel.Children.Add(labelSummary);

            var labelMsg = new TextBox
                            {
                                FontWeight = FontWeights.ExtraBlack,
                                Text = UserInterfaceStrings.UnhandledException,
                                Margin = margin,
                                HorizontalAlignment = HorizontalAlignment.Stretch,
                                VerticalAlignment = VerticalAlignment.Top,
                                TextWrapping = TextWrapping.Wrap,
                                IsReadOnly = true,
                                BorderBrush = Brushes.Red,
                                BorderThickness = new Thickness(1),
                                SnapsToDevicePixels = true,
                                Padding = new Thickness(5)
                            };
            //labelMsg.SetValue(DockPanel.DockProperty, Dock.Top);
            panel.Children.Add(labelMsg);


            var stackTrace = new TextBox
                                 {
                                     Text = ex.StackTrace,
                                     HorizontalAlignment = HorizontalAlignment.Stretch,
                                     VerticalAlignment = VerticalAlignment.Stretch,
                                     TextWrapping = TextWrapping.Wrap,
                                     IsReadOnly = true,
                                     Background = SystemColors.ControlLightLightBrush,
                                     BorderBrush = SystemColors.ControlDarkDarkBrush,
                                     BorderThickness = new Thickness(1),
                                     SnapsToDevicePixels = true,
                                     Padding = new Thickness(5)
                                 };

            var details = new ScrollViewer
                             {
                                 Content = stackTrace,
                                 Margin = margin,
                                 HorizontalAlignment = HorizontalAlignment.Stretch,
                                 VerticalAlignment = VerticalAlignment.Stretch
                             };
            //panel.Children.Add(scroll);

            /*
            var logStr = String.Format("CANNOT OPEN [{0}] !", logPath);

            var logFile = File.Open(logPath, FileMode.Open, FileAccess.Read, FileShare.Read);
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
                                                   false, 500, 200, details, 100);

            SystemSounds.Exclamation.Play();
            windowPopup.Show();

            if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Ok
                && doExit)
            {
                Application.Current.Shutdown();
                //Environment.Exit(1);
            }
        }
    }
}