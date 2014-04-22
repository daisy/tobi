using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa.ExternalFiles;
using Microsoft.Practices.Unity;

namespace Tobi.Plugin.Urakawa
{
    public partial class UrakawaSession
    {
        public RichDelegateCommand ClearRecentFilesCommand { get; private set; }
        public RichDelegateCommand OpenRecentCommand { get; private set; }

        private const string RECENT_FILES_FILENAME = @"Tobi_RecentFiles.txt";
        private static readonly string m_RecentFiles_FilePath = Path.Combine(ExternalFilesDataManager.STORAGE_FOLDER_PATH, RECENT_FILES_FILENAME);

        public ObservableCollection<Uri> RecentFiles
        {
            get;
            private set;
        }

        //private readonly List<Uri> m_RecentFiles = new List<Uri>();
        //public IEnumerable<Uri> RecentFiles
        //{
        //    get
        //    {
        //        foreach (var fileUrl in m_RecentFiles)
        //        {
        //            yield return fileUrl;
        //        }
        //    }
        //}


        private void InitializeRecentFiles()
        {
            OpenRecentCommand = new RichDelegateCommand(
                Tobi_Plugin_Urakawa_Lang.CmdOpenRecent_ShortDesc,
                Tobi_Plugin_Urakawa_Lang.CmdOpenRecent_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon(@"folder-saved-search"),
                () =>
                {
                    m_Logger.Log("UrakawaSession.OpenRecentCommand", Category.Debug, Priority.Medium);

                    var view = m_Container.Resolve<RecentFilesView>();

                    var windowPopup = new PopupModalWindow(m_ShellView,
                                                           UserInterfaceStrings.EscapeMnemonic(
                                                           Tobi_Plugin_Urakawa_Lang.Menu_OpenRecent
                        //Tobi_Plugin_Urakawa_Lang.CmdOpenRecent_ShortDesc
                                                           ),
                                                           view,
                                                           PopupModalWindow.DialogButtonsSet.OkCancel,
                                                           PopupModalWindow.DialogButton.Ok,
                                                           true, 800, 500, null, 0,null);
                    //view.OwnerWindow = windowPopup;

                    windowPopup.EnableEnterKeyDefault = true;

                    windowPopup.ShowModal();

                    if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Ok)
                    {
                        if (view.RecentFilesList.SelectedItem != null)
                        {
                            try
                            {
                                OpenFile(((RecentFileWrapper)view.RecentFilesList.SelectedItem).Uri.ToString());
                            }
                            catch (Exception ex)
                            {
                                ExceptionHandler.Handle(ex, false, m_ShellView);
                            }
                        }
                    }
                },
                () => !isAudioRecording,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_OpenRecent));

            m_ShellView.RegisterRichCommand(OpenRecentCommand);
            //
            ClearRecentFilesCommand = new RichDelegateCommand(Tobi_Plugin_Urakawa_Lang.CmdMenuClearRecentFiles_ShortDesc,
                                                   Tobi_Plugin_Urakawa_Lang.CmdMenuClearRecentFiles_LongDesc,
                                                   null,
                                                   m_ShellView.LoadGnomeNeuIcon(@"Neu_view-refresh"),
                                                   ClearRecentFiles,
                                                   () => !isAudioRecording,
                                                   null, null);
            m_ShellView.RegisterRichCommand(ClearRecentFilesCommand);
            //
            RecentFiles = new ObservableCollection<Uri>();

            if (!File.Exists(m_RecentFiles_FilePath))
            {
                return;
            }

            StreamReader streamReader = new StreamReader(m_RecentFiles_FilePath, Encoding.UTF8);
            try
            {
                string recentFileUriString;
                while ((recentFileUriString = streamReader.ReadLine()) != null)
                {
                    Uri recentFileUri;
                    Uri.TryCreate(recentFileUriString, UriKind.Absolute, out recentFileUri);

                    if (recentFileUri == null
                        //||    //TODO: should we filter the URI scheme at this stage?
                        //recentFileUri.Scheme.ToLower() != "file"
                        //&& recentFileUri.Scheme.ToLower() != "http"
                        ) continue;

                    if (!RecentFiles.Contains(recentFileUri))
                        RecentFiles.Add(recentFileUri);
                }
            }
            finally
            {
                streamReader.Close();
            }
        }


        public void AddRecentFile(Uri fileURI)
        {
            if (RecentFiles.Contains(fileURI))
            {
                RecentFiles.Remove(fileURI);
            }
            RecentFiles.Add(fileURI);
            SaveRecentFiles();
        }


        public void SaveRecentFiles()
        {
            StreamWriter streamWriter = new StreamWriter(m_RecentFiles_FilePath, false, Encoding.UTF8);
            try
            {
                foreach (Uri recentFileUri in RecentFiles)
                {
                    streamWriter.WriteLine(recentFileUri.ToString());
                }
            }
            finally
            {
                streamWriter.Close();
            }
        }

        public void ClearRecentFiles()
        {
            RecentFiles.Clear();
            SaveRecentFiles();
        }
    }
}