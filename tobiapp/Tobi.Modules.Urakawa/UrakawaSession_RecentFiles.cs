using System.Collections.Generic;
using System.IO;
using urakawa.ExternalFiles;

namespace Tobi.Plugin.Urakawa
{
    public partial class UrakawaSession
    {
        private const string RECENT_FILES_FILENAME = @"Tobi_RecentFiles.txt";
        private static readonly string m_RecentFiles_FilePath = Path.Combine(ExternalFilesDataManager.STORAGE_FOLDER_PATH, RECENT_FILES_FILENAME);

        private static readonly List<string> m_RecentFiles = new List<string>();

        public IEnumerable<string> RecentFiles
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
            FileStream recentFilesStream = null;
            StreamReader textFileReader = null;
            
            if (File.Exists(m_RecentFiles_FilePath))
            {
                recentFilesStream = File.Open(m_RecentFiles_FilePath, FileMode.Open, FileAccess.Read);
            }
            try
            {
                if (recentFilesStream != null)
                {
                    textFileReader = new StreamReader(recentFilesStream);
                    string recentFileURL = null;

                    while ((recentFileURL = textFileReader.ReadLine()) != null)
                    {
                        if (!m_RecentFiles.Contains(recentFileURL))
                            m_RecentFiles.Add(recentFileURL);
                    }
                }
            }
            finally
            {
                if (textFileReader != null) textFileReader.Close();
            }
        }


        public void AddRecentFile(string fileURL)
        {
            if (!m_RecentFiles.Contains(fileURL))
            {
                m_RecentFiles.Add(fileURL);
                SaveRecentFiles();
            }
        }


        public void SaveRecentFiles()
        {
            FileStream recentFileStream = null;
            StreamWriter textFileWriter = null;
            try
            {
                recentFileStream = File.Create(m_RecentFiles_FilePath);

                textFileWriter = new StreamWriter(recentFileStream);

                for (int i = 0; i < m_RecentFiles.Count; i++)
                {
                    textFileWriter.WriteLine(m_RecentFiles[i]);
                }
            }
            finally
            {
                if (textFileWriter != null) textFileWriter.Close();
                if (recentFileStream != null) recentFileStream.Close();
            }
        }

        public void ClearRecentFiles()
        {
            m_RecentFiles.Clear();
            SaveRecentFiles();
        }
    }
}