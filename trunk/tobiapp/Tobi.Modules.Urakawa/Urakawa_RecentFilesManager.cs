using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using Tobi.Common.MVVM.Command;

namespace Tobi.Plugin.Urakawa
    {
    public partial class UrakawaSession
        {

        private string m_RecentFilesListStorageFileName = "Tobi_RecentFiles.txt";
        private List<string> m_recent_files_list = new List<string> ();

        public List<string> RecentFilesList
            {
            get
                {
                if (m_recent_files_list.Count == 0)
                    {
                    InitializeRecentFilesList ();
                    }
                return m_recent_files_list;
                }
            }

        public void InitializeRecentFilesList ()
            {
            IsolatedStorageFileStream storageStream = null;

            IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication ();

            if (store.GetFileNames ( m_RecentFilesListStorageFileName ).Length > 0)
                {

                storageStream =
                    new IsolatedStorageFileStream ( m_RecentFilesListStorageFileName, FileMode.Open, FileAccess.Read, FileShare.None, store );
                }


            if (storageStream != null)
                {
                StreamReader sr = new StreamReader ( storageStream );
                string fullFilePath = null;

                while ((fullFilePath = sr.ReadLine ()) != null)
                    {
                    if (!m_recent_files_list.Contains ( fullFilePath ))
                        m_recent_files_list.Add ( fullFilePath );

                    }
                sr.Close ();
                storageStream.Close ();
                store.Close ();
                }
            }


        public void AddToRecentFilesList ( string file_path )
            {
            if (m_recent_files_list.Contains ( file_path ))
                {
                m_recent_files_list.Add ( file_path );
                SaveRecentFilesList ();
                }
            }


        public void SaveRecentFilesList ()
            {
            IsolatedStorageFileStream storageStream = null;

            IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication ();

            storageStream =
           new IsolatedStorageFileStream ( m_RecentFilesListStorageFileName, FileMode.Create, store );


            StreamWriter writeText = new StreamWriter ( storageStream );

            for (int i = 0; i < m_recent_files_list.Count; i++)
                {
                writeText.WriteLine ( m_recent_files_list[i] );
                }
            writeText.Close ();
            storageStream.Close ();
            store.Close ();
            }

        public void Clear ()
            {
            m_recent_files_list.Clear ();
            SaveRecentFilesList ();
            }
        }
    }