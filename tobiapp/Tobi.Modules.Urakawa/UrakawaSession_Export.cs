using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Tobi.Common.MVVM;
using Tobi.Common.UI;
using Tobi.Common.Validation;
using urakawa.daisy.export;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using HorizontalAlignment = System.Windows.Forms.HorizontalAlignment;
using Orientation = System.Windows.Forms.Orientation;

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

                    bool thereIsAtLeastOneError = false;
                    if (m_Validators != null)
                    {
                        foreach (var validator in m_Validators)
                        {
                            foreach (var validationItem in validator.ValidationItems)
                            {
                                if (validationItem.Severity == ValidationSeverity.Error)
                                {
                                    thereIsAtLeastOneError = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (thereIsAtLeastOneError)
                    {
                        m_Logger.Log("UrakawaSession.Expor VALIDATION", Category.Debug, Priority.Medium);

                        var label = new TextBlock
                        {
                            Text = Tobi_Plugin_Urakawa_Lang.ValidationIssuesConfirmExport,
                            Margin = new Thickness(8, 0, 8, 0),
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Focusable = true,
                            TextWrapping = TextWrapping.Wrap
                        };

                        var iconProvider = new ScalableGreyableImageProvider(m_ShellView.LoadTangoIcon("dialog-warning"), m_ShellView.MagnificationLevel);

                        var panel = new StackPanel
                        {
                            Orientation = System.Windows.Controls.Orientation.Horizontal,
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Stretch,
                        };
                        panel.Children.Add(iconProvider.IconLarge);
                        panel.Children.Add(label);

                        var windowPopup = new PopupModalWindow(m_ShellView,
                                                               UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Urakawa_Lang.ValidationIssuesConfirmExport_Title),
                                                               panel,
                                                               PopupModalWindow.DialogButtonsSet.OkCancel,
                                                               PopupModalWindow.DialogButton.Cancel,
                                                               true, 350, 160, null, 40);

                        windowPopup.ShowModal();

                        if (PopupModalWindow.IsButtonEscCancel(windowPopup.ClickedDialogButton))
                        {
                            m_EventAggregator.GetEvent<ValidationReportRequestEvent>().Publish(null);
                            return;
                        }
                    }

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
