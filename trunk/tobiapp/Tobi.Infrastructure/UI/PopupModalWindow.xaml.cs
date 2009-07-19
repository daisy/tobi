using System;
using System.ComponentModel;
using System.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Tobi.Infrastructure.Commanding;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Tobi.Infrastructure.UI
{
    /// <summary>
    /// Interaction logic for PopupModalWindow.xaml
    /// </summary>
    public partial class PopupModalWindow : IInputBindingManager, INotifyPropertyChangedEx
    {
        #region INotifyPropertyChangedEx

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(PropertyChangedEventArgs e)
        {
            //m_ClassInstancePropertyHost.PropertyChanged.Invoke(m_ClassInstancePropertyHost, e);

            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        #endregion INotifyPropertyChangedEx


        public RichDelegateCommand<object> CommandDetailsExpand { get; private set; }
        public RichDelegateCommand<object> CommandDetailsCollapse { get; private set; }

        public IInputBindingManager InputBindingManager
        {
            get
            {
                return this;
            }
        }

        private PopupModalWindow(IShellPresenter presenter)
        {
            ShellPresenter = presenter;

            ClickedDialogButton = DialogButton.ESC;

            m_PropertyChangeHandler = new PropertyChangedNotifyBase();
            m_PropertyChangeHandler.InitializeDependentProperties(this);

            InitializeComponent();

            CommandDetailsExpand = new RichDelegateCommand<object>(UserInterfaceStrings.DetailsExpand,
                UserInterfaceStrings.DetailsExpand_,
                UserInterfaceStrings.DetailsExpand_KEYS,
                ShellPresenter.LoadTangoIcon("go-down"),
                obj => IsDetailsExpanded = true,
                obj => CanDetailsExpand);

            AddInputBinding(CommandDetailsExpand.KeyBinding);

            CommandDetailsCollapse = new RichDelegateCommand<object>(UserInterfaceStrings.DetailsCollapse,
                UserInterfaceStrings.DetailsCollapse_,
                UserInterfaceStrings.DetailsCollapse_KEYS,
                ShellPresenter.LoadTangoIcon("go-up"),
                obj => IsDetailsExpanded = false,
                obj => CanDetailsCollapse);

            AddInputBinding(CommandDetailsCollapse.KeyBinding);

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

                    m_PropertyChangeHandler.OnPropertyChanged(() => IsDetailsExpanded);
                }
            }
            get { return m_IsDetailsExpanded; }
        }

        public double DetailsHeight { get; set; }

        public bool HasDetails
        {
            get { return DetailsPlaceHolder.Content != null; }
        }

        [NotifyDependsOn("IsDetailsExpanded")]
        [NotifyDependsOn("HasDetails")]
        public bool CanDetailsExpand
        {
            get { return HasDetails && !IsDetailsExpanded; }
        }

        [NotifyDependsOn("IsDetailsExpanded")]
        [NotifyDependsOn("HasDetails")]
        public bool CanDetailsCollapse
        {
            get { return HasDetails && IsDetailsExpanded; }
        }

        public void ShowModal()
        {
            ensureVisible(true);

            ShellPresenter.DimBackgroundWhile(() => ShowDialog());
        }

        public void ShowFloating(Action whenDoneAction)
        {
            m_whenDoneAction = whenDoneAction;

            ensureVisible(true);

            Show();
        }

        private void ensureVisible(bool authorizeHorizontalAdjust)
        {
            // For some reason, WindowStartupLocation.CenterOwner doesn't work in non-modal display mode.
            WindowStartupLocation = WindowStartupLocation.Manual;

            double finalLeft = Math.Max(0, Owner.Left + (Owner.Width - Width) / 2);
            double finalTop = Math.Max(0, Owner.Top + (Owner.Height - Height) / 2);

            double availableWidth = SystemParameters.WorkArea.Width; //Screen.PrimaryScreen.Bounds.Width
            double availableHeight = SystemParameters.WorkArea.Height; //Screen.PrimaryScreen.Bounds.Height

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

        public PopupModalWindow(IShellPresenter presenter, string title,
            object content,
            DialogButtonsSet buttons, DialogButton button, bool allowEscapeAndCloseButton,
            double width, double height,
            object details, double detailsHeight)
            : this(presenter)
        {
            if (presenter == null)
            {
                Owner = Application.Current.MainWindow;
            }
            else { Owner = presenter.View.Window; }

            //DataContext = Owner;

            var zoom = (Double)Application.Current.Resources["MagnificationLevel"];

            Width = zoom * width;
            Height = zoom * height;

            DetailsHeight = zoom * detailsHeight;

            CommandDetailsCollapse.IconProvider.IconDrawScale = zoom;
            CommandDetailsExpand.IconProvider.IconDrawScale = zoom;

            Title = title;
            Icon = null;
            ContentPlaceHolder.Content = content;
            DetailsPlaceHolder.Content = details;
            DialogButtons = buttons;
            DefaultDialogButton = button;
            AllowEscapeAndCloseButton = allowEscapeAndCloseButton;
        }

        public PopupModalWindow(IShellPresenter window, string title, object content,
            DialogButtonsSet buttons, DialogButton button, bool allowEscapeAndCloseButton, double width, double height)
            : this(window, title, content, buttons, button, allowEscapeAndCloseButton, width, height, null, 0)
        {
        }

        private bool m_ButtonTriggersClose = false;
        private PropertyChangedNotifyBase m_PropertyChangeHandler;
        private readonly IShellPresenter ShellPresenter;
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
                return;
            }

            if (!AllowEscapeAndCloseButton)
            {
                SystemSounds.Asterisk.Play();
                e.Cancel = true;
                return;
            }

            ContentPlaceHolder.Content = null;

            if (m_whenDoneAction != null)
            {
                m_whenDoneAction.Invoke();
            }
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                //pack://application:,,,/Tobi.Infrastructure;component/tango-icons/media-playback-pause.xaml
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

            buttonToFocus.Focus();
            FocusManager.SetFocusedElement(this, buttonToFocus);
            Keyboard.Focus(buttonToFocus);
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
            m_PropertyChangeHandler.OnPropertyChanged(() => IsButtonActive_Close);
            m_PropertyChangeHandler.OnPropertyChanged(() => IsButtonActive_Apply);

            m_PropertyChangeHandler.OnPropertyChanged(() => IsButtonActive_Ok);
            m_PropertyChangeHandler.OnPropertyChanged(() => IsButtonActive_Cancel);

            m_PropertyChangeHandler.OnPropertyChanged(() => IsButtonActive_Yes);
            m_PropertyChangeHandler.OnPropertyChanged(() => IsButtonActive_No);

            m_PropertyChangeHandler.OnPropertyChanged(() => IsButtonDefault_Close);
            m_PropertyChangeHandler.OnPropertyChanged(() => IsButtonDefault_Apply);

            m_PropertyChangeHandler.OnPropertyChanged(() => IsButtonDefault_Ok);
            m_PropertyChangeHandler.OnPropertyChanged(() => IsButtonDefault_Cancel);

            m_PropertyChangeHandler.OnPropertyChanged(() => IsButtonDefault_Yes);
            m_PropertyChangeHandler.OnPropertyChanged(() => IsButtonDefault_No);
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
}
