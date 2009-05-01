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
        public const string Menu_Open = "_Open...";
        public const string Menu_Save = "_Save";
        public const string Menu_SaveAs = "S_ave as";
        public const string Menu_Close = "_Close";
        public const string Menu_Exit = "_Quit";
        public const string Menu_Exit_ = "Exit the application";

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

        public const string UI_IncreaseMagnification = "Increase the UI magnification level";
        public const string UI_DecreaseMagnification = "Decrease the UI magnification level";

        public const string Audio_SwitchPrevious = "Switch to previous audio";
        public const string Audio_SwitchNext = "Switch to next audio";

        public const string Audio_GotoBegin = "Rewind to begining of the audio stream";
        public const string Audio_GotoEnd = "Fast Forward to the end of the audio stream";

        public const string Audio_StepBack = "Step back one phrase";
        public const string Audio_StepForward = "Step forward one phrase";

        public const string Audio_FastForward = "Fast-forward by a pre-defined time increment";
        public const string Audio_Rewind = "Rewind by a pre-defined time increment";

        public const string Audio_ClearSelection = "Clear audio selection";
        public const string Audio_ZoomSelection = "Zoom waveform selection into view";

        public const string Audio_FitFull = "Fit the entire audio waveform into view";
        public const string Audio_Reload = "Reload the audio waveform data";

        public const string Audio_StopRecord = "Stop recording";
        public const string Audio_StartRecord = "Start recording";

        public const string Audio_Pause = "Pause";
        public const string Audio_Play = "Play";

        public const string Audio_AutoPlay = "Switch autoplay on/off";

        public const string SelectAll = "Select all";
    }
}
