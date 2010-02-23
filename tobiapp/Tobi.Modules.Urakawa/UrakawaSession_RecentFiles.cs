using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using urakawa.ExternalFiles;

namespace Tobi.Plugin.Urakawa
{
    public partial class UrakawaSession
    {
        private const string RECENT_FILES_FILENAME = @"Tobi_RecentFiles.txt";
        private static readonly string m_RecentFiles_FilePath = Path.Combine(ExternalFilesDataManager.STORAGE_FOLDER_PATH, RECENT_FILES_FILENAME);

        private readonly List<Uri> m_RecentFiles = new List<Uri>();

        public IEnumerable<Uri> RecentFiles
        {
            get
            {
                foreach (var fileUrl in m_RecentFiles)
                {
                    yield return fileUrl;
                }
            }
        }

        private void InitializeRecentFiles()
        {
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

                    if (!m_RecentFiles.Contains(recentFileUri))
                        m_RecentFiles.Add(recentFileUri);
                }
            }
            finally
            {
                streamReader.Close();
            }
        }


        public void AddRecentFile(Uri fileURI)
        {
            if (!m_RecentFiles.Contains(fileURI))
            {
                m_RecentFiles.Add(fileURI);
                SaveRecentFiles();
            }
        }


        public void SaveRecentFiles()
        {
            StreamWriter streamWriter = new StreamWriter(m_RecentFiles_FilePath, false, Encoding.UTF8);
            try
            {
                foreach (Uri recentFileUri in m_RecentFiles)
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
            m_RecentFiles.Clear();
            SaveRecentFiles();
        }
    }
}