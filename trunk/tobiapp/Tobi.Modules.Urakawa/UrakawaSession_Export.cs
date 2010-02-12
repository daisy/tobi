using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Tobi.Common.MVVM;
using urakawa.daisy.export;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa.events.progress;
using urakawa.xuk;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Orientation = System.Windows.Controls.Orientation;
using ProgressBar = System.Windows.Controls.ProgressBar;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.UI;
using urakawa.daisy.import;

namespace Tobi.Plugin.Urakawa
{
    public partial class UrakawaSession
    {
        public RichDelegateCommand ExportCommand { get; private set; }

        private void initCommands_Export()
        {
            //
            ExportCommand = new RichDelegateCommand(
                UserInterfaceStrings.Export,
                UserInterfaceStrings.Export_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon(@"emblem-symbolic-link"),
                //ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Save_As")),
                () =>
                {
                    if (DocumentProject == null)
                    {
                        return;
                    }

                    m_Logger.Log(@"UrakawaSession.Export", Category.Debug, Priority.Medium);

                    var dlg = new FolderBrowserDialog
                    {
                        ShowNewFolderButton = true,
                        Description = @"Tobi: " + UserInterfaceStrings.EscapeMnemonic(UserInterfaceStrings.Export)
                    };

                    DialogResult result = DialogResult.Abort;

                    m_ShellView.DimBackgroundWhile(() => { result = dlg.ShowDialog(); });

                    if (result != DialogResult.OK && result != DialogResult.Yes)
                    {
                        return;
                    }

                    if (Directory.Exists(dlg.SelectedPath))
                    {
                        if (!askUserConfirmOverwriteFileFolder(dlg.SelectedPath, true))
                        {
                            return;
                        }

                        Directory.Delete(dlg.SelectedPath, true);
                        Directory.CreateDirectory(dlg.SelectedPath);
                    }

                    doExport(dlg.SelectedPath);
                },
                () => DocumentProject != null,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Export));

            m_ShellView.RegisterRichCommand(ExportCommand);
        }

        private void doExport(string path)
        {
            m_Logger.Log(String.Format(@"UrakawaSession.doExport() [{0}]", path), Category.Debug, Priority.Medium);

            var converter = new Daisy3_Export(DocumentProject.Presentations.Get(0), path, null);

            DoWorkProgressUI("Exporting ...",
                converter,
                () =>
                {
                    ;
                },
                () =>
                {
                    m_Logger.Log(@"UrakawaSession-Daisy3_Export-DONE-OpenFolder", Category.Debug, Priority.Medium);

                    var p = new Process
                    {
                        StartInfo = { FileName = path }
                    };
                    p.Start();
                });
        }
    }
}
