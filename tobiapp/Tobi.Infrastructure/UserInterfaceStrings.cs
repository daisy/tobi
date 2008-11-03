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
        public const string Menu_Open = "_Open...";
        public const string Menu_Save = "_Save";
        public const string Menu_SaveAs = "S_ave as";
        public const string Menu_Close = "_Close";
        public const string Menu_Exit = "_Quit";

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
        public const string Menu_Zoom = "UI _Zoom"; // TODO: move this language resource in its own module (ZoomModule)
        public const string Menu_NavigationPane = "_Navigation panel"; // TODO: move this language resource in its own module (NavigationModule)
        public const string Menu_DocumentPane = "_Document panel"; // TODO: move this language resource in its own module (DocumentModule)
        public const string Menu_AudioPane = "_Audio panel"; // TODO: move this language resource in its own module (AudioModule)

        public const string Menu_Help = "_Help";
        public const string Menu_About = "_About";

        public static string UnhandledException = "An unhandled exception occurred, and the application is terminating. For more information, see your Application log.";
    }
}
