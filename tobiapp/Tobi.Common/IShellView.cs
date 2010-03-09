using System.Deployment.Application;
using System.IO;
using System.Reflection;
using System.Windows;
using Tobi.Common.MVVM;
using System;
using System.Windows.Media;
using Tobi.Common.MVVM.Command;

namespace Tobi.Common
{
    public static class ApplicationConstants
    {
        //http://daisy.trac.cvsdude.com/tobi/wiki/Alpha
        public readonly static Uri TobiHomeUri = new Uri("http://www.digitaltalkingbook.com/projects/tobi/install/alpha/", UriKind.Absolute);

        public const string LOG_FILE_NAME = "Tobi.log";
        public static readonly string LOG_FILE_PATH;

        static ApplicationConstants()
        {
            //Directory.GetCurrentDirectory()
            //string apppath = (new FileInfo(Assembly.GetExecutingAssembly().CodeBase)).DirectoryName;
            //AppDomain.CurrentDomain.BaseDirectory

            string currentAssemblyDirectoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            LOG_FILE_PATH = currentAssemblyDirectoryName + @"\" + LOG_FILE_NAME;
            APP_VERSION = GetVersion();
        }

        public static readonly string APP_VERSION;
        private static string GetVersion()
        {
            if (ApplicationDeployment.IsNetworkDeployed)
            {
                return ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
            }
            return Application.Current.GetType().Assembly.GetName().Version.ToString();
            //return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            // DIFFERENT than FileVersion !!
            // NOT: System.Diagnostics.FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location)
        }
    }

    public static class RegionNames
    {
        public const string MenuBar_File = UserInterfaceStrings.Menu_File;
        public const string MenuBar_OpenRecent = UserInterfaceStrings.Menu_OpenRecent;
        public const string MenuBar_Edit = UserInterfaceStrings.Menu_Edit;

        public const string MenuBar_Audio = UserInterfaceStrings.Menu_Audio;
        public const string MenuBar_AudioPlayback = UserInterfaceStrings.Menu_AudioPlayback;
        public const string MenuBar_AudioRecording = UserInterfaceStrings.Menu_AudioRecording;
        public const string MenuBar_AudioTransport = UserInterfaceStrings.Menu_AudioTransport;
        public const string MenuBar_AudioSelection = UserInterfaceStrings.Menu_AudioSelection;
        public const string MenuBar_AudioZoom = UserInterfaceStrings.Menu_AudioZoom;

        public const string MenuBar_Tools = UserInterfaceStrings.Menu_Tools;
        public const string MenuBar_Structure = UserInterfaceStrings.Menu_Structure;
        public const string MenuBar_View = UserInterfaceStrings.Menu_View;
        public const string MenuBar_Focus = UserInterfaceStrings.Menu_Focus;
        public const string MenuBar_Navigation = UserInterfaceStrings.Menu_Navigation;
        public const string MenuBar_Find = UserInterfaceStrings.Menu_Find;
        public const string MenuBar_Magnification = UserInterfaceStrings.Menu_Magnification;

        public const string MenuBar_System = UserInterfaceStrings.Menu_System;

        public const string MenuBar = "MenuBar";
        public const string MainToolbar = "MainToolbar";

        public const string StatusBar = "StatusBar";
        public const string ToolBars = "ToolBars";
        public const string DocumentPane = "DocumentPane";
        public const string StructureTrailPane = "StructureTrailPane";
        public const string AudioPane = "AudioPane";

        public const string NavigationPane = "NavigationPane";
        public const string NavigationPaneTabs = "NavigationPaneTabs";
    }

    public interface IShellView : INotifyPropertyChangedEx, IInputBindingManager
    {
        bool IsUIAutomationDisabled { get; }

        void Show();

        bool SplitterDrag { get; }

        double MagnificationLevel { get; set; }

        void RegisterRichCommand(RichDelegateCommand command);

        VisualBrush LoadTangoIcon(string resourceKey);
        VisualBrush LoadGnomeNeuIcon(string resourceKey);
        VisualBrush LoadGnomeGionIcon(string resourceKey);
        VisualBrush LoadGnomeFoxtrotIcon(string resourceKey);

        void DimBackgroundWhile(Action action);
        void ExecuteShellProcess(string shellCmd);
    }
}