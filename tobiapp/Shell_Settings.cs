using System;
using System.ComponentModel;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.UI;

namespace Tobi
{
    public partial class Shell
    {
        //m_PropertyChangeHandler.RaisePropertyChanged(() => WindowTitle);
        //public double SettingsWindowShellHeight
        //{
        //    get
        //    {
        //        return Settings.Default.WindowShellSize.Height;
        //    }
        //    set
        //    {
        //        Settings.Default.WindowShellSize = new Size(Settings.Default.WindowShellSize.Width, value);
        //    }
        //}

        private bool m_SettingsDone;
        private void trySettings()
        {
            if (!m_SettingsDone && m_SettingsAggregator != null)
            {
                m_SettingsDone = true;

                m_Logger.Log(@"settings applied to Shell Window", Category.Debug, Priority.Medium);








                bool clickOnce = ApplicationDeployment.IsNetworkDeployed;
                if (!clickOnce)
                {
                    string thisVersion = ApplicationConstants.GetVersion();

                    string url = "http://data.daisy.org/projects/tobi/install/net4/Tobi_NET4.application";
                    var webClient = new WebClient { UseDefaultCredentials = true };
                    StreamReader streamReader = null;
                    try
                    {
                        streamReader = new StreamReader(webClient.OpenRead(url), Encoding.UTF8);
                        string xmlStr = streamReader.ReadToEnd();
                        //m_Logger.Log(str, Category.Info, Priority.High);

                        if (!string.IsNullOrEmpty(xmlStr))
                        {
                            int i = xmlStr.IndexOf(" version=", 20);
                            if (i > 0)
                            {
                                int k = i + 10;
                                int j = xmlStr.IndexOf("\"", k);
                                if (j > 0 && j > i)
                                {
                                    string latestVersion = xmlStr.Substring(k, j - k);

                                    if (latestVersion != thisVersion
                                        && Settings.Default.UpdateRejected != latestVersion)
                                    {
                                        bool update = askUserAppUpdate(thisVersion, latestVersion);
                                        if (update)
                                        {
                                            ExecuteShellProcess("http://daisy.org/tobi");
                                        }
                                        else
                                        {
                                            Settings.Default.UpdateRejected = latestVersion;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        m_Logger.Log(@"Problem checking Tobi latest version? " + ex.Message, Category.Exception, Priority.High);
                        //ExceptionHandler.LogException(ex);
#if DEBUG
                        Debugger.Break();
#endif // DEBUG
                    }
                    finally
                    {
                        if (streamReader != null)
                            streamReader.Close();
                    }
                }
            }
        }

        private bool askUserAppUpdate(string thisVersion, string latestVersion)
        {
            m_Logger.Log("ShellView.askUserAppUpdate", Category.Debug, Priority.Medium);

            var label = new TextBlock
            {
                Text = Tobi_Lang.TobiUpdate_Message,
                Margin = new Thickness(8, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap
            };

            var label2 = new TextBlock
            {
                Text = "[" + thisVersion + " --> " + latestVersion + "]",
                Margin = new Thickness(8, 0, 8, 8),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap
            };

            var iconProvider = new ScalableGreyableImageProvider(LoadTangoIcon("help-browser"), MagnificationLevel);

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            panel.Children.Add(iconProvider.IconLarge);

            var panel2 = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
            };
            panel.Children.Add(panel2);

            panel2.Children.Add(label);
            panel2.Children.Add(label2);
            //panel.Margin = new Thickness(8, 8, 8, 0);


            //var details = new TextBoxReadOnlyCaretVisible
            //                  {
            //    TextReadOnly = Tobi_Lang.ExitConfirm
            //};

            var windowPopup = new PopupModalWindow(this,
                                                   UserInterfaceStrings.EscapeMnemonic(Tobi_Lang.TobiUpdate_Title),
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.YesNo,
                                                   PopupModalWindow.DialogButton.No,
                                                   true, 400, 200, null, 40, null);

            windowPopup.ShowModal();

            if (PopupModalWindow.IsButtonOkYesApply(windowPopup.ClickedDialogButton))
            {
                if (m_UrakawaSession != null &&
                    m_UrakawaSession.DocumentProject != null && m_UrakawaSession.IsDirty)
                {
                    PopupModalWindow.DialogButton button = m_UrakawaSession.CheckSaveDirtyAndClose(PopupModalWindow.DialogButtonsSet.YesNoCancel, "exit");
                    if (PopupModalWindow.IsButtonEscCancel(button))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }


        private void SettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!e.PropertyName.StartsWith(@"Keyboard_")) return;

            foreach (var command in m_listOfRegisteredRichCommands)
            {
                if (command.KeyGestureSettingName == e.PropertyName)
                {
                    command.RefreshKeyGestureSetting();
                }
            }
        }

        private void saveSettings()
        {
            // NOTE: not needed because of TwoWay Data Binding in the XAML
            //if (WindowState == WindowState.Maximized)
            //{
            //    // Use the RestoreBounds as the current values will be 0, 0 and the size of the screen
            //    Settings.Default.WindowShellHeight = RestoreBounds.Height;
            //    Settings.Default.WindowShellWidth = RestoreBounds.Width;
            //    Settings.Default.WindowShellFullScreen = true;
            //}
            //else
            //{
            //    Settings.Default.WindowShellHeight = Height;
            //    Settings.Default.WindowShellWidth = Width;
            //    Settings.Default.WindowShellFullScreen = false;
            //}

            if (m_SettingsAggregator != null)
            {
                m_SettingsAggregator.SaveAll();
            }
        }
    }
}
