using System;
using System.ComponentModel.Composition;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Practices.Composite.Logging;
using SystemColors = System.Windows.SystemColors;

namespace Tobi.Common.UI
{
    public static class ExceptionHandler
    {
        public static ILoggerFacade LOGGER = null;

        public static void LogException(Exception ex)
        {
            if (LOGGER == null) return;

            LOGGER.Log("[" + ex.GetType().FullName + "] ", Category.Exception, Priority.High);

            if (ex.Message != null)
            {
                LOGGER.Log(ex.Message, Category.Exception, Priority.High);
            }

            if (ex.StackTrace != null)
            {
                LOGGER.Log(ex.StackTrace, Category.Exception, Priority.High);
            }

            if (ex.InnerException != null)
            {
                LogException(ex.InnerException);
            }

            var compEx = ex as CompositionException;
            if (compEx != null)
            {
                foreach (var error in compEx.Errors)
                {
                    LOGGER.Log(error.Description, Category.Exception, Priority.High);

                    if (error.Exception != null)
                    {
                        LogException(error.Exception);
                    }
                }
            }
        }

        public static void Handle(Exception ex, bool doExit, IShellView shellView)
        {
            if (ex == null)
                return;
            //#if DEBUG
            //            Debugger.Break();
            //#endif

            LogException(ex);

            var margin = new Thickness(0, 0, 0, 8);

            var panel = new DockPanel { LastChildFill = true };
            //var panel = new StackPanel { Orientation = Orientation.Vertical };



            var labelMsg = new TextBoxReadOnlyCaretVisible(UserInterfaceStrings.UnhandledException)
            {
                FontWeight = FontWeights.ExtraBlack,
                Margin = margin,
                BorderBrush = Brushes.Red
            };


            //var binding = new Binding
            //                  {
            //                      Mode = BindingMode.OneWay,
            //                      Source = UserInterfaceStrings.UnhandledException,
            //                      Path = new PropertyPath(".")
            //                  };
            //var expr = labelMsg.SetBinding(TextBox.TextProperty, binding);

            //labelMsg.PreviewKeyDown += ((sender, e) =>
            //                              {
            //                                  if (!(e.Key == Key.Down || e.Key == Key.Up || e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Home || e.Key == Key.End || e.Key == Key.PageDown || e.Key == Key.PageUp || e.Key == Key.Tab))
            //                                      e.Handled = true;
            //                              });

            labelMsg.SetValue(DockPanel.DockProperty, Dock.Top);
            panel.Children.Add(labelMsg);


            //var exMessage = UserInterfaceStrings.UnhandledException;
            //exMessage += Environment.NewLine;
            //exMessage += @"===============";
            //exMessage += Environment.NewLine;
            var exMessage = "[" + ex.GetType().FullName + "] " + ex.Message;
            Exception exinnerd = ex;
            while (exinnerd.InnerException != null)
            {
                if (!String.IsNullOrEmpty(exinnerd.InnerException.Message))
                {
                    exMessage += Environment.NewLine;
                    exMessage += @"======";
                    exMessage += Environment.NewLine;
                    exMessage += "[" + exinnerd.GetType().FullName + "] ";
                    exMessage += exinnerd.InnerException.Message;
                }

                exinnerd = exinnerd.InnerException;
            }

            var compEx = ex as CompositionException;

            if (compEx != null)
            {
                foreach (var error in compEx.Errors)
                {
                    exMessage += Environment.NewLine;
                    exMessage += @"======";
                    exMessage += Environment.NewLine;
                    exMessage += error.Description;

                    if (error.Exception != null)
                    {
                        exMessage += Environment.NewLine;
                        exMessage += @"======";
                        exMessage += Environment.NewLine;
                        exMessage += "[" + error.Exception.GetType().FullName + "] ";
                        exMessage += error.Exception.Message;

                        exinnerd = error.Exception;
                        while (exinnerd.InnerException != null)
                        {
                            if (!String.IsNullOrEmpty(exinnerd.InnerException.Message))
                            {
                                exMessage += Environment.NewLine;
                                exMessage += @"======";
                                exMessage += Environment.NewLine;
                                exMessage += "[" + exinnerd.InnerException.GetType().FullName + "] ";
                                exMessage += exinnerd.InnerException.Message;
                            }

                            exinnerd = exinnerd.InnerException;
                        }
                    }
                }
            }

            var labelSummary = new TextBoxReadOnlyCaretVisible(exMessage)
            {
                FontWeight = FontWeights.ExtraBlack,
                //Margin = margin,

                BorderBrush = null,
                BorderThickness = new Thickness(0)
            };

            var scroll = new ScrollViewer
            {
                Content = labelSummary,
                //Margin = margin,

                BorderBrush = SystemColors.ControlDarkBrush,
                //BorderBrush = Brushes.Red,

                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            //scroll.SetValue(DockPanel.DockProperty, Dock.Top);

            panel.Children.Add(scroll);

            // DETAILS:

            var exStackTrace = ex.StackTrace;
            Exception exinner = ex;
            while (exinner.InnerException != null)
            {
                if (!String.IsNullOrEmpty(exinner.InnerException.StackTrace))
                {
                    exStackTrace += Environment.NewLine;
                    exStackTrace += "=========================";
                    exStackTrace += Environment.NewLine;
                    exStackTrace += "-------------------------";
                    exStackTrace += Environment.NewLine;
                    exStackTrace += exinner.InnerException.StackTrace;
                }

                exinner = exinner.InnerException;
            }

            if (compEx != null)
            {
                foreach (var error in compEx.Errors)
                {
                    if (error.Exception != null)
                    {
                        exStackTrace += Environment.NewLine;
                        exStackTrace += "=========================";
                        exStackTrace += Environment.NewLine;
                        exStackTrace += "-------------------------";
                        exStackTrace += Environment.NewLine;
                        exStackTrace += error.Exception.StackTrace;

                        exinner = error.Exception;
                        while (exinner.InnerException != null)
                        {
                            if (!String.IsNullOrEmpty(exinner.InnerException.StackTrace))
                            {
                                exStackTrace += Environment.NewLine;
                                exStackTrace += "=========================";
                                exStackTrace += Environment.NewLine;
                                exStackTrace += "-------------------------";
                                exStackTrace += Environment.NewLine;
                                exStackTrace += exinner.InnerException.StackTrace;
                            }

                            exinner = exinner.InnerException;
                        }
                    }
                }
            }

            var stackTrace = new TextBoxReadOnlyCaretVisible(exStackTrace)
            {
                BorderBrush = null,
                BorderThickness = new Thickness(0)
            };

            var details = new ScrollViewer
            {
                Content = stackTrace,
                Margin = margin,
                BorderBrush = SystemColors.ControlDarkDarkBrush,
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            //panel.Children.Add(scroll);

            /*
            var logStr = String.Format("CANNOT OPEN [{0}] !", logPath);

            var logFile = File.Open(logPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (logFile.CanRead)
            {
                logFile.Close();
                //logFile.Read(bytes, int, int)
                logStr = File.ReadAllText(logPath);
            }

            var log = new TextBlock
            {
                Text = logStr,
                Margin = new Thickness(15),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                TextWrapping = TextWrapping.Wrap,
            };

            var scroll2 = new ScrollViewer
            {
                Content = log,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            panel.Children.Add(scroll2);
             * */

            var windowPopup = new PopupModalWindow(shellView,
                                                   UserInterfaceStrings.EscapeMnemonic(
                                                       UserInterfaceStrings.Unexpected),
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.Ok,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   false, 500, 250,
                                                   (String.IsNullOrEmpty(exStackTrace) ? null : details), 130);

            SystemSounds.Exclamation.Play();

            try
            {
                windowPopup.ShowModal();
            }
            catch
            {
                //ignore.
                int debug = 1;
            }

            if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Ok
                && doExit)
            {
                Application.Current.Shutdown();
                //Environment.Exit(1);
            }
        }
    }
}
