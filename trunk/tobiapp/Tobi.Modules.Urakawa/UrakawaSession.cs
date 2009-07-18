using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;
using Microsoft.Win32;
using Tobi.Infrastructure;
using Tobi.Infrastructure.Commanding;
using Tobi.Infrastructure.UI;
using urakawa;
using urakawa.events.progress;
using urakawa.xuk;

namespace Tobi.Modules.Urakawa
{
    ///<summary>
    ///</summary>
    public class UrakawaSession : IUrakawaSession
    {
        protected ILoggerFacade Logger { get; private set; }
        protected IRegionManager RegionManager { get; private set; }

        protected IUnityContainer Container { get; private set; }
        protected IEventAggregator EventAggregator { get; private set; }

        public RichDelegateCommand<object> SaveAsCommand { get; private set; }
        public RichDelegateCommand<object> SaveCommand { get; private set; }

        public RichDelegateCommand<object> NewCommand { get; private set; }
        public RichDelegateCommand<object> OpenCommand { get; private set; }
        public RichDelegateCommand<object> CloseCommand { get; private set; }

        public RichDelegateCommand<object> UndoCommand { get; private set; }
        public RichDelegateCommand<object> RedoCommand { get; private set; }


        public Project DocumentProject
        {
            get;
            set;
        }

        public string DocumentFilePath
        {
            get;
            set;
        }

        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        public UrakawaSession(IUnityContainer container,
                            ILoggerFacade logger,
                            IRegionManager regionManager,
                            IEventAggregator eventAggregator)
        {
            Logger = logger;
            Container = container;
            RegionManager = regionManager;
            EventAggregator = eventAggregator;

            IsDirty = false;

            initCommands();
        }

        private void initCommands()
        {
            var shellPresenter = Container.Resolve<IShellPresenter>();

            //
            UndoCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Undo,
                UserInterfaceStrings.Undo_,
                UserInterfaceStrings.Undo_KEYS,
                shellPresenter.LoadTangoIcon("edit-undo"),
                obj => DocumentProject.Presentations.Get(0).UndoRedoManager.Undo(),
                obj => DocumentProject != null && DocumentProject.Presentations.Get(0).UndoRedoManager.CanUndo);

            shellPresenter.RegisterRichCommand(UndoCommand);
            //
            RedoCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Redo,
                UserInterfaceStrings.Redo_,
                UserInterfaceStrings.Redo_KEYS,
                shellPresenter.LoadTangoIcon("edit-redo"),
                obj => DocumentProject.Presentations.Get(0).UndoRedoManager.Redo(),
                obj => DocumentProject != null && DocumentProject.Presentations.Get(0).UndoRedoManager.CanRedo);

            shellPresenter.RegisterRichCommand(RedoCommand);
            //
            //
            SaveAsCommand = new RichDelegateCommand<object>(UserInterfaceStrings.SaveAs,
                UserInterfaceStrings.SaveAs_,
                UserInterfaceStrings.SaveAs_KEYS,
                shellPresenter.LoadTangoIcon("document-save"),
                //RichDelegateCommand<object>.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Save_As")),
                obj => saveAs(),
                obj => IsProjectLoaded);

            shellPresenter.RegisterRichCommand(SaveAsCommand);
            //
            SaveCommand = new RichDelegateCommand<object>(
                UserInterfaceStrings.Save,
                UserInterfaceStrings.Save_,
                UserInterfaceStrings.Save_KEYS,
                shellPresenter.LoadTangoIcon("media-floppy"),
                //RichDelegateCommand<object>.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Save")),
                obj => save()
                , obj => IsProjectLoadedAndDirty || IsProjectLoadedAndNotDirty); //todo: just for testing save even when not dirty

            shellPresenter.RegisterRichCommand(SaveCommand);
            //
            NewCommand = new RichDelegateCommand<object>(UserInterfaceStrings.New,
                UserInterfaceStrings.New_,
                UserInterfaceStrings.New_KEYS,
                shellPresenter.LoadTangoIcon("document-new"),
                obj => openDefaultTemplate(),
                obj => !IsProjectLoaded);

            shellPresenter.RegisterRichCommand(NewCommand);
            //
            OpenCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Open,
                UserInterfaceStrings.Open_,
                UserInterfaceStrings.Open_KEYS,
                shellPresenter.LoadTangoIcon("document-open"),
                obj => openFile(), obj => !IsProjectLoaded);

            shellPresenter.RegisterRichCommand(OpenCommand);
            //
            CloseCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Close,
                UserInterfaceStrings.Close_,
                UserInterfaceStrings.Close_KEYS,
                shellPresenter.LoadTangoIcon("go-jump"),
                obj => closeProject(), obj => IsProjectLoaded);

            shellPresenter.RegisterRichCommand(CloseCommand);
        }

        public bool IsDirty
        {
            get; set;
        }

        private bool IsProjectLoadedAndDirty
        {
            get
            {
                return IsProjectLoaded && IsDirty;
            }
        }

        private bool IsProjectLoadedAndNotDirty
        {
            get
            {
                return IsProjectLoaded && !IsDirty;
            }
        }

        private bool IsProjectLoaded
        {
            get
            {
                return DocumentProject != null;
            }
        }

        private void closeProject()
        {
            if (DocumentProject == null)
            {
                return;
            }

            Logger.Log("-- PublishEvent [ProjectUnLoadedEvent] UrakawaSession.closeProject", Category.Debug, Priority.Medium);

            //todo check IsDirty and ask for confirmation. See ShellPresenter.askUserConfirmExit()

            var shellPresenter = Container.Resolve<IShellPresenter>();

            EventAggregator.GetEvent<ProjectUnLoadedEvent>().Publish(DocumentProject);

            DocumentFilePath = null;
            DocumentProject = null;
        }


        private BackgroundWorker m_SaveXukActionWorker;
        private bool m_SaveXukActionCancelFlag;
        private int m_SaveXukActionCurrentPercentage;

        private void OnSaveXukAction_cancelled(object sender, CancelledEventArgs e)
        {
            IsDirty = true;

            m_SaveXukActionWorker.CancelAsync();
        }

        private void OnSaveXukAction_finished(object sender, FinishedEventArgs e)
        {
            //DoClose();
        }

        private void OnSaveXukAction_progress(object sender, ProgressEventArgs e)
        {
            double val = e.Current;
            double max = e.Total;
            var percent = (int)((val / max) * 100);

            if (percent != m_SaveXukActionCurrentPercentage)
            {
                m_SaveXukActionCurrentPercentage = percent;
                m_SaveXukActionWorker.ReportProgress(m_SaveXukActionCurrentPercentage);
            }

            if (m_SaveXukActionCancelFlag)
            {
                e.Cancel();
            }
        }

        private void save()
        {
            if (DocumentProject == null)
            {
                return;
            }

            Logger.Log(String.Format("UrakawaSession.save() [{0}]", DocumentFilePath), Category.Debug, Priority.Medium);

            m_SaveXukActionCancelFlag = false;
            m_SaveXukActionCurrentPercentage = 0;

            var uri = new Uri(DocumentFilePath, UriKind.Absolute);
            //DocumentProject.OpenXuk(uri);

            var action = new SaveXukAction(DocumentProject, DocumentProject, uri)
            {
                ShortDescription = "Saving XUK file...",
                LongDescription = "Serializing the document object model from the Urakawa SDK as XML content into a XUK file..."
            };

            action.Progress += OnSaveXukAction_progress;
            action.Finished += OnSaveXukAction_finished;
            action.Cancelled += OnSaveXukAction_cancelled;

            var shellPresenter = Container.Resolve<IShellPresenter>();
            var window = shellPresenter.View as Window;

            var progressBar = new ProgressBar
            {
                IsIndeterminate = true,
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

            var details = new TextBox
            {
                Text = action.LongDescription,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                TextWrapping = TextWrapping.Wrap,
                IsReadOnly = true,
                Background = SystemColors.ControlLightLightBrush,
                BorderBrush = SystemColors.ControlDarkDarkBrush,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6),
                SnapsToDevicePixels = true
            };

            var windowPopup = new PopupModalWindow(shellPresenter,
                                                   UserInterfaceStrings.EscapeMnemonic(
                                                       UserInterfaceStrings.RunningTask),
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.Cancel,
                                                   PopupModalWindow.DialogButton.Cancel,
                                                   false, 500, 150, details, 80);

            m_SaveXukActionWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = true
            };

            m_SaveXukActionWorker.DoWork += delegate(object s, DoWorkEventArgs args)
            {
                var dummy = (string)args.Argument;

                if (m_SaveXukActionWorker.CancellationPending)
                {
                    args.Cancel = true;
                    return;
                }

                action.Execute();

                args.Result = "dummy result";
            };

            m_SaveXukActionWorker.ProgressChanged += delegate(object s, ProgressChangedEventArgs args)
            {
                progressBar.Value = args.ProgressPercentage;
            };

            m_SaveXukActionWorker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
            {
                if (args.Cancelled)
                {
                    IsDirty = true;
                }

                windowPopup.ForceClose();

                var result = (string)args.Result;

                m_SaveXukActionWorker = null;
            };

            m_SaveXukActionWorker.RunWorkerAsync("dummy arg");
            windowPopup.ShowModal();

            if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Cancel)
            {
                m_SaveXukActionCancelFlag = true;
            }
        }

        private void saveAs()
        {
            throw new NotImplementedException("Functionality not implemented, sorry :(",
                    new NotImplementedException("Just trying nested expections",
                    new NotImplementedException("The last inner exception ! :)")));
        }

        private void openDefaultTemplate()
        {
            //closeProject();

            string currentAssemblyDirectoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            openFile(currentAssemblyDirectoryName + @"\empty-dtbook-z3986-2005.xml");
        }

        private void openFile()
        {
            var dlg = new OpenFileDialog
                          {
                              FileName = "dtbook",
                              DefaultExt = ".xml",
                              Filter = "DTBook, OPF, EPUB or XUK (.xml, *.opf, *.xuk, *.epub)|*.xml;*.opf;*.xuk;*.epub"
                          };
            bool? result = dlg.ShowDialog();
            if (result == false)
            {
                return;
            }
            openFile(dlg.FileName);
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
            var percent = (int) ((val/max)*100);

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

        private void openFile(string filename)
        {
            closeProject();

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

                var details = new TextBox
                {
                    Text = action.LongDescription,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    TextWrapping = TextWrapping.Wrap,
                    IsReadOnly = true,
                    Background = SystemColors.ControlLightLightBrush,
                    BorderBrush = SystemColors.ControlDarkDarkBrush,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(6),
                    SnapsToDevicePixels = true
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
                    }
                    else
                    {
                        DocumentProject = project;
                    }

                    windowPopup.ForceClose();

                    var result = (string)args.Result;

                    m_OpenXukActionWorker = null;
                };

                m_OpenXukActionWorker.RunWorkerAsync("dummy arg");
                windowPopup.ShowModal();

                if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Cancel)
                {
                    m_OpenXukActionCancelFlag = true;
                    return;
                }
            }
            else
            {
                var converter = new XukImport.DaisyToXuk(DocumentFilePath);

                DocumentFilePath = converter.XukPath;
                DocumentProject = converter.Project;
            }

            if (DocumentProject != null)
            {
                Logger.Log("-- PublishEvent [ProjectLoadedEvent] UrakawaSession.OpenFile", Category.Debug,
                           Priority.Medium);

                EventAggregator.GetEvent<ProjectLoadedEvent>().Publish(DocumentProject);
            }
        }
    }
}
