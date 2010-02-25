using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using urakawa.ExternalFiles;

namespace Tobi.Plugin.Urakawa
{
    public partial class UrakawaSession
    {
        public RichDelegateCommand ClearRecentFilesCommand { get; private set; }

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
            ClearRecentFilesCommand = new RichDelegateCommand(UserInterfaceStrings.Menu_ClearRecentFiles,
                                                   UserInterfaceStrings.Menu_ClearRecentFiles_,
                                                   null,
                                                   m_ShellView.LoadGnomeNeuIcon(@"Neu_view-refresh"),
                                                   ClearRecentFiles,
                                                   () => true,
                                                   null, null);

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
            if (!RecentFiles.Contains(fileURI))
            {
                RecentFiles.Add(fileURI);
                SaveRecentFiles();
            }
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