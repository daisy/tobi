﻿using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Win32;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa;
using urakawa.exception;
using urakawa.xuk;

namespace Tobi.Plugin.Urakawa
{
    public partial class UrakawaSession
    {
        public RichDelegateCommand OpenCommand { get; private set; }

        private void initCommands_Open()
        {
            //NewCommand = new RichDelegateCommand(
            //    UserInterfaceStrings.New,
            //    UserInterfaceStrings.New_,
            //    UserInterfaceStrings.New_KEYS,
            //    shellView.LoadTangoIcon("document-new"),
            //    ()=>
            //    {
            //        string currentAssemblyDirectoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //        openFile(currentAssemblyDirectoryName + @"\empty-dtbook-z3986-2005.xml");
            //    },
            //    ()=> true);
            //shellView.RegisterRichCommand(NewCommand);
            //
            OpenCommand = new RichDelegateCommand(
                Tobi_Plugin_Urakawa_Lang.Open,
                Tobi_Plugin_Urakawa_Lang.Open_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon(@"document-open"),
                () =>
                {
                    var dlg = new OpenFileDialog
                    {
                        FileName = @"",
                        DefaultExt = @".xml",
#if DEBUG
                        Filter = @"DTBook, OPF, XUK or EPUB (*.xml, *.opf, *.xuk, *.epub)|*.xml;*.opf;*.xuk;*.epub",
#else
                        Filter = @"DTBook, OPF or XUK (*.xml, *.opf, *.xuk)|*.xml;*.opf;*.xuk",
#endif //DEBUG
                        CheckFileExists = false,
                        CheckPathExists = false,
                        AddExtension = true,
                        DereferenceLinks = true,
                        Title = @"Tobi: " + UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Urakawa_Lang.Open)
                    };

                    bool? result = false;

                    m_ShellView.DimBackgroundWhile(() => { result = dlg.ShowDialog(); });

                    if (result == false)
                    {
                        return;
                    }
                    try
                    {
                        OpenFile(dlg.FileName);
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandler.Handle(ex, false, m_ShellView);
                    }
                },
                () => true,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Open));

            m_ShellView.RegisterRichCommand(OpenCommand);
        }

        public bool OpenFile(string filename)
        {
            var fileUri = new Uri(filename, UriKind.Absolute);
            AddRecentFile(fileUri);

            //todo: should we implement HTTP open/import ?
            if (false &&
                (!fileUri.IsFile //fileUri.Scheme.ToLower() != "file"
                || !File.Exists(fileUri.LocalPath)))
            {
                var label = new TextBlock
                {
                    Text = Tobi_Plugin_Urakawa_Lang.CannotOpenLocalFile_,
                    Margin = new Thickness(8, 0, 8, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Focusable = true,
                    TextWrapping = TextWrapping.Wrap
                };

                var iconProvider = new ScalableGreyableImageProvider(m_ShellView.LoadTangoIcon("dialog-warning"), m_ShellView.MagnificationLevel);

                var panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Stretch,
                };
                panel.Children.Add(iconProvider.IconLarge);
                panel.Children.Add(label);

                var details = new TextBoxReadOnlyCaretVisible(fileUri.ToString());

                var windowPopup = new PopupModalWindow(m_ShellView,
                                                       UserInterfaceStrings.EscapeMnemonic(
                                                           Tobi_Plugin_Urakawa_Lang.CannotOpenLocalFile),
                                                       panel,
                                                       PopupModalWindow.DialogButtonsSet.Close,
                                                       PopupModalWindow.DialogButton.Close,
                                                       true, 300, 160, details, 40);

                windowPopup.ShowModal();

                return false;
            }

            if (!Close())
            {
                return false;
            }

            var backWorker = new BackgroundWorker
                {
                    WorkerSupportsCancellation = true,
                    WorkerReportsProgress = true
                };

            string ext = Path.GetExtension(fileUri.ToString()).ToLower();
            if (ext == @".xuk")
            {
                //todo: should we implement HTTP open ?
                if (!fileUri.IsFile)
                    throw new InvalidUriException("The URI to open must point to a local file! " + Environment.NewLine + fileUri.ToString());

                m_Logger.Log(String.Format(@"UrakawaSession.openFile(XUK) [{0}]", DocumentFilePath), Category.Debug, Priority.Medium);

                //fileUri.Scheme.ToLower() == "file"
                DocumentFilePath = fileUri.IsFile ? fileUri.LocalPath : filename;

                if (!File.Exists(DocumentFilePath))
                    throw new InvalidUriException("The import URI must point to an existing file! " + Environment.NewLine + fileUri.ToString());

                int currentPercentage = 0;
                bool cancelFlag = false;

                var project = new Project();

                //var uri = new Uri(DocumentFilePath, UriKind.Absolute);
                //DocumentProject.OpenXuk(uri);

                var action = new OpenXukAction(project, fileUri)
                {
                    ShortDescription = "Opening XUK file...",                                                                                            // TODO LOCALIZE OpenXukFile
                    LongDescription = "Parsing the XML content of a XUK file and building the in-memory document object model into the Urakawa SDK..."   // TODO LOCALIZE ParsingXMLDoc_BuildingDOM
                };

                action.Progress += (sender, e) =>
                {
                    double val = e.Current;
                    double max = e.Total;
                    var percent = (int)((val / max) * 100);

                    if (percent != currentPercentage)
                    {
                        currentPercentage = percent;
                        backWorker.ReportProgress(currentPercentage);
                    }

                    if (cancelFlag)
                    {
                        e.Cancel();
                    }
                };
                action.Finished += (sender, e) => { };
                action.Cancelled += (sender, e) =>
                    {
                        DocumentFilePath = null;
                        DocumentProject = null;

                        backWorker.CancelAsync();
                    };

                var progressBar = new ProgressBar
                {
                    IsIndeterminate = false,
                    Height = 18,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0
                };

                var label = new TextBlock
                {
                    Text = action.ShortDescription,
                    Margin = new Thickness(0, 0, 0, 8),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Focusable = true,
                };
                var panel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                panel.Children.Add(label);
                panel.Children.Add(progressBar);

                var details = new TextBoxReadOnlyCaretVisible(action.LongDescription);

                var windowPopup = new PopupModalWindow(m_ShellView,
                                                       UserInterfaceStrings.EscapeMnemonic(
                                                           Tobi_Plugin_Urakawa_Lang.RunningTask),
                                                       panel,
                                                       PopupModalWindow.DialogButtonsSet.Cancel,
                                                       PopupModalWindow.DialogButton.Cancel,
                                                       false, 500, 150, details, 80);


                Exception workException = null;
                backWorker.DoWork += delegate(object s, DoWorkEventArgs args)
                {
                    //var dummy = (string)args.Argument;

                    if (backWorker.CancellationPending)
                    {
                        args.Cancel = true;
                        return;
                    }

                    action.Execute();

                    args.Result = @"dummy result";
                };

                backWorker.ProgressChanged += delegate(object s, ProgressChangedEventArgs args)
                {
                    progressBar.Value = args.ProgressPercentage;
                };

                backWorker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
                {
                    workException = args.Error;

                    if (cancelFlag)
                    {
                        DocumentFilePath = null;
                        DocumentProject = null;
                    }
                    else if (args.Cancelled)
                    {
                        DocumentFilePath = null;
                        DocumentProject = null;
                        windowPopup.ForceClose(PopupModalWindow.DialogButton.Cancel);
                    }
                    else if (workException != null)
                    {
                        windowPopup.ForceClose(PopupModalWindow.DialogButton.ESC);
                    }
                    else
                    {
                        if (project.Presentations.Count == 0)
                        {
                            workException = new XukException("Project does not contain a Presentation !" + Environment.NewLine + fileUri.ToString());
                        }
                        else
                            DocumentProject = project;
                        windowPopup.ForceClose(PopupModalWindow.DialogButton.ESC);
                    }


                    //var result = (string)args.Result;

                    backWorker = null;
                };

                backWorker.RunWorkerAsync(@"dummy arg");
                windowPopup.ShowModal();

                if (workException != null)
                {
                    throw workException;
                }

                if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Cancel)
                {
                    cancelFlag = true;
                    return false;
                }
            }
            else
            {
                //todo: should we implement HTTP import ?
                if (!fileUri.IsFile)
                    throw new InvalidUriException("The import URI must point to a local file!" + Environment.NewLine + fileUri.ToString());

                //fileUri.Scheme.ToLower() == "file"
                DocumentFilePath = fileUri.IsFile ? fileUri.LocalPath : filename;

                if (!File.Exists(DocumentFilePath))
                    throw new InvalidUriException("The import URI must point to an existing file! " + Environment.NewLine + fileUri.ToString());

                if (!doImport())
                {
                    return false;
                }
            }

            if (DocumentProject != null)
            {
                m_Logger.Log(@"-- PublishEvent [ProjectLoadedEvent] UrakawaSession.OpenFile", Category.Debug,
                           Priority.Medium);

                m_EventAggregator.GetEvent<ProjectLoadedEvent>().Publish(DocumentProject);

                var treeNode = DocumentProject.Presentations.Get(0).RootNode.GetFirstDescendantWithText();
                if (treeNode != null)
                {
                    m_Logger.Log(@"-- PublishEvent [TreeNodeSelectedEvent] DocumentPaneView.OnFlowDocumentLoaded",
                               Category.Debug, Priority.Medium);

                    m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(treeNode);
                }

                return true;
            }
            return false;
        }
    }
}