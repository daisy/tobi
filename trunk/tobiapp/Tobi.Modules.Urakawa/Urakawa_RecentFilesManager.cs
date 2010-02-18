using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using Tobi.Common.MVVM.Command;
using urakawa.ExternalFiles;

namespace Tobi.Plugin.Urakawa
{
    public partial class UrakawaSession
    {
        private string m_RecentFilesListStorageFileName = "Tobi_RecentFiles.txt";
        private List<string> m_recent_files_list = new List<string>();
        private string m_recentFiles_Save_Path = null;

        public List<string> RecentFilesList
        {
            get
            {
                if (m_recent_files_list.Count == 0)
                {
                    InitializeRecentFilesList();
                }
                return m_recent_files_list;
            }
        }

        public void InitializeRecentFilesList()
        {
            FileStream recentFilesStream = null;
            StreamReader textFileReader = null;
            m_recentFiles_Save_Path = Path.Combine(ExternalFilesDataManager.STORAGE_FOLDER_PATH,
                                                   m_RecentFilesListStorageFileName);
            if (File.Exists(m_recentFiles_Save_Path))
            {
                recentFilesStream = File.Open(m_recentFiles_Save_Path, FileMode.Open, FileAccess.Read);
            }
            try
            {
                if (recentFilesStream != null)
                {
                    textFileReader = new StreamReader(recentFilesStream);
                    string fullFilePath = null;

                    while ((fullFilePath = textFileReader.ReadLine()) != null)
                    {
                        if (!m_recent_files_list.Contains(fullFilePath))
                            m_recent_files_list.Add(fullFilePath);
                    }
                }
            }
            finally
            {
                if (textFileReader != null) textFileReader.Close();
                if (recentFilesStream != null) recentFilesStream.Close();
            }
        }


        public void AddToRecentFilesList(string file_path)
        {
            if (!m_recent_files_list.Contains(file_path))
            {
                m_recent_files_list.Add(file_path);
                SaveRecentFilesList();
            }
        }


        public void SaveRecentFilesList()
        {
            FileStream recentFileStream = null;
            StreamWriter textFileWriter = null;
            try
            {
                recentFileStream = File.Create(m_recentFiles_Save_Path);

                textFileWriter = new StreamWriter(recentFileStream);

                for (int i = 0; i < m_recent_files_list.Count; i++)
                {
                    textFileWriter.WriteLine(m_recent_files_list[i]);
                }
            }
            finally
            {
                if (textFileWriter != null) textFileWriter.Close();
                if (recentFileStream != null) recentFileStream.Close();
                if (recentFileStream != null) recentFileStream.Close();
            }
        }

        public void Clear()
        {
            m_recent_files_list.Clear();
            SaveRecentFilesList();
        }
    }
}