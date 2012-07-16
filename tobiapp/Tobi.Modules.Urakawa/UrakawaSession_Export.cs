using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using AudioLib;
using Tobi.Common.MVVM;
using Tobi.Common.UI;
using Tobi.Common.Validation;
using urakawa.daisy.export;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using urakawa.data;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;
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
                //m_ShellView.LoadGnomeNeuIcon(@"Neu_accessories-archiver"),
                m_ShellView.LoadTangoIcon(@"media-eject"),
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
                                                               true, 350, 160, null, 40, null);

                        windowPopup.ShowModal();

                        if (PopupModalWindow.IsButtonEscCancel(windowPopup.ClickedDialogButton))
                        {
                            if (m_EventAggregator != null)
                            {
                                m_EventAggregator.GetEvent<ValidationReportRequestEvent>().Publish(null);
                            }
                            return;
                        }
                    }





                    var combo = new ComboBox
                    {
                        Margin = new Thickness(0, 0, 0, 12)
                    };

                    ComboBoxItem item1 = new ComboBoxItem();
                    item1.Content = AudioLib.SampleRate.Hz11025.ToString();
                    combo.Items.Add(item1);

                    ComboBoxItem item2 = new ComboBoxItem();
                    item2.Content = AudioLib.SampleRate.Hz22050.ToString();
                    combo.Items.Add(item2);

                    ComboBoxItem item3 = new ComboBoxItem();
                    item3.Content = AudioLib.SampleRate.Hz44100.ToString();
                    combo.Items.Add(item3);

                    switch (Settings.Default.AudioExportSampleRate)
                    {
                        case AudioLib.SampleRate.Hz11025:
                            {
                                combo.SelectedItem = item1;
                                combo.Text = item1.Content.ToString();
                                break;
                            }
                        case AudioLib.SampleRate.Hz22050:
                            {
                                combo.SelectedItem = item2;
                                combo.Text = item2.Content.ToString();
                                break;
                            }
                        case AudioLib.SampleRate.Hz44100:
                            {
                                combo.SelectedItem = item3;
                                combo.Text = item3.Content.ToString();
                                break;
                            }
                    }

                    var checkBox = new CheckBox
                    {
                        IsThreeState = false,
                        IsChecked = Settings.Default.AudioExportEncodeToMp3,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    var label_ = new TextBlock
                    {
                        Text = Tobi_Plugin_Urakawa_Lang.ExportEncodeMp3,
                        Margin = new Thickness(8, 0, 8, 0),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Focusable = true,
                        TextWrapping = TextWrapping.Wrap
                    };


                    var panel__ = new StackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Horizontal,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    panel__.Children.Add(label_);
                    panel__.Children.Add(checkBox);

                    var checkBoxz = new CheckBox
                    {
                        IsThreeState = false,
                        IsChecked = Settings.Default.ExportIncludeImageDescriptions,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    var label_z = new TextBlock
                    {
                        Text = Tobi_Plugin_Urakawa_Lang.ExportIncludeImageDescriptions,
                        Margin = new Thickness(8, 0, 8, 0),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Focusable = true,
                        TextWrapping = TextWrapping.Wrap
                    };



                    var panel_z_ = new StackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Horizontal,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    panel_z_.Children.Add(label_z);
                    panel_z_.Children.Add(checkBoxz);

                    var panel_ = new StackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Vertical,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    panel_.Children.Add(combo);
                    panel_.Children.Add(panel__);
                    panel_.Children.Add(panel_z_);

                    var windowPopup_ = new PopupModalWindow(m_ShellView,
                                                           UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Urakawa_Lang.ExportSettings),
                                                           panel_,
                                                           PopupModalWindow.DialogButtonsSet.OkCancel,
                                                           PopupModalWindow.DialogButton.Ok,
                                                           false, 300, 180, null, 40, null);

                    windowPopup_.EnableEnterKeyDefault = true;

                    windowPopup_.ShowModal();

                    if (!PopupModalWindow.IsButtonOkYesApply(windowPopup_.ClickedDialogButton))
                    {
                        return;
                    }

                    Settings.Default.AudioExportEncodeToMp3 = checkBox.IsChecked.Value;
                    Settings.Default.ExportIncludeImageDescriptions = checkBoxz.IsChecked.Value;

                    if (combo.SelectedItem == item1)
                    {
                        Settings.Default.AudioExportSampleRate = SampleRate.Hz11025;
                    }
                    else if (combo.SelectedItem == item2)
                    {
                        Settings.Default.AudioExportSampleRate = SampleRate.Hz22050;
                    }
                    else if (combo.SelectedItem == item3)
                    {
                        Settings.Default.AudioExportSampleRate = SampleRate.Hz44100;
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
                        if (!askUserConfirmOverwriteFileFolder(exportDir, true, null))
                        {
                            return;
                        }

                        FileDataProvider.DeleteDirectory(exportDir);
                    }

                    FileDataProvider.CreateDirectory(exportDir);

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

            var converter = new Daisy3_Export(DocumentProject.Presentations.Get(0), path, null,
                Settings.Default.AudioExportEncodeToMp3, (ushort)Settings.Default.AudioExportMp3Bitrate,
                Settings.Default.AudioExportSampleRate, Settings.Default.AudioExportStereo,
                IsAcmCodecsDisabled, Settings.Default.ExportIncludeImageDescriptions);

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
