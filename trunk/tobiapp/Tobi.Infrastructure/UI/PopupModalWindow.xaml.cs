using System;
using System.ComponentModel;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Tobi.Infrastructure.Onyx.Reflection;

namespace Tobi.Infrastructure.UI
{
    /// <summary>
    /// Interaction logic for PopupModalWindow.xaml
    /// </summary>
    public partial class PopupModalWindow : INotifyPropertyChanged
    {
        public PopupModalWindow()
        {
            InitializeComponent();
        }

        public PopupModalWindow(Window window, string title, object content,
            DialogButtonsSet buttons, DialogButton button, bool allowEscapeAndCloseButton)
            : this()
        {
            Owner = window;
            Title = title;
            Icon = null;
            ContentPlaceHolder.Content = content;
            DialogButtons = buttons;
            DefaultDialogButton = button;
            AllowEscapeAndCloseButton = allowEscapeAndCloseButton;
        }

        private bool m_ButtonTriggersClose = false;

        public bool AllowEscapeAndCloseButton
        {
            get; private set;
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
            if (m_ButtonTriggersClose) return;

            if (!AllowEscapeAndCloseButton)
            {
                SystemSounds.Asterisk.Play();
                e.Cancel = true;
                return;
            }

            ClickedDialogButton = DialogButton.ESC;
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
                return DialogButtons == DialogButtonsSet.Close;
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
                return DialogButtons == DialogButtonsSet.OkApplyClose
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
            OnPropertyChanged(() => IsButtonActive_Close);
            OnPropertyChanged(() => IsButtonActive_Apply);

            OnPropertyChanged(() => IsButtonActive_Ok);
            OnPropertyChanged(() => IsButtonActive_Cancel);

            OnPropertyChanged(() => IsButtonActive_Yes);
            OnPropertyChanged(() => IsButtonActive_No);

            OnPropertyChanged(() => IsButtonDefault_Close);
            OnPropertyChanged(() => IsButtonDefault_Apply);

            OnPropertyChanged(() => IsButtonDefault_Ok);
            OnPropertyChanged(() => IsButtonDefault_Cancel);

            OnPropertyChanged(() => IsButtonDefault_Yes);
            OnPropertyChanged(() => IsButtonDefault_No);
        }

        public new void Show()
        {
            ShowDialog();
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            //PropertyChanged.Invoke(this, e);

            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        protected void OnPropertyChanged<T>(System.Linq.Expressions.Expression<Func<T>> expression)
        {
            OnPropertyChanged(Reflect.GetProperty(expression).Name);
        }

        #endregion INotifyPropertyChanged
        
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
    }
}
