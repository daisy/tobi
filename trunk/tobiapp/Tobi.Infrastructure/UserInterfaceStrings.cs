using System.Windows.Input;

namespace Tobi.Infrastructure
{
    ///<summary>
    /// Application-wide user-interface localization strings.
    /// TODO: move to a Localization module (service obtainable from the DI container)
    ///</summary>
    public static class UserInterfaceStrings
    {
        public const string Menu_File = "_File";

        public const string Menu_New = "_New...";
        public const string Menu_New_ = "Create new document";
        public static KeyGesture Menu_New_KEYS = new KeyGesture(Key.N, ModifierKeys.Control);

        public const string Menu_Open = "_Open...";
        public const string Menu_Save = "_Save";
        public const string Menu_SaveAs = "S_ave as";
        public const string Menu_Close = "_Close";

        public const string Menu_Exit = "_Quit";
        public const string Menu_Exit_ = "Exit the application";
        public static KeyGesture Menu_Exit_KEYS = new KeyGesture(Key.Q, ModifierKeys.Control);

        public const string Menu_Edit = "_Edit";
        public const string Menu_Undo = "_Undo";
        public const string Menu_Redo = "_Redo";
        public const string Menu_Copy = "_Copy";
        public const string Menu_Cut = "C_ut";
        public const string Menu_Paste = "_Paste";
        public const string Menu_Delete = "_Delete";

        public const string Menu_Tools = "_Tools";
        public const string Menu_Options = "_Preferences";
        public const string Menu_Addins = "_Add-in manager";
        public const string Menu_Logging = "_Log window";

        public const string Menu_View = "_View"; // This is a top-menu that is empty by default and gets filled dynamically by modules that register GUI views
        public const string Menu_ToolBars = "_Tool bars"; // TODO: move this language resource in its own module (ToolBarsModule)
        public const string Menu_StatusBar = "_Status bar"; // TODO: move this language resource in its own module (StatusBarModule)
        //public const string Menu_Zoom = "UI _Zoom"; // TODO: move this language resource in its own module (ZoomModule)
        public const string Menu_NavigationPane = "_Navigation panel"; // TODO: move this language resource in its own module (NavigationModule)
        public const string Menu_DocumentPane = "_Document panel"; // TODO: move this language resource in its own module (DocumentModule)
        public const string Menu_AudioPane = "_Audio panel"; // TODO: move this language resource in its own module (AudioModule)

        public const string Menu_Help = "_Help";
        public const string Menu_About = "_About";

        public const string UnhandledException = "An unhandled exception occurred, and the application is terminating. For more information, see your Application log.";

        public const string Audio_OpenFile = "Open a local WAV file";
        public static KeyGesture Audio_OpenFile_KEYS = new KeyGesture(Key.O,
                                                                                ModifierKeys.Control |
                                                                                ModifierKeys.Shift);

        public const string UI_IncreaseMagnification = "Increase the UI magnification level";
        public static KeyGesture UI_IncreaseMagnification_KEYS = new KeyGesture(Key.F2, ModifierKeys.Control);

        public const string UI_ManageShortcuts = "Keyboard shortcuts";
        public const string UI_ManageShortcuts_ = "View keyboard shortcuts";
        public static KeyGesture UI_ManageShortcuts_KEYS = new KeyGesture(Key.Enter, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt);

        public const string UI_DecreaseMagnification = "Decrease the UI magnification level";
        public static KeyGesture UI_DecreaseMagnification_KEYS = new KeyGesture(Key.F2, ModifierKeys.Control | ModifierKeys.Shift);

        public const string Audio_SwitchPrevious = "Switch to previous audio";
        public static KeyGesture Audio_SwitchPrevious_KEYS = new KeyGesture(Key.Down, ModifierKeys.Control);

        public const string Audio_SwitchNext = "Switch to next audio";
        public static KeyGesture Audio_SwitchNext_KEYS = new KeyGesture(Key.Up, ModifierKeys.Control);

        public const string Audio_GotoBegin = "Rewind to begining of the audio stream";
        public static KeyGesture Audio_GotoBegin_KEYS = new KeyGesture(Key.Left,
                                                                        ModifierKeys.Control | ModifierKeys.Shift |
                                                                        ModifierKeys.Alt);

        public const string Audio_GotoEnd = "Fast Forward to the end of the audio stream";
        public static KeyGesture Audio_GotoEnd_KEYS = new KeyGesture(Key.Right,
                                                                       ModifierKeys.Control | ModifierKeys.Shift |
                                                                       ModifierKeys.Alt);

        public const string Audio_StepBack = "Step back one phrase";
        public static KeyGesture Audio_StepBack_KEYS = new KeyGesture(Key.Left, ModifierKeys.Control | ModifierKeys.Shift);

        public const string Audio_StepForward = "Step forward one phrase";
        public static KeyGesture Audio_StepForward_KEYS = new KeyGesture(Key.Right, ModifierKeys.Control | ModifierKeys.Shift);

        public const string Audio_FastForward = "Fast-forward by a pre-defined time increment";
        public static KeyGesture Audio_FastForward_KEYS = new KeyGesture(Key.Right, ModifierKeys.Control);

        public const string Audio_Rewind = "Rewind by a pre-defined time increment";
        public static KeyGesture Audio_Rewind_KEYS = new KeyGesture(Key.Left, ModifierKeys.Control);

        public const string Audio_ClearSelection = "Clear audio selection";
        public static KeyGesture Audio_ClearSelection_KEYS = new KeyGesture(Key.D, ModifierKeys.Control);

        public const string Audio_ZoomSelection = "Zoom waveform selection into view";
        public static KeyGesture Audio_ZoomSelection_KEYS = new KeyGesture(Key.W, ModifierKeys.Control);

        public const string Audio_FitFull = "Fit the entire audio waveform into view";
        public static KeyGesture Audio_FitFull_KEYS = new KeyGesture(Key.W,
                                                                           ModifierKeys.Control | ModifierKeys.Shift);
        public const string Audio_Reload = "Reload the audio waveform data";

        public const string Audio_StopRecord = "Stop recording";

        public const string Audio_StartRecord = "Start recording";
        public static KeyGesture Audio_StartRecord_KEYS = new KeyGesture(Key.Enter,
                                                                     ModifierKeys.Control | ModifierKeys.Shift);

        public const string Audio_Pause = "Pause";

        public const string Audio_Play = "Play";
        public static KeyGesture Audio_Play_KEYS = new KeyGesture(Key.Enter, ModifierKeys.Control);

        public const string Audio_AutoPlay = "Switch autoplay on/off";
        public static KeyGesture Audio_AutoPlay_KEYS = new KeyGesture(Key.Y, ModifierKeys.Control);

        public const string SelectAll = "Select all";
        public static KeyGesture SelectAll_KEYS = new KeyGesture(Key.A, ModifierKeys.Control);
    }
}
