using System.Deployment.Application;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Practices.Composite;
using Tobi.Common.MVVM;
using System;
using System.Windows.Media;
using Tobi.Common.MVVM.Command;
using System.Management;

namespace Tobi.Common
{
    public static class ApplicationConstants
    {
        //http://daisy.trac.cvsdude.com/tobi/
        public readonly static Uri TobiHomeUri = new Uri("http://www.daisy.org/projects/tobi/", UriKind.Absolute);

        public const string TOBI_ANON_USAGE_URI = "http://data.daisy.org/projects/tobi/Tobi_AnonymousUsageLogger.php";

        public const string LOG_FILE_NAME = "Tobi.log";
        public static readonly string LOG_FILE_PATH;

        public static readonly string DOTNET_INFO;

        static ApplicationConstants()
        {
            //Directory.GetCurrentDirectory()
            //string apppath = (new FileInfo(Assembly.GetExecutingAssembly().CodeBase)).DirectoryName;
            //AppDomain.CurrentDomain.BaseDirectory

            string currentAssemblyDirectoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            LOG_FILE_PATH = currentAssemblyDirectoryName + @"\" + LOG_FILE_NAME;
            APP_VERSION = GetVersion();

            string coreLibVersion = null;
            string name = "";
            foreach (Assembly item in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (item.GlobalAssemblyCache)
                {
                    if (!string.IsNullOrEmpty(item.FullName)
                        && item.FullName.Contains("mscorlib"))
                    {
                        name = item.FullName;
                        coreLibVersion = item.ImageRuntimeVersion;
                        break;
                    }
                }
            }

            bool isV4 = !string.IsNullOrEmpty(coreLibVersion) && coreLibVersion.Contains("v4.")
                || name.Contains("Version=4");

#if NET40
            const string NET4 = " [.NET 4]";
            DOTNET_INFO = NET4;
#else
            const string NET4_ButTobi3 = " [.NET 3/4]";
            const string NET3 = " [.NET 3]";
            DOTNET_INFO = (isV4 ? NET4_ButTobi3 : NET3);
#endif
            OS_INFORMATION = getOSInfo() + DOTNET_INFO + " -- (" + (IsRunning64() ? "64-bit" : "32-bit") + " .NET)";
        }

        public static readonly string APP_VERSION;
        public static string GetVersion()
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

        public static readonly string OS_INFORMATION;
        /// <summary>
        /// Gets Operating System Name, Service Pack, and Architecture using WMI with the legacy methods as a fallback
        /// </summary>
        /// <returns>String containing the name of the operating system followed by its service pack (if any) and architecture</returns>
        private static string getOSInfo()
        {
            var objMOS = new ManagementObjectSearcher("SELECT * FROM  Win32_OperatingSystem");

            //Variables to hold our return value
            string os = "";
            int OSArch = 0;

            try
            {
                foreach (ManagementObject objManagement in objMOS.Get())
                {
                    // Get OS version from WMI - This also gives us the edition
                    object osCaption = objManagement.GetPropertyValue("Caption");
                    if (osCaption != null)
                    {
                        // Remove all non-alphanumeric characters so that only letters, numbers, and spaces are left.
                        string osC = Regex.Replace(osCaption.ToString(), "[^A-Za-z0-9 ]", "");
                        //string osC = osCaption.ToString();
                        // If the OS starts with "Microsoft," remove it.  We know that already
                        if (osC.StartsWith("Microsoft"))
                        {
                            osC = osC.Substring(9);
                        }
                        // If the OS now starts with "Windows," again... useless.  Remove it.
                        if (osC.Trim().StartsWith("Windows"))
                        {
                            osC = osC.Trim().Substring(7);
                        }
                        // Remove any remaining beginning or ending spaces.
                        os = osC.Trim();
                        // Only proceed if we actually have an OS version - service pack is useless without the OS version.
                        if (!String.IsNullOrEmpty(os))
                        {
                            object osSP = null;
                            try
                            {
                                // Get OS service pack from WMI
                                osSP = objManagement.GetPropertyValue("ServicePackMajorVersion");
                                if (osSP != null && osSP.ToString() != "0")
                                {
                                    os += " Service Pack " + osSP.ToString();
                                }
                                else
                                {
                                    // Service Pack not found.  Try built-in Environment class.
                                    os += getOSServicePackLegacy();
                                }
                            }
                            catch (Exception)
                            {
                                // There was a problem getting the service pack from WMI.  Try built-in Environment class.
                                os += getOSServicePackLegacy();
                            }
                        }
                        object osA = null;
                        try
                        {
                            // Get OS architecture from WMI
                            osA = objManagement.GetPropertyValue("OSArchitecture");
                            if (osA != null)
                            {
                                string osAString = osA.ToString();
                                // If "64" is anywhere in there, it's a 64-bit architectore.
                                OSArch = (osAString.Contains("64") ? 64 : 32);
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            // If WMI couldn't tell us the OS, use our legacy method.
            // We won't get the exact OS edition, but something is better than nothing.
            if (os == "")
            {
                os = getOSLegacy();
            }
            // If WMI couldn't tell us the architecture, use our legacy method.
            if (OSArch == 0)
            {
                OSArch = getOSArchitectureLegacy();
            }
            else
            {
#if NET40
                DebugFix.Assert((OSArch == 64) == Environment.Is64BitOperatingSystem);
#endif
            }
            return os + " " + OSArch.ToString() + "-bit";
        }

        /// <summary>
        /// Gets Operating System Name using .Net's Environment class.
        /// </summary>
        /// <returns>String containing the name of the operating system followed by its service pack (if any)</returns>
        private static string getOSLegacy()
        {
            //Get Operating system information.
            OperatingSystem os = Environment.OSVersion;
            //Get version information about the os.
            Version vs = os.Version;

            //Variable to hold our return value
            string operatingSystem = "";

            if (os.Platform == PlatformID.Win32Windows)
            {
                //This is a pre-NT version of Windows
                switch (vs.Minor)
                {
                    case 0:
                        operatingSystem = "95";
                        break;
                    case 10:
                        if (vs.Revision.ToString() == "2222A")
                            operatingSystem = "98SE";
                        else
                            operatingSystem = "98";
                        break;
                    case 90:
                        operatingSystem = "Me";
                        break;
                    default:
                        break;
                }
            }
            else if (os.Platform == PlatformID.Win32NT)
            {
                switch (vs.Major)
                {
                    case 3:
                        operatingSystem = "NT 3.51";
                        break;
                    case 4:
                        operatingSystem = "NT 4.0";
                        break;
                    case 5:
                        if (vs.Minor == 0)
                        {
                            operatingSystem = "2000";
                        }
                        else
                        {
                            operatingSystem = "XP";
                        }
                        break;
                    case 6:
                        if (vs.Minor == 0)
                        {
                            operatingSystem = "Vista";
                        }
                        else
                        {
                            operatingSystem = "7";
                        }
                        break;
                    default:
                        break;
                }
            }
            //Make sure we actually got something in our OS check
            //We don't want to just return " Service Pack 2"
            //That information is useless without the OS version.
            if (operatingSystem != "")
            {
                //Got something.  Let's see if there's a service pack installed.
                operatingSystem += getOSServicePackLegacy();
            }
            //Return the information we've gathered.
            return operatingSystem;
        }

        /// <summary>
        /// Gets the installed Operating System Service Pack using .Net's Environment class.
        /// </summary>
        /// <returns>String containing the operating system's installed service pack (if any)</returns>
        private static string getOSServicePackLegacy()
        {
            // Get service pack from Environment Class
            string sp = Environment.OSVersion.ServicePack;
            if (sp != null && sp.ToString() != "" && sp.ToString() != " ")
            {
                // If there's a service pack, return it with a space in front (for formatting)
                return " " + sp.ToString();
            }
            // No service pack.  Return an empty string
            return "";
        }

        /// <summary>
        /// Gets Operating System Architecture.  This does not tell you if the program in running in
        /// 32- or 64-bit mode or if the CPU is 64-bit capable.  It tells you whether the actual Operating
        /// System is 32- or 64-bit.
        /// </summary>
        /// <returns>Int containing 32 or 64 representing the number of bits in the OS Architecture</returns>
        private static int getOSArchitectureLegacy()
        {
            string pa = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");

            int bits = ((String.IsNullOrEmpty(pa) || String.Compare(pa, 0, "x86", 0, 3, true) == 0) ? 32 : 64);
#if NET40
            DebugFix.Assert((bits == 64) == Environment.Is64BitOperatingSystem);
#endif
            return bits;
        }

        private static bool IsRunning64()
        {
            bool is64 = IntPtr.Size == 8;
#if NET40
            DebugFix.Assert(is64 == Environment.Is64BitProcess);
#endif
            return is64; //4 in x86 / 32 bits arch
        }
    }

    public static class RegionNames
    {
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
        RichDelegateCommand ExitCommand { get; }

        void RaiseEscapeEvent();

        IActiveAware ActiveAware { get; }

        event EventHandler DeviceRemoved;
        event EventHandler DeviceArrived;

        void PumpDispatcherFrames(DispatcherPriority prio);

        bool RunModalCancellableProgressTask(bool inSeparateThread, string title, IDualCancellableProgressReporter reporter, Action actionCancelled, Action actionCompleted);

        bool IsUIAutomationDisabled { get; }

        void Show();
        bool Activate();

        bool SplitterDrag { get; }

        double MagnificationLevel { get; set; }

        void RegisterRichCommand(RichDelegateCommand command);

        VisualBrush LoadTangoIcon(string resourceKey);
        VisualBrush LoadGnomeNeuIcon(string resourceKey);
        VisualBrush LoadGnomeGionIcon(string resourceKey);
        VisualBrush LoadGnomeFoxtrotIcon(string resourceKey);

        void DimBackgroundWhile(Action action);
        void DimBackgroundWhile(Action action, Window owner);

        void ExecuteShellProcess(string shellCmd);

        void TryOpenFile(string filePath);
    }
}