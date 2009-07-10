using System.IO;
using System.Reflection;
using System.Windows.Input;

namespace Tobi.Infrastructure
{
    ///<summary>
    /// Application-wide user-interface localization strings.
    /// TODO: move to a Localization module (service obtainable from the DI container)
    ///</summary>
    public static class UserInterfaceStrings
    {
        public static readonly string UnhandledException;

        public static readonly string LOG_FILE_NAME = "Tobi.log";
        public static readonly string LOG_FILE_PATH;
        static UserInterfaceStrings()
        {
            //Directory.GetCurrentDirectory()
            //string apppath = (new FileInfo(Assembly.GetExecutingAssembly().CodeBase)).DirectoryName;
            //AppDomain.CurrentDomain.BaseDirectory

            string currentAssemblyDirectoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            LOG_FILE_PATH = currentAssemblyDirectoryName + @"\" + LOG_FILE_NAME;
            
            UnhandledException = "An unhandled exception occurred, the application is now closing. For more information, see the '" + LOG_FILE_NAME + "' file: [ " + LOG_FILE_PATH + " ].";
        }

        public static string EscapeMnemonic(string str)
        {
            return str.Replace("_", "");
        }

        public const string ComboLabel_Playback = "Playback: ";
        public const string ComboLabel_Recording = "Recording: ";


        public const string Unexpected = "Unexpected problem !";

        public const string Loading = "Loading...";

        public const string Ok = "_Ok";
        public const string Cancel = "_Cancel";

        public const string Yes = "_Yes";
        public const string No = "_No";

        public const string Apply = "_Apply";
        public const string CloseDialog = "_Close";

        public const string RunningTask = "Running task...";

        public const string ShowMetadata = "_Edit metadata";
        public const string ShowMetadata_ = "Metadata viewer/editor";
        public static KeyGesture ShowMetadata_KEYS = new KeyGesture(Key.E, ModifierKeys.Control);

        public const string Audio_BeginSelection = "_Begin selection";
        public const string Audio_BeginSelection_ = "Begin audio waveform selection";
        public static KeyGesture Audio_BeginSelection_KEYS = new KeyGesture(Key.OemOpenBrackets, ModifierKeys.Control);

        public const string Audio_EndSelection = "_End selection";
        public const string Audio_EndSelection_ = "End audio waveform selection";
        public static KeyGesture Audio_EndSelection_KEYS = new KeyGesture(Key.OemCloseBrackets, ModifierKeys.Control);


        public const string TreeExpandAll = "E_xpand all";
        public const string TreeExpandAll_ = "Expand all children in the tree";

        public const string TreeCollapseAll = "_Collapse all";
        public const string TreeCollapseAll_ = "Collapse all children in the tree";

        public const string Menu_File = "_File";

        public const string Exit = "E_xit";
        public const string ExitConfirm = "Are you sure you want to exit Tobi ?";

        public const string Menu_Exit = "_Quit";
        public const string Menu_Exit_ = "Exit the application";
        public static KeyGesture Menu_Exit_KEYS = new KeyGesture(Key.Q, ModifierKeys.Control);

        public const string Menu_Close = "_Close";

        public const string Menu_Edit = "_Edit";

        public const string Menu_Tools = "_Tools";
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

        public const string Audio_OpenFile = "_Open audio";
        public const string Audio_OpenFile_ = "Open a local WAV file";
        public static KeyGesture Audio_OpenFile_KEYS = new KeyGesture(Key.O,
                                                                                ModifierKeys.Control |
                                                                                ModifierKeys.Shift);


        public const string DetailsExpand = "_Show details";
        public const string DetailsExpand_ = "Display detailed information";
        public static KeyGesture DetailsExpand_KEYS = new KeyGesture(Key.D, ModifierKeys.Control);

        public const string DetailsCollapse = "_Hide details";
        public const string DetailsCollapse_ = "Hide detailed information";
        public static KeyGesture DetailsCollapse_KEYS = DetailsExpand_KEYS;

        public const string New = "_New";
        public const string New_ = "New document";
        public static KeyGesture New_KEYS = new KeyGesture(Key.N, ModifierKeys.Control);

        public const string Open = "_Open / import";
        public const string Open_ = "Open or import a document";
        public static KeyGesture Open_KEYS = new KeyGesture(Key.O, ModifierKeys.Control);

        public const string Close = "_Close";
        public const string Close_ = "Close document";
        public static KeyGesture Close_KEYS = new KeyGesture(Key.W, ModifierKeys.Control);

        public const string Undo = "_Undo";
        public const string Undo_ = "Undo last action";
        public static KeyGesture Undo_KEYS = new KeyGesture(Key.Z, ModifierKeys.Control);

        public const string Redo = "_Redo";
        public const string Redo_ = "Redo last undone action";
        public static KeyGesture Redo_KEYS = new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift);


        public const string Copy = "_Copy";
        public const string Copy_ = "Copy to clipboard";
        public static KeyGesture Copy_KEYS = new KeyGesture(Key.C, ModifierKeys.Control);

        public const string Cut = "C_ut";
        public const string Cut_ = "Cut to clipboard";
        public static KeyGesture Cut_KEYS = new KeyGesture(Key.X, ModifierKeys.Control);

        public const string Paste = "_Paste";
        public const string Paste_ = "Paste from clipboard";
        public static KeyGesture Paste_KEYS = new KeyGesture(Key.V, ModifierKeys.Control);

        public const string Help = "_Help";
        public const string Help_ = "Get help";
        public static KeyGesture Help_KEYS = new KeyGesture(Key.F1, ModifierKeys.None);

        public const string Preferences = "_Preferences";
        public const string Preferences_ = "Application configuration";
        public static KeyGesture Preferences_KEYS = new KeyGesture(Key.F2, ModifierKeys.None);

        public const string WebHome = "Tobi-_web";
        public const string WebHome_ = "Open the Tobi homepage";
        public static KeyGesture WebHome_KEYS = new KeyGesture(Key.F1, ModifierKeys.Control);

        public const string NavNext = "_Next";
        public const string NavNext_ = "Navigate to the next item";
        public static KeyGesture NavNext_KEYS = new KeyGesture(Key.F7, ModifierKeys.Control);

        public const string NavPrevious = "_Previous";
        public const string NavPrevious_ = "Navigate to the previous item";
        public static KeyGesture NavPrevious_KEYS = new KeyGesture(Key.F6, ModifierKeys.Control);

        public const string Save = "_Save";
        public const string Save_ = "Save the current document";
        public static KeyGesture Save_KEYS = new KeyGesture(Key.S, ModifierKeys.Control);

        public const string SaveAs = "S_ave as...";
        public const string SaveAs_ = "Save the current document as...";
        public static KeyGesture SaveAs_KEYS = new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift);

        public const string UI_IncreaseMagnification = "_Increase magnification";
        public const string UI_IncreaseMagnification_ = "Increase the UI magnification level";
        public static KeyGesture UI_IncreaseMagnification_KEYS = new KeyGesture(Key.Y, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt);

        public const string UI_DecreaseMagnification = "_Decrease magnification";
        public const string UI_DecreaseMagnification_ = "Decrease the UI magnification level";
        public static KeyGesture UI_DecreaseMagnification_KEYS = new KeyGesture(Key.H, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt);

        public const string ManageShortcuts = "_Keyboard shortcuts";
        public const string ManageShortcuts_ = "View keyboard shortcuts";
        public static KeyGesture ManageShortcuts_KEYS = new KeyGesture(Key.Back, ModifierKeys.Control);


        public const string Event_SwitchPrevious = "_Previous phrase";
        public const string Event_SwitchPrevious_ = "Select the previous phrase";
        public static KeyGesture Event_SwitchPrevious_KEYS = new KeyGesture(Key.J, ModifierKeys.Control);

        public const string Event_SwitchNext = "_Next phrase";
        public const string Event_SwitchNext_ = "Select the next phrase";
        public static KeyGesture Event_SwitchNext_KEYS = new KeyGesture(Key.I, ModifierKeys.Control);

        public const string Audio_GotoBegin = "_Rewind to begin";
        public const string Audio_GotoBegin_ = "Rewind to the begining of the audio stream";
        public static KeyGesture Audio_GotoBegin_KEYS = new KeyGesture(Key.K,
                                                                        ModifierKeys.Control
                                                                        | ModifierKeys.Shift
                                                                        | ModifierKeys.Alt);

        public const string Audio_GotoEnd = "_Fast Forward to end";
        public const string Audio_GotoEnd_ = "Fast Forward to the end of the audio stream";
        public static KeyGesture Audio_GotoEnd_KEYS = new KeyGesture(Key.L,
                                                                       ModifierKeys.Control | ModifierKeys.Shift |
                                                                       ModifierKeys.Alt);


        public const string Audio_SelectPreviousChunk = "Select previous phrase";
        public const string Audio_SelectPreviousChunk_ = "Select the entire audio phrase preceding the current one";
        public static KeyGesture Audio_SelectPreviousChunk_KEYS = new KeyGesture(Key.OemOpenBrackets, ModifierKeys.Control | ModifierKeys.Shift);

        public const string Audio_SelectNextChunk = "Select next phrase";
        public const string Audio_SelectNextChunk_ = "Select the entire audio phrase following the current one";
        public static KeyGesture Audio_SelectNextChunk_KEYS = new KeyGesture(Key.OemCloseBrackets, ModifierKeys.Control | ModifierKeys.Shift);

        public const string Audio_StepBack = "_Back one phrase";
        public const string Audio_StepBack_ = "Step back one phrase";
        public static KeyGesture Audio_StepBack_KEYS = new KeyGesture(Key.K, ModifierKeys.Control | ModifierKeys.Shift);

        public const string Audio_StepForward = "_Forward one phrase";
        public const string Audio_StepForward_ = "Step forward one phrase";
        public static KeyGesture Audio_StepForward_KEYS = new KeyGesture(Key.L, ModifierKeys.Control | ModifierKeys.Shift);

        public const string Audio_FastForward = "_Fast-forward";
        public const string Audio_FastForward_ = "Fast-forward by a pre-defined time increment";
        public static KeyGesture Audio_FastForward_KEYS = new KeyGesture(Key.L, ModifierKeys.Control);

        public const string Audio_Rewind = "_Rewind";
        public const string Audio_Rewind_ = "Rewind by a pre-defined time increment";
        public static KeyGesture Audio_Rewind_KEYS = new KeyGesture(Key.K, ModifierKeys.Control);

        public const string Audio_ClearSelection = "_Clear selection";
        public const string Audio_ClearSelection_ = "Clear the current audio selection";
        public static KeyGesture Audio_ClearSelection_KEYS = new KeyGesture(Key.D, ModifierKeys.Control);

        public const string Audio_ZoomSelection = "_Zoom on selection";
        public const string Audio_ZoomSelection_ = "Zoom waveform selection into view";
        public static KeyGesture Audio_ZoomSelection_KEYS = new KeyGesture(Key.W, ModifierKeys.Control);

        public const string Audio_FitFull = "_Fit into view";
        public const string Audio_FitFull_ = "Fit the entire audio waveform into view";
        public static KeyGesture Audio_FitFull_KEYS = new KeyGesture(Key.W,
                                                                           ModifierKeys.Control | ModifierKeys.Shift);
        public const string Audio_Reload = "_Reload audio";
        public const string Audio_Reload_ = "Reload the audio waveform data";


        public const string Audio_AutoPlay = "_Toggle autoplay";
        public const string Audio_AutoPlay_ = "Switch autoplay on/off";
        public static KeyGesture Audio_AutoPlay_KEYS = new KeyGesture(Key.G, ModifierKeys.Control);

        public const string SelectAll = "_Select all";
        public const string SelectAll_ = "Select all content";
        public static KeyGesture SelectAll_KEYS = new KeyGesture(Key.A, ModifierKeys.Control);



        public const string Audio_StartRecord = "St_art recording";
        public const string Audio_StartRecord_ = "Start recording audio";
        public static KeyGesture Audio_StartRecord_KEYS = new KeyGesture(Key.R,
                                                                     ModifierKeys.Control);

        public const string Audio_StopRecord = "St_op recording";
        public const string Audio_StopRecord_ = "Stop the current recording";
        public static KeyGesture Audio_StopRecord_KEYS = Audio_StartRecord_KEYS;

        public const string Audio_StartMonitor = "St_art monitoring";
        public const string Audio_StartMonitor_ = "Start monitoring audio input";
        public static KeyGesture Audio_StartMonitor_KEYS = new KeyGesture(Key.M,
                                                                     ModifierKeys.Control);

        public const string Audio_StopMonitor = "St_op monitoring";
        public const string Audio_StopMonitor_ = "Stop the audio input monitoring";
        public static KeyGesture Audio_StopMonitor_KEYS = Audio_StartMonitor_KEYS;


        public const string Audio_Play = "Pla_y";
        public const string Audio_Play_ = "Start playback";
        public static KeyGesture Audio_Play_KEYS = new KeyGesture(Key.P, ModifierKeys.Control);

        public const string Audio_PlayPreviewLeft = "Preview _before";
        public const string Audio_PlayPreviewLeft_ = "Preview the audio just before the current cursor position";
        public static KeyGesture Audio_PlayPreviewLeft_KEYS = new KeyGesture(Key.P, ModifierKeys.Control | ModifierKeys.Shift);

        public const string Audio_PlayPreviewRight = "Preview _after";
        public const string Audio_PlayPreviewRight_ = "Preview the audio right after the current cursor position";
        public static KeyGesture Audio_PlayPreviewRight_KEYS = new KeyGesture(Key.P, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt);

        public const string Audio_Pause = "_Pause";
        public const string Audio_Pause_ = "Pause playback";
        public static KeyGesture Audio_Pause_KEYS = Audio_Play_KEYS;

    }
}
