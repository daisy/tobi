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
            StreamReader textFileReader = null ;

            try
                {
                if (store.GetFileNames ( m_RecentFilesListStorageFileName ).Length > 0)
                    {
                    storageStream =
        new IsolatedStorageFileStream ( m_RecentFilesListStorageFileName, FileMode.Open, FileAccess.Read, FileShare.None, store );
                    }


                if (storageStream != null)
                    {
                    textFileReader = new StreamReader ( storageStream );
                    string fullFilePath = null;

                    while ((fullFilePath = textFileReader.ReadLine ()) != null)
                        {
                        if (!m_recent_files_list.Contains ( fullFilePath ))
                            m_recent_files_list.Add ( fullFilePath );

                        }

                    }
                }
            finally
                {
                if ( textFileReader != null )  textFileReader.Close ();
                if ( storageStream != null )  storageStream.Close ();
                if ( store != null )  store.Close ();
                }
            }


        public void AddToRecentFilesList ( string file_path )
            {
            if (!m_recent_files_list.Contains ( file_path ))
                {
                m_recent_files_list.Add ( file_path );
                SaveRecentFilesList ();
                }
            }


        public void SaveRecentFilesList ()
            {
            IsolatedStorageFileStream storageStream = null;
            StreamWriter textFileWriter = null;

            IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication ();

            try
                {
                storageStream =
               new IsolatedStorageFileStream ( m_RecentFilesListStorageFileName, FileMode.Create, store );

                textFileWriter = new StreamWriter ( storageStream );

                for (int i = 0; i < m_recent_files_list.Count; i++)
                    {
                    textFileWriter.WriteLine ( m_recent_files_list[i] );
                    }
                
                }
            finally
                {
                if ( textFileWriter != null )  textFileWriter.Close ();
                if ( storageStream != null )  storageStream.Close ();
                if ( store != null )  store.Close ();
                }
            
            }

        public void Clear ()
            {
            m_recent_files_list.Clear ();
            SaveRecentFilesList ();
            }
        }
    }