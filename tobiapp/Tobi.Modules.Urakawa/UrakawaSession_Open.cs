using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Win32;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa;
using urakawa.events.progress;
using urakawa.xuk;

namespace Tobi.Modules.Urakawa
{
    public partial class UrakawaSession
    {
        private void initCommands_Open()
        {
            var shellPresenter = Container.Resolve<IShellPresenter>();
            //
            NewCommand = new RichDelegateCommand<object>(
                UserInterfaceStrings.New,
                UserInterfaceStrings.New_,
                UserInterfaceStrings.New_KEYS,
                shellPresenter.LoadTangoIcon("document-new"),
                obj =>
                {
                    string currentAssemblyDirectoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    openFile(currentAssemblyDirectoryName + @"\empty-dtbook-z3986-2005.xml");
                },
                obj => true);

            shellPresenter.RegisterRichCommand(NewCommand);
            //
            OpenCommand = new RichDelegateCommand<object>(
                UserInterfaceStrings.Open,
                UserInterfaceStrings.Open_,
                UserInterfaceStrings.Open_KEYS,
                shellPresenter.LoadTangoIcon("document-open"),
                obj =>
                {
                    var dlg = new OpenFileDialog
                    {
                        FileName = "dtbook",
                        DefaultExt = ".xml",
                        Filter = "DTBook, OPF, EPUB or XUK (.xml, *.opf, *.xuk, *.epub)|*.xml;*.opf;*.xuk;*.epub"
                    };

                    var shellPresenter_ = Container.Resolve<IShellPresenter>();

                    bool? result = false;

                    shellPresenter_.DimBackgroundWhile(() => { result = dlg.ShowDialog(); });

                    if (result == false)
                    {
                        return;
                    }
                    
                    openFile(dlg.FileName);
                },
                obj => true);

            shellPresenter.RegisterRichCommand(OpenCommand);
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

        private void action_finished(object sender, FinishedEventArgs e)
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
            if (Path.GetExtension(DocumentFilePath) == ".xuk")
            {
                Logger.Log(String.Format("UrakawaSession.openFile(XUK) [{0}]", DocumentFilePath), Category.Debug, Priority.Medium);

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

                var shellPresenter = Container.Resolve<IShellPresenter>();

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
                    Focusable = false
                };
                var panel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                panel.Children.Add(label);
                panel.Children.Add(progressBar);

                var details = new TextBoxEx(action.LongDescription)
                {
                };

                var windowPopup = new PopupModalWindow(shellPresenter,
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
                    var dummy = (string)args.Argument;

                    if (m_OpenXukActionWorker.CancellationPending)
                    {
                        args.Cancel = true;
                        return;
                    }

                    action.Execute();

                    args.Result = "dummy result";
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


                    var result = (string)args.Result;

                    m_OpenXukActionWorker = null;
                };

                m_OpenXukActionWorker.RunWorkerAsync("dummy arg");
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
                var converter = new XukImport.DaisyToXuk(DocumentFilePath);

                DocumentFilePath = converter.XukPath;
                DocumentProject = converter.Project;
            }

            if (DocumentProject != null)
            {
                Logger.Log("-- PublishEvent [ProjectLoadedEvent] UrakawaSession.OpenFile", Category.Debug,
                           Priority.Medium);

                EventAggregator.GetEvent<ProjectLoadedEvent>().Publish(DocumentProject);

                return true;
            }
            return false;
        }
    }
}
