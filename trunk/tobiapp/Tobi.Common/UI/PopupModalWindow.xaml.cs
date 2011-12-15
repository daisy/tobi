using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Media;
using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Practices.Composite;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI.XAML;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Tobi.Common.UI
{
    /// <summary>
    /// Interaction logic for PopupModalWindow.xaml
    /// </summary>
    public partial class PopupModalWindow : IInputBindingManager, INotifyPropertyChangedEx
    {
        public IInputBindingManager InputBindingManager { get { return this; } }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            if (ShellView != null && ShellView.IsUIAutomationDisabled)
            {
#if DEBUG
                Debugger.Break();
#endif
                return new DisabledUIAutomationWindowAutomationPeer(this);
            }

            return base.OnCreateAutomationPeer();
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

        public RichDelegateCommand CommandDetailsExpand { get; private set; }
        public RichDelegateCommand CommandDetailsCollapse { get; private set; }

        //public IInputBindingManager InputBindingManager
        //{
        //    get
        //    {
        //        return this;
        //    }
        //}

        private void initCommands()
        {
            CommandDetailsExpand = new RichDelegateCommand(
                Tobi_Common_Lang.CmdDetailsExpand_ShortDesc,
                Tobi_Common_Lang.CmdDetailsExpand_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                (ShellView == null ? null : ShellView.LoadGnomeFoxtrotIcon("Foxtrot_go-down")),
                () => IsDetailsExpanded = true,
                () => HasDetails && !IsDetailsExpanded,
                Settings_KeyGesture.Default,
                null // PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGesture.Default.Keyboard_DialogExpandCollapse)
                );

            //AddInputBinding(CommandDetailsExpand.KeyBinding);
            //
            CommandDetailsCollapse = new RichDelegateCommand(
                Tobi_Common_Lang.CmdDetailsCollapse_ShortDesc,
                Tobi_Common_Lang.CmdDetailsCollapse_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                (ShellView == null ? null : ShellView.LoadGnomeFoxtrotIcon("Foxtrot_go-up")),
                () => IsDetailsExpanded = false,
                () => HasDetails && IsDetailsExpanded,
                Settings_KeyGesture.Default,
                null // PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGesture.Default.Keyboard_DialogExpandCollapse)
                );

            //AddInputBinding(CommandDetailsCollapse.KeyBinding);
        }

        public IActiveAware ActiveAware { get; private set; }

        private PopupModalWindow(IShellView shellView)
        {
            PreviewKeyDown += new KeyEventHandler(OnThisKeyDown);

            ShellView = shellView;

            ClickedDialogButton = DialogButton.ESC;

            m_PropertyChangeHandler = new PropertyChangedNotifyBase();
            m_PropertyChangeHandler.InitializeDependentProperties(this);

            ActiveAware = new FocusActiveAwareAdapter(this);
            ActiveAware.IsActiveChanged += (sender, e) =>
            {
                if (!ActiveAware.IsActive)
                {
                    Console.WriteLine("@@@@ popup lost focus");
                }
                else
                {
                    Console.WriteLine("@@@@ popup gained focus");
                }
                CommandManager.InvalidateRequerySuggested();
            };

            InitializeComponent();

            //RegionManager.SetRegionManager(this, m_Container.Resolve<IRegionManager>());
            //RegionManager.UpdateRegions();

            initCommands();

            m_IsDetailsExpanded = false;
        }

        private bool m_IsDetailsExpanded = false;
        public bool IsDetailsExpanded
        {
            set
            {
                if (m_IsDetailsExpanded != value)
                {
                    m_IsDetailsExpanded = value;
                    if (m_IsDetailsExpanded)
                    {
                        Height += DetailsHeight;
                    }
                    else
                    {
                        Height -= DetailsHeight;
                    }
                    ensureVisible();

                    m_PropertyChangeHandler.RaisePropertyChanged(() => IsDetailsExpanded);
                }
            }
            get { return m_IsDetailsExpanded; }
        }

        public double DetailsHeight { get; set; }

        public bool HasDetails
        {
            get
            {
                if (DetailsPlaceHolder.Content == null) return false;
                var ui = DetailsPlaceHolder.Content as UIElement;
                if (ui == null) return true;
                if (ui.Visibility != Visibility.Visible) return false;
                return true;
            }
        }

        public void ShowModal()
        {
            if (ShellView != null)
            {
                ShellView.RaiseEscapeEvent();
            }

            ensureVisible();

            ShowInTaskbar = false;

            ActiveAware.IsActive = true;
            if (ShellView != null)
            {
                ShellView.DimBackgroundWhile(() => ShowDialog());
            }
            else
            {
                try
                {
                    ShowDialog();
                }
                catch (Exception ex)
                {
                    // oops !
                }
            }
        }

        public void ShowFloating(Action whenDoneAction)
        {
            if (ShellView != null)
            {
                ShellView.RaiseEscapeEvent();
            }

            m_whenDoneAction = whenDoneAction;

            ensureVisible();

            ShowInTaskbar = true;

            ActiveAware.IsActive = true;
            Show();
        }

        private void ensureVisible()
        {
            // For some reason, WindowStartupLocation.CenterOwner doesn't work in non-modal display mode.
            WindowStartupLocation = WindowStartupLocation.Manual;

            if (Owner != null && Owner.WindowState == WindowState.Minimized)
            {
                Owner.WindowState = WindowState.Normal;
            }

            double leftOwnerCentered = Math.Max(0, (Owner == null ? 10 : Owner.Left) + ((Owner == null ? 800 : Owner.Width) - Width) / 2);
            double topOwnerCentered = Math.Max(0, (Owner == null ? 10 : Owner.Top) + ((Owner == null ? 600 : Owner.Height) - Height) / 2);

            double availableScreenWidth = SystemParameters.WorkArea.Width; //Screen.PrimaryScreen.Bounds.Width
            double availableScreenHeight = SystemParameters.WorkArea.Height; //Screen.PrimaryScreen.Bounds.Height

            if (Owner != null && Owner.WindowState == WindowState.Maximized)
            {
                leftOwnerCentered = Math.Max(0, (availableScreenWidth - Width) / 2);
                topOwnerCentered = Math.Max(0, (availableScreenHeight - Height) / 2);
            }

            double finalWidth = Math.Min(availableScreenWidth, Width);
            double finalHeight = Math.Min(availableScreenHeight, Height);

            double finalLeft = Left == -1 ? leftOwnerCentered : Math.Max(0, Left);
            double finalTop = Top == -1 ? topOwnerCentered : Math.Max(0, Top);

            double right = finalLeft + finalWidth;
            if (right > availableScreenWidth)
            {
                double extraWidth = right - availableScreenWidth;
                finalLeft -= extraWidth;
                if (finalLeft < 0)
                {
                    finalWidth -= (-finalLeft);
                    finalLeft = 0;
                }
            }

            double bottom = finalTop + finalHeight;
            if (bottom > availableScreenHeight)
            {
                double extraHeight = bottom - availableScreenHeight;
                finalTop -= extraHeight;
                if (finalTop < 0)
                {
                    finalHeight -= (-finalTop);
                    finalTop = 0;
                }
            }
            bottom = finalTop + finalHeight; // + DetailsHeight);
            if (bottom > availableScreenHeight)
            {
                double extraHeight = bottom - availableScreenHeight;
                finalTop -= extraHeight;
                if (finalTop < 0)
                {
                    finalHeight -= (-finalTop);
                    finalTop = 0;
                }
            }

            Left = finalLeft;
            Width = finalWidth;

            Top = finalTop;
            Height = finalHeight;
        }

        public PopupModalWindow(IShellView shellView, string title,
            object content,
            DialogButtonsSet buttons, DialogButton button, bool allowEscapeAndCloseButton,
            double width, double height,
            object details, double detailsHeight)
            : this(shellView)
        {
            if (this != Application.Current.MainWindow)
            {
                try
                {
                    Owner = Application.Current.MainWindow;
                }
                catch
                {
                    Console.WriteLine(@"Failed to set Owner of popup dialog window !");
                }
            }

            //if (this != Application.Current.MainWindow)
            //{
            //    Owner = ShellView == null ? Application.Current.MainWindow : ShellView.View.Window;
            //}
            //else { Owner = null; }

            //#if NET_3_5

            //#else  // NET_4_0 || BOOTSTRAP_NET_4_0

            //#endif


            //DataContext = Owner;

            var zoom = (ShellView != null ? ShellView.MagnificationLevel : (Double)FindResource("MagnificationLevel"));

            Left = -1;
            Top = -1;
            Width = zoom * width;
            Height = zoom * height;

            DetailsHeight = zoom * detailsHeight;

            CommandDetailsCollapse.SetIconProviderDrawScale(zoom);
            CommandDetailsExpand.SetIconProviderDrawScale(zoom);

            Title = "Tobi: " + title;
            Icon = null;
            ContentPlaceHolder.Content = content;

            DetailsPlaceHolder.Content = details;
            //if (details != null && details is UIElement)
            //{
            //    ((UIElement)details).IsVisibleChanged += (sender, ev) => DetailsPlaceHolder.Visibility = ((UIElement)details).Visibility;
            //}
            var ui = DetailsPlaceHolder.Content as UIElement;
            if (ui != null)
            {
                DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(UIElement.VisibilityProperty, typeof(UIElement));
                if (dpd != null)
                {
                    dpd.AddValueChanged(ui, delegate
                    {
                        m_PropertyChangeHandler.RaisePropertyChanged(() => HasDetails);
                        //m_PropertyChangeHandler.RaisePropertyChanged(() => IsDetailsExpanded);
                        CommandManager.InvalidateRequerySuggested();
                    });
                }
                //ui.IsVisibleChanged += (obj, args) => m_PropertyChangeHandler.RaisePropertyChanged(() => HasDetails);
            }

            DialogButtons = buttons;
            DefaultDialogButton = button;
            AllowEscapeAndCloseButton = allowEscapeAndCloseButton;
        }

        //        public void RaiseHasDetailChanged()
        //        {
        //            m_PropertyChangeHandler.RaisePropertyChanged(() => HasDetails);
        //        }

        //        static PopupModalWindow()
        //        {
        //            VisibilityProperty.OverrideMetadata(typeof(PopupModalWindow),
        //                   new FrameworkPropertyMetadata(true,
        //                       new PropertyChangedCallback(OnVisibilityChanged)));
        //        }

        //        private static void OnVisibilityChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        //        {
        //            var pop = source as PopupModalWindow;
        //            if (pop == null)
        //            {
        //#if DEBUG
        //                Debugger.Break();
        //#endif
        //                return;
        //            }
        //            var vis = (Visibility)args.NewValue;
        //            pop.RaiseHasDetailChanged();
        //        }

        //public PopupModalWindow(IShellView window, string title, object content,
        //    DialogButtonsSet buttons, DialogButton button, bool allowEscapeAndCloseButton, double width, double height)
        //    : this(window, title, content, buttons, button, allowEscapeAndCloseButton, width, height, null, 0)
        //{
        //}

        private bool m_ButtonTriggersClose = false;
        private readonly IShellView ShellView;
        private Action m_whenDoneAction;

        public bool AllowEscapeAndCloseButton
        {
            get;
            private set;
        }

        public bool IgnoreEscape
        {
            get;
            set;
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (!IgnoreEscape && e.Key == Key.Escape)
            {
                if (AllowEscapeAndCloseButton)
                {
                    m_ButtonTriggersClose = false;
                    Close();
                }
                else
                {
                    AudioCues.PlayAsterisk();
                }
            }
            base.OnKeyUp(e);
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (m_ButtonTriggersClose)
            {
                ContentPlaceHolder.Content = null;
                DetailsPlaceHolder.Content = null;
                return;
            }

            if (!AllowEscapeAndCloseButton)
            {
                AudioCues.PlayAsterisk();
                e.Cancel = true;
                return;
            }

            ContentPlaceHolder.Content = null;
            DetailsPlaceHolder.Content = null;

            if (m_whenDoneAction != null)
            {
                m_whenDoneAction.Invoke();
                m_whenDoneAction = null;
            }
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
#if NET40
            this.SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Ideal);
            this.SetValue(TextOptions.TextRenderingModeProperty, TextRenderingMode.Auto);
            this.SetValue(TextOptions.TextHintingModeProperty, TextHintingMode.Fixed);
#endif //NET40

            Console.WriteLine(@"Popup WpfSoftwareRender => " + Tobi.Common.Settings.Default.WpfSoftwareRender);

            if (Tobi.Common.Settings.Default.WpfSoftwareRender)
            {
                var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
                if (hwndSource != null)
                {
                    HwndTarget hwndTarget = hwndSource.CompositionTarget;
                    hwndTarget.RenderMode = RenderMode.SoftwareOnly;
                }
            }

            //0 => No graphics hardware acceleration available for the application on the device.
            //1 => Partial graphics hardware acceleration available on the video card. This corresponds to a DirectX version that is greater than or equal to 7.0 and less than 9.0.
            //2 => A rendering tier value of 2 means that most of the graphics features of WPF should use hardware acceleration provided the necessary system resources have not been exhausted. This corresponds to a DirectX version that is greater than or equal to 9.0.
            int renderingTier = (RenderCapability.Tier >> 16);

            Console.WriteLine(@"Popup RenderCapability.Tier => " + renderingTier);

            try
            {
                //Uri iconUri = new Uri("Tobi.ico", UriKind.RelativeOrAbsolute);
                //Uri iconUri = new Uri("pack://application:,,,/Tobi;component/Tobi.ico", UriKind.Absolute);

                string appFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string iconPath = Path.Combine(appFolder, "Shortcut.ico");
                var iconUri = new Uri("file://" + iconPath, UriKind.Absolute);

                Icon = BitmapFrame.Create(iconUri);
            }
            finally
            {
                //ignore
            }

            OnPropertyChangedButtonsSet();

            Button buttonToFocus = ButtonOK;
            switch (DefaultDialogButton)
            {
                case DialogButton.Yes:
                    {
                        buttonToFocus = ButtonYes;
                        break;
                    }
                case DialogButton.Ok:
                    {
                        buttonToFocus = ButtonOK;
                        break;
                    }
                case DialogButton.No:
                    {
                        buttonToFocus = ButtonNo;
                        break;
                    }
                case DialogButton.Close:
                    {
                        buttonToFocus = ButtonClose;
                        break;
                    }
                case DialogButton.Cancel:
                    {
                        buttonToFocus = ButtonCancel;
                        break;
                    }
                case DialogButton.Apply:
                    {
                        buttonToFocus = ButtonApply;
                        break;
                    }
            }

            FocusHelper.Focus(buttonToFocus);
        }

        public static bool IsButtonOkYesApply(DialogButton button)
        {
            return button == DialogButton.Apply || button == DialogButton.Ok || button == DialogButton.Yes;
        }
        public static bool IsButtonEscCancel(DialogButton button)
        {
            return button == DialogButton.ESC || button == DialogButton.Cancel;
        }
        public static bool IsButtonCloseNo(DialogButton button)
        {
            return button == DialogButton.Close || button == DialogButton.No;
        }

        public enum DialogButton
        {
            Ok,
            Cancel,
            Apply,
            Yes,
            No,
            Close,
            ESC
        }

        public enum DialogButtonsSet
        {
            None,
            Ok,
            OkCancel,
            Cancel,
            OkApplyClose,
            YesNo,
            YesNoCancel,
            Close,
        }

        #region ButtonDefault
        public bool IsButtonDefault_Close
        {
            get
            {
                return DefaultDialogButton == DialogButton.Close;
            }
        }
        public bool IsButtonDefault_Apply
        {
            get
            {
                return DefaultDialogButton == DialogButton.Apply;
            }
        }
        public bool IsButtonDefault_Ok
        {
            get
            {
                return DefaultDialogButton == DialogButton.Ok;
            }
        }
        public bool IsButtonDefault_Cancel
        {
            get
            {
                return DefaultDialogButton == DialogButton.Cancel;
            }
        }
        public bool IsButtonDefault_Yes
        {
            get
            {
                return DefaultDialogButton == DialogButton.Yes;
            }
        }
        public bool IsButtonDefault_No
        {
            get
            {
                return DefaultDialogButton == DialogButton.No;
            }
        }

        #endregion ButtonDefault
        #region ButtonActive
        public bool IsButtonActive_Close
        {
            get
            {
                return DialogButtons == DialogButtonsSet.Close
                    || DialogButtons == DialogButtonsSet.OkApplyClose;
            }
        }
        public bool IsButtonActive_Apply
        {
            get
            {
                return DialogButtons == DialogButtonsSet.OkApplyClose;
            }
        }
        public bool IsButtonActive_Ok
        {
            get
            {
                return DialogButtons == DialogButtonsSet.Ok
                       || DialogButtons == DialogButtonsSet.OkApplyClose
                       || DialogButtons == DialogButtonsSet.OkCancel;
            }
        }
        public bool IsButtonActive_Cancel
        {
            get
            {
                return DialogButtons == DialogButtonsSet.Cancel
                       || DialogButtons == DialogButtonsSet.OkCancel
                       || DialogButtons == DialogButtonsSet.YesNoCancel;
            }
        }
        public bool IsButtonActive_Yes
        {
            get
            {
                return DialogButtons == DialogButtonsSet.YesNo
                       || DialogButtons == DialogButtonsSet.YesNoCancel;
            }
        }
        public bool IsButtonActive_No
        {
            get
            {
                return DialogButtons == DialogButtonsSet.YesNo
                       || DialogButtons == DialogButtonsSet.YesNoCancel;
            }
        }

        #endregion ButtonActive

        public DialogButton ClickedDialogButton
        {
            get;
            private set;
        }

        public DialogButton DefaultDialogButton
        {
            get;
            private set;
        }

        public DialogButtonsSet DialogButtons
        {
            get;
            private set;
        }

        private void OnPropertyChangedButtonsSet()
        {
            m_PropertyChangeHandler.RaisePropertyChanged(() => IsButtonActive_Close);
            m_PropertyChangeHandler.RaisePropertyChanged(() => IsButtonActive_Apply);

            m_PropertyChangeHandler.RaisePropertyChanged(() => IsButtonActive_Ok);
            m_PropertyChangeHandler.RaisePropertyChanged(() => IsButtonActive_Cancel);

            m_PropertyChangeHandler.RaisePropertyChanged(() => IsButtonActive_Yes);
            m_PropertyChangeHandler.RaisePropertyChanged(() => IsButtonActive_No);

            m_PropertyChangeHandler.RaisePropertyChanged(() => IsButtonDefault_Close);
            m_PropertyChangeHandler.RaisePropertyChanged(() => IsButtonDefault_Apply);

            m_PropertyChangeHandler.RaisePropertyChanged(() => IsButtonDefault_Ok);
            m_PropertyChangeHandler.RaisePropertyChanged(() => IsButtonDefault_Cancel);

            m_PropertyChangeHandler.RaisePropertyChanged(() => IsButtonDefault_Yes);
            m_PropertyChangeHandler.RaisePropertyChanged(() => IsButtonDefault_No);
        }

        public void ForceClose(DialogButton buttonResult)
        {
            ClickedDialogButton = buttonResult;
            AllowEscapeAndCloseButton = true;
            m_ButtonTriggersClose = false;
            Close();
        }

        #region ButtonClick

        private void OnOkButtonClick(object sender, RoutedEventArgs e)
        {
            ClickedDialogButton = DialogButton.Ok;
            m_ButtonTriggersClose = true;
            Close();
        }
        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            ClickedDialogButton = DialogButton.Cancel;
            m_ButtonTriggersClose = true;
            Close();
        }
        private void OnYesButtonClick(object sender, RoutedEventArgs e)
        {
            ClickedDialogButton = DialogButton.Yes;
            m_ButtonTriggersClose = true;
            Close();
        }
        private void OnNoButtonClick(object sender, RoutedEventArgs e)
        {
            ClickedDialogButton = DialogButton.No;
            m_ButtonTriggersClose = true;
            Close();
        }
        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            ClickedDialogButton = DialogButton.Close;
            m_ButtonTriggersClose = true;
            Close();
        }
        private void OnApplyButtonClick(object sender, RoutedEventArgs e)
        {
            ClickedDialogButton = DialogButton.Apply;
        }
        #endregion ButtonClick

        public static bool isAltKeyDown()
        {
            return (Keyboard.Modifiers &
                    (ModifierKeys.Alt
                    )
                    ) != ModifierKeys.None;
        }
        public static bool isShiftKeyDown()
        {
            return (Keyboard.Modifiers &
                    (ModifierKeys.Shift
                    )
                    ) != ModifierKeys.None;
        }
        public static bool isControlKeyDown()
        {
            return (Keyboard.Modifiers &
                    (ModifierKeys.Control
                //| ModifierKeys.Shift
                    )
                    ) != ModifierKeys.None;

            //Keyboard.IsKeyDown(Key.LeftShift)
            //System.Windows.Forms.Control.ModifierKeys == Keys.Control;
            // (System.Windows.Forms.Control.ModifierKeys & Keys.Control) != Keys.None;
        }

        public static bool modifiersMatch(ModifierKeys modifiers)
        {
            bool isControlDown = (modifiers & ModifierKeys.Control) != ModifierKeys.None;
            bool isShiftDown = (modifiers & ModifierKeys.Shift) != ModifierKeys.None;
            bool isAltDown = (modifiers & ModifierKeys.Alt) != ModifierKeys.None;

            return modifiers == ModifierKeys.None
                && Keyboard.Modifiers == ModifierKeys.None ||
                isControlDown == isControlKeyDown() &&
                   isShiftDown == isShiftKeyDown() &&
                   isAltDown == isAltKeyDown();
        }

        public static void checkSpaceKeyButtonActivation(object sender, KeyEventArgs e, InputBindingCollection InputBindings)
        {
            if (e.Key != Key.Space) return;

            if (
                !(
                (e.OriginalSource is Button || e.OriginalSource is RepeatButton)
                && ((UIElement)e.OriginalSource).IsFocused
                )
                )
                return;

            e.Handled = true;

            var commandsToExecute = new List<ICommand>(1);

            foreach (var inputBinding in InputBindings)
            {
                if (!(inputBinding is KeyBinding)) continue;
                if (!(((KeyBinding)inputBinding).Gesture is KeyGesture)) continue;

                if (((KeyGesture)((KeyBinding)inputBinding).Gesture).Key != e.Key) continue;

                var modifiers = ((KeyGesture)((KeyBinding)inputBinding).Gesture).Modifiers;
                if (!modifiersMatch(modifiers)) continue;

                if (((KeyBinding)inputBinding).Command != null && ((KeyBinding)inputBinding).Command.CanExecute(null))
                {
                    commandsToExecute.Add(((KeyBinding)inputBinding).Command);
                }
            }

            foreach (var command in commandsToExecute)
            {
                command.Execute(null);
            }
        }

        public static void checkEnterKeyButtonActivation(object sender, KeyEventArgs e, InputBindingCollection InputBindings)
        {
            if (e.Key != Key.Return) return;

            if (!isControlKeyDown() && !isShiftKeyDown() && !isAltKeyDown())
                return;

            //if (!(e.OriginalSource is Button && ((UIElement)e.OriginalSource).IsFocused))
            //    return;

            e.Handled = true;

            var commandsToExecute = new List<ICommand>(1);

            foreach (var inputBinding in InputBindings)
            {
                if (!(inputBinding is KeyBinding)) continue;
                if (!(((KeyBinding)inputBinding).Gesture is KeyGesture)) continue;

                if (((KeyGesture)((KeyBinding)inputBinding).Gesture).Key != e.Key) continue;

                var modifiers = ((KeyGesture)((KeyBinding)inputBinding).Gesture).Modifiers;
                if (!modifiersMatch(modifiers)) continue;

                if (((KeyBinding)inputBinding).Command != null && ((KeyBinding)inputBinding).Command.CanExecute(null))
                {
                    commandsToExecute.Add(((KeyBinding)inputBinding).Command);
                }
            }

            foreach (var command in commandsToExecute)
            {
                command.Execute(null);
            }
        }

        public static void checkFlowDocumentCommands(object sender, KeyEventArgs e, InputBindingCollection InputBindings)
        {
            if (
                !(
                Keyboard.Modifiers == ModifierKeys.Control
                && (e.Key == Key.P || e.Key == Key.A || e.Key == Key.C || e.Key == Key.X || e.Key == Key.V)
                || e.Key == Key.Home || e.Key == Key.End
                || e.Key == Key.Left || e.Key == Key.Right
                || e.Key == Key.Up || e.Key == Key.Down
                //|| e.Key == Key.Space
                )
                )
            {
                return;
            }

            if (
                !(
                e.OriginalSource is FlowDocumentScrollViewer && ((UIElement)e.OriginalSource).IsFocused
                )
                )
                return;

            e.Handled = true;

            var commandsToExecute = new List<ICommand>(1);

            foreach (var inputBinding in InputBindings)
            {
                if (!(inputBinding is KeyBinding)) continue;
                if (!(((KeyBinding)inputBinding).Gesture is KeyGesture)) continue;

                if (((KeyGesture)((KeyBinding)inputBinding).Gesture).Key != e.Key) continue;

                var modifiers = ((KeyGesture)((KeyBinding)inputBinding).Gesture).Modifiers;
                if (!modifiersMatch(modifiers)) continue;

                if (((KeyBinding)inputBinding).Command != null && ((KeyBinding)inputBinding).Command.CanExecute(null))
                {
                    commandsToExecute.Add(((KeyBinding)inputBinding).Command);
                }
            }

            foreach (var command in commandsToExecute)
            {
                command.Execute(null);
            }
        }

        private void OnThisKeyDown(object sender, KeyEventArgs e)
        {
            checkFlowDocumentCommands(sender, e, InputBindings);
            checkSpaceKeyButtonActivation(sender, e, InputBindings);
            checkEnterKeyButtonActivation(sender, e, InputBindings);
        }

        public bool AddInputBinding(InputBinding inputBindingz)
        {
            if (inputBindingz != null)
            {
                foreach (var inputBinding in InputBindings)
                {
                    if (inputBindingz == inputBinding)
                    {
                        // HAPPENS DURING TOGGLE BUTTON RECONFIGURATION
                        //#if DEBUG
                        //                        Debugger.Break();
                        //#endif
                        return false;
                    }

                    if (!(inputBinding is KeyBinding)) continue;

                    if (((KeyBinding)inputBindingz).Command == ((KeyBinding)inputBinding).Command)
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }

                    if (!(((KeyBinding)inputBinding).Gesture is KeyGesture)) continue;

                    if (((KeyGesture)((KeyBinding)inputBindingz).Gesture).Key == ((KeyGesture)((KeyBinding)inputBinding).Gesture).Key
                        &&
                        ((KeyGesture)((KeyBinding)inputBindingz).Gesture).Modifiers == ((KeyGesture)((KeyBinding)inputBinding).Gesture).Modifiers)
                    {
                        // TOGGLE BUTTON COMMANDS HAVE IDENTICAL SHORTCUTS:
                        // PLAY/PAUSE
                        // START/STOP RECORD
                        // START/STOP MONITORING

                        //#if DEBUG
                        //                        Debugger.Break();
                        //#endif
                    }
                }
                //logInputBinding(inputBinding);
                InputBindings.Add(inputBindingz);
                return true;
            }

            return false;
        }

        public void RemoveInputBinding(InputBinding inputBinding)
        {
            if (inputBinding != null)
            {
                //logInputBinding(inputBinding);
                InputBindings.Remove(inputBinding);
            }
        }
    }

    [ValueConversion(typeof(object), typeof(GridLength))]
    public class ContentToGridDimMultiConverter : ValueConverterMarkupExtensionBase<ContentToGridDimMultiConverter>
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(GridLength))
                throw new InvalidOperationException("The target must be a GridLength !");

            if (values.Length < 2)
            {
                return null;
            }

            if (values[0] != null) // && values[0].GetType() == typeof(ScrollViewer)
            {
                if (values[1].GetType() == typeof(Boolean))
                {
                    if ((Boolean)values[1])
                        return new GridLength(1.5, GridUnitType.Star);

                    return new GridLength(0.0, GridUnitType.Pixel);
                }
                if (values[1].GetType() == typeof(Visibility))
                {
                    if ((Visibility)values[1] == Visibility.Visible)
                        return new GridLength(1.0, GridUnitType.Star);

                    return new GridLength(1.0, GridUnitType.Star);
                }
            }
            return GridLength.Auto;
        }
    }


    [ValueConversion(typeof(object), typeof(GridLength))]
    public class ContentToGridDimConverter : ValueConverterMarkupExtensionBase<ContentToGridDimConverter>
    {
        #region IValueConverter Members

        public override object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(GridLength))
                throw new InvalidOperationException("The target must be a GridLength !");

            if (value.GetType() == typeof(ScrollViewer)) return "*";
            return "Auto";
        }

        #endregion
    }

    public class NoGridResizePanel : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            Size measureSize = new Size(
                availableSize.Width ==
                    Double.PositiveInfinity ? 0.0 : availableSize.Width,
                availableSize.Height ==
                    Double.PositiveInfinity ? 0.0 : availableSize.Height);

            foreach (UIElement child in Children)
            {
                child.Measure(measureSize);
            }
            //always return Size(0.0,0.0) so grid will not expand
            //the cell we are in based on our size
            return new Size(0.0, 0.0);
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            //overlay all children on top of each other
            foreach (UIElement child in Children)
            {
                child.Arrange(new Rect(new Point(0.0, 0.0), finalSize));
            }
            return finalSize;
        }
    }
}
