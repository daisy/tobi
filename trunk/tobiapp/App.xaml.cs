using System;
using System.Windows;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling;
using Tobi.Infrastructure;

namespace Tobi
{
    /// <summary>
    /// The application delegates to the Composite WPF <see cref="Bootstrapper"/>
    /// </summary>
    public partial class App : Application
    {
        ///<summary>
        /// Implements 2 runtimes: DEBUG and RELEASE
        ///</summary>
        ///<param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

#if (DEBUG)
            runInDebugMode();
#else
            runInReleaseMode();
#endif
            ShutdownMode = ShutdownMode.OnMainWindowClose;
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

            ExceptionPolicy.HandleException(ex, "Default Policy");
            MessageBox.Show(UserInterfaceStrings.UnhandledException);
            Environment.Exit(1);
        }
    }
}