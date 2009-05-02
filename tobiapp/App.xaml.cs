using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Navigation;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling;
using Sid.Windows.Controls;
using Tobi.Infrastructure;

namespace Tobi
{
    /// <summary>
    /// The application delegates to the Composite WPF <see cref="Bootstrapper"/>
    /// </summary>
    public partial class App
    {
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }

        public SplashScreen SplashScreen
        {
            get; set;
        }

        ///<summary>
        /// Implements 2 runtimes: DEBUG and RELEASE
        ///</summary>
        ///<param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
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

            SplashScreen = new SplashScreen("TobiSplashScreen.png");
            SplashScreen.Show(false);

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

#if (DEBUG)
            runInDebugMode();
#else
            runInReleaseMode();
#endif
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
                handleException(ex);
            }
        }

        private static void appDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            handleException(e.ExceptionObject as Exception);
        }

        private static void handleException(Exception ex)
        {
            if (ex == null)
                return;

            //ExceptionPolicy.HandleException(ex, "Default Policy");
            //MessageBox.Show(UserInterfaceStrings.UnhandledException);
            TaskDialog.ShowException("Unhandled Exception !", UserInterfaceStrings.UnhandledException, ex);
            Environment.Exit(1);
        }

        private void OnApplicationLoadCompleted(object sender, NavigationEventArgs e)
        {
            bool debug = true;
        }

        private void OnApplicationStartup(object sender, StartupEventArgs e)
        {
            bool debug = true;
        }
    }
}