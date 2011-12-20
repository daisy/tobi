using System;
using System.Diagnostics;
using System.IO;
using AudioLib;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common.UI;
using urakawa.daisy.import;

namespace Tobi.Plugin.Urakawa
{
    public partial class UrakawaSession
    {
        private const string XUK_DIR = "_XUK"; // prepend with '_' so it appears at the top of the alphabetical sorting in the file explorer window

        private bool doImport()
        {
            m_Logger.Log(String.Format(@"UrakawaSession.doImport() [{0}]", DocumentFilePath), Category.Debug, Priority.Medium);

            var converter = new Daisy3_Import(DocumentFilePath,
                Path.Combine(Path.GetDirectoryName(DocumentFilePath),
                XUK_DIR),
                IsAcmCodecsDisabled,
                Settings.Default.AudioProjectSampleRate,
                Settings.Default.XUK_PrettyFormat
                ); //Directory.GetParent(bookfile).FullName


            if (File.Exists(converter.XukPath))
            {
                if (!askUserConfirmOverwriteFileFolder(converter.XukPath, false))
                {
                    return false;
                }
            }

            bool cancelled = false;

            bool result = m_ShellView.RunModalCancellableProgressTask(true,
                Tobi_Plugin_Urakawa_Lang.Importing,
                converter,
                () =>
                {
                    cancelled = true;
                    DocumentFilePath = null;
                    DocumentProject = null;
                },
                () =>
                {
                    cancelled = false;
                    if (string.IsNullOrEmpty(converter.XukPath))
                    {
                        return;
                    }

                    //DocumentFilePath = converter.XukPath;
                    //DocumentProject = converter.Project;

                    //AddRecentFile(new Uri(DocumentFilePath, UriKind.Absolute));
                });

            if (result) //NOT cancelled
            {
                DebugFix.Assert(!cancelled);

                if (string.IsNullOrEmpty(converter.XukPath)) return false;

                DocumentFilePath = null;
                DocumentProject = null;
                try
                {
                    OpenFile(converter.XukPath);
                }
                catch (Exception ex)
                {
                    ExceptionHandler.Handle(ex, false, m_ShellView);
                    return false;
                }
            }

            return result;
        }
    }
}
