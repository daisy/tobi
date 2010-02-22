using Tobi.Common.MVVM;
using System;
using System.Windows.Media;
using Tobi.Common.MVVM.Command;

namespace Tobi.Common
{
    public static class RegionNames
    {
        public const string MenuBar = "MenuBar";

        public const string MenuBar_File = UserInterfaceStrings.Menu_File;
        public const string MenuBar_Edit = UserInterfaceStrings.Menu_Edit;

        public const string MenuBar_Audio = UserInterfaceStrings.Menu_Audio;
        public const string MenuBar_AudioPlayback = UserInterfaceStrings.Menu_AudioPlayback;
        public const string MenuBar_AudioRecording = UserInterfaceStrings.Menu_AudioRecording;
        public const string MenuBar_AudioTransport = UserInterfaceStrings.Menu_AudioTransport;
        public const string MenuBar_AudioSelection = UserInterfaceStrings.Menu_AudioSelection;
        public const string MenuBar_AudioZoom = UserInterfaceStrings.Menu_AudioZoom;

        public const string MenuBar_Tools = UserInterfaceStrings.Menu_Tools;
        public const string MenuBar_View = UserInterfaceStrings.Menu_View;
        public const string MenuBar_Focus = UserInterfaceStrings.Menu_Focus;
        public const string MenuBar_Navigation = UserInterfaceStrings.Menu_Navigation;
        public const string MenuBar_Magnification = UserInterfaceStrings.Menu_Magnification;

        public const string MenuBar_System = UserInterfaceStrings.Menu_System;

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
    }
}