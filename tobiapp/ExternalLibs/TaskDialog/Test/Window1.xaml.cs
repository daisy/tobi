using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Sid.Windows.Controls;

namespace Test
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window, INotifyPropertyChanged
    {
        [DllImport("user32.dll", SetLastError = false)]
        static extern IntPtr GetDesktopWindow();

        public event PropertyChangedEventHandler PropertyChanged;

        #region Fields

        private string _titleText;

        private string _headerText;
        private double _headerFontSize;
        private TaskDialogIcon _headerIcon;
        private Brush _headerBackground;
        private Brush _headerForeground;
        private string _contentText;
        private string _detailText;
        private string _footerText;
        private TaskDialogIcon _footerIcon;
        private TaskDialogButton _button;
        private TaskDialogResult _defaultResult;
        private TaskDialogSound _sound;
        private string _button1Text;
        private string _button2Text;
        private string _button3Text;

        #endregion

        #region Constructor

        public Window1()
        {
            InitializeComponent();

            TitleText = "Task Dialog Options";
            Header = "The Header text for the message box.";
            ContentText = "The Content text for the message box. This text will automatically wrap and is selectable";
            Detail = "The Detail text for the message box. This text will automatically wrap and is selectable";
            Footer = "The Footer text for the message box.";
            Button1Text = "Button1";
            Button2Text = "Button2";
            Button3Text = "Button3";

            _sound = TaskDialogSound.Beep;
            _headerBackground = Brushes.White;
            _headerForeground = Brushes.Navy;
            _headerIcon = TaskDialogIcon.Information;
            _footerIcon = TaskDialogIcon.Shield;
            _button = TaskDialogButton.Ok;
        }

        #endregion

        #region clr properties

        /// <summary>
        ///     Button1Text Property 
        /// </summary>
        public string Button1Text
        {
            get { return _button1Text; }
            set
            {
                if (_button1Text != value)
                {
                    _button1Text = value;
                    OnPropertyChanged("Button1Text");
                }
            }
        }
        /// <summary>
        ///     Button2Text Property 
        /// </summary>
        public string Button2Text
        {
            get { return _button2Text; }
            set
            {
                if (_button2Text != value)
                {
                    _button2Text = value;
                    OnPropertyChanged("Button2Text");
                }
            }
        }
        /// <summary>
        ///     Button3Text Property 
        /// </summary>
        public string Button3Text
        {
            get { return _button3Text; }
            set
            {
                if (_button3Text != value)
                {
                    _button3Text = value;
                    OnPropertyChanged("Button3Text");
                }
            }
        }

        /// <summary>
        ///     TitleText Property 
        /// </summary>
        public string TitleText
        {
            get { return _titleText; }
            set
            {
                if (_titleText != value)
                {
                    _titleText = value;
                    OnPropertyChanged("TitleText");
                }
            }
        }
        /// <summary>
        ///     HeaderContent Property 
        /// </summary>
        public string Header
        {
            get { return _headerText; }
            set
            {
                if (_headerText != value)
                {
                    _headerText = value;
                    OnPropertyChanged("Header");
                }
            }
        }

        /// <summary>
        ///     HeaderFontSize Property 
        /// </summary>
        public double HeaderFontSize
        {
            get { return _headerFontSize; }
            set
            {
                if (_headerFontSize != value)
                {
                    _headerFontSize = value;
                    OnPropertyChanged("HeaderFontSize");
                }
            }
        }
        /// <summary>
        ///     Body Property 
        /// </summary>
        public string ContentText
        {
            get { return _contentText; }
            set
            {
                if (_contentText != value)
                {
                    _contentText = value;
                    OnPropertyChanged("ContentText");
                }
            }
        }

        /// <summary>
        ///     Detail Property 
        /// </summary>
        public string Detail
        {
            get { return _detailText; }
            set
            {
                if (_detailText != value)
                {
                    _detailText = value;
                    OnPropertyChanged("Detail");
                }
            }
        }

        /// <summary>
        ///     Footer Property 
        /// </summary>
        public string Footer
        {
            get { return _footerText; }
            set
            {
                if (_footerText != value)
                {
                    _footerText = value;
                    OnPropertyChanged("Footer");
                }
            }
        }

        #endregion

        /// <summary>
        ///     Show a dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            TaskDialog dialog = new TaskDialog();

            // center in the parent window, otherwise centers on the active window
            if ((bool)checkParent.IsChecked)
                dialog.ParentWindow = this;

            dialog.TaskDialogWindow.Title = this.TitleText;
            dialog.HeaderIcon = _headerIcon;
            dialog.TaskDialogButton = _button;
            dialog.DefaultResult = _defaultResult;
            dialog.SystemSound = _sound;

            if (!nullHeader.IsChecked.Value)
                dialog.Header = _headerText;
            dialog.HeaderBackground = _headerBackground;
            dialog.HeaderForeground = _headerForeground;

            if (!nullContent.IsChecked.Value)
                dialog.Content = _contentText;

            if (!nullDetail.IsChecked.Value)
                dialog.Detail = _detailText;

            if (!nullFooter.IsChecked.Value)
                dialog.Footer = _footerText;
            dialog.FooterIcon = _footerIcon;

            if (dialog.TaskDialogButton == TaskDialogButton.Custom)
            {
                dialog.Button1Text = _button1Text;
                dialog.Button2Text = _button2Text;
                dialog.Button3Text = _button3Text;
            }

            dialog.Show();
        }


        /// <summary>
        ///     Show a dialog that simulates a UAC Dialog prompt,
        ///     the Content property is a Usercontrol that has two TaskDialogCommandButtons
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UAC1_Click(object sender, RoutedEventArgs e)
        {
            TaskDialog dialog = new TaskDialog();
            dialog.MaxWidth = 480;
            dialog.TaskDialogWindow.Title = "User Account Control";
            dialog.TaskDialogWindow.IsCloseButtonEnabled = false; // disables the close button on the window (defaults to enabled)
            dialog.SystemSound = TaskDialogSound.Exclamation;   // play a system sound

            dialog.Header = "An Unidentified program wants access to your computer";
            dialog.HeaderIcon = TaskDialogIcon.Warning;
            dialog.HeaderBackground = Brushes.Gold;
            dialog.HeaderForeground = Brushes.Black;

            // add a usercontrol that has two command buttons on it, pass in the TaskDialogWindow to the usercontrol so that it can close the dialog window
            Uac uac1 = new Uac(dialog.TaskDialogWindow);
            dialog.Content = uac1;

            // for the body,use a custom Usercontrol to display an image with the text
            dialog.Detail = @"C:\Program Files\Some program.exe";
            dialog.Footer = "User Account Control helps stop unauthorised changes to your computer";

            dialog.Show();
        }

        /// <summary>
        ///     Show a dialog that simulates a UAC Dialog prompt,
        ///     the Content property is a Usercontrol
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UAC2_Click(object sender, RoutedEventArgs e)
        {
            TaskDialog dialog = new TaskDialog();
            dialog.MaxWidth = 440;
            dialog.TopMost = InteropWindowZOrder.TopMost;
            dialog.TaskDialogWindow.Title = "User Account Control";
            dialog.HeaderIcon = TaskDialogIcon.Warning;
            dialog.SystemSound = TaskDialogSound.Exclamation;
            // set custom button captions
            dialog.TaskDialogButton = TaskDialogButton.Custom;
            dialog.Button1Text = "_Continue";
            dialog.Button2Text = "C_ancel";
            dialog.DefaultResult = TaskDialogResult.Button2;
            dialog.IsButton2Cancel = true;
            // header properties
            dialog.Header = "A program needs your permission to continue.";
            dialog.HeaderBackground = Brushes.DarkGray;
            dialog.HeaderForeground = Brushes.White;

            // for the body, use a custom Usercontrol to display an image with the text
            Uac uac = new Uac(dialog.TaskDialogWindow);
            uac.buttonAllow.Visibility = Visibility.Collapsed;
            uac.buttonCancel.Visibility = Visibility.Collapsed;
            dialog.Content = uac;
            dialog.Detail = @"C:\Program Files\Some program.exe";
            dialog.Footer = "User Account Control helps stop unauthorised changes to your computer";

            dialog.Show();
        }

        /// <summary>
        ///     Show a dialog using a static method call, this is the simplest version of the static overloads
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MessageBox1_Click(object sender, RoutedEventArgs e)
        {
            TaskDialog.Show("Static TaskDialog", "The Simplest MessageBox window possible", "just three properties set, (Header, Content and Title)");
        }

        /// <summary>
        ///     Show a dialog using a static method call, this is the most verbode of the static overloads
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MessageBox2_Click(object sender, RoutedEventArgs e)
        {
            TaskDialog.Show("Static TaskDialog",
                "A Full MessageBox window",
                "This message box uses the full static method overload.",
                "This is the most verbose method of invoking the TaskDialog without creating an instance",
                "this is the footer",
                TaskDialogButton.OkCancel,
                TaskDialogResult.Cancel,
                TaskDialogIcon.Shield,
                TaskDialogIcon.Information,
                Brushes.White,
                Brushes.Navy);
        }

        /// <summary>
        ///     Show a dialog formatted to display an Exception message
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Exception_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                File.Open("FileNotFound.txt", FileMode.Open);
            }
            catch (Exception exception)
            {
                TaskDialog.ShowException(exception);
            }

        }

        private void FileCopy_Click(object sender, RoutedEventArgs e)
        {
            IntPtr parent = GetDesktopWindow();
 
            TaskDialog dialog = new TaskDialog();
            dialog.ParentWindowHandle = parent;

            dialog.MaxWidth = 440;
            dialog.TaskDialogWindow.Title = "8 Minutes and 40 seconds remaining";
            dialog.HeaderIcon = TaskDialogIcon.None;
            dialog.TaskDialogWindow.ShowInTaskbar = true;
            // user defined button
            dialog.TaskDialogButton = TaskDialogButton.Custom;
            dialog.Button1Text = "_Cancel";
            dialog.Button2Text = "_Hide";
            dialog.DefaultResult = TaskDialogResult.Button2;

            // change the default caption for the Toggle Button
            dialog.ToggleButtonTexts = new TaskDialogToggleButtonTexts { CollapseText = "Less Information", ExpandText = "More Information" };
            // open a non-modal window
            dialog.TaskDialogWindow.IsModal = false;
            // header properties

            // listen to the TaskDialogWindow Initialized event to set the TaskDialogWIndow TopMost
            dialog.WindowInitialized +=
                delegate
                {
                    dialog.TaskDialogWindow.SetWindowTopMost(InteropWindowZOrder.TopMost);
                };

            LinearGradientBrush linearGradientBrush = new LinearGradientBrush(
                Color.FromRgb(0x17, 0x4c, 0x78),
                Color.FromRgb(0x23, 0x9b, 0x8f),
                0d);

            dialog.Header = "Copying 1 item (4.37 GB)";
            dialog.HeaderForeground = Brushes.White;
            dialog.HeaderBackground = linearGradientBrush;

            // for the body, use a custom Usercontrol to display an image with the text
            FileCopy uac = new FileCopy();
            dialog.Content = uac;

            dialog.Show();

        }
        private void DataTemplate_Click(object sender, RoutedEventArgs e)
        {
            TaskDialog dialog = new TaskDialog();
            dialog.TaskDialogWindow.Title = "User-Defined DataTemplate";
            dialog.HeaderIcon = TaskDialogIcon.Information;
            dialog.SystemSound = TaskDialogSound.Beep;

            dialog.TaskDialogButton = TaskDialogButton.Ok;

            // header
            dialog.Header = "Header Text formatted with a user defined DataTemplate";
            dialog.HeaderTemplate = (DataTemplate)this.FindResource("_customHeaderDataTemplate");

            // content
            dialog.Content = "Content Text formatted with a user defined DataTemplate";
            dialog.ContentTemplate = (DataTemplate)this.FindResource("_customContentDataTemplate");

            // Detail
            dialog.Detail = "Detail Text formatted with a user defined DataTemplate";
            dialog.DetailTemplate = (DataTemplate)this.FindResource("_customDetailDataTemplate");

            // Footer
            dialog.Footer = "Footer Text formatted with a user defined DataTemplate";
            dialog.FooterTemplate = (DataTemplate)this.FindResource("_customFooterDataTemplate");


            dialog.Show();
        }



        #region Events

        private void HeaderIcon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _headerIcon = (TaskDialogIcon)e.AddedItems[0];
        }
        private void FooterIcon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _footerIcon = (TaskDialogIcon)e.AddedItems[0];
        }


        private void HeaderBackground_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _headerBackground = BrushFromKnownColor((KnownColor)e.AddedItems[0]);
        }
        private void HeaderForeground_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _headerForeground = BrushFromKnownColor((KnownColor)e.AddedItems[0]);
        }
        private void Buttons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _button = (TaskDialogButton)e.AddedItems[0];
            if (btnButton1 == null) return;
            btnButton1.IsEnabled = _button == TaskDialogButton.Custom ? true : false;
            btnButton2.IsEnabled = _button == TaskDialogButton.Custom ? true : false;
            btnButton3.IsEnabled = _button == TaskDialogButton.Custom ? true : false;
        }
        private void DefaultResult_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _defaultResult = (TaskDialogResult)e.AddedItems[0];
        }
        private void Sound_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _sound = (TaskDialogSound)e.AddedItems[0];

        }

        #endregion
        #region INotifyPropertyChanged Members

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        #endregion

        static Brush BrushFromKnownColor(KnownColor color)
        {
            byte[] argb = BitConverter.GetBytes((uint)color);
            return new SolidColorBrush(Color.FromArgb(argb[3], argb[2], argb[1], argb[0]));
        }
    }
}
