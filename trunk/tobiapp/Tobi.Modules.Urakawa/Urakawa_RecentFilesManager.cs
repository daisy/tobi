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
        private List<string> m_recent_files_list = new List<string>();
        public void Add(string file_path)
        {
            if (m_recent_files_list.Contains(file_path))
            {
                m_recent_files_list.Add(file_path);
                Save();
            }
        }
        public void Save()
        {
            string mydocpath = System.AppDomain.CurrentDomain.BaseDirectory + "\\TobiRecentFilesList.txt";

            StreamWriter writeText = File.CreateText(mydocpath);
            for (int i = 0; i < m_recent_files_list.Count; i++)
            {
                writeText.WriteLine(m_recent_files_list[i]);
            }
            writeText.Close();
        }

        public void Clear()
        {
            m_recent_files_list.Clear();
            Save();
        }

        public List<string> RecentFilesList
        {
            get { return m_recent_files_list; }

        }
        
    }
}
