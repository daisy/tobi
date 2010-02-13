using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Tobi.Common.MVVM.Command;

namespace Tobi.Plugin.Urakawa
    {
    public partial class UrakawaSession
        {

        private string m_File_Save_Path = null;
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
            m_File_Save_Path = System.AppDomain.CurrentDomain.BaseDirectory + "\\TobiRecentFilesList.txt"; // can store it in better place

            if (File.Exists ( m_File_Save_Path ))
                {
                StreamReader sr = new StreamReader ( m_File_Save_Path );
                string fullFilePath = null ;

                while ((fullFilePath = sr.ReadLine () ) != null)
                    {
                     if ( !m_recent_files_list.Contains (fullFilePath ))
                         m_recent_files_list.Add ( fullFilePath );
                    }
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
            
            StreamWriter writeText = File.CreateText ( m_File_Save_Path);
            for (int i = 0; i < m_recent_files_list.Count; i++)
                {
                writeText.WriteLine ( m_recent_files_list[i] );
                }
            writeText.Close ();
            }

        public void Clear ()
            {
            m_recent_files_list.Clear ();
            SaveRecentFilesList ();
            }
        }
    }