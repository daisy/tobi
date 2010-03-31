using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Tobi.Common.MVVM;
using urakawa.daisy.export;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM.Command;

namespace Tobi.Plugin.Urakawa
{
    public partial class UrakawaSession
    {
        public RichDelegateCommand ExportCommand { get; private set; }

        private void initCommands_Export()
        {
            //
            ExportCommand = new RichDelegateCommand(
                Tobi_Plugin_Urakawa_Lang.CmdExport_ShortDesc,
                Tobi_Plugin_Urakawa_Lang.CmdExport_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadGnomeNeuIcon(@"Neu_accessories-archiver"),
                //ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Save_As")),
                () =>
                {
                    if (DocumentProject == null)
                    {
                        return;
                    }

                    m_Logger.Log(@"UrakawaSession.Export", Category.Debug, Priority.Medium);

                    string rootFolder = Path.GetDirectoryName(DocumentFilePath);

                    var dlg = new FolderBrowserDialog
                      {
                          RootFolder = Environment.SpecialFolder.MyComputer,
                          SelectedPath = rootFolder,
                          ShowNewFolderButton = true,
                          Description = @"Tobi: " + UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Urakawa_Lang.CmdExport_ShortDesc)
                      };

                    DialogResult result = DialogResult.Abort;

                    m_ShellView.DimBackgroundWhile(() => { result = dlg.ShowDialog(); });

                    if (result != DialogResult.OK && result != DialogResult.Yes)
                    {
                        return;
                    }
                    if (!Directory.Exists(dlg.SelectedPath))
                    {
                        return;
                    }

                    string exportFolderName = Path.GetFileName(DocumentFilePath) + "__EXPORT";
                    string exportDir = Path.Combine(dlg.SelectedPath, exportFolderName);

                    if (Directory.Exists(exportDir))
                    {
                        if (!askUserConfirmOverwriteFileFolder(exportDir, true))
                        {
                            return;
                        }

                        Directory.Delete(exportDir, true);
                    }
                    
                    Directory.CreateDirectory(exportDir);

                    if (!Directory.Exists(exportDir))
                    {
                        return;
                    }

                    doExport(exportDir);
                },
                () => DocumentProject != null,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Export));

            m_ShellView.RegisterRichCommand(ExportCommand);
        }

        private void doExport(string path)
        {
            m_Logger.Log(String.Format(@"UrakawaSession.doExport() [{0}]", path), Category.Debug, Priority.Medium);

            var converter = new Daisy3_Export(DocumentProject.Presentations.Get(0), path, null, Settings.Default.AudioExportEncodeToMp3);

            m_ShellView.RunModalCancellableProgressTask(true,
                Tobi_Plugin_Urakawa_Lang.Exporting,
                converter,
                () =>
                {
                    m_Logger.Log(@"UrakawaSession-Daisy3_Export-CANCELED-ShowFolder", Category.Debug, Priority.Medium);

                    m_ShellView.ExecuteShellProcess(path);
                },
                () =>
                {
                    m_Logger.Log(@"UrakawaSession-Daisy3_Export-DONE-ShowFolder", Category.Debug, Priority.Medium);

                    m_ShellView.ExecuteShellProcess(path);
                });
        }
    }
}
