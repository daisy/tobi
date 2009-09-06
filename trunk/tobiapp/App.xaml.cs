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

            SplashScreen = new SplashScreen(Assembly.GetExecutingAssembly(), "TobiSplashScreen.png");
            SplashScreen.Show(false);

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            //to use on individual forms: this.Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);
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

            var panel = new DockPanel { LastChildFill = true };
            //var panel = new StackPanel { Orientation = Orientation.Vertical };



            var labelMsg = new TextBoxEx(UserInterfaceStrings.UnhandledException)
            {
                FontWeight = FontWeights.ExtraBlack,
                Margin = margin,
                BorderBrush = Brushes.Red
            };
            

            //var binding = new Binding
            //                  {
            //                      Mode = BindingMode.OneWay,
            //                      Source = UserInterfaceStrings.UnhandledException,
            //                      Path = new PropertyPath(".")
            //                  };
            //var expr = labelMsg.SetBinding(TextBox.TextProperty, binding);

            //labelMsg.PreviewKeyDown += ((sender, e) =>
            //                              {
            //                                  if (!(e.Key == Key.Down || e.Key == Key.Up || e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Home || e.Key == Key.End || e.Key == Key.PageDown || e.Key == Key.PageUp || e.Key == Key.Tab))
            //                                      e.Handled = true;
            //                              });

            labelMsg.SetValue(DockPanel.DockProperty, Dock.Top);
            panel.Children.Add(labelMsg);


            var exMessage = ex.Message;
            Exception exinnerd = ex;
            while (exinnerd.InnerException != null)
            {
                if (!String.IsNullOrEmpty(exinnerd.InnerException.Message))
                {
                    exMessage += Environment.NewLine;
                    exMessage += "======";
                    exMessage += Environment.NewLine;
                    exMessage += exinnerd.InnerException.Message;
                }

                exinnerd = exinnerd.InnerException;
            }

            var labelSummary = new TextBoxEx(exMessage)
            {
                BorderBrush = null,
                BorderThickness = new Thickness(0)
            };

            var scroll = new ScrollViewer
            {
                Content = labelSummary,
                Margin = margin,
                BorderBrush = SystemColors.ControlDarkBrush,
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            //scroll.SetValue(DockPanel.DockProperty, Dock.Top);

            panel.Children.Add(scroll);

            // DETAILS:

            var exStackTrace = ex.StackTrace;
            Exception exinner = ex;
            while (exinner.InnerException != null)
            {
                if (!String.IsNullOrEmpty(exinner.InnerException.StackTrace))
                {
                    exStackTrace += Environment.NewLine;
                    exStackTrace += "=========================";
                    exStackTrace += Environment.NewLine;
                    exStackTrace += "-------------------------";
                    exStackTrace += Environment.NewLine;
                    exStackTrace += exinner.InnerException.StackTrace;
                }

                exinner = exinner.InnerException;
            }

            var stackTrace = new TextBoxEx(exStackTrace)
            {
                BorderBrush = null,
                BorderThickness = new Thickness(0)
            };

            var details = new ScrollViewer
                             {
                                 Content = stackTrace,
                                 Margin = margin,
                                 BorderBrush = SystemColors.ControlDarkDarkBrush,
                                 BorderThickness = new Thickness(1),
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

            var windowPopup = new PopupModalWindow(null,
                                                   UserInterfaceStrings.EscapeMnemonic(
                                                       UserInterfaceStrings.Unexpected),
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.Ok,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   false, 500, 200,
                                                   details, 130);

            SystemSounds.Exclamation.Play();
            windowPopup.ShowModal();

            if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Ok
                && doExit)
            {
                Application.Current.Shutdown();
                //Environment.Exit(1);
            }
        }

        public static void LoadAndMergeResourceDictionary(string path, string assemblyFullName)
        {
            string dictionaryName = string.Format("/{0};component/{1}", assemblyFullName, path);
            var uri = new Uri(dictionaryName, UriKind.Relative);
            var dictionary = (ResourceDictionary)Application.LoadComponent(uri);
            Application.Current.Resources.MergedDictionaries.Add(dictionary);
        }
    }
}