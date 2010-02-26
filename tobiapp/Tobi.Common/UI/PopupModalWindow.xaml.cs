using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Media;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
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
                Tobi_Common_Lang.DetailsExpand,
                Tobi_Common_Lang.DetailsExpand_,
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
                Tobi_Common_Lang.DetailsCollapse,
                Tobi_Common_Lang.DetailsCollapse_,
                null, // KeyGesture obtained from settings (see last parameters below)
                (ShellView == null ? null : ShellView.LoadGnomeFoxtrotIcon("Foxtrot_go-up")),
                () => IsDetailsExpanded = false,
                () => HasDetails && IsDetailsExpanded,
                Settings_KeyGesture.Default,
                null // PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGesture.Default.Keyboard_DialogExpandCollapse)
                );

            //AddInputBinding(CommandDetailsCollapse.KeyBinding);
        }

        private PopupModalWindow(IShellView shellView)
        {
            ShellView = shellView;

            ClickedDialogButton = DialogButton.ESC;

            m_PropertyChangeHandler = new PropertyChangedNotifyBase();
            m_PropertyChangeHandler.InitializeDependentProperties(this);

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
                        ensureVisible(false);
                        Height += DetailsHeight;
                    }
                    else
                    {
                        Height -= DetailsHeight;
                    }

                    m_PropertyChangeHandler.RaisePropertyChanged(() => IsDetailsExpanded);
                }
            }
            get { return m_IsDetailsExpanded; }
        }

        public double DetailsHeight { get; set; }

        public bool HasDetails
        {
            get { return DetailsPlaceHolder.Content != null; }
        }

        public void ShowModal()
        {
            ensureVisible(true);

            ShowInTaskbar = false;

            if (ShellView != null)
            {
                ShellView.DimBackgroundWhile(() => ShowDialog());
            }
            else
            {
                ShowDialog();
            }
        }

        public void ShowFloating(Action whenDoneAction)
        {
            m_whenDoneAction = whenDoneAction;

            ensureVisible(true);

            ShowInTaskbar = true;

            Show();
        }

        private void ensureVisible(bool authorizeHorizontalAdjust)
        {
            // For some reason, WindowStartupLocation.CenterOwner doesn't work in non-modal display mode.
            WindowStartupLocation = WindowStartupLocation.Manual;

            if (Owner != null && Owner.WindowState == WindowState.Minimized)
            {
                Owner.WindowState = WindowState.Normal;
            }

            double finalLeft = Math.Max(0, (Owner == null ? 10 : Owner.Left) + ((Owner == null ? 800 : Owner.Width) - Width) / 2);
            double finalTop = Math.Max(0, (Owner == null ? 10 : Owner.Top) + ((Owner == null ? 600 : Owner.Height) - Height) / 2);

            double availableWidth = SystemParameters.WorkArea.Width; //Screen.PrimaryScreen.Bounds.Width
            double availableHeight = SystemParameters.WorkArea.Height; //Screen.PrimaryScreen.Bounds.Height

            if (Owner != null && Owner.WindowState == WindowState.Maximized)
            {
                finalLeft = Math.Max(0, (availableWidth - Width) / 2);
                finalTop = Math.Max(0, (availableHeight - Height) / 2);
            }

            double finalWidth = Math.Min(availableWidth, Width);
            double finalHeight = Math.Min(availableHeight, Height);

            double right = finalLeft + finalWidth;
            if (right > availableWidth)
            {
                double extraWidth = right - availableWidth;
                finalLeft -= extraWidth;
                if (finalLeft < 0)
                {
                    finalWidth -= (-finalLeft);
                    finalLeft = 0;
                }
            }

            double bottom = finalTop + finalHeight;
            if (bottom > availableHeight)
            {
                double extraHeight = bottom - availableHeight;
                finalTop -= extraHeight;
                if (finalTop < 0)
                {
                    finalHeight -= (-finalTop);
                    finalTop = 0;
                }
            }
            bottom = finalTop + (finalHeight + DetailsHeight);
            if (bottom > availableHeight)
            {
                double extraHeight = bottom - availableHeight;
                finalTop -= extraHeight;
                if (finalTop < 0)
                {
                    finalHeight -= (-finalTop);
                    finalTop = 0;
                }
            }
            if (authorizeHorizontalAdjust)
            {
                Left = finalLeft;
                Width = finalWidth;
            }
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

            Width = zoom * width;
            Height = zoom * height;

            DetailsHeight = zoom * detailsHeight;

            CommandDetailsCollapse.SetIconProviderDrawScale(zoom);
            CommandDetailsExpand.SetIconProviderDrawScale(zoom);

            Title = "Tobi: " + title;
            Icon = null;
            ContentPlaceHolder.Content = content;
            DetailsPlaceHolder.Content = details;
            DialogButtons = buttons;
            DefaultDialogButton = button;
            AllowEscapeAndCloseButton = allowEscapeAndCloseButton;
        }

        public PopupModalWindow(IShellView window, string title, object content,
            DialogButtonsSet buttons, DialogButton button, bool allowEscapeAndCloseButton, double width, double height)
            : this(window, title, content, buttons, button, allowEscapeAndCloseButton, width, height, null, 0)
        {
        }

        private bool m_ButtonTriggersClose = false;
        private readonly IShellView ShellView;
        private Action m_whenDoneAction;

        public bool AllowEscapeAndCloseButton
        {
            get;
            private set;
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (AllowEscapeAndCloseButton)
                {
                    m_ButtonTriggersClose = false;
                    Close();
                }
                else
                {
                    SystemSounds.Asterisk.Play();
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
                SystemSounds.Asterisk.Play();
                e.Cancel = true;
                return;
            }

            ContentPlaceHolder.Content = null;
            DetailsPlaceHolder.Content = null;

            if (m_whenDoneAction != null)
            {
                m_whenDoneAction.Invoke();
            }
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Uri iconUri = new Uri("pack://application:,,,/Tobi;component/Tobi.ico", UriKind.Absolute);
                //Uri iconUri = new Uri("Tobi.ico", UriKind.RelativeOrAbsolute);
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

        public bool AddInputBinding(InputBinding inputBinding)
        {
            InputBindings.Add(inputBinding);
            return true;
        }

        public void RemoveInputBinding(InputBinding inputBinding)
        {
            InputBindings.Remove(inputBinding);
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
