﻿using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Practices.Composite;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.UI;
using Application = System.Windows.Application;

namespace Tobi
{
    /// <summary>
    /// 'Code behind' for the Shell window which host the entire application
    /// </summary>
    [Export(typeof(IShellView)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class Shell : IShellView, IPartImportsSatisfiedNotification
    {
        public bool IsUIAutomationDisabled
        {
            get { return Settings.Default.WindowDisableUIAutomation; }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            if (IsUIAutomationDisabled)
            {
#if DEBUG
                Debugger.Break();
#endif
                return new DisabledUIAutomationWindowAutomationPeer(this);
            }

            return base.OnCreateAutomationPeer();
        }

#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied()
#pragma warning restore 1591
        {
            //#if DEBUG
            //            Debugger.Break();
            //#endif

            // If the toolbar has been resolved, we can push our commands into it.
            tryToolbarCommands();

            // If the menubar has been resolved, we can push our commands into it.
            tryMenubarCommands();

            // If the Urakawa Session has been resolved, we can bind the window title.
            trySessionWindowTitle();

            // If the Settings has been resolved, we can update the window state.
            trySettings();
        }

#pragma warning disable 649 // non-initialized fields

        [Import(typeof(IToolBarsView), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = true, AllowDefault = true)]
        private IToolBarsView m_ToolBarsView;

        [Import(typeof(IMenuBarView), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = true, AllowDefault = true)]
        private IMenuBarView m_MenuBarView;

        [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = true, AllowDefault = true)]
        private IUrakawaSession m_UrakawaSession;

        [Import(typeof(ISettingsAggregator), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = true, AllowDefault = true)]
        private ISettingsAggregator m_SettingsAggregator;

#pragma warning restore 649

        private readonly ILoggerFacade m_Logger;
        private readonly IEventAggregator m_EventAggregator;

        public IActiveAware ActiveAware { get; private set; }

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity container, it's a built-in CAG service</param>
        [ImportingConstructor]
        public Shell(ILoggerFacade logger, IEventAggregator eventAggregator)
        {
            m_Logger = logger;
            m_EventAggregator = eventAggregator;

            m_Logger.Log(@"ShellView.ctor", Category.Debug, Priority.Medium);

            ExceptionHandler.LOGGER = m_Logger;

            m_Exiting = false;

            m_PropertyChangeHandler = new PropertyChangedNotifyBase();
            m_PropertyChangeHandler.InitializeDependentProperties(this);

            ActiveAware = new FocusActiveAwareAdapter(this);
            ActiveAware.IsActiveChanged += (sender, e) =>
                   {
                       if (!ActiveAware.IsActive)
                       {
                           Console.WriteLine("@@@@ shell lost focus");
                       }
                       else
                       {
                           Console.WriteLine("@@@@ shell gained focus");
                       }
                       CommandManager.InvalidateRequerySuggested();
                   };

            initCommands();

            m_InConstructor = true;

            InitializeComponent();

            m_InConstructor = false;

            Settings.Default.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == PropertyChangedNotifyBase.GetMemberName(() => Settings.Default.Window_UI_Zoom))
                {
                    if (MagnificationLevel != Settings.Default.Window_UI_Zoom)
                        MagnificationLevel = Settings.Default.Window_UI_Zoom;
                }
            };

            //IRegionManager regionManager = Container.Resolve<IRegionManager>();
            //string regionName = "AvalonDockRegion_1";
            //regionManager.Regions.Add(new AvalonDockRegion() { Name = regionName });
            //((AvalonDockRegion)regionManager.Regions[regionName]).Bind(DocumentContent2);
        }

        private bool m_SessionTitleDone;
        private void trySessionWindowTitle()
        {
            if (!m_SessionTitleDone && m_UrakawaSession != null)
            {
                m_UrakawaSession.BindPropertyChangedToAction(() => m_UrakawaSession.DocumentFilePath,
                    () => m_PropertyChangeHandler.RaisePropertyChanged(() => WindowTitle));

                m_UrakawaSession.BindPropertyChangedToAction(() => m_UrakawaSession.IsDirty,
                    () => m_PropertyChangeHandler.RaisePropertyChanged(() => WindowTitle));

                m_PropertyChangeHandler.RaisePropertyChanged(() => WindowTitle);

                m_SessionTitleDone = true;

                m_Logger.Log(@"Urakawa session commands pushed to toolbar", Category.Debug, Priority.Medium);
            }
        }

        private bool m_ToolBarCommandsDone;
        private void tryToolbarCommands()
        {
            if (!m_ToolBarCommandsDone && m_ToolBarsView != null)
            {
                //int uid3 = m_ToolBarsView.AddToolBarGroup(new[] { ExitCommand }, PreferredPosition.First);

                int uid2 = m_ToolBarsView.AddToolBarGroup(new[] { MagnifyUiResetCommand, MagnifyUiDecreaseCommand, MagnifyUiIncreaseCommand }, PreferredPosition.Any);
                //int uid1 = m_ToolBarsView.AddToolBarGroup(new[] { ManageShortcutsCommand }, PreferredPosition.Any);

                //int uid3 = m_ToolBarsView.AddToolBarGroup(new[] { CopyCommand, CutCommand, PasteCommand }, PreferredPosition.First);

                //#if DEBUG
                //                int uidX = m_ToolBarsView.AddToolBarGroup(new[] { ShowLogFilePathCommand }, PreferredPosition.Last);
                //#endif

#if ICONS
                int uidXX = m_ToolBarsView.AddToolBarGroup(new[] { DisplayPreviewIconsDebugCommand });
#endif
                m_ToolBarCommandsDone = true;

                m_Logger.Log(@"Shell commands pushed to toolbar", Category.Debug, Priority.Medium);
            }
        }

        private bool m_MenuBarCommandsDone;
        private void tryMenubarCommands()
        {
            if (!m_MenuBarCommandsDone && m_MenuBarView != null)
            {
                int uid1 = m_MenuBarView.AddMenuBarGroup(
                    RegionNames.MenuBar_File, PreferredPosition.Last, true,
                    null, PreferredPosition.Any, true,
                    new[] { ExitCommand });

                int uid2 = m_MenuBarView.AddMenuBarGroup(
                    RegionNames.MenuBar_View, PreferredPosition.First, true,
                    RegionNames.MenuBar_Magnification, PreferredPosition.First, true,
                    new[] { MagnifyUiResetCommand, MagnifyUiDecreaseCommand, MagnifyUiIncreaseCommand });
#if DEBUG
                int uid3 = m_MenuBarView.AddMenuBarGroup(
                    RegionNames.MenuBar_View, PreferredPosition.Last, true,
                    null, PreferredPosition.Last, true,
                    new[] { ManageShortcutsCommand });
#endif
                int uid4 = m_MenuBarView.AddMenuBarGroup(
                    RegionNames.MenuBar_Tools, PreferredPosition.Last, true,
                    RegionNames.MenuBar_System, PreferredPosition.First, false,
                    new[] { OpenTobiSettingsFolderCommand, OpenTobiFolderCommand, OpenTobiIsolatedStorageCommand });

#if DEBUG
                //int uidX = m_MenuBarView.AddMenuBarGroup(RegionNames.MenuBar_Tools, new[] { ShowLogFilePathCommand }, null, false);
#endif

#if ICONS
                int uidXX = m_MenuBarView.AddMenuBarGroup(RegionNames.MenuBar_Tools, new[] { DisplayPreviewIconsDebugCommand }, null, false);
#endif

                m_MenuBarCommandsDone = true;

                m_Logger.Log(@"Shell commands pushed to menubar", Category.Debug, Priority.Medium);
            }
        }


        //private void OnWindowGotFocus(object sender, RoutedEventArgs e)
        //{
        //    foreach (Window w in Application.Current.Windows)
        //    {
        //        if (w != this && !w.ShowInTaskbar)
        //        {
        //            w.Focus();
        //            w.BringIntoView();
        //        }
        //    }
        //}

        // To avoid the shutting-down loop in OnShellWindowClosing()
        private bool m_Exiting;
        private void exit()
        {
            m_Logger.Log("ShellView.exit", Category.Debug, Priority.Medium);

            //MessageBox.ShowModal("Good bye !", "Tobi says:");
            /*TaskDialog.ShowModal("Tobi is exiting.",
                "Just saying goodbye...",
                "The Tobi application is now closing.",
                TaskDialogIcon.Information);*/
            m_Exiting = true;
            Application.Current.Shutdown();
        }

        [Conditional("DEBUG")]
        public void CheckParseScanWalkUiTreeThing()
        {
            Stopwatch startRecursiveDepth = Stopwatch.StartNew();
            VisualLogicalTreeWalkHelper.GetElements(this, false, false, false);
            startRecursiveDepth.Stop();
            TimeSpan timeRecursiveDepth = startRecursiveDepth.Elapsed;

            Stopwatch startRecursiveLeaf = Stopwatch.StartNew();
            VisualLogicalTreeWalkHelper.GetElements(this, false, true, false);
            startRecursiveLeaf.Stop();
            TimeSpan timeRecursiveLeaf = startRecursiveLeaf.Elapsed;

            Stopwatch startNonRecursiveDepth = Stopwatch.StartNew();
            VisualLogicalTreeWalkHelper.GetElements(this, true, false, false);
            startNonRecursiveDepth.Stop();
            TimeSpan timeNonRecursiveDepth = startNonRecursiveDepth.Elapsed;

            Stopwatch startNonRecursiveLeaf = Stopwatch.StartNew();
            VisualLogicalTreeWalkHelper.GetElements(this, true, true, false);
            startNonRecursiveLeaf.Stop();
            TimeSpan timeNonRecursiveLeaf = startNonRecursiveLeaf.Elapsed;

#if DEBUG
            int nVisualLeafNoError = ValidationErrorTreeSearch.CheckTreeWalking(this, false, true, false);
            int nVisualDepthNoError = ValidationErrorTreeSearch.CheckTreeWalking(this, false, false, false);

            int nLogicalLeafNoError = ValidationErrorTreeSearch.CheckTreeWalking(this, false, true, true);
            int nLogicalDepthNoError = ValidationErrorTreeSearch.CheckTreeWalking(this, false, false, true);

            MessageBox.Show(String.Format(
                "VisualLeafNoError={0}"
                + Environment.NewLine
                + "VisualDepthNoError={1}"
                + Environment.NewLine
                + "LogicalLeafNoError={2}"
                + Environment.NewLine
                + "LogicalDepthNoError={3}"
                + Environment.NewLine + Environment.NewLine
                + "timeNonRecursiveDepth={4}"
                + Environment.NewLine
                + "timeNonRecursiveLeaf={5}"
                + Environment.NewLine
                + "timeRecursiveDepth={6}"
                + Environment.NewLine
                + "timeRecursiveLeaf={7}"
                + Environment.NewLine
                , nVisualLeafNoError, nVisualDepthNoError, nLogicalLeafNoError, nLogicalDepthNoError, timeNonRecursiveDepth, timeNonRecursiveLeaf, timeRecursiveDepth, timeRecursiveLeaf));
#endif
        }

        protected void OnWindowClosing(object sender, CancelEventArgs e)
        {
            resetDeviceHook();

            saveSettings();

            /*
            e.Cancel = true;
            // Workaround for not being able to hide a window during closing.
            // This behavior was needed in WPF to ensure consistent window visiblity state
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, (DispatcherOperationCallback)delegate(object o)
            {
                Hide();
                return null;
            }, null);
             */

            bool leaving = false;

            if (m_Exiting) leaving = true;

            if (ExitCommand.CanExecute())
            {
                if (askUserConfirmExit())
                {
                    exit();
                    leaving = true;
                }
            }

            if (!leaving) e.Cancel = true;
        }

        private bool askUserConfirmExit()
        {
            m_Logger.Log("ShellView.askUserConfirmExit", Category.Debug, Priority.Medium);

            var label = new TextBlock
            {
                Text = Tobi_Lang.ExitConfirm,
                Margin = new Thickness(8, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap
            };

            var iconProvider = new ScalableGreyableImageProvider(LoadTangoIcon("help-browser"), MagnificationLevel);

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            panel.Children.Add(iconProvider.IconLarge);
            panel.Children.Add(label);
            //panel.Margin = new Thickness(8, 8, 8, 0);


            var details = new TextBoxReadOnlyCaretVisible(Tobi_Lang.ExitConfirm)
            {
            };

            var windowPopup = new PopupModalWindow(this,
                                                   UserInterfaceStrings.EscapeMnemonic(
                                                       Tobi_Lang.Exit),
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.YesNo,
                                                   PopupModalWindow.DialogButton.No,
                                                   true, 300, 160, null, 40);

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

        public void ExecuteShellProcess(string shellCmd)
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = shellCmd,
                    RedirectStandardError = false, // We can't redirect messages when shell-execute
                    RedirectStandardOutput = false, // We can't redirect messages when shell-execute
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal,
                    Arguments = ""
                }
            };
            process.Start();

            //process.WaitForExit(); We don't have a process to wait for when shell-execute
            //if (process.ExitCode != 0)
            //{
            //    StreamReader stdErr = process.StandardError;
            //    string toLog = stdErr.ReadToEnd();
            //    if (!string.IsNullOrEmpty(toLog))
            //    {
            //        m_Logger.Log(toLog, Category.Debug, Priority.Medium);
            //        ExceptionHandler.Handle(new InvalidOperationException(toLog), false, this);
            //    }
            //}
            //else
            //{
            //    StreamReader stdOut = process.StandardOutput;
            //    string toLog = stdOut.ReadToEnd();
            //    if (!string.IsNullOrEmpty(toLog))
            //    {
            //        m_Logger.Log(toLog, Category.Debug, Priority.Medium);
            //    }
            //}
        }

        public void DimBackgroundWhile(Action action)
        {
            var aLayer = AdornerLayer.GetAdornerLayer((Visual)Content);

            var theAdorner = new DimAdorner((UIElement)Content);
            if (aLayer != null)
            {
                aLayer.Add(theAdorner);
            }

            Effect = new BlurEffect();

            action.Invoke();

            Effect = null;

            if (aLayer != null)
            {
                aLayer.Remove(theAdorner);
            }
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            //SystemEvents.DisplaySettingsChanged += OnSystemEventsDisplaySettingsChanged;

            try
            {
                var iconUri = new Uri("pack://application:,,,/" + GetType().Assembly.GetName().Name
                                        + ";component/Tobi.ico", UriKind.Absolute);
                //Uri iconUri = new Uri("Tobi.ico", UriKind.RelativeOrAbsolute);
                Icon = BitmapFrame.Create(iconUri);
            }
            catch
            {
                //ignore
            }

            var app = Application.Current as App;
            if (app != null)
            {
                try
                {
                    app.SplashScreen.Close(TimeSpan.FromSeconds(0.5));
                }
                catch (Exception splashEx)
                {
                    Console.WriteLine(@"SplashScreen.Close() Exception: " + splashEx.Message);
                }
                //app.Dispatcher.BeginInvoke((Action)(() => app.SplashScreen.Close(TimeSpan.Zero)), DispatcherPriority.Loaded);
            }


            //Activate();

            /*
            IconBitmapDecoder ibd = new IconBitmapDecoder(new Uri(
                            @"pack://application:,,/Resources/Tobi.ico",
                            UriKind.RelativeOrAbsolute),
                            BitmapCreateOptions.None, BitmapCacheOption.Default);
            Icon = ibd.Frames[0];
            */
        }

        //public static PropertyPath GetPropertyPath<T>(System.Linq.Expressions.Expression<Func<T>> expression)
        //{
        //    return new PropertyPath(Reflect.GetProperty(expression).Name);
        //}
        //public static PropertyPath WindowTitlePath = GetPropertyPath(() => Shell.WindowTitlePath);
        public String WindowTitle
        {
            get
            {
                if (m_UrakawaSession == null)
                {
                    return String.Format(String.Format(Tobi_Lang.WindowsTitleKey_UrakawaSessionIsNull, Tobi_Lang.PleaseWait), ApplicationConstants.APP_VERSION);    // TODO LOCALIZE WindowsTitleKey_UrakawaSessionIsNull, PleaseWait
                }
                return String.Format(Tobi_Lang.WindowsTitleKey_UrakawaSessionIsNotNull,
                    ApplicationConstants.APP_VERSION,
                    (m_UrakawaSession.IsDirty ? @"* " : @""),
                    (m_UrakawaSession.DocumentProject == null ? Tobi_Lang.NoDocument : m_UrakawaSession.DocumentFilePath)
                    );    // TODO LOCALIZE WindowsTitleKey_UrakawaSessionIsNotNull, NoDocument
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        public void DispatchPropertyChangedEvent(PropertyChangedEventArgs e)
        {
            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        private PropertyChangedNotifyBase m_PropertyChangeHandler;


        //private void OnSystemEventsDisplaySettingsChanged(object sender, EventArgs e)
        //{
        //    // update DPI-dependent stuff
        //}


        // Could be used after a ClickOnce update:
        // private void OnUpdatingCompleted(object sender, AsyncCompletedEventArgs e)
        //private void restart()
        //{
        //    string batchFile = Path.ChangeExtension(Path.GetTempFileName(), "bat");

        //    using (StreamWriter writer = new StreamWriter(File.Create(batchFile)))
        //    {
        //        string location = Assembly.GetExecutingAssembly().Location;
        //        writer.WriteLine("start " + location);
        //    }

        //    Application.Current.Exit += (o, a) =>
        //    {
        //        using (Process p = new Process())
        //        {
        //            p.StartInfo.FileName = batchFile;
        //            p.Start();
        //        }
        //    };

        //    exit();
        //}


        static Shell()
        {
            EventManager.RegisterClassHandler(typeof(TextBox),
                UIElement.GotFocusEvent,
                new RoutedEventHandler(TextBox_GotFocus));

            //EventManager.RegisterClassHandler(typeof(UIElement),
            //    UIElement.GotKeyboardFocusEvent,
            //    new RoutedEventHandler(UIElement_GotKeyboardFocus));

            //EventManager.RegisterClassHandler(typeof(UIElement),
            //    UIElement.LostKeyboardFocusEvent,
            //    new RoutedEventHandler(UIElement_LostKeyboardFocus));
        }

        private static void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (((TextBox)sender).SelectionLength == 0)
                ((TextBox)sender).SelectAll();
        }


        //private static void UIElement_LostKeyboardFocus(object sender, RoutedEventArgs e)
        //{
        //    var ui = ((UIElement)sender);

        //    var oldALayer = AdornerLayer.GetAdornerLayer(ui);
        //    if (oldALayer == null)
        //    {
        //        return;
        //    }
        //    Adorner[] adorners = oldALayer.GetAdorners(ui);
        //    if (adorners == null)
        //    {
        //        return;
        //    }
        //    foreach (Adorner adorner in adorners)
        //    {
        //        if (adorner is FocusOutlineAdorner)
        //        {
        //            oldALayer.Remove(adorner);
        //        }
        //    }
        //}

        //private static void UIElement_GotKeyboardFocus(object sender, RoutedEventArgs e)
        //{
        //    var ui = ((UIElement)sender);

        //    var aLayer = AdornerLayer.GetAdornerLayer(ui);
        //    if (aLayer == null)
        //    {
        //        return;
        //    }
        //    var theAdorner = new FocusOutlineAdorner(ui);
        //    aLayer.Add(theAdorner);
        //    theAdorner.InvalidateVisual();
        //}

        private void OnWindowKeyUp(object sender, KeyEventArgs e)
        {
            var key = (e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));

            if (key == Key.Escape)
            {
                m_EventAggregator.GetEvent<EscapeEvent>().Publish(null);
            }
        }

        // Must be called on the UI thread
        private bool RunModalCancellableProgressTask_(string title, IDualCancellableProgressReporter reporter,
                                                     Action actionCancelled, Action actionCompleted)
        {
            var progressBar = new ProgressBar
            {
                IsIndeterminate = false,
                Height = 18,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };

            var progressBar2 = new ProgressBar
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
                Text = title,
                Margin = new Thickness(0, 0, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Focusable = true,
            };
            var label2 = new TextBlock
            {
                Text = "...",
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
            //panel.Children.Add(new TextBlock(new Run(" ")));
            //panel.Children.Add(label2);
            //panel.Children.Add(progressBar2);

            //label2.Visibility = Visibility.Collapsed;
            //progressBar2.Visibility = Visibility.Collapsed;

            //var details = new TextBoxReadOnlyCaretVisible("Converting data and building the in-memory document object model into the Urakawa SDK...");
            var details = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
            };
            details.Children.Add(label2);
            details.Children.Add(progressBar2);
            details.Visibility = Visibility.Collapsed;

            // TODO: Caveat: cannot be cancelled !! :(
            // Problem with Window.ShowDialog() not returning once closed,
            // Dispatcher probably waiting for DoWork() to complete...yet the priorities are set correctly. ??
            var windowPopup = new PopupModalWindow(this,
                                                   title,
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.None,
                                                   PopupModalWindow.DialogButton.ESC,
                //PopupModalWindow.DialogButtonsSet.Cancel,
                //PopupModalWindow.DialogButton.Cancel,
                                                   false, 500, 120, details, 80);

            Stopwatch m_StopWatch = null;
            Stopwatch m_StopWatch2 = null;

            reporter.ProgressChangedEvent += (sender, args) =>
            {
                if (m_StopWatch != null) m_StopWatch.Stop();
                if (m_StopWatch == null || m_StopWatch.ElapsedMilliseconds >= 300)
                {
                    if (args.ProgressPercentage < 0)
                    {
                        progressBar.IsIndeterminate = true;
                    }
                    else
                    {
                        progressBar.IsIndeterminate = false;
                        progressBar.Value = args.ProgressPercentage;
                    }

                    label.Text = (string)args.UserState;

                    if (m_StopWatch == null) m_StopWatch = new Stopwatch();
                    m_StopWatch.Reset();
                }
                m_StopWatch.Start();
            };

            reporter.SubProgressChangedEvent += (sender, e) =>
            {
                if (m_StopWatch2 != null) m_StopWatch2.Stop();
                if (m_StopWatch2 == null || m_StopWatch2.ElapsedMilliseconds >= 300)
                {
                    if (e.ProgressPercentage < 0 && e.UserState == null)
                    {
                        details.Visibility = Visibility.Collapsed;
                        //progressBar2.Visibility = Visibility.Hidden;
                        //label2.Visibility = Visibility.Hidden;
                        return;
                    }
                    if (details.Visibility != Visibility.Visible)
                        details.Visibility = Visibility.Visible;

                    //if (progressBar2.Visibility != Visibility.Visible)
                    //    progressBar2.Visibility = Visibility.Visible;

                    //if (label2.Visibility != Visibility.Visible)
                    //    label2.Visibility = Visibility.Visible;

                    if (e.ProgressPercentage < 0)
                    {
                        progressBar2.IsIndeterminate = true;
                    }
                    else
                    {
                        progressBar2.IsIndeterminate = false;
                        progressBar2.Value = e.ProgressPercentage;
                    }

                    label2.Text = (string)e.UserState;

                    if (m_StopWatch2 == null) m_StopWatch2 = new Stopwatch();
                    m_StopWatch2.Reset();
                }
                m_StopWatch2.Start();
            };

            reporter.SetDoEventsMethod(() =>
            {
                windowPopup.DoEvents(DispatcherPriority.Input); // See DispatcherObjectExtensions.cs
                //this.DoEvents(DispatcherPriority.ApplicationIdle);
            });

            bool windowForcedClose = false;

            Exception workException = null;
            Dispatcher.BeginInvoke((Action)(() =>
            {
                if (m_StopWatch != null) m_StopWatch.Stop();
                m_StopWatch = null;
                if (m_StopWatch2 != null) m_StopWatch2.Stop();
                m_StopWatch2 = null;

                try
                {
                    reporter.DoWork();
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debugger.Break();
#endif
                    reporter.RequestCancellation = true;
                    workException = ex;
                    //throw ex;
                }
                finally
                {
                    if (m_StopWatch != null) m_StopWatch.Stop();
                    m_StopWatch = null;
                    if (m_StopWatch2 != null) m_StopWatch2.Stop();
                    m_StopWatch2 = null;
                }
                windowForcedClose = true;
                if (reporter.RequestCancellation)
                {
                    actionCancelled();
                    windowPopup.ForceClose(PopupModalWindow.DialogButton.Cancel);
                }
                else
                {
                    actionCompleted();
                    windowPopup.ForceClose(PopupModalWindow.DialogButton.ESC);
                }
            }),
            DispatcherPriority.Background);

            windowPopup.ShowModal();

            if (workException != null)
            {
                ExceptionHandler.Handle(workException, false, this);
                return false;
                //throw workException;
            }

            if (reporter.RequestCancellation)
            {
                return false;
            }

            if (!windowForcedClose && // User clicked CANCEL
                windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Cancel)
            {
                progressBar.IsIndeterminate = true;
                label.Text = Tobi_Lang.PleaseWait;

                details.Visibility = Visibility.Collapsed;
                //progressBar2.Visibility = Visibility.Collapsed;
                //label2.Visibility = Visibility.Collapsed;

                //details.Text = "Cancelling the current operation...";

                windowPopup = new PopupModalWindow(this,
                                                       UserInterfaceStrings.EscapeMnemonic(
                                                           Tobi_Lang.CancellingTask),
                                                       panel,
                                                       PopupModalWindow.DialogButtonsSet.None,
                                                       PopupModalWindow.DialogButton.ESC,
                                                       false, 500, 150, null, 80);

                reporter.RequestCancellation = true;

                windowPopup.ShowModal();

                return false;
            }

            return true;
        }

        // Must be called on the UI thread
        // CAVEAT: when not inSeparateThread => not cancellable (see above)
        public bool RunModalCancellableProgressTask(bool inSeparateThread, string title, IDualCancellableProgressReporter reporter,
                                                     Action actionCancelled, Action actionCompleted)
        {
            if (!Dispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
            }
            m_Logger.Log(String.Format(@"Shell.RunModalCancellableProgressTask() [{0}]", title), Category.Debug, Priority.Medium);

            if (!inSeparateThread)
            {
                bool result = false;

                // we ensure the Dispatcher priority
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    result = RunModalCancellableProgressTask_(title, reporter, actionCancelled, actionCompleted);
                }));

                return result;
            }


            var progressBar = new ProgressBar
            {
                IsIndeterminate = false,
                Height = 18,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };

            var progressBar2 = new ProgressBar
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
                Text = title,
                Margin = new Thickness(0, 0, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Focusable = true,
            };
            var label2 = new TextBlock
            {
                Text = "...",
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
            //panel.Children.Add(new TextBlock(new Run(" ")));
            //panel.Children.Add(label2);
            //panel.Children.Add(progressBar2);

            //label2.Visibility = Visibility.Collapsed;
            //progressBar2.Visibility = Visibility.Collapsed;

            //var details = new TextBoxReadOnlyCaretVisible("Converting data and building the in-memory document object model into the Urakawa SDK...");
            var details = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
            };
            details.Children.Add(label2);
            details.Children.Add(progressBar2);
            details.Visibility = Visibility.Collapsed;

            var windowPopup = new PopupModalWindow(this,
                                                   title,
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.Cancel,
                                                   PopupModalWindow.DialogButton.Cancel,
                                                   false, 500, 150, details, 80);


            var backWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = true
            };

            Exception workException = null;
            backWorker.DoWork += delegate(object s, DoWorkEventArgs args)
            {

                //var dummy = (string)args.Argument;

                if (backWorker.CancellationPending)
                {
                    args.Cancel = true;
                    return;
                }

                Stopwatch m_StopWatch = null;
                Stopwatch m_StopWatch2 = null;

                reporter.ProgressChangedEvent += (sender, e) =>
                {
                    if (m_StopWatch != null) m_StopWatch.Stop();
                    if (m_StopWatch == null || m_StopWatch.ElapsedMilliseconds >= 500)
                    {
                        backWorker.ReportProgress(e.ProgressPercentage, e.UserState);

                        if (m_StopWatch == null) m_StopWatch = new Stopwatch();
                        m_StopWatch.Reset();
                    }
                    m_StopWatch.Start();
                };

                reporter.SubProgressChangedEvent += (sender, e) =>
                    Dispatcher.BeginInvoke((Action)(
                   () =>
                   {
                       if (m_StopWatch2 != null) m_StopWatch2.Stop();
                       if (m_StopWatch2 == null || m_StopWatch2.ElapsedMilliseconds >= 500)
                       {
                           if (e.ProgressPercentage < 0 && e.UserState == null)
                           {
                               details.Visibility = Visibility.Collapsed;
                               //progressBar2.Visibility = Visibility.Hidden;
                               //label2.Visibility = Visibility.Hidden;
                               return;
                           }
                           if (details.Visibility != Visibility.Visible)
                               details.Visibility = Visibility.Visible;

                           //if (progressBar2.Visibility != Visibility.Visible)
                           //    progressBar2.Visibility = Visibility.Visible;

                           //if (label2.Visibility != Visibility.Visible)
                           //    label2.Visibility = Visibility.Visible;

                           if (e.ProgressPercentage < 0)
                           {
                               progressBar2.IsIndeterminate = true;
                           }
                           else
                           {
                               progressBar2.IsIndeterminate = false;
                               progressBar2.Value = e.ProgressPercentage;
                           }

                           label2.Text = (string)e.UserState;

                           if (m_StopWatch2 == null) m_StopWatch2 = new Stopwatch();
                           m_StopWatch2.Reset();
                       }
                       m_StopWatch2.Start();
                   }
                               ),
                       DispatcherPriority.Normal);

                if (m_StopWatch != null) m_StopWatch.Stop();
                m_StopWatch = null;
                if (m_StopWatch2 != null) m_StopWatch2.Stop();
                m_StopWatch2 = null;
#if DEBUG
                try
                {
#endif
                    reporter.DoWork();

                    args.Result = @"dummy result";
#if DEBUG
                }
                catch (Exception ex)
                {
                    Debugger.Break();
                    throw ex;
                }
                finally
                {
                    if (m_StopWatch != null) m_StopWatch.Stop();
                    m_StopWatch = null;
                    if (m_StopWatch2 != null) m_StopWatch2.Stop();
                    m_StopWatch2 = null;
                }
#endif
            };

            backWorker.ProgressChanged += delegate(object s, ProgressChangedEventArgs args)
            {
                //PumpDispatcherFrames();

                if (reporter.RequestCancellation)
                {
                    return;
                }

                if (args.ProgressPercentage < 0)
                {
                    progressBar.IsIndeterminate = true;
                }
                else
                {
                    progressBar.IsIndeterminate = false;
                    progressBar.Value = args.ProgressPercentage;
                }

                label.Text = (string)args.UserState;
            };

            backWorker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
            {
                workException = args.Error;
#if DEBUG
                if (workException != null)
                    Debugger.Break();
#endif
                backWorker = null;

                if (reporter.RequestCancellation || args.Cancelled)
                {
                    actionCancelled();
                    windowPopup.ForceClose(PopupModalWindow.DialogButton.Cancel);
                }
                else
                {
                    actionCompleted();
                    windowPopup.ForceClose(PopupModalWindow.DialogButton.ESC);
                }

                //var result = (string)args.Result;
            };
            backWorker.RunWorkerAsync(@"dummy arg");

            windowPopup.ShowModal();

            if (workException != null)
            {
                ExceptionHandler.Handle(workException, false, this);
                return false;
                //throw workException;
            }

            if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Cancel)
            {
                if (backWorker == null) return false;

                progressBar.IsIndeterminate = true;
                label.Text = Tobi_Lang.PleaseWait;

                details.Visibility = Visibility.Collapsed;
                //progressBar2.Visibility = Visibility.Collapsed;
                //label2.Visibility = Visibility.Collapsed;

                //details.Text = "Cancelling the current operation...";

                windowPopup = new PopupModalWindow(this,
                                                       UserInterfaceStrings.EscapeMnemonic(
                                                           Tobi_Lang.CancellingTask),
                                                       panel,
                                                       PopupModalWindow.DialogButtonsSet.None,
                                                       PopupModalWindow.DialogButton.ESC,
                                                       false, 500, 150, null, 80);

                //m_OpenXukActionWorker.CancelAsync();
                reporter.RequestCancellation = true;

                windowPopup.ShowModal();

                return false;
            }

            return true;
        }

        // DoEvent() equivalent
        public void PumpDispatcherFrames(DispatcherPriority prio)
        {
            this.DoEvents(prio); // See DispatcherObjectExtensions
        }
    }
}
