using System;

namespace Tobi.Common
{
    ///<summary>
    /// Application-wide user-interface localization strings.
    /// TODO: move to a Localization module (service obtainable from the DI container)
    ///</summary>
    public static class UserInterfaceStrings
    {
        public static readonly string UnhandledException = "Oops, a problem occurred." + Environment.NewLine + "The full error message is displayed below.\nAlternatively, you may open the '" + ApplicationConstants.LOG_FILE_NAME + "' file, located here: [ " + ApplicationConstants.LOG_FILE_PATH + " ].";

        public static string EscapeMnemonic(string str)
        {
            return str.Replace("_", "");
        }

        public const string CannotOpenLocalFile = "Cannot open file.";
        public const string CannotOpenLocalFile_ = "Failed to open file!";

        public const string No_Document = "No document.";
        public const string Feature_Not_Available = "This feature is not available.";

        public const string Navigation_Focus = "Navigation pane focus";
        //public static KeyGesture Navigation_Focus_KEYS = new KeyGesture(Key.F6, ModifierKeys.None);

        public const string Document_Focus = "Document pane focus";
        //public static KeyGesture Document_Focus_KEYS = new KeyGesture(Key.F8, ModifierKeys.None);

        public const string Toolbar_Focus = "Toolbar focus";
        //public static KeyGesture Toolbar_Focus_KEYS = new KeyGesture(Key.F9, ModifierKeys.None);

        public const string Audio_Focus = "Audio pane focus";
        //public static KeyGesture Audio_Focus_KEYS = new KeyGesture(Key.F7, ModifierKeys.None);

        public const string Audio_FocusStatusBar = "Status bar focus";
        //public static KeyGesture Audio_FocusStatusBar_KEYS = new KeyGesture(Key.F4, ModifierKeys.None);


        public const string FileSystem_MyComputer = "Computer drives";  // Not able to find Usage


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
        public const string CancellingTask = "Cancelling...";

        public const string ShowMetadata = "_Edit metadata";
        public const string ShowMetadata_ = "Metadata viewer/editor";
        //public static KeyGesture ShowMetadata_KEYS = new KeyGesture(Key.E, ModifierKeys.Control);

        public const string Audio_BeginSelection = "_Begin selection";
        public const string Audio_BeginSelection_ = "Begin audio waveform selection";
        //public static KeyGesture Audio_BeginSelection_KEYS = new KeyGesture(Key.OemOpenBrackets, ModifierKeys.Control);

        public const string Audio_PlayRateReset = "_Reset playback rate";
        public const string Audio_PlayRateReset_ = "Restore the normal playback speed";
        public const string Audio_PlayRateDown = "_Descrease playback rate";
        public const string Audio_PlayRateDown_ = "Turn the playback speed down";
        public const string Audio_PlayRateUp = "_Increase playback rate";
        public const string Audio_PlayRateUp_ = "Turn the playback speed up";

        public const string Audio_EndSelection = "_End selection";
        public const string Audio_EndSelection_ = "End audio waveform selection";
        //public static KeyGesture Audio_EndSelection_KEYS = new KeyGesture(Key.OemCloseBrackets, ModifierKeys.Control);

        public const string Audio_Delete = "_Delete audio selection";
        public const string Audio_Delete_ = "Delete the audio in the waveform selection";
        //public static KeyGesture Audio_Delete_KEYS = new KeyGesture(Key.Back, ModifierKeys.Control);


        public const string TreeExpandAll = "E_xpand all";
        public const string TreeExpandAll_ = "Expand all children in the tree";

        public const string TreeExpand = "Ex_pand";
        public const string TreeExpand_ = "Expand the current item in the tree";

        public const string TreeCollapseAll = "_Collapse all";
        public const string TreeCollapseAll_ = "Collapse all children in the tree";

        public const string TreeCollapse = "Co_llapse";
        public const string TreeCollapse_ = "Collapse the current item in the tree";

        //public const string TreeEdit = "Edit _Heading";
        //public const string TreeEdit_ = "Edit the current heading";

        //public const string TreeFindNext = "Find Next";
        //public const string TreeFindNext_ = "Find the next matching item in the tree";
        //public static KeyGesture TreeFindNext_KEYS = new KeyGesture(Key.F3);
        public const string TreeFindNext_FAILURE = "No More Matches Found";

        //public const string TreeFindPrev = "Find Previous";
        //public const string TreeFindPrev_ = "Find the previous matching item in the tree";
        //public static KeyGesture TreeFindPrev_KEYS = new KeyGesture(Key.F3, ModifierKeys.Shift);
        public const string TreeFindPrev_FAILURE = "No More Matches Found";

        //public const string PageFindNext = "Find Next";
        //public const string PageFindNext_ = "Find the next matching item in the page list";
        //public static KeyGesture PageFindNext_KEYS = new KeyGesture(Key.F3, ModifierKeys.Alt);
        public const string PageFindNext_FAILURE = "No More Matches Found";

        //public const string PageFindPrev = "Find Previous";
        //public const string PageFindPrev_ = "Find the previous matching item in the page list";
        //public static KeyGesture PageFindPrev_KEYS = new KeyGesture(Key.F3, ModifierKeys.Alt | ModifierKeys.Shift);
        public const string PageFindPrev_FAILURE = "No More Matches Found";

        //public const string HeadingEdit = "Edit Heading";
        //public const string HeadingEdit_ = "Please Enter The New Value For This Heading.";

        public const string Menu_File = "_File";
        public const string Menu_OpenRecent = "_Open recent";

        public const string Overwrite = "_Overwrite";
        public const string OverwriteConfirm_File = "You are about to overwrite a file.\nAre you sure ?";
        public const string OverwriteConfirm_Folder = "You are about to overwrite a folder.\nAre you sure ?";

        public const string Exit = "E_xit";
        public const string ExitConfirm = "Are you sure you want to exit Tobi ?";

        public const string UnsavedChanges = "_Unsaved changes";
        public const string UnsavedChangesConfirm = "There are unsaved document changes.\nWould you like to save now ?";
        public const string UnsavedChangesDetails = "If you close the document without saving,\nany changes will be lost !";


        public const string Menu_ClearRecentFiles = "_Clear";
        public const string Menu_ClearRecentFiles_ = "Clear the list of recently-opened files.";

        public const string Menu_Exit = "_Quit";
        public const string Menu_Exit_ = "Exit the application";
        ////public static KeyGesture Menu_Exit_KEYS = new KeyGesture(Key.Q, ModifierKeys.Control);

        public const string Menu_Close = "_Close";

        public const string Menu_Edit = "_Edit";

        public const string Menu_Tools = "_Tools";
        public const string Menu_Structure = "_Structure";
        public const string Menu_Addins = "_Add-in manager";
        public const string Menu_Logging = "_Log window";

        public const string Menu_Focus = "_Focus";
        public const string Menu_View = "_View";
        public const string Menu_Magnification = "Ma_gnification";
        public const string Menu_System = "_Browse folder";

        public const string Menu_ToolBars = "_Tool bars"; // TODO: move this language resource in its own module (ToolBarsModule)
        public const string Menu_StatusBar = "_Status bar"; // TODO: move this language resource in its own module (StatusBarModule)
        //public const string Menu_Zoom = "UI _Zoom"; // TODO: move this language resource in its own module (ZoomModule)
        public const string Menu_NavigationPane = "_Navigation panel"; // TODO: move this language resource in its own module (NavigationModule)
        public const string Menu_DocumentPane = "_Document panel"; // TODO: move this language resource in its own module (DocumentModule)
        public const string Menu_AudioPane = "_Audio panel"; // TODO: move this language resource in its own module (AudioModule)

        public const string Menu_Navigation = "Navi_gation";
        public const string Menu_Find = "_Find";

        public const string Menu_Help = "_Help";
        public const string Menu_About = "_About";
        public const string Menu_Audio = "_Audio";

        public const string Menu_AudioPlayback = "_Playback";
        public const string Menu_AudioRecording = "_Recording";
        public const string Menu_AudioTransport = "_Navigation";
        public const string Menu_AudioSelection = "_Selection";
        public const string Menu_AudioZoom = "_Zoom";

        public const string Audio_InsertFile = "_Insert audio file";
        public const string Audio_InsertFile_ = "Inserts a local WAV file (PCM 16 bits)";
        //public static KeyGesture Audio_InsertFile_KEYS = new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift);


        public const string Audio_OpenFile = "_Open audio file";
        public const string Audio_OpenFile_ = "Open a local WAV file (PCM 16 bits)";
        //public static KeyGesture Audio_OpenFile_KEYS = new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift);


        public const string DetailsExpand = "_Show details";
        public const string DetailsExpand_ = "Display detailed information";
        //public static KeyGesture DetailsExpand_KEYS = new KeyGesture(Key.D, ModifierKeys.Control);

        public const string DetailsCollapse = "_Hide details";
        public const string DetailsCollapse_ = "Hide detailed information";
        //public static KeyGesture DetailsCollapse_KEYS = DetailsExpand_KEYS;

        public const string New = "_New";
        public const string New_ = "New document";
        //public static KeyGesture New_KEYS = new KeyGesture(Key.N, ModifierKeys.Control);

        public const string Open = "_Open / import";
        public const string Open_ = "Open or import a document";
        //public static KeyGesture Open_KEYS = new KeyGesture(Key.O, ModifierKeys.Control);

        public const string DataCleanup = "_Cleanup unused data";
        public const string DataCleanup_ = "Remove files unused by the document";

        public const string Close = "_Close";
        public const string Close_ = "Close document";
        //public static KeyGesture Close_KEYS = new KeyGesture(Key.W, ModifierKeys.Control);

        public const string Undo = "_Undo";
        public const string Undo_ = "Undo last action";
        //public static KeyGesture Undo_KEYS = new KeyGesture(Key.Z, ModifierKeys.Control);

        public const string Redo = "_Redo";
        public const string Redo_ = "Redo last undone action";
        //public static KeyGesture Redo_KEYS = new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift);


        public const string Copy = "_Copy";
        public const string Copy_ = "Copy to clipboard";
        //public static KeyGesture Copy_KEYS = new KeyGesture(Key.C, ModifierKeys.Control);

        public const string Cut = "C_ut";
        public const string Cut_ = "Cut to clipboard";
        //public static KeyGesture Cut_KEYS = new KeyGesture(Key.X, ModifierKeys.Control);

        public const string Paste = "_Paste";
        public const string Paste_ = "Paste from clipboard";
        //public static KeyGesture Paste_KEYS = new KeyGesture(Key.V, ModifierKeys.Control);


        //public const string ShowLogFilePath = "DEBUG: _Where is [" + LOG_FILE_NAME + "] ?";
        //public const string ShowLogFilePath_ = "Here is the path to the Tobi log file.\nYou may copy/paste into the file explorer\nand open with any text editor.";
        //public static KeyGesture ShowLogFilePath_KEYS = new KeyGesture(Key.F1, ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift);

        public const string OpenTobiIsolatedStorage = "Browse storage folder";
        public const string OpenTobiIsolatedStorage_ = "Open a file browser where Tobi's files are located.";

        public const string OpenTobiFolder = "Browse application folder";
        public const string OpenTobiFolder_ = "Open a file browser where the Tobi.exe application is run from (and where Tobi.log resides).";
        //public static KeyGesture OpenTobiFolder_KEYS = new KeyGesture(Key.F2, ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift);

        public const string OpenTobiSettingsFolder = "Browse settings folder";
        public const string OpenTobiSettingsFolder_ = "Open a file browser where the Tobi user settings are stored.";
        //public static KeyGesture OpenTobiSettingsFolder_KEYS = new KeyGesture(Key.F3, ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift);


        public const string Help = "_Help";
        public const string Help_ = "Get help";
        //public static KeyGesture Help_KEYS = new KeyGesture(Key.F1, ModifierKeys.None);

        public const string Preferences = "_Preferences";
        public const string Preferences_ = "Application configurable options";
        //public static KeyGesture Preferences_KEYS = new KeyGesture(Key.F2, ModifierKeys.None);

        //public const string WebHome = "Tobi-_web";
        //public const string WebHome_ = "Open the Tobi homepage";
        ////public static KeyGesture WebHome_KEYS = new KeyGesture(Key.F1, ModifierKeys.Control);

        public const string NavNext = "_Next";
        public const string NavNext_ = "Navigate to the next item";
        //public static KeyGesture NavNext_KEYS = new KeyGesture(Key.F7, ModifierKeys.Control);

        public const string NavPrevious = "_Previous";
        public const string NavPrevious_ = "Navigate to the previous item";
        //public static KeyGesture NavPrevious_KEYS = new KeyGesture(Key.F6, ModifierKeys.Control);

        public const string Save = "_Save";
        public const string Save_ = "Save the current document";
        //public static KeyGesture Save_KEYS = new KeyGesture(Key.S, ModifierKeys.Control);


        public const string Export = "Export...";
        public const string Export_ = "Export the current document  to DAISY...";
        //public static KeyGesture Export_KEYS = new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt);


        public const string SaveAs = "S_ave as...";
        public const string SaveAs_ = "Save the current document as...";
        //public static KeyGesture SaveAs_KEYS = new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift);

        public const string UI_ResetMagnification = "_Reset magnification";
        public const string UI_ResetMagnification_ = "Reset the UI magnification level";
        //public static KeyGesture UI_ResetMagnification_KEYS = new KeyGesture(Key.Y, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt);

        public const string UI_IncreaseMagnification = "_Increase magnification";
        public const string UI_IncreaseMagnification_ = "Increase the UI magnification level";
        //public static KeyGesture UI_IncreaseMagnification_KEYS = new KeyGesture(Key.Y, ModifierKeys.Control | ModifierKeys.Shift);

        public const string UI_DecreaseMagnification = "_Decrease magnification";
        public const string UI_DecreaseMagnification_ = "Decrease the UI magnification level";
        //public static KeyGesture UI_DecreaseMagnification_KEYS = new KeyGesture(Key.Y, ModifierKeys.Control);

        public const string ManageShortcuts = "_Keyboard shortcuts";
        public const string ManageShortcuts_ = "View keyboard shortcuts";
        //public static KeyGesture ManageShortcuts_KEYS = new KeyGesture(Key.F4, ModifierKeys.Control);

        public const string IconsDebug = "Icons preview (debug view)";
        //public static KeyGesture IconsDebug_KEYS = new KeyGesture(Key.F4, ModifierKeys.Control | ModifierKeys.Shift);


        public const string StructureUp = "_Expand structure selection";
        public const string StructureUp_ = "Move the selection up one level in the document tree structure";

        public const string StructureDown = "_Narrow structure selection";
        public const string StructureDown_ = "Move the selection down one level in the document tree structure";

        public const string Event_SwitchPrevious = "_Previous phrase";
        public const string Event_SwitchPrevious_ = "Select the previous phrase";
        //public static KeyGesture Event_SwitchPrevious_KEYS = new KeyGesture(Key.I, ModifierKeys.Control);

        public const string Event_SwitchNext = "_Next phrase";
        public const string Event_SwitchNext_ = "Select the next phrase";
        //public static KeyGesture Event_SwitchNext_KEYS = new KeyGesture(Key.J, ModifierKeys.Control);

        public const string Audio_GotoBegin = "_Go to begin";
        public const string Audio_GotoBegin_ = "Rewind to the begining of the audio stream";
        //public static KeyGesture Audio_GotoBegin_KEYS = new KeyGesture(Key.K, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt);

        public const string Audio_GotoEnd = "_Go to end";
        public const string Audio_GotoEnd_ = "Fast Forward to the end of the audio stream";
        //public static KeyGesture Audio_GotoEnd_KEYS = new KeyGesture(Key.L, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt);


        public const string Audio_SelectPreviousChunk = "Select previous phrase";
        public const string Audio_SelectPreviousChunk_ = "Select the entire audio phrase preceding the current one";
        //public static KeyGesture Audio_SelectPreviousChunk_KEYS = new KeyGesture(Key.OemOpenBrackets, ModifierKeys.Control | ModifierKeys.Shift);

        public const string Audio_SelectNextChunk = "Select next phrase";
        public const string Audio_SelectNextChunk_ = "Select the entire audio phrase following the current one";
        //public static KeyGesture Audio_SelectNextChunk_KEYS = new KeyGesture(Key.OemCloseBrackets, ModifierKeys.Control | ModifierKeys.Shift);

        public const string Audio_StepBack = "_Back one phrase";
        public const string Audio_StepBack_ = "Step back one phrase";
        //public static KeyGesture Audio_StepBack_KEYS = new KeyGesture(Key.K, ModifierKeys.Control | ModifierKeys.Shift);

        public const string Audio_StepForward = "_Forward one phrase";
        public const string Audio_StepForward_ = "Step forward one phrase";
        //public static KeyGesture Audio_StepForward_KEYS = new KeyGesture(Key.L, ModifierKeys.Control | ModifierKeys.Shift);

        public const string Audio_FastForward = "_Step forward";
        public const string Audio_FastForward_ = "Jump forward by a pre-defined time increment";
        //public static KeyGesture Audio_FastForward_KEYS = new KeyGesture(Key.L, ModifierKeys.Control);

        public const string Audio_Rewind = "_Step backwards";
        public const string Audio_Rewind_ = "Jump back by a pre-defined time increment";
        //public static KeyGesture Audio_Rewind_KEYS = new KeyGesture(Key.K, ModifierKeys.Control);

        public const string Audio_ClearSelection = "_Clear selection";
        public const string Audio_ClearSelection_ = "Clear the current audio selection";
        //public static KeyGesture Audio_ClearSelection_KEYS = new KeyGesture(Key.D, ModifierKeys.Control);

        public const string Audio_ZoomSelection = "_Zoom on selection";
        public const string Audio_ZoomSelection_ = "Zoom waveform selection into view";
        //public static KeyGesture Audio_ZoomSelection_KEYS = new KeyGesture(Key.W, ModifierKeys.Control);

        public const string Audio_Settings = "Audio se_ttings";
        public const string Audio_Settings_ = "Settings for the audio player and recorder";

        public const string Audio_FitFull = "_Fit into view";
        public const string Audio_FitFull_ = "Fit the entire audio waveform into view";
        //public static KeyGesture Audio_FitFull_KEYS = new KeyGesture(Key.W, ModifierKeys.Control | ModifierKeys.Shift);
        public const string Audio_Reload = "_Reload audio";
        public const string Audio_Reload_ = "Reload the audio waveform data";


        public const string Audio_AutoPlay = "_Toggle autoplay";
        public const string Audio_AutoPlay_ = "Switch autoplay on/off";
        //public static KeyGesture Audio_AutoPlay_KEYS = new KeyGesture(Key.G, ModifierKeys.Control);

        public const string SelectAll = "_Select all";
        public const string SelectAll_ = "Select all content";
        //public static KeyGesture SelectAll_KEYS = new KeyGesture(Key.A, ModifierKeys.Control);



        public const string Audio_StartRecord = "St_art recording";
        public const string Audio_StartRecord_ = "Start recording audio";
        //public static KeyGesture Audio_StartRecord_KEYS = new KeyGesture(Key.R, ModifierKeys.Control);

        public const string Audio_StopRecord = "St_op recording";
        public const string Audio_StopRecord_ = "Stop the current recording";
        //public static KeyGesture Audio_StopRecord_KEYS = Audio_StartRecord_KEYS;

        public const string Audio_StartMonitor = "St_art monitoring";
        public const string Audio_StartMonitor_ = "Start monitoring audio input";
        //public static KeyGesture Audio_StartMonitor_KEYS = new KeyGesture(Key.M, ModifierKeys.Control);

        public const string Audio_StopMonitor = "St_op monitoring";
        public const string Audio_StopMonitor_ = "Stop the audio input monitoring";
        //public static KeyGesture Audio_StopMonitor_KEYS = Audio_StartMonitor_KEYS;


        public const string Audio_Play = "Pla_y";
        public const string Audio_Play_ = "Start playback";
        //public static KeyGesture Audio_Play_KEYS = new KeyGesture(Key.P, ModifierKeys.Control);

        public const string Audio_PlayPreviewLeft = "Preview _before";
        public const string Audio_PlayPreviewLeft_ = "Preview the audio just before the current cursor position";
        //public static KeyGesture Audio_PlayPreviewLeft_KEYS = new KeyGesture(Key.P, ModifierKeys.Control | ModifierKeys.Shift);

        public const string Audio_PlayPreviewRight = "Preview _after";
        public const string Audio_PlayPreviewRight_ = "Preview the audio right after the current cursor position";
        //public static KeyGesture Audio_PlayPreviewRight_KEYS = new KeyGesture(Key.P, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt);

        public const string Audio_Pause = "_Pause";
        public const string Audio_Pause_ = "Pause playback";
        //public static KeyGesture Audio_Pause_KEYS = Audio_Play_KEYS;

        public const string Audio_ShowOptions = "Show audio options";
        //public static KeyGesture Audio_ShowOptions_KEYS = new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt);


        public const string Ready = "Ready.";

        public const string Audio_ZoomSlider = "Audio waveform zoom";
    }
}