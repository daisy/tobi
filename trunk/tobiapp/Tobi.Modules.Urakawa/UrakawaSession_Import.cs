using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using AudioLib;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
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

            string outputDirectory = Path.Combine(Path.GetDirectoryName(DocumentFilePath),
                                                  XUK_DIR + Path.DirectorySeparatorChar +
                                                  Path.GetFileName(DocumentFilePath) + XUK_DIR);

            string xukPath = Daisy3_Import.GetXukFilePath(outputDirectory, DocumentFilePath);
            if (File.Exists(xukPath))
            {
                if (!askUserConfirmOverwriteFileFolder(xukPath, false, null))
                {
                    return false;
                }
            }


            var combo = new ComboBox();

            ComboBoxItem item1 = new ComboBoxItem();
            item1.Content = AudioLib.SampleRate.Hz11025.ToString();
            combo.Items.Add(item1);

            ComboBoxItem item2 = new ComboBoxItem();
            item2.Content = AudioLib.SampleRate.Hz22050.ToString();
            combo.Items.Add(item2);

            ComboBoxItem item3 = new ComboBoxItem();
            item3.Content = AudioLib.SampleRate.Hz44100.ToString();
            combo.Items.Add(item3);

            switch (Settings.Default.AudioProjectSampleRate)
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

            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
            };
            panel.Children.Add(combo);

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Urakawa_Lang.ProjectSampleRate),
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   false, 300, 135, null, 40, null);

            windowPopup.EnableEnterKeyDefault = true;

            windowPopup.ShowModal();

            if (!PopupModalWindow.IsButtonOkYesApply(windowPopup.ClickedDialogButton))
            {
                return false;
            }

            if (combo.SelectedItem == item1)
            {
                Settings.Default.AudioProjectSampleRate = SampleRate.Hz11025;
            }
            else if (combo.SelectedItem == item2)
            {
                Settings.Default.AudioProjectSampleRate = SampleRate.Hz22050;
            }
            else if (combo.SelectedItem == item3)
            {
                Settings.Default.AudioProjectSampleRate = SampleRate.Hz44100;
            }



            var converter = new Daisy3_Import(DocumentFilePath, outputDirectory,
                IsAcmCodecsDisabled,
                Settings.Default.AudioProjectSampleRate,
                Settings.Default.XUK_PrettyFormat
                ); //Directory.GetParent(bookfile).FullName


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
