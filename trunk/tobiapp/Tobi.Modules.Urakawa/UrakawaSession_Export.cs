using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Threading;
using AudioLib;
using Tobi.Common.MVVM;
using Tobi.Common.UI;
using Tobi.Common.Validation;
using urakawa;
using urakawa.daisy.export;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using urakawa.daisy.import;
using urakawa.data;
using Application = System.Windows.Application;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;
using HorizontalAlignment = System.Windows.Forms.HorizontalAlignment;
using MessageBox = System.Windows.Forms.MessageBox;
using Orientation = System.Windows.Forms.Orientation;

namespace Tobi.Plugin.Urakawa
{
    public partial class UrakawaSession
    {
        public RichDelegateCommand ExportCommand { get; private set; }

        private string m_ExportSpineItemProjectPath = null;

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

                    string exportSpineItemProjectPath = m_ExportSpineItemProjectPath;
                    m_ExportSpineItemProjectPath = null;

                    if (!IsXukSpine && HasXukSpine)
                    {
                        m_ExportSpineItemProjectPath = DocumentFilePath;

                        //MessageBox.Show
                        //messageBoxAlert("WARNING: single chapter export is an experimental and incomplete EPUB feature!", null);

                        Application.Current.MainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                            (Action)(() =>
                            {
                                bool opened = false;
                                try
                                {
                                    opened = OpenFile(XukSpineProjectPath, false);
                                }
                                catch (Exception ex)
                                {
                                    ExceptionHandler.Handle(ex, false, m_ShellView);
                                }

                                if (opened)
                                {
                                    ExportCommand.Execute();
                                }

                                m_ExportSpineItemProjectPath = null;
                            }
                            ));
                        return;
                    }



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





                    var comboSampleRates = new ComboBox
                    {
                        Margin = new Thickness(0, 0, 0, 2)
                    };

                    ComboBoxItem item1 = new ComboBoxItem();
                    item1.Content = AudioLib.SampleRate.Hz11025.ToString();
                    comboSampleRates.Items.Add(item1);

                    ComboBoxItem item2 = new ComboBoxItem();
                    item2.Content = AudioLib.SampleRate.Hz22050.ToString();
                    comboSampleRates.Items.Add(item2);

                    ComboBoxItem item3 = new ComboBoxItem();
                    item3.Content = AudioLib.SampleRate.Hz44100.ToString();
                    comboSampleRates.Items.Add(item3);

                    switch (Settings.Default.AudioExportSampleRate)
                    {
                        case AudioLib.SampleRate.Hz11025:
                            {
                                comboSampleRates.SelectedItem = item1;
                                comboSampleRates.Text = item1.Content.ToString();
                                break;
                            }
                        case AudioLib.SampleRate.Hz22050:
                            {
                                comboSampleRates.SelectedItem = item2;
                                comboSampleRates.Text = item2.Content.ToString();
                                break;
                            }
                        case AudioLib.SampleRate.Hz44100:
                            {
                                comboSampleRates.SelectedItem = item3;
                                comboSampleRates.Text = item3.Content.ToString();
                                break;
                            }
                    }

                    var labelStereo = new TextBlock
                    {
                        Text = Tobi_Plugin_Urakawa_Lang.Stereo,
                        Margin = new Thickness(8, 0, 8, 0),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Focusable = true,
                        TextWrapping = TextWrapping.Wrap
                    };

                    var checkBoxStereo = new CheckBox
                    {
                        IsThreeState = false,
                        IsChecked = Settings.Default.AudioExportStereo,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    var panelStereo = new StackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Horizontal,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 12),
                    };
                    panelStereo.Children.Add(labelStereo);
                    panelStereo.Children.Add(checkBoxStereo);


                    var labelEncodeMP3 = new TextBlock
                    {
                        Text = Tobi_Plugin_Urakawa_Lang.ExportEncodeMp3,
                        Margin = new Thickness(8, 0, 8, 0),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Focusable = true,
                        TextWrapping = TextWrapping.Wrap
                    };

                    var checkBoxEncodeMP3 = new CheckBox
                    {
                        IsThreeState = false,
                        IsChecked = Settings.Default.AudioExportEncodeToMp3,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    var panelEncodeMP3 = new StackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Horizontal,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 2)
                    };
                    panelEncodeMP3.Children.Add(labelEncodeMP3);
                    panelEncodeMP3.Children.Add(checkBoxEncodeMP3);

                    var comboMp3BitRates = new ComboBox
                    {
                        Margin = new Thickness(0, 0, 0, 12)
                    };

                    ComboBoxItem itemBitRate1 = new ComboBoxItem();
                    itemBitRate1.Content = AudioLib.Mp3BitRate.kbps_32.ToString();
                    comboMp3BitRates.Items.Add(itemBitRate1);

                    ComboBoxItem itemBitRate2 = new ComboBoxItem();
                    itemBitRate2.Content = AudioLib.Mp3BitRate.kbps_48.ToString();
                    comboMp3BitRates.Items.Add(itemBitRate2);

                    ComboBoxItem itemBitRate3 = new ComboBoxItem();
                    itemBitRate3.Content = AudioLib.Mp3BitRate.kbps_64.ToString();
                    comboMp3BitRates.Items.Add(itemBitRate3);

                    ComboBoxItem itemBitRate4 = new ComboBoxItem();
                    itemBitRate4.Content = AudioLib.Mp3BitRate.kbps_128.ToString();
                    comboMp3BitRates.Items.Add(itemBitRate4);

                    ComboBoxItem itemBitRate5 = new ComboBoxItem();
                    itemBitRate5.Content = AudioLib.Mp3BitRate.kbps_196.ToString();
                    comboMp3BitRates.Items.Add(itemBitRate5);

                    ComboBoxItem itemBitRate6 = new ComboBoxItem();
                    itemBitRate6.Content = AudioLib.Mp3BitRate.kbps_320.ToString();
                    comboMp3BitRates.Items.Add(itemBitRate6);


                    switch (Settings.Default.AudioExportMp3Bitrate)
                    {
                        case AudioLib.Mp3BitRate.kbps_32:
                            {
                                comboMp3BitRates.SelectedItem = itemBitRate1;
                                comboMp3BitRates.Text = itemBitRate1.Content.ToString();
                                break;
                            }
                        case AudioLib.Mp3BitRate.kbps_48:
                            {
                                comboMp3BitRates.SelectedItem = itemBitRate2;
                                comboMp3BitRates.Text = itemBitRate2.Content.ToString();
                                break;
                            }
                        case AudioLib.Mp3BitRate.kbps_64:
                            {
                                comboMp3BitRates.SelectedItem = itemBitRate3;
                                comboMp3BitRates.Text = itemBitRate3.Content.ToString();
                                break;
                            }
                        case AudioLib.Mp3BitRate.kbps_128:
                            {
                                comboMp3BitRates.SelectedItem = itemBitRate4;
                                comboMp3BitRates.Text = itemBitRate4.Content.ToString();
                                break;
                            }
                        case AudioLib.Mp3BitRate.kbps_196:
                            {
                                comboMp3BitRates.SelectedItem = itemBitRate5;
                                comboMp3BitRates.Text = itemBitRate5.Content.ToString();
                                break;
                            }
                        case AudioLib.Mp3BitRate.kbps_320:
                            {
                                comboMp3BitRates.SelectedItem = itemBitRate6;
                                comboMp3BitRates.Text = itemBitRate6.Content.ToString();
                                break;
                            }
                    }

                    comboMp3BitRates.IsEnabled = checkBoxEncodeMP3.IsChecked.Value;

                    checkBoxEncodeMP3.Click += (object sender, RoutedEventArgs e) =>
                        {
                            comboMp3BitRates.IsEnabled = checkBoxEncodeMP3.IsChecked.Value;
                        };

                    var labelIncludeImageDescriptions = new TextBlock
                    {
                        Text = Tobi_Plugin_Urakawa_Lang.ExportIncludeImageDescriptions,
                        Margin = new Thickness(8, 0, 8, 0),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Focusable = true,
                        TextWrapping = TextWrapping.Wrap
                    };

                    var checkBoxIncludeImageDescriptions = new CheckBox
                    {
                        IsThreeState = false,
                        IsChecked = Settings.Default.ExportIncludeImageDescriptions,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    //TODO: DIAGRAM export with EPUB 3
                    if (IsXukSpine || HasXukSpine)
                    {
                        checkBoxIncludeImageDescriptions.IsEnabled = false;
                    }

                    var panelIncludeImageDescriptions = new StackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Horizontal,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    panelIncludeImageDescriptions.Children.Add(labelIncludeImageDescriptions);
                    panelIncludeImageDescriptions.Children.Add(checkBoxIncludeImageDescriptions);


                    var rootPanel = new StackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Vertical,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    rootPanel.Children.Add(comboSampleRates);
                    rootPanel.Children.Add(panelStereo);
                    rootPanel.Children.Add(panelEncodeMP3);
                    rootPanel.Children.Add(comboMp3BitRates);
                    rootPanel.Children.Add(panelIncludeImageDescriptions);

                    var windowPopup_ = new PopupModalWindow(m_ShellView,
                                                           UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Urakawa_Lang.ExportSettings),
                                                           rootPanel,
                                                           PopupModalWindow.DialogButtonsSet.OkCancel,
                                                           PopupModalWindow.DialogButton.Ok,
                                                           false, 300, 220, null, 40, null);

                    windowPopup_.EnableEnterKeyDefault = true;

                    windowPopup_.ShowModal();

                    if (!PopupModalWindow.IsButtonOkYesApply(windowPopup_.ClickedDialogButton))
                    {
                        return;
                    }

                    Settings.Default.AudioExportStereo = checkBoxStereo.IsChecked.Value;
                    Settings.Default.AudioExportEncodeToMp3 = checkBoxEncodeMP3.IsChecked.Value;
                    Settings.Default.ExportIncludeImageDescriptions = checkBoxIncludeImageDescriptions.IsChecked.Value;

                    if (comboSampleRates.SelectedItem == item1)
                    {
                        Settings.Default.AudioExportSampleRate = SampleRate.Hz11025;
                    }
                    else if (comboSampleRates.SelectedItem == item2)
                    {
                        Settings.Default.AudioExportSampleRate = SampleRate.Hz22050;
                    }
                    else if (comboSampleRates.SelectedItem == item3)
                    {
                        Settings.Default.AudioExportSampleRate = SampleRate.Hz44100;
                    }


                    if (comboMp3BitRates.SelectedItem == itemBitRate1)
                    {
                        Settings.Default.AudioExportMp3Bitrate = Mp3BitRate.kbps_32;
                    }
                    else if (comboMp3BitRates.SelectedItem == itemBitRate2)
                    {
                        Settings.Default.AudioExportMp3Bitrate = Mp3BitRate.kbps_48;
                    }
                    else if (comboMp3BitRates.SelectedItem == itemBitRate3)
                    {
                        Settings.Default.AudioExportMp3Bitrate = Mp3BitRate.kbps_64;
                    }
                    else if (comboMp3BitRates.SelectedItem == itemBitRate4)
                    {
                        Settings.Default.AudioExportMp3Bitrate = Mp3BitRate.kbps_128;
                    }
                    else if (comboMp3BitRates.SelectedItem == itemBitRate5)
                    {
                        Settings.Default.AudioExportMp3Bitrate = Mp3BitRate.kbps_196;
                    }
                    else if (comboMp3BitRates.SelectedItem == itemBitRate6)
                    {
                        Settings.Default.AudioExportMp3Bitrate = Mp3BitRate.kbps_320;
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

                    string path = DocumentFilePath;
                    //string title = Daisy3_Import.GetTitle(DocumentProject.Presentations.Get(0));
                    //if (!string.IsNullOrEmpty(title))
                    //{
                    //    path = Daisy3_Import.GetXukFilePath(dlg.SelectedPath, DocumentFilePath, title, IsXukSpine);
                    //}
                    string exportFolderName = Path.GetFileName(path) + "_EX";

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

                    doExport(exportDir, exportSpineItemProjectPath);
                },
                () => DocumentProject != null
                //&& (IsXukSpine || !HasXukSpine)
                ,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Export));

            m_ShellView.RegisterRichCommand(ExportCommand);
        }

        private void doExport(string exportDirectory, string exportSpineItemProjectPath)
        {
            m_Logger.Log(String.Format(@"UrakawaSession.doExport() [{0}]", exportDirectory), Category.Debug, Priority.Medium);

            Presentation pres = DocumentProject.Presentations.Get(0);

            bool isEPUB = IsXukSpine
                          || @"body".Equals(pres.RootNode.GetXmlElementLocalName(), StringComparison.OrdinalIgnoreCase);


            IDualCancellableProgressReporter converter = null;
            if (isEPUB)
            {
#if DEBUG
                if (IsXukSpine)
                {
                    DebugFix.Assert(@"spine".Equals(pres.RootNode.GetXmlElementLocalName(), StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    Debugger.Break();

                    DebugFix.Assert(HasXukSpine);
                }
#endif
                converter = new Epub3_Export(DocumentFilePath, pres, exportDirectory,
                     Settings.Default.AudioExportEncodeToMp3, (ushort)Settings.Default.AudioExportMp3Bitrate,
                     Settings.Default.AudioExportSampleRate, Settings.Default.AudioExportStereo,
                     IsAcmCodecsDisabled, Settings.Default.ExportIncludeImageDescriptions, exportSpineItemProjectPath,
                     Settings.Default.MediaOverlayActiveCSS);
            }
            else
            {
                DebugFix.Assert(string.IsNullOrEmpty(exportSpineItemProjectPath));

                converter = new Daisy3_Export(pres, exportDirectory, null,
                 Settings.Default.AudioExportEncodeToMp3, (ushort)Settings.Default.AudioExportMp3Bitrate,
                 Settings.Default.AudioExportSampleRate, Settings.Default.AudioExportStereo,
                 IsAcmCodecsDisabled, Settings.Default.ExportIncludeImageDescriptions);
            }

            bool error = m_ShellView.RunModalCancellableProgressTask(true,
                Tobi_Plugin_Urakawa_Lang.Exporting,
                converter,
                () =>
                {
                    m_Logger.Log(@"UrakawaSession-Daisy3_Export-CANCELED-ShowFolder", Category.Debug, Priority.Medium);

                    m_ShellView.ExecuteShellProcess(exportDirectory);
                },
                () =>
                {
                    m_Logger.Log(@"UrakawaSession-Daisy3_Export-DONE-ShowFolder", Category.Debug, Priority.Medium);

                    Application.Current.MainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        (Action)(() =>
                        {
                            try
                            {
                                if (isEPUB)
                                {
                                    string epub = ((Epub3_Export)converter).EpubFilePath;
                                    checkEpub(epub, null);
                                }
                            }
                            finally
                            {
                                m_ShellView.ExecuteShellProcess(exportDirectory);
                            }
                        }
                        ));
                });
        }

        private void checkEpub(string path, string mode)
        {
            try
            {
                if (!string.IsNullOrEmpty(path)
                    && (mode == "exp" ? Directory.Exists(path) : File.Exists(path))
                    && askUser("EPUB-Check?", path))
                {
                    string workingDir =
                        Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                    string jar = Path.Combine(workingDir, "epubcheck.jar");

                    Process process = new Process();

                    process.StartInfo.WorkingDirectory = workingDir;
                    process.StartInfo.FileName = @"java.exe";
                    process.StartInfo.Arguments = "-jar \"" + jar + "\""
                        + (!string.IsNullOrEmpty(mode) ? " -mode " + mode + " -v 3.0" : "")
                         + " \"" + path + "\"";

                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.CreateNoWindow = false;
                    process.StartInfo.ErrorDialog = false;



                    process.Start();
                    process.WaitForExit();

                    string report = null;
                    string text = null;
                    if (process.ExitCode != 0)
                    {
                        StreamReader stdErr = process.StandardError;
                        if (!stdErr.EndOfStream)
                        {
                            report = stdErr.ReadToEnd();
                            text = "Error!";
                        }
                    }
                    else
                    {
                        StreamReader stdOut = process.StandardOutput;
                        if (!stdOut.EndOfStream)
                        {
                            report = stdOut.ReadToEnd();
                        }
                    }

                    if (!string.IsNullOrEmpty(report))
                    {
                        Console.Write(report);

                        if (text == null)
                        {
                            if (report.IndexOf("No errors", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                text = "Success.";
                            }
                            else
                            {
                                text = "There are errors or warnings!";
                            }
                        }

                        messageBoxText("EPUB Check", text, report);
                    }
                }
            }
            catch (Exception ex)
            {
                messageBoxText("Oops :(", "Problem running EPUB Check!", ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
    }
}
