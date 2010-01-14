﻿using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Win32;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa;
using urakawa.daisy.import;
using urakawa.events.progress;
using urakawa.xuk;

namespace Tobi.Plugin.Urakawa
{
    public partial class UrakawaSession
    {
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
                UserInterfaceStrings.Open,
                UserInterfaceStrings.Open_,
                UserInterfaceStrings.Open_KEYS,
                m_ShellView.LoadTangoIcon(@"document-open"),
                ()=>
                {
                    var dlg = new OpenFileDialog
                    {
                        FileName = @"dtbook",
                        DefaultExt = @".xml",
#if DEBUG
                        Filter = @"DTBook, OPF, XUK or EPUB (.xml, *.opf, *.xuk, *.epub)|*.xml;*.opf;*.xuk;*.epub",
#else
                        Filter = @"DTBook, OPF or XUK (.xml, *.opf, *.xuk)|*.xml;*.opf;*.xuk",
#endif //DEBUG
                        CheckFileExists = false,
                        CheckPathExists = false,
                        AddExtension = true,
                        DereferenceLinks = true,
                        Title = @"Tobi: " + UserInterfaceStrings.EscapeMnemonic(UserInterfaceStrings.Open)
                    };

                    bool? result = false;

                    m_ShellView.DimBackgroundWhile(() => { result = dlg.ShowDialog(); });

                    if (result == false)
                    {
                        return;
                    }
                    
                    openFile(dlg.FileName);
                },
                () => true);

            m_ShellView.RegisterRichCommand(OpenCommand);
        }


        private BackgroundWorker m_OpenXukActionWorker;
        private bool m_OpenXukActionCancelFlag;
        private int m_OpenXukActionCurrentPercentage;

        private void action_cancelled(object sender, CancelledEventArgs e)
        {
            DocumentFilePath = null;
            DocumentProject = null;

            m_OpenXukActionWorker.CancelAsync();
        }

// ReSharper disable MemberCanBeMadeStatic.Local
        private void action_finished(object sender, FinishedEventArgs e)
// ReSharper restore MemberCanBeMadeStatic.Local
        {
            //DoClose();
        }

        private void action_progress(object sender, ProgressEventArgs e)
        {
            double val = e.Current;
            double max = e.Total;
            var percent = (int)((val / max) * 100);

            if (percent != m_OpenXukActionCurrentPercentage)
            {
                m_OpenXukActionCurrentPercentage = percent;
                m_OpenXukActionWorker.ReportProgress(m_OpenXukActionCurrentPercentage);
            }

            if (m_OpenXukActionCancelFlag)
            {
                e.Cancel();
            }
        }

        private bool openFile(string filename)
        {
            if (!Close())
            {
                return false;
            }

            DocumentFilePath = filename;
            if (Path.GetExtension(DocumentFilePath) == @".xuk")
            {
                m_Logger.Log(String.Format(@"UrakawaSession.openFile(XUK) [{0}]", DocumentFilePath), Category.Debug, Priority.Medium);

                m_OpenXukActionCancelFlag = false;
                m_OpenXukActionCurrentPercentage = 0;

                var project = new Project();

                var uri = new Uri(DocumentFilePath, UriKind.Absolute);
                //DocumentProject.OpenXuk(uri);

                var action = new OpenXukAction(project, uri)
                {
                    ShortDescription = "Opening XUK file...",
                    LongDescription = "Parsing the XML content of a XUK file and building the in-memory document object model into the Urakawa SDK..."
                };

                action.Progress += action_progress;
                action.Finished += action_finished;
                action.Cancelled += action_cancelled;

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
                                                           UserInterfaceStrings.RunningTask),
                                                       panel,
                                                       PopupModalWindow.DialogButtonsSet.Cancel,
                                                       PopupModalWindow.DialogButton.Cancel,
                                                       false, 500, 150, details, 80);

                m_OpenXukActionWorker = new BackgroundWorker
                {
                    WorkerSupportsCancellation = true,
                    WorkerReportsProgress = true
                };

                m_OpenXukActionWorker.DoWork += delegate(object s, DoWorkEventArgs args)
                {
                    //var dummy = (string)args.Argument;

                    if (m_OpenXukActionWorker.CancellationPending)
                    {
                        args.Cancel = true;
                        return;
                    }

                    action.Execute();

                    args.Result = @"dummy result";
                };

                m_OpenXukActionWorker.ProgressChanged += delegate(object s, ProgressChangedEventArgs args)
                {
                    progressBar.Value = args.ProgressPercentage;
                };

                m_OpenXukActionWorker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
                {
                    if (args.Cancelled)
                    {
                        DocumentFilePath = null;
                        DocumentProject = null;
                        windowPopup.ForceClose(PopupModalWindow.DialogButton.Cancel);
                    }
                    else
                    {
                        DocumentProject = project;
                        windowPopup.ForceClose(PopupModalWindow.DialogButton.ESC);
                    }


                    //var result = (string)args.Result;

                    m_OpenXukActionWorker = null;
                };

                m_OpenXukActionWorker.RunWorkerAsync(@"dummy arg");
                windowPopup.ShowModal();

                if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Cancel)
                {
                    m_OpenXukActionCancelFlag = true;
                    return false;
                }
            }
            else
            {
                //TODO: progress bar !
                var converter = new Daisy3_Import(DocumentFilePath);

                DocumentFilePath = converter.XukPath;
                DocumentProject = converter.Project;
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
