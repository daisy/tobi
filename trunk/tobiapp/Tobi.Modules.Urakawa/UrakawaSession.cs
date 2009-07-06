using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Media;
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

            initCommands();
        }

        private void initCommands()
        {
            var shellPresenter = Container.Resolve<IShellPresenter>();

            SaveAsCommand = new RichDelegateCommand<object>(UserInterfaceStrings.SaveAs,
                UserInterfaceStrings.SaveAs_,
                UserInterfaceStrings.SaveAs_KEYS,
                (VisualBrush)Application.Current.FindResource("document-save"),
                //RichDelegateCommand<object>.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Save_As")),
                obj => { throw new NotImplementedException("Functionality not implemented, sorry :("); }, obj => true);

            shellPresenter.RegisterRichCommand(SaveAsCommand);
            //
            SaveCommand = new RichDelegateCommand<object>(
                UserInterfaceStrings.Save,
                UserInterfaceStrings.Save_,
                UserInterfaceStrings.Save_KEYS,
                (VisualBrush)Application.Current.FindResource("media-floppy"),
                //RichDelegateCommand<object>.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Save")),
                obj =>
                {
                    throw new NotImplementedException("Functionality not implemented, sorry :(",
                        new NotImplementedException("Just trying nested expections",
                        new NotImplementedException("The last inner exception ! :)")));
                }, obj => true);

            shellPresenter.RegisterRichCommand(SaveCommand);
            //
            NewCommand = new RichDelegateCommand<object>(UserInterfaceStrings.New,
                UserInterfaceStrings.New_,
                UserInterfaceStrings.New_KEYS,
                (VisualBrush)Application.Current.FindResource("document-new"),
                obj => openDefaultTemplate(), obj => true);

            shellPresenter.RegisterRichCommand(NewCommand);
            //
            OpenCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Open,
                UserInterfaceStrings.Open_,
                UserInterfaceStrings.Open_KEYS,
                (VisualBrush)Application.Current.FindResource("document-open"),
                obj => openFile(), obj => true);

            shellPresenter.RegisterRichCommand(OpenCommand);
            //
            CloseCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Close,
                UserInterfaceStrings.Close_,
                UserInterfaceStrings.Close_KEYS,
                (VisualBrush)Application.Current.FindResource("go-jump"),
                obj => closeProject(), obj => IsProjectLoaded);

            shellPresenter.RegisterRichCommand(CloseCommand);
        }

        public bool IsDirty
        {
            get
            {
                return false;
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
            Logger.Log("-- PublishEvent [ProjectUnLoadedEvent] UrakawaSession.closeProject", Category.Debug, Priority.Medium);

            EventAggregator.GetEvent<ProjectUnLoadedEvent>().Publish(DocumentProject);

            DocumentFilePath = null;
            DocumentProject = null;
        }

        private void openDefaultTemplate()
        {
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
        private int m_CurrentPercentage;

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

            if (percent != m_CurrentPercentage)
            {
                m_CurrentPercentage = percent;
                m_OpenXukActionWorker.ReportProgress(m_CurrentPercentage);
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
                m_CurrentPercentage = 0;

                DocumentProject = new Project();

                var uri = new Uri(DocumentFilePath, UriKind.Absolute);
                //DocumentProject.OpenXuk(uri);

                var action = new OpenXukAction(DocumentProject, uri)
                {
                    ShortDescription = "Opening XUK file...",
                    LongDescription = "Parsing the XML content of a XUK file and building the in-memory document object model..."
                };

                action.Progress += action_progress;
                action.Finished += action_finished;
                action.Cancelled += action_cancelled;

                var shellPresenter = Container.Resolve<IShellPresenter>();
                var window = shellPresenter.View as Window;

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

                var windowPopup = new PopupModalWindow(window ?? Application.Current.MainWindow,
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

                    windowPopup.ForceClose();

                    var result = (string)args.Result;

                    m_OpenXukActionWorker = null;
                };

                SystemSounds.Asterisk.Play();

                m_OpenXukActionWorker.RunWorkerAsync("dummy arg");
                windowPopup.Show();

                if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Cancel)
                {
                    m_OpenXukActionCancelFlag = true;
                    return;
                }
            }
            else
            {
                var converter = new XukImport.DaisyToXuk(DocumentFilePath);
                DocumentProject = converter.Project;
            }

            Logger.Log("-- PublishEvent [ProjectLoadedEvent] UrakawaSession.OpenFile", Category.Debug, Priority.Medium);

            EventAggregator.GetEvent<ProjectLoadedEvent>().Publish(DocumentProject);
        }
    }
}
