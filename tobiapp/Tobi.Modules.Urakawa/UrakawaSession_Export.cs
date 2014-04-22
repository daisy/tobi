using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Xml;
using AudioLib;
using Microsoft.Win32;
using Tobi.Common.MVVM;
using Tobi.Common.UI;
using Tobi.Common.Validation;
using urakawa;
using urakawa.daisy;
using urakawa.daisy.export;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using urakawa.daisy.import;
using urakawa.data;
using urakawa.metadata;
using urakawa.metadata.daisy;
using urakawa.xuk;
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

                    Metadata foundDate = null;
                    bool dateIsEmpty = false;
                    foreach (var metadata in DocumentProject.Presentations.Get(0).Metadatas.ContentsAs_Enumerable)
                    {
                        if (metadata.NameContentAttribute != null && metadata.NameContentAttribute.Name.Equals(SupportedMetadata_Z39862005.DC_Date,
                                   StringComparison.OrdinalIgnoreCase))
                        {
                            foundDate = metadata;

                            dateIsEmpty = !string.IsNullOrEmpty(metadata.NameContentAttribute.Value) &&
                                          metadata.NameContentAttribute.Value.Equals(SupportedMetadata_Z39862005.MagicStringEmpty, StringComparison.OrdinalIgnoreCase);
                            break;
                        }
                    }
                    string date = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);//yyyy-MM-ddTHH:mm:ssZ;
                    if (foundDate == null)
                    {
                        var metadata = DocumentProject.Presentations.Get(0).MetadataFactory.CreateMetadata();
                        metadata.NameContentAttribute = new MetadataAttribute();
                        metadata.NameContentAttribute.Name = SupportedMetadata_Z39862005.DC_Date;
                        metadata.NameContentAttribute.Value = date;
                    }
                    else if (dateIsEmpty)
                    {
                        foundDate.NameContentAttribute.Value = date;
                    }


                    bool thereIsAtLeastOneError = false;
                    if (m_Validators != null)
                    {
                        foreach (var validator in m_Validators)
                        {
                            //if (validator is MetadataValidator)
                            validator.Validate();

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

                    if (foundDate == null)
                    {
                        DocumentProject.Presentations.Get(0).DeleteMetadata(SupportedMetadata_Z39862005.DC_Date);
                    }
                    else if (dateIsEmpty)
                    {
                        foundDate.NameContentAttribute.Value = SupportedMetadata_Z39862005.MagicStringEmpty;
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




                    bool isEPUB = IsXukSpine || HasXukSpine;


                    var labelImageDescriptions_AriaDescribedAt = new TextBlock
                    {
                        Text = Tobi_Plugin_Urakawa_Lang.ExportImageDescriptions_AriaDescribedAt,
                        Margin = new Thickness(8, 0, 8, 0),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Focusable = true,
                        TextWrapping = TextWrapping.Wrap
                    };

                    var checkBoxImageDescriptions_AriaDescribedAt = new CheckBox
                    {
                        IsThreeState = false,
                        IsChecked = Settings.Default.ExportImageDescriptions_AriaDescribedAt,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    var panelImageDescriptions_AriaDescribedAt = new StackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Horizontal,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 12, 0),
                    };
                    panelImageDescriptions_AriaDescribedAt.Children.Add(labelImageDescriptions_AriaDescribedAt);
                    panelImageDescriptions_AriaDescribedAt.Children.Add(checkBoxImageDescriptions_AriaDescribedAt);



                    var labelImageDescriptions_AriaDescribedBy = new TextBlock
                    {
                        Text = Tobi_Plugin_Urakawa_Lang.ExportImageDescriptions_AriaDescribedBy,
                        Margin = new Thickness(8, 0, 8, 0),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Focusable = true,
                        TextWrapping = TextWrapping.Wrap
                    };

                    var checkBoxImageDescriptions_AriaDescribedBy = new CheckBox
                    {
                        IsThreeState = false,
                        IsChecked = Settings.Default.ExportImageDescriptions_AriaDescribedBy,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    var panelImageDescriptions_AriaDescribedBy = new StackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Horizontal,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(20, 0, 12, 0),
                    };
                    panelImageDescriptions_AriaDescribedBy.Children.Add(labelImageDescriptions_AriaDescribedBy);
                    panelImageDescriptions_AriaDescribedBy.Children.Add(checkBoxImageDescriptions_AriaDescribedBy);



                    var labelImageDescriptions_HtmlLongDesc = new TextBlock
                    {
                        Text = Tobi_Plugin_Urakawa_Lang.ExportImageDescriptions_HtmlLongDesc,
                        Margin = new Thickness(8, 0, 8, 0),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Focusable = true,
                        TextWrapping = TextWrapping.Wrap
                    };

#if ENABLE_DIAGRAM_LONGDESC_USER_CHOICE

                    var checkBoxImageDescriptions_HtmlLongDesc = new CheckBox
                    {
                        IsThreeState = false,
                        IsChecked = Settings.Default.ExportImageDescriptions_HtmlLongDesc,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    var panelImageDescriptions_HtmlLongDesc = new StackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Horizontal,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(20, 0, 12, 0),
                    };
                    panelImageDescriptions_HtmlLongDesc.Children.Add(labelImageDescriptions_HtmlLongDesc);
                    panelImageDescriptions_HtmlLongDesc.Children.Add(checkBoxImageDescriptions_HtmlLongDesc);
#endif

#if ENABLE_INLINE_DIAGRAM

                    var labelImageDescriptions_inlineTextAudio = new TextBlock
                    {
                        Text = Tobi_Plugin_Urakawa_Lang.ExportImageDescriptions_inlineTextAudio,
                        Margin = new Thickness(8, 0, 8, 0),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Focusable = true,
                        TextWrapping = TextWrapping.Wrap
                    };

                    var checkBoxImageDescriptions_inlineTextAudio = new CheckBox
                    {
                        IsThreeState = false,
                        IsChecked = Settings.Default.ExportImageDescriptions_inlineTextAudio,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    var panelImageDescriptions_inlineTextAudio = new StackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Horizontal,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(20, 0, 12, 0),
                    };
                    panelImageDescriptions_inlineTextAudio.Children.Add(labelImageDescriptions_inlineTextAudio);
                    panelImageDescriptions_inlineTextAudio.Children.Add(checkBoxImageDescriptions_inlineTextAudio);

#endif

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

                    if (isEPUB)
                    {
                        checkBoxIncludeImageDescriptions.Checked += new RoutedEventHandler((sender, ev) =>
                        {
                            checkBoxImageDescriptions_AriaDescribedAt.IsEnabled = true;
                            checkBoxImageDescriptions_AriaDescribedBy.IsEnabled = true;
#if ENABLE_DIAGRAM_LONGDESC_USER_CHOICE
                            checkBoxImageDescriptions_HtmlLongDesc.IsEnabled = true;
#endif

#if ENABLE_INLINE_DIAGRAM
                            checkBoxImageDescriptions_inlineTextAudio.IsEnabled = true;
#endif
                        });
                        checkBoxIncludeImageDescriptions.Unchecked += new RoutedEventHandler((sender, ev) =>
                        {
                            checkBoxImageDescriptions_AriaDescribedAt.IsEnabled = false;
                            checkBoxImageDescriptions_AriaDescribedBy.IsEnabled = false;
#if ENABLE_DIAGRAM_LONGDESC_USER_CHOICE
                            checkBoxImageDescriptions_HtmlLongDesc.IsEnabled = false;
#endif

#if ENABLE_INLINE_DIAGRAM
                            checkBoxImageDescriptions_inlineTextAudio.IsEnabled = false;
#endif
                        });

                        checkBoxIncludeImageDescriptions.IsChecked = !Settings.Default.ExportIncludeImageDescriptions;
                        checkBoxIncludeImageDescriptions.IsChecked = Settings.Default.ExportIncludeImageDescriptions;
                    }

                    //if (isEPUB)
                    //{
                    //    checkBoxIncludeImageDescriptions.IsEnabled = false;
                    //}

                    var panelIncludeImageDescriptions = new StackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Horizontal,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    panelIncludeImageDescriptions.Children.Add(labelIncludeImageDescriptions);
                    panelIncludeImageDescriptions.Children.Add(checkBoxIncludeImageDescriptions);









                    var labelGenSmilNote = new TextBlock
                    {
                        Text = Tobi_Plugin_Urakawa_Lang.ExportGenerateSmilNotes,
                        Margin = new Thickness(8, 0, 8, 0),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Focusable = true,
                        TextWrapping = TextWrapping.Wrap
                    };

                    var checkBoxGenSmilNote = new CheckBox
                    {
                        IsThreeState = false,
                        IsChecked = Settings.Default.ExportGenerateSmilNotes,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    //TODO: noteref SMIL export with EPUB 3?
                    if (isEPUB)
                    {
                        checkBoxGenSmilNote.IsEnabled = false;
                    }

                    var panelGenSmilNote = new StackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Horizontal,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    panelGenSmilNote.Children.Add(labelGenSmilNote);
                    panelGenSmilNote.Children.Add(checkBoxGenSmilNote);







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
                    if (isEPUB)
                    {
                        rootPanel.Children.Add(panelImageDescriptions_AriaDescribedBy);
                        rootPanel.Children.Add(panelImageDescriptions_AriaDescribedAt);
                        
#if ENABLE_INLINE_DIAGRAM
                        rootPanel.Children.Add(panelImageDescriptions_HtmlLongDesc);
#endif

#if ENABLE_INLINE_DIAGRAM
                        rootPanel.Children.Add(panelImageDescriptions_inlineTextAudio);
#endif
                    }

                    if (!isEPUB)
                    {
                        rootPanel.Children.Add(panelGenSmilNote);
                    }

                    var windowPopup_ = new PopupModalWindow(m_ShellView,
                                                           UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Urakawa_Lang.ExportSettings),
                                                           rootPanel,
                                                           PopupModalWindow.DialogButtonsSet.OkCancel,
                                                           PopupModalWindow.DialogButton.Ok,
                                                           false, 300, isEPUB ? 290 : 250, null, 40, null);

                    windowPopup_.EnableEnterKeyDefault = true;

                    windowPopup_.ShowModal();

                    if (!PopupModalWindow.IsButtonOkYesApply(windowPopup_.ClickedDialogButton))
                    {
                        return;
                    }

                    Settings.Default.AudioExportStereo = checkBoxStereo.IsChecked.Value;
                    Settings.Default.AudioExportEncodeToMp3 = checkBoxEncodeMP3.IsChecked.Value;

                    Settings.Default.ExportIncludeImageDescriptions = checkBoxIncludeImageDescriptions.IsChecked.Value;

                    Settings.Default.ExportImageDescriptions_AriaDescribedAt = checkBoxImageDescriptions_AriaDescribedAt.IsChecked.Value;
                    Settings.Default.ExportImageDescriptions_AriaDescribedBy = checkBoxImageDescriptions_AriaDescribedBy.IsChecked.Value;

#if ENABLE_DIAGRAM_LONGDESC_USER_CHOICE
                    Settings.Default.ExportImageDescriptions_HtmlLongDesc = checkBoxImageDescriptions_HtmlLongDesc.IsChecked.Value;
#else
                    Settings.Default.ExportImageDescriptions_HtmlLongDesc = true;
#endif

#if ENABLE_INLINE_DIAGRAM
                    Settings.Default.ExportImageDescriptions_inlineTextAudio = checkBoxImageDescriptions_inlineTextAudio.IsChecked.Value;
#else
                    Settings.Default.ExportImageDescriptions_inlineTextAudio = false;
#endif


                    Settings.Default.ExportGenerateSmilNotes = checkBoxGenSmilNote.IsChecked.Value;

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


                    if (checkWarningFilePathLength(dlg.SelectedPath))
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

                        FileDataProvider.TryDeleteDirectory(exportDir, true);
                    }

                    FileDataProvider.CreateDirectory(exportDir);

                    if (foundDate == null)
                    {
                        var metadata = DocumentProject.Presentations.Get(0).MetadataFactory.CreateMetadata();
                        metadata.NameContentAttribute = new MetadataAttribute();
                        metadata.NameContentAttribute.Name = SupportedMetadata_Z39862005.DC_Date;
                        metadata.NameContentAttribute.Value = date;
                    }
                    else if (dateIsEmpty)
                    {
                        foundDate.NameContentAttribute.Value = date;
                    }
                    try
                    {
                        doExport(exportDir, exportSpineItemProjectPath);
                    }
                    finally
                    {
                        if (foundDate == null)
                        {
                            DocumentProject.Presentations.Get(0).DeleteMetadata(SupportedMetadata_Z39862005.DC_Date);
                        }
                        else if (dateIsEmpty)
                        {
                            foundDate.NameContentAttribute.Value = SupportedMetadata_Z39862005.MagicStringEmpty;
                        }
                    }
                },
                () => DocumentProject != null && !IsSplitMaster && !isAudioRecording //&& !IsSplitSub
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
                          || @"body".Equals(pres.RootNode.GetXmlElementLocalName(), StringComparison.OrdinalIgnoreCase)
                //|| HasXukSpine
                          ;


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
                     Settings.Default.MediaOverlayPlaybackActiveCSS,
                    Settings.Default.ExportImageDescriptions_AriaDescribedAt,
                    Settings.Default.ExportImageDescriptions_AriaDescribedBy,
                    Settings.Default.ExportImageDescriptions_HtmlLongDesc,
                    Settings.Default.ExportImageDescriptions_inlineTextAudio);
            }
            else
            {
                DebugFix.Assert(string.IsNullOrEmpty(exportSpineItemProjectPath));

                converter = new Daisy3_Export(pres, exportDirectory, null,
                 Settings.Default.AudioExportEncodeToMp3, (ushort)Settings.Default.AudioExportMp3Bitrate,
                 Settings.Default.AudioExportSampleRate, Settings.Default.AudioExportStereo,
                 IsAcmCodecsDisabled, Settings.Default.ExportIncludeImageDescriptions, Settings.Default.ExportGenerateSmilNotes);
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
                                else
                                {
                                    string opf = ((Daisy3_Export)converter).OpfFilePath;
                                    checkDAISY(opf);
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

        private void executeProcess(string workingDir, string title, string exe, string args,
            Func<String, String> checkErrorsOrWarning)
        {
            bool isPipeline = exe.IndexOf("dp2.exe", StringComparison.OrdinalIgnoreCase) >= 0;

            using (Process process = new Process())
            {
                bool gone = false;

                process.StartInfo.WorkingDirectory = workingDir;
                process.StartInfo.FileName = exe;
                if (!string.IsNullOrEmpty(args))
                {
                    process.StartInfo.Arguments = args;
                }

                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.ErrorDialog = false;

                Console.WriteLine("process: " + exe + " " + (!string.IsNullOrEmpty(args) ? args : "no-args"));

                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();

                process.Disposed += (object sender, EventArgs e) =>
                {
                    Console.WriteLine("process.Disposed");
                };

                process.Exited += (object sender, EventArgs e) =>
                {
                    Console.WriteLine("process.Exited");
                };

                using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                {
                    process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                    {
                        Console.WriteLine("process.ErrorDataReceived: " + e.Data);

                        if (e.Data == null)
                        {
                            if (!gone)
                            {
                                errorWaitHandle.Set();
                            }
                        }
                        else
                        {
                            error.AppendLine(e.Data);

                            // Hack for dp2.exe
                            if (e.Data.IndexOf("[DP2] DONE", StringComparison.Ordinal) >= 0
                                || e.Data.IndexOf("[DP2] ERROR", StringComparison.Ordinal) >= 0
                                || (isPipeline && e.Data.IndexOf("ERROR", StringComparison.OrdinalIgnoreCase) >= 0)
                                || e.Data.IndexOf("dp2 help COMMAND", StringComparison.OrdinalIgnoreCase) >= 0
                                )
                            {
                                if (!gone)
                                {
                                    outputWaitHandle.Set();
                                    errorWaitHandle.Set();

                                    if (!process.HasExited)
                                    {
                                        process.Kill();
                                    }
                                }
                            }
                        }
                    };

                    process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                    {
                        Console.WriteLine("process.OutputDataReceived: " + e.Data);

                        if (e.Data == null)
                        {
                            if (!gone)
                            {
                                outputWaitHandle.Set();
                            }
                        }
                        else
                        {
                            output.AppendLine(e.Data);

                            // Hack for dp2.exe
                            if (e.Data.IndexOf("[DP2] DONE", StringComparison.Ordinal) >= 0
                                || e.Data.IndexOf("[DP2] ERROR", StringComparison.Ordinal) >= 0
                                || (isPipeline && e.Data.IndexOf("ERROR", StringComparison.OrdinalIgnoreCase) >= 0)
                                || e.Data.IndexOf("dp2 help COMMAND", StringComparison.OrdinalIgnoreCase) >= 0
                                )
                            {
                                if (!gone)
                                {
                                    outputWaitHandle.Set();
                                    errorWaitHandle.Set();

                                    if (!process.HasExited)
                                    {
                                        process.Kill();
                                    }
                                }
                            }
                        }
                    };

                    process.EnableRaisingEvents = true;

                    process.Start();

                    process.PriorityBoostEnabled = true;
                    process.PriorityClass = ProcessPriorityClass.AboveNormal;

                    process.BeginErrorReadLine();
                    process.BeginOutputReadLine();

                    int timeout = (int)(1000 * 60 * Math.Abs(Settings.Default.ProcessTimeoutMinutes));
                    if (timeout <= 0)
                    {
                        timeout = 1000 * 60 * 2; // 2 minutes
                    }
                    if (isPipeline)
                    {
                        timeout *= 2;
                    }

                    bool notTimeout = isPipeline ? true : process.WaitForExit(timeout);

                    string EXEC = exe + " " + (!string.IsNullOrEmpty(args) ? args : "no-args") + Environment.NewLine + "========================" + Environment.NewLine + Environment.NewLine;

                    if (notTimeout)
                    {
                        notTimeout = outputWaitHandle.WaitOne(timeout);
                        if (notTimeout)
                        {
                            notTimeout = errorWaitHandle.WaitOne(timeout);
                        }

                        string report = output.ToString();
                        string errorStr = error.ToString();

                        if (!isPipeline || report.IndexOf("[DP2] DONE", StringComparison.Ordinal) < 0)
                        {
                            if (!notTimeout || !process.HasExited)
                            {
                                messageBoxText(title, "Timeout?",
                                    EXEC + errorStr + Environment.NewLine + Environment.NewLine + report);
                            }
                            else if (process.ExitCode != 0)
                            {
                                if (!isPipeline
                                    && report.IndexOf("ERROR", StringComparison.Ordinal) < 0
                                    && errorStr.IndexOf("ERROR", StringComparison.Ordinal) < 0
                                    )
                                {
                                    messageBoxText(title, "Warning",
                                        EXEC + errorStr + Environment.NewLine + Environment.NewLine + report);
                                }
                                else
                                {
                                    messageBoxText(title, "Error!",
                                        EXEC + errorStr + Environment.NewLine + Environment.NewLine + report);
                                }
                            }
                        }
                        else if (checkErrorsOrWarning != null)
                        {
                            string text = "Success.";

                            string errorWarningReport = checkErrorsOrWarning(report);

                            report = EXEC + report;

                            if (!string.IsNullOrEmpty(errorWarningReport))
                            {
                                text = "There are errors or warnings!";

                                report = errorWarningReport + Environment.NewLine + Environment.NewLine + report;
                            }

                            messageBoxText(title, text, report + Environment.NewLine + Environment.NewLine + errorStr);
                        }

                        //if (process.StartInfo.RedirectStandardOutput && process.StartInfo.RedirectStandardError)
                        //{
                        //    if (process.ExitCode != 0)
                        //    {
                        //        StreamReader stdErr = process.StandardError;
                        //        if (!stdErr.EndOfStream)
                        //        {
                        //            report = stdErr.ReadToEnd();
                        //        }
                        //    }
                        //    else
                        //    {
                        //        StreamReader stdOut = process.StandardOutput;
                        //        if (!stdOut.EndOfStream)
                        //        {
                        //            report = stdOut.ReadToEnd();
                        //        }
                        //    }
                        //}
                    }
                    else
                    {
                        outputWaitHandle.WaitOne(timeout);
                        errorWaitHandle.WaitOne(timeout);
                        messageBoxText(title, "Process timeout!", EXEC + output.ToString() + Environment.NewLine + error.ToString());
                    }

                    //if (exe.IndexOf("dp2.exe") >= 0)
                    //{
                    //    process.Kill();
                    //}

                    gone = true;
                }
            }
        }

        private string obtainPipelineExe()
        {
            //messageBoxAlert("WARNING: Pipeline 2 support is experimental!", null);

            //string ext = @".exe";
            string exeOrBat = @"dp2.exe";
            if (!Settings.Default.Pipeline2OldExe)
            {
                //ext = @".bat";
                exeOrBat = @"pipeline2.bat";
            }

            bool registryChecked = false;

            string pipeline_ExePath = Settings.Default.Pipeline2Path;
            while (string.IsNullOrEmpty(pipeline_ExePath)
                || pipeline_ExePath.IndexOf(exeOrBat, StringComparison.OrdinalIgnoreCase) < 0
                || !File.Exists(pipeline_ExePath))
            {
                if (!registryChecked)
                {
                    registryChecked = true;

                    string folderPath = null;
                    try
                    {
                        object val = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\DAISY Pipeline 2").GetValue(@"Pipeline2Home");
                        folderPath = val as string;
                        pipeline_ExePath = Path.Combine(folderPath, (exeOrBat.EndsWith(".exe") ? "cli" : "bin") + "\\" + exeOrBat);

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                    }

                    continue;
                }

                if (true)
                {
                    pipeline_ExePath = messageBoxFilePick("Pipeline2 (" + exeOrBat + ")", exeOrBat);

                    if (string.IsNullOrEmpty(pipeline_ExePath))
                    {
                        return null;
                    }
                }
                else
                {
                    messageBoxText("Pipeline2", "Please specify the location of [" + exeOrBat + "]...", exeOrBat);

                    string ext = Path.GetExtension(exeOrBat);

                    var dlg_ = new Microsoft.Win32.OpenFileDialog
                        {
                            FileName = exeOrBat,
                            DefaultExt = ext,

                            Filter = @"Executable (*" + ext + ")|*" + ext + "",
                            CheckFileExists = false,
                            CheckPathExists = false,
                            AddExtension = true,
                            DereferenceLinks = true,
                            Title =
                                @"Tobi: " +
                                "Pipeline2 (" + exeOrBat + ")"
                        };

                    bool? result_ = false;

                    m_ShellView.DimBackgroundWhile(() => { result_ = dlg_.ShowDialog(); });

                    if (result_ == false)
                    {
                        return null;
                    }

                    pipeline_ExePath = dlg_.FileName;
                }
            }

            if (!string.IsNullOrEmpty(pipeline_ExePath))
            {
                Settings.Default.Pipeline2Path = pipeline_ExePath;

                double version = 1.7;
                string filePath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(pipeline_ExePath)), "etc\\system.properties");
                if (File.Exists(filePath))
                {
                    string content = File.ReadAllText(filePath);
                    string match = "org.daisy.pipeline.version";
                    int index = content.IndexOf(match);
                    if (index >= 0)
                    {
                        string vStr = content.Substring(index + match.Length + 1, 3);
                        version = Double.Parse(vStr);
                    }
                }
                if (version < 1.7)
                {
                    messageBoxText("Pipeline2", Tobi_Plugin_Urakawa_Lang.PleaseUpgradePipeline, "Pipeline2 v" + version + " (> 1.7)\n\nhttp://daisy.org/pipeline2");
                    return null;
                }
            }

            return pipeline_ExePath;
        }

        private bool checkDAISY(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path)
                    && File.Exists(path))
                {
                    if (!askUser("DAISY Check?", path))
                    {
                        return false;
                    }

                    string workingDir =
                        Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                    workingDir = Path.Combine(workingDir, "ZedVal");

                    var dirInfo = new DirectoryInfo(workingDir);

#if NET40
                    IEnumerable<FileInfo> jars = dirInfo.EnumerateFiles("*.jar", SearchOption.TopDirectoryOnly);
#else
                    FileInfo[] jars = dirInfo.GetFiles("*.jar", SearchOption.TopDirectoryOnly);
#endif
                    string classpath = "";
                    foreach (FileInfo jarInfo in jars)
                    {
                        classpath += (jarInfo.FullName + ";");
                    }

                    //classpath = workingDir + Path.DirectorySeparatorChar + "zedval-2.1.jar;";
                    //classpath += (workingDir + Path.DirectorySeparatorChar + "batik-css-1.6-1.jar;");
                    //classpath += (workingDir + Path.DirectorySeparatorChar + "batik-util-1.6-1.jar;");
                    //classpath += (workingDir + Path.DirectorySeparatorChar + "commons-cli-1.1.jar;");
                    //classpath += (workingDir + Path.DirectorySeparatorChar + "daisy-util-20100125.jar;");
                    //classpath += (workingDir + Path.DirectorySeparatorChar + "jing-20091111.jar;");
                    //classpath += (workingDir + Path.DirectorySeparatorChar + "jlayer-1.0.1.jar;");
                    //classpath += (workingDir + Path.DirectorySeparatorChar + "sac-1.3.jar;");
                    //classpath += (workingDir + Path.DirectorySeparatorChar + "Saxon-HE-9.4.jar;");
                    //classpath += (workingDir + Path.DirectorySeparatorChar + "stax-api-1.0.1.jar;");
                    //classpath += (workingDir + Path.DirectorySeparatorChar + "wstx-lgpl-3.2.9.jar;");
                    //classpath += (workingDir + Path.DirectorySeparatorChar + "xercesImpl-2.9.1.jar;");
                    //classpath += (workingDir + Path.DirectorySeparatorChar + "xml-apis-1.0.b2.jar;");

                    Func<String, String> checkErrorsOrWarning =
                        (string report) =>
                        {
                            if (report.IndexOf("<message", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                string xmlInfo = "";

                                XmlDocument xmlDoc = XmlReaderWriterHelper.ParseXmlDocumentFromString(report, false,
                                                                                                      false);

                                IEnumerable<XmlNode> messages = XmlDocumentHelper.GetChildrenElementsOrSelfWithName(
                                    xmlDoc.DocumentElement,
                                    true,
                                    "message",
                                    null,
                                    false);

                                foreach (XmlNode message in messages)
                                {
                                    XmlNode file = XmlDocumentHelper.GetFirstChildElementOrSelfWithName(
                                        message,
                                        false,
                                        "file",
                                        null);

                                    if (file != null && file.Attributes != null)
                                    {
                                        XmlNode nameAttr = file.Attributes.GetNamedItem("name");
                                        string name = null;
                                        if (nameAttr != null)
                                        {
                                            name = nameAttr.Value;
                                        }

                                        XmlNode lineAttr = file.Attributes.GetNamedItem("line");
                                        string line = null;
                                        if (lineAttr != null)
                                        {
                                            line = lineAttr.Value;
                                        }

                                        XmlNode columnAttr = file.Attributes.GetNamedItem("column");
                                        string column = null;
                                        if (columnAttr != null)
                                        {
                                            column = columnAttr.Value;
                                        }

                                        xmlInfo += "\n----------------------------------";
                                        xmlInfo += ("\nPATH: " + name);
                                        xmlInfo += ("\nLINE: " + line);
                                        xmlInfo += ("\nCOLUMN: " + column);
                                    }

                                    XmlNode detail = XmlDocumentHelper.GetFirstChildElementOrSelfWithName(
                                        message,
                                        false,
                                        "detail",
                                        null);

                                    if (detail != null)
                                    {
                                        xmlInfo += "\n";
                                        xmlInfo += detail.InnerText;
                                        xmlInfo += "\n----------------------------------";
                                    }
                                }

                                return xmlInfo;
                            }

                            return null;
                        };

                    executeProcess(
                        workingDir,
                        "DAISY Check",
                        "java.exe",
                        "-classpath \""
                        + classpath
                        + "\" org.daisy.zedval.ZedVal"
                        + " -timeTolerance 50"
                        + " \"" + path + "\"",
                        checkErrorsOrWarning);
                }
            }
            catch (Exception ex)
            {
                messageBoxText("Oops :(", "Problem running DAISY Check! (ZedVal)", ex.Message + Environment.NewLine + ex.StackTrace);
            }

            return true;
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

                    Func<String, String> checkErrorsOrWarning = (string report) =>
                        {
                            if (report.IndexOf("No errors", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return null;
                            }
                            return string.Empty;
                        };

                    executeProcess(
                        workingDir,
                        "EPUB Check",
                        "java.exe",
                        "-jar \""
                        + jar
                        + "\""
                        + (!string.IsNullOrEmpty(mode) ? " -mode " + mode + " -v 3.0" : "")
                         + " \""
                         + path
                         + "\"",
                        checkErrorsOrWarning);
                }
            }
            catch (Exception ex)
            {
                messageBoxText("Oops :(", "Problem running EPUB Check!", ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
    }
}
