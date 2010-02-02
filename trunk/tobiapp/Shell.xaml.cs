using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity container, it's a built-in CAG service</param>
        [ImportingConstructor]
        public Shell(ILoggerFacade logger)
        {
            m_Logger = logger;

            m_Logger.Log(@"ShellView.ctor", Category.Debug, Priority.Medium);

            App.LOGGER = m_Logger;

            m_Exiting = false;

            initCommands();

            m_PropertyChangeHandler = new PropertyChangedNotifyBase();
            m_PropertyChangeHandler.InitializeDependentProperties(this);

            m_InConstructor = true;

            InitializeComponent();

            m_InConstructor = false;

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
                // TODO: remove this ! (just for debug)
                int uid3 = m_ToolBarsView.AddToolBarGroup(new[] { ExitCommand }, PreferredPosition.First);

                int uid2 = m_ToolBarsView.AddToolBarGroup(new[] { MagnifyUiResetCommand, MagnifyUiDecreaseCommand, MagnifyUiIncreaseCommand }, PreferredPosition.Any);
                int uid1 = m_ToolBarsView.AddToolBarGroup(new[] { ManageShortcutsCommand }, PreferredPosition.Any);
                
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
                int uid1 = m_MenuBarView.AddMenuBarGroup(RegionNames.MenuBar_File, null, new[] { ExitCommand }, PreferredPosition.Last, true);
                int uid2 = m_MenuBarView.AddMenuBarGroup(RegionNames.MenuBar_View, RegionNames.MenuBar_Magnification, new[] { MagnifyUiResetCommand, MagnifyUiDecreaseCommand, MagnifyUiIncreaseCommand }, PreferredPosition.Last, true);
                int uid3 = m_MenuBarView.AddMenuBarGroup(RegionNames.MenuBar_View, null, new[] { ManageShortcutsCommand }, PreferredPosition.First, true);
                int uid4 = m_MenuBarView.AddMenuBarGroup(RegionNames.MenuBar_Tools, RegionNames.MenuBar_System, new[] { OpenTobiFolderCommand, OpenTobiSettingsFolderCommand, OpenTobiIsolatedStorageCommand }, PreferredPosition.Last, true);

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
                "VisualLeafNoError={0}\nVisualDepthNoError={1}\nLogicalLeafNoError={2}\nLogicalDepthNoError={3}\n\ntimeNonRecursiveDepth={4}\ntimeNonRecursiveLeaf={5}\ntimeRecursiveDepth={6}\ntimeRecursiveLeaf={7}\n"
                , nVisualLeafNoError, nVisualDepthNoError, nLogicalLeafNoError, nLogicalDepthNoError, timeNonRecursiveDepth, timeNonRecursiveLeaf, timeRecursiveDepth, timeRecursiveLeaf));
#endif
        }

        protected void OnClosing(object sender, CancelEventArgs e)
        {
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
                Text = UserInterfaceStrings.ExitConfirm,
                Margin = new Thickness(8, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap
            };

            var iconProvider = new ScalableGreyableImageProvider(LoadTangoIcon("help-browser"), MagnificationLevel);
            //var zoom = (Double)Resources["MagnificationLevel"]; //Application.Current.

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            panel.Children.Add(iconProvider.IconLarge);
            panel.Children.Add(label);
            //panel.Margin = new Thickness(8, 8, 8, 0);


            var details = new TextBoxReadOnlyCaretVisible(UserInterfaceStrings.ExitConfirm)
            {
            };

            var windowPopup = new PopupModalWindow(this,
                                                   UserInterfaceStrings.EscapeMnemonic(
                                                       UserInterfaceStrings.Exit),
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.YesNo,
                                                   PopupModalWindow.DialogButton.No,
                                                   true, 300, 160, details, 40);

            windowPopup.ShowModal();

            if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Yes)
            {
                if (m_UrakawaSession != null &&
                    m_UrakawaSession.DocumentProject != null && m_UrakawaSession.IsDirty)
                {
                    if (!m_UrakawaSession.Close())
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }


        public void DimBackgroundWhile(Action action)
        {
            var aLayer = AdornerLayer.GetAdornerLayer((Visual)Content);

            var theAdorner = new DimAdorner((UIElement)Content);
            if (aLayer != null)
            {
                aLayer.Add(theAdorner);
            }

            action.Invoke();

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
            finally
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
                    return @"Tobi" + @" {" + UserInterfaceStrings.APP_VERSION + @"}" + @" - Please wait...";
                }
                return @"Tobi" + @" {" + UserInterfaceStrings.APP_VERSION + @"} " + (m_UrakawaSession.IsDirty ? @"* " : @"") + @"[" + (m_UrakawaSession.DocumentProject == null ? @"no document" : m_UrakawaSession.DocumentFilePath) + @"]";
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

        private void MainWindow_Closed(object sender, EventArgs e)
        {

        }

        private void MainWindow_Closed_1(object sender, EventArgs e)
        {

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

    }
}
