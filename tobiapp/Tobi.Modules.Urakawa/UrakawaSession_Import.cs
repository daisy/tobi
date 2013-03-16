using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using AudioLib;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.UI;
using urakawa.daisy.import;
using urakawa.data;
using System.Xml;
using urakawa.daisy;
using urakawa.xuk;

namespace Tobi.Plugin.Urakawa
{
    public partial class UrakawaSession
    {
        private bool doImport()
        {
            m_Logger.Log(String.Format(@"UrakawaSession.doImport() [{0}]", DocumentFilePath), Category.Debug, Priority.Medium);

            string ext = Path.GetExtension(DocumentFilePath);

            if (".opf".Equals(ext, StringComparison.OrdinalIgnoreCase))
            {
                int levelsUp = 0;

                string parentDir = Path.GetDirectoryName(DocumentFilePath);

                DirectoryInfo parentDirInfo = new DirectoryInfo(parentDir);

            tryAgain:
                levelsUp++;

#if NET40
                IEnumerable<DirectoryInfo> metaInfDirs = parentDirInfo.EnumerateDirectories("META-INF", SearchOption.TopDirectoryOnly);
#else
            DirectoryInfo[] metaInfDirs = parentDirInfo.GetDirectories("META-INF", SearchOption.TopDirectoryOnly);
#endif

                bool found = false;

                foreach (DirectoryInfo dirInfo in metaInfDirs)
                {
                    string containerXml = Path.Combine(dirInfo.FullName, "container.xml");
                    if (File.Exists(containerXml))
                    {
                        DocumentFilePath = containerXml;
                        ext = Path.GetExtension(DocumentFilePath);
                        found = true;
                        break;
                    }
                }

                if (!found && levelsUp <= 2 && parentDirInfo.Parent != null)
                {
                    parentDirInfo = parentDirInfo.Parent;
                    goto tryAgain;
                }
            }

            if (".epub".Equals(ext, StringComparison.OrdinalIgnoreCase))
            {
                checkEpub(DocumentFilePath, null);
            }
            else if (".xml".Equals(ext, StringComparison.OrdinalIgnoreCase)
                &&
                FileDataProvider.NormaliseFullFilePath(DocumentFilePath).IndexOf(
                        @"META-INF"
                //+ Path.DirectorySeparatorChar
                        + '/'
                        + @"container.xml"
                        , StringComparison.OrdinalIgnoreCase) >= 0
                //DocumentFilePath.IndexOf("container.xml", StringComparison.OrdinalIgnoreCase) >= 0
            )
            {
                string parentDir = Path.GetDirectoryName(DocumentFilePath);
                DirectoryInfo dirInfo = new DirectoryInfo(parentDir);
                if (dirInfo.Parent != null)
                {
                    string checkEpubPath = dirInfo.Parent.FullName;
                    checkEpub(checkEpubPath, "exp");
                }
            }
            else if (".opf".Equals(ext, StringComparison.OrdinalIgnoreCase))
            {
                if (!checkDAISY(DocumentFilePath))
                {
                    //checkEpub(DocumentFilePath, "opf"); assume container.xml was found (see above)
                }
            }
            else if (".html".Equals(ext, StringComparison.OrdinalIgnoreCase)
                || ".xhtml".Equals(ext, StringComparison.OrdinalIgnoreCase))
            {
                //MessageBox.Show
                messageBoxAlert("WARNING: single HTML import is an experimental and incomplete EPUB feature!", null);

                checkEpub(DocumentFilePath, "xhtml");
            }
            else if (".xml".Equals(ext, StringComparison.OrdinalIgnoreCase))
            {
                //checkDAISY(DocumentFilePath);
                if (false && // TODO: Pipeline 2 with better support for DTBOOK validation (currently skips metadata values)
                    askUser("DAISY Check?", DocumentFilePath))
                {
                    string pipeline_ExePath = obtainPipelineExe();
                    if (!string.IsNullOrEmpty(pipeline_ExePath))
                    {
                        string outDir = Path.GetDirectoryName(DocumentFilePath);
                        outDir = Path.Combine(outDir, Path.GetFileName(DocumentFilePath) + "_PIPEVAL");


                        bool success = false;
                        Func<String, String> checkErrorsOrWarning =
                            (string report) =>
                            {
                                if (report.IndexOf("[DP2] DONE", StringComparison.Ordinal) < 0)
                                {
                                    return "Pipeline job doesn't appear to have completed?";
                                }

                                string reportFile = Path.Combine(outDir, "report.xhtml");
                                if (File.Exists(reportFile))
                                {
                                    string reportXmlSource = File.ReadAllText(reportFile);
                                    if (!string.IsNullOrEmpty(reportXmlSource))
                                    {
                                        string xmlInfo = "";

                                        XmlDocument xmlDoc = XmlReaderWriterHelper.ParseXmlDocumentFromString(reportXmlSource, false,
                                                                                                              false);

                                        IEnumerable<XmlNode> lis = XmlDocumentHelper.GetChildrenElementsOrSelfWithName(
                                            xmlDoc.DocumentElement,
                                            true,
                                            "li",
                                            null,
                                            false);

                                        foreach (XmlNode li in lis)
                                        {
                                            if (li.Attributes == null)
                                            {
                                                continue;
                                            }
                                            XmlNode classAttr = li.Attributes.GetNamedItem("class");
                                            if (classAttr == null || classAttr.Value != "error")
                                            {
                                                continue;
                                            }
                                            xmlInfo += li.InnerText;
                                            xmlInfo += Environment.NewLine;
                                        }

                                        if (string.IsNullOrEmpty(xmlInfo))
                                        {
                                            success = true;
                                            return null;
                                        }

                                        return xmlInfo;
                                    }
                                }

                                success = true;
                                return null;
                            };

                        try
                        {
                            string workingDir = Path.GetDirectoryName(pipeline_ExePath);
                            //Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                            executeProcess(
                                workingDir,
                                "DAISY Pipeline (validation)",
                                //"\"" +
                                pipeline_ExePath
                                //+ "\""
                                ,
                                "dtbook-validator " +
                                "--i-source \"" + DocumentFilePath + "\" " +
                                "--x-output-dir \"" + outDir + "\" ",
                                checkErrorsOrWarning);
                        }
                        catch (Exception ex)
                        {
                            messageBoxText("Oops :(", "Problem running DAISY Pipeline (validation)!",
                                           ex.Message + Environment.NewLine + ex.StackTrace);
                        }

                        if (Directory.Exists(outDir))
                        {
                            //m_ShellView.ExecuteShellProcess(outDir);
                            FileDataProvider.TryDeleteDirectory(outDir, false);
                        }
                    }
                }
            }




            string outputDirectory = Path.Combine(
                Path.GetDirectoryName(DocumentFilePath),
                Daisy3_Import.GetXukDirectory(DocumentFilePath));

            //string xukPath = Daisy3_Import.GetXukFilePath(outputDirectory, DocumentFilePath);
            //if (File.Exists(xukPath))
            if (Directory.Exists(outputDirectory))
            {
                if (!askUserConfirmOverwriteFileFolder(outputDirectory, true, null))
                {
                    return false;
                }

                FileDataProvider.TryDeleteDirectory(outputDirectory, true);
            }

            var combo = new ComboBox
            {
                Margin = new Thickness(0, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
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

            var label_ = new TextBlock
            {
                Text = Tobi_Plugin_Urakawa_Lang.Stereo,
                Margin = new Thickness(0, 0, 8, 0),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap
            };

            var checkBox = new CheckBox
            {
                IsThreeState = false,
                IsChecked = Settings.Default.AudioProjectStereo,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };


            var panel__ = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 22),
            };
            panel__.Children.Add(label_);
            panel__.Children.Add(checkBox);


            var label = new TextBlock
            {
                Text = Tobi_Plugin_Urakawa_Lang.UseSourceAudioFormat,
                Margin = new Thickness(0, 0, 8, 0),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap
            };

            var checkAuto = new CheckBox
            {
                IsThreeState = false,
                IsChecked = false,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var panel_ = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12),
            };
            panel_.Children.Add(label);
            panel_.Children.Add(checkAuto);

            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
            };
            panel.Children.Add(combo);
            panel.Children.Add(panel__);
            panel.Children.Add(panel_);


            //var line = new Separator()
            //{
            //    Margin = new Thickness(0, 8, 0, 8),
            //};
            //line.HorizontalAlignment = HorizontalAlignment.Stretch;
            //line.VerticalAlignment = VerticalAlignment.Center;

            //panel.Children.Add(line);


            var details = new TextBoxReadOnlyCaretVisible
            {
                FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],

                BorderThickness = new Thickness(1),
                Padding = new Thickness(6),
                TextReadOnly = Tobi_Plugin_Urakawa_Lang.UseSourceAudioFormatTip
            };

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Urakawa_Lang.ProjectAudioFormat),
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   false, 320, 200, details, 40, null);

            windowPopup.EnableEnterKeyDefault = true;

            windowPopup.ShowModal();

            if (!PopupModalWindow.IsButtonOkYesApply(windowPopup.ClickedDialogButton))
            {
                return false;
            }

            Settings.Default.AudioProjectStereo = checkBox.IsChecked.Value;

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
                Settings.Default.AudioProjectStereo,
                checkAuto.IsChecked.Value,
                Settings.Default.XUK_PrettyFormat
                ); //Directory.GetParent(bookfile).FullName


            //converter.VerifyHtml5OutliningAlgorithmUsingPipelineTestSuite();

            bool cancelled = false;

            bool error = m_ShellView.RunModalCancellableProgressTask(true,
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

            if (!cancelled)
            {
                //DebugFix.Assert(!report);

                string xukPath = converter.XukPath;

                if (string.IsNullOrEmpty(xukPath))
                {
                    return false;
                }

                string projectDir = Path.GetDirectoryName(xukPath);
                DebugFix.Assert(outputDirectory == projectDir);

                string title = converter.GetTitle();
                if (!string.IsNullOrEmpty(title))
                {
                    string fileName = Daisy3_Import.GetXukFilePath(projectDir, DocumentFilePath, title, false);
                    fileName = Path.GetFileNameWithoutExtension(fileName);

                    string parent = Path.GetDirectoryName(projectDir);

                    //string fileName = Path.GetFileNameWithoutExtension(xukPath);

                    ////while (fileName.StartsWith("_"))
                    ////{
                    ////    fileName = fileName.Substring(1, fileName.Length - 1);
                    ////}

                    //char[] chars = new char[] { '_' };
                    //fileName = fileName.TrimStart(chars);


                    string newProjectDir = Path.Combine(parent, fileName); // + Daisy3_Import.XUK_DIR

                    if (newProjectDir != projectDir)
                    {
                        bool okay = true;

                        if (Directory.Exists(newProjectDir))
                        {
                            if (askUserConfirmOverwriteFileFolder(newProjectDir, true, null))
                            {
                                try
                                {
                                    FileDataProvider.TryDeleteDirectory(newProjectDir, false);
                                }
                                catch (Exception ex)
                                {
#if DEBUG
                                    Debugger.Break();
#endif // DEBUG
                                    Console.WriteLine(ex.Message);
                                    Console.WriteLine(ex.StackTrace);

                                    okay = false;
                                }
                            }
                            else
                            {
                                okay = false;
                            }
                        }

                        if (okay)
                        {
                            Directory.Move(projectDir, newProjectDir);
                            xukPath = Path.Combine(newProjectDir, Path.GetFileName(xukPath));
                        }
                    }
                }

                DocumentFilePath = null;
                DocumentProject = null;
                try
                {
                    OpenFile(xukPath);
                }
                catch (Exception ex)
                {
                    ExceptionHandler.Handle(ex, false, m_ShellView);
                    return false;
                }
            }

            return !cancelled;
        }
    }
}
