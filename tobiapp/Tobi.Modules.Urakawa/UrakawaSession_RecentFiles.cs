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
        private const string m_RecentFilesListStorageFileName = "Tobi_RecentFiles.txt";
        private static readonly List<string> m_recent_files_list = new List<string>();
        private string m_recentFiles_Save_Path = null;

        public List<string> RecentFilesList
        {
            get
            {
                //This part has been commented so InitializeRecentFilesList() should be called explicitly .
               /* if (m_recent_files_list.Count == 0)
                {
                    InitializeRecentFilesList();
                } */
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
                    string recentFileURL = null;

                    while ((recentFileURL = textFileReader.ReadLine()) != null)
                    {
                        if (!m_recent_files_list.Contains(recentFileURL))
                            m_recent_files_list.Add(recentFileURL);
                    }
                }
            }
            finally
            {
                if (textFileReader != null) textFileReader.Close();
            }
        }


        public void AddToRecentFilesList(string fileURL)
        {
            if (!m_recent_files_list.Contains(fileURL))
            {
                m_recent_files_list.Add(fileURL);
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
            }
        }

        public void Clear()
        {
            m_recent_files_list.Clear();
            SaveRecentFilesList();
        }
    }
}