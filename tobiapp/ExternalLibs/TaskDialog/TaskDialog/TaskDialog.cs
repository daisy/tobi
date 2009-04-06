using System;
using System.Security;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Sid.Windows.Controls
{
    /// <summary>
    ///     TaskDialog control 
    /// </summary>
    [TemplatePart(Name = "PART_Button1", Type = typeof(Button))]
    [TemplatePart(Name = "PART_Button2", Type = typeof(Button))]
    [TemplatePart(Name = "PART_Button3", Type = typeof(Button))]
    public sealed class TaskDialog : HeaderedContentControl
    {
        // raise an event when the parent window has been initialized
        public event TaskDialogEventHandler WindowInitialized;
        private InteropWindowZOrder _topMost;
        private TaskDialogResult _defaultResult;

        #region Construction

        static TaskDialog()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TaskDialog), new FrameworkPropertyMetadata(typeof(TaskDialog)));
        }

        public TaskDialog()
        {
            ToggleButtonTexts = new TaskDialogToggleButtonTexts();

            // create the default TemplateSelector's
            HeaderTemplateSelector = new HeaderDataTemplateSelector();
            ContentTemplateSelector = new ContentDataTemplateSelector();
            DetailTemplateSelector = new DetailDataTemplateSelector();
            FooterTemplateSelector = new FooterDataTemplateSelector();

            // create a host window and set it's content to the Task dialog.
            TaskDialogWindow = new TaskDialogWindow { Content = this, IsModal = true };
        }

        #endregion

        #region Overrides
        /// <summary>
        ///     OnApplyTemplate override
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            SetUpButtonCommandBindings();
        }
        #endregion

        #region Static Methods

        /// <summary>
        ///     Displays a Text based Task Dialog TaskDialogWindow with an 'Ok' Button and an Information icon
        /// </summary>
        [SecurityCritical]
        public static TaskDialogResult Show(string title, string header, string content)
        {
            return Show(title, header, content, null, null, TaskDialogButton.Ok, TaskDialogResult.Ok, TaskDialogIcon.None, TaskDialogIcon.None);
        }
        /// <summary>
        ///     Displays a Text based Task Dialog TaskDialogWindow with an 'Ok' Button and an Information icon
        /// </summary>
        [SecurityCritical]
        public static TaskDialogResult Show(Window parent, string title, string header, string content)
        {
            return Show(parent, title, header, content, null, null, TaskDialogButton.Ok, TaskDialogResult.Ok, TaskDialogIcon.None, TaskDialogIcon.None);
        }
        /// <summary>
        ///     Displays a Text based Task Dialog TaskDialogWindow with an 'Ok' Button and an Information icon
        /// </summary>
        [SecurityCritical]
        public static TaskDialogResult Show(IntPtr parent, string title, string header, string content)
        {
            return Show(parent, title, header, content, null, null, TaskDialogButton.Ok, TaskDialogResult.Ok, TaskDialogIcon.None, TaskDialogIcon.None);
        }
        /// <summary>
        ///     Displays a Text based Task Dialog TaskDialogWindow with a Header and Body text and an 'Ok' Button 
        /// </summary>
        [SecurityCritical]
        public static TaskDialogResult Show(string title, string header, string content, TaskDialogIcon icon)
        {
            return Show(title, header, content, null, null, TaskDialogButton.Ok, TaskDialogResult.Ok, icon, TaskDialogIcon.None);
        }
        /// <summary>
        ///     Displays a Text based Task Dialog TaskDialogWindow with a Header and Body text and an 'Ok' Button 
        /// </summary>
        [SecurityCritical]
        public static TaskDialogResult Show(Window parent, string title, string header, string content, TaskDialogIcon icon)
        {
            return Show(parent, title, header, content, null, null, TaskDialogButton.Ok, TaskDialogResult.Ok, icon, TaskDialogIcon.None);
        }
        /// <summary>
        ///     Displays a Text based Task Dialog TaskDialogWindow with a Header and Body text and an 'Ok' Button 
        /// </summary>
        [SecurityCritical]
        public static TaskDialogResult Show(IntPtr parent, string title, string header, string content, TaskDialogIcon icon)
        {
            return Show(parent, title, header, content, null, null, TaskDialogButton.Ok, TaskDialogResult.Ok, icon, TaskDialogIcon.None);
        }
        /// <summary>
        ///     Displays a Text based Task Dialog TaskDialogWindow with a Header, Body and footer text and an 'Ok' Button 
        /// </summary>
        [SecurityCritical]
        public static TaskDialogResult Show(string title, string header, string content, string footer, TaskDialogIcon headerIcon, TaskDialogIcon footerIcon)
        {
            return Show(title, header, content, null, footer, TaskDialogButton.Ok, TaskDialogResult.Ok, headerIcon, footerIcon);
        }
        /// <summary>
        ///     Displays a Text based Task Dialog TaskDialogWindow with a Header, Body and footer text and an 'Ok' Button 
        /// </summary>
        [SecurityCritical]
        public static TaskDialogResult Show(Window parent, string title, string header, string content, string footer, TaskDialogIcon headerIcon, TaskDialogIcon footerIcon)
        {
            return Show(parent, title, header, content, null, footer, TaskDialogButton.Ok, TaskDialogResult.Ok, headerIcon, footerIcon);
        }
        /// <summary>
        ///     Displays a Text based Task Dialog TaskDialogWindow with a Header, Body and footer text and an 'Ok' Button 
        /// </summary>
        [SecurityCritical]
        public static TaskDialogResult Show(IntPtr parent, string title, string header, string content, string footer, TaskDialogIcon headerIcon, TaskDialogIcon footerIcon)
        {
            return Show(parent, title, header, content, null, footer, TaskDialogButton.Ok, TaskDialogResult.Ok, headerIcon, footerIcon);
        }
        /// <summary>
        ///     Displays a Text based Task Dialog TaskDialogWindow
        /// </summary>
        [SecurityCritical]
        public static TaskDialogResult Show(string title, string header, string content, string detail, string footer, TaskDialogButton button, TaskDialogResult defaultResult, TaskDialogIcon headerIcon, TaskDialogIcon footerIcon)
        {
            return Show(title, header, content, detail, footer, button, defaultResult, headerIcon, footerIcon, null, Brushes.Navy);
        }
        /// <summary>
        ///     Displays a Text based Task Dialog TaskDialogWindow
        /// </summary>
        [SecurityCritical]
        public static TaskDialogResult Show(Window parent, string title, string header, string content, string detail, string footer, TaskDialogButton button, TaskDialogResult defaultResult, TaskDialogIcon headerIcon, TaskDialogIcon footerIcon)
        {
            return Show(parent, title, header, content, detail, footer, button, defaultResult, headerIcon, footerIcon, null, Brushes.Navy);
        }        /// <summary>
        ///     Displays a Text based Task Dialog TaskDialogWindow
        /// </summary>
        [SecurityCritical]
        public static TaskDialogResult Show(IntPtr parent, string title, string header, string content, string detail, string footer, TaskDialogButton button, TaskDialogResult defaultResult, TaskDialogIcon headerIcon, TaskDialogIcon footerIcon)
        {
            return Show(parent, title, header, content, detail, footer, button, defaultResult, headerIcon, footerIcon, null, Brushes.Navy);
        }
        /// <summary>
        ///     Displays a Text based Task Dialog TaskDialogWindow
        /// </summary>
        [SecurityCritical]
        public static TaskDialogResult Show(string title, string header, string content, string detail, string footer, TaskDialogButton button, TaskDialogResult defaultResult, TaskDialogIcon headerIcon, TaskDialogIcon footerIcon, Brush headerBackground, Brush headerForeground)
        {
            return Show(IntPtr.Zero, title, header, content, detail, footer, button, defaultResult, headerIcon, footerIcon, headerBackground, headerForeground);
        }

        /// <summary>
        ///     Displays a Text based Task Dialog TaskDialogWindow
        /// </summary>
        [SecurityCritical]
        public static TaskDialogResult Show(Window parent, string title, string header, string content, string detail, string footer, TaskDialogButton button, TaskDialogResult defaultResult, TaskDialogIcon headerIcon, TaskDialogIcon footerIcon, Brush headerBackground, Brush headerForeground)
        {
            return Show(new WindowInteropHelper(parent).Handle, title, header, content, detail, footer, button, defaultResult, headerIcon, footerIcon, headerBackground, headerForeground);
        }

        /// <summary>
        ///     Displays a Text based Task Dialog TaskDialogWindow
        /// </summary>
        [SecurityCritical]
        public static TaskDialogResult Show(IntPtr parent, string title, string header, string content, string detail, string footer, TaskDialogButton button, TaskDialogResult defaultResult, TaskDialogIcon headerIcon, TaskDialogIcon footerIcon, Brush headerBackground, Brush headerForeground)
        {
            TaskDialog td = new TaskDialog
            {
                TaskDialogButton = button,
                DefaultResult = defaultResult,
                HeaderIcon = headerIcon,
                Header = header,
                Content = content,
                Detail = detail,
                Footer = footer,
                FooterIcon = footerIcon,
                HeaderBackground = headerBackground,
                HeaderForeground = headerForeground
            };

            td.TaskDialogWindow.Title = title;

            switch (td.HeaderIcon)
            {
                case TaskDialogIcon.None:
                case TaskDialogIcon.Shield:
                case TaskDialogIcon.Information:
                    td.SystemSound = TaskDialogSound.Beep;
                    break;
                case TaskDialogIcon.Question:
                    td.SystemSound = TaskDialogSound.Question;
                    break;
                case TaskDialogIcon.Error:
                    td.SystemSound = TaskDialogSound.Hand;
                    break;

                case TaskDialogIcon.Warning:
                    td.SystemSound = TaskDialogSound.Exclamation;
                    break;
            }


            TaskDialogWindow window = td.TaskDialogWindow;
            window.ParentWindowHandle = parent;
            window.ShowDialog();

            return td.Result;
        }

        /// <summary>
        ///     Displays a Task Dialog TaskDialogWindow to display an Exception
        /// </summary>
        public static void ShowException(Exception exception)
        {
            ShowException("System Error", string.Format("An Unhandled {0} has occurred.", exception.GetType().Name), exception);
        }


        /// <summary>
        ///     Displays a Task Dialog TaskDialogWindow to display an Exception
        /// </summary>
        public static void ShowException(string title, Exception exception)
        {
            ShowException(title, string.Format("An Unhandled {0} has occurred.", exception.GetType().Name), exception);
        }

        /// <summary>
        ///     Displays a Task Dialog TaskDialogWindow to display an Exception
        /// </summary>
        public static void ShowException(string title, string header, Exception exception)
        {
            TaskDialog td = new TaskDialog
            {
                TaskDialogButton = TaskDialogButton.Ok,
                IsButton2Cancel = true,
                HeaderIcon = TaskDialogIcon.Error,
                Header = header,
                Content = exception.Message,
                Detail = exception.StackTrace,
                HeaderBackground = Brushes.Red,
                HeaderForeground = Brushes.White,
                SystemSound = TaskDialogSound.Hand,
                ToggleButtonTexts = new TaskDialogToggleButtonTexts { ExpandText = "Show StackTrace", CollapseText = "Hide StackTrace" }
            };
            TaskDialogWindow window = td.TaskDialogWindow;
            window.Title = title;
            window.ShowDialog();
        }


        #endregion
        #region Instance Methods

        /// <summary>
        ///     Displays a Task Dialog TaskDialogWindow
        /// </summary>
        public TaskDialogResult Show()
        {
            try
            {
                if (ParentWindow != null && ParentWindowHandle == IntPtr.Zero)
                    ParentWindowHandle = new WindowInteropHelper(ParentWindow).Handle;

                TaskDialogWindow.ParentWindow = ParentWindow;
                TaskDialogWindow.ParentWindowHandle = ParentWindowHandle;

                if (TaskDialogWindow.IsModal)
                    TaskDialogWindow.ShowDialog();
                else
                    TaskDialogWindow.Show();
            }
            catch (InvalidOperationException)
            {
                // this exception happens when we try to call ShowDialog on a window that has been shown then closed.
                throw new TaskDialogException(
                    "Cannot set Visibility or call Show after window has closed\n" +
                    "You must create a new instance of the taskDialog class for each call to the 'Show' method"
                    );
            }

            return Result;
        }


        internal void OnWindowLoaded()
        {
            Button b1 = this.GetTemplateChild("PART_Button1") as Button;
            if (b1 == null) b1 = FindName("PART_Button1") as Button;
            //if (b1 != null) b1.PreviewKeyDown += OnButtonPreviewKeyDown;
            Button b2 = this.GetTemplateChild("PART_Button2") as Button;
            if (b1 == null) b1 = FindName("PART_Button2") as Button;
            //if (b2 != null) b2.PreviewKeyDown += OnButtonPreviewKeyDown;
            Button b3 = this.GetTemplateChild("PART_Button3") as Button;
            if (b1 == null) b1 = FindName("PART_Button3") as Button;
            //if (b3 != null) b3.PreviewKeyDown += OnButtonPreviewKeyDown;

            switch (DefaultResult)
            {
                case TaskDialogResult.Button1:
                case TaskDialogResult.Ok:
                case TaskDialogResult.Yes:
                    if (Button1Visibility == Visibility.Visible && b1 != null)
                    {
                        FocusManager.SetFocusedElement(TaskDialogWindow, b1);
                        Keyboard.Focus(b1);
                        b1.Focus();
                    }
                    break;

                case TaskDialogResult.Button2:
                case TaskDialogResult.No:
                    if (Button2Visibility == Visibility.Visible && b2 != null)
                    {
                        FocusManager.SetFocusedElement(TaskDialogWindow, b2);
                        Keyboard.Focus(b2);
                        b2.Focus();
                    }
                    break;

                case TaskDialogResult.Cancel:
                    if (TaskDialogButton == TaskDialogButton.OkCancel
                        && Button2Visibility == Visibility.Visible && b2 != null)
                    {
                        FocusManager.SetFocusedElement(TaskDialogWindow, b2);
                        Keyboard.Focus(b2);
                        b2.Focus();
                    }
                    else if (Button3Visibility == Visibility.Visible && b3 != null)
                    {
                        FocusManager.SetFocusedElement(TaskDialogWindow, b3);
                        Keyboard.Focus(b3);
                        b3.Focus();
                    }
                    break;

                case TaskDialogResult.Button3:
                    if (Button3Visibility == Visibility.Visible && b3 != null)
                    {
                        FocusManager.SetFocusedElement(TaskDialogWindow, b3);
                        Keyboard.Focus(b3);
                        b3.Focus();
                    }
                    break;
            }

            FocusNavigationDirection direction = FocusNavigationDirection.Left;
            TraversalRequest request = new TraversalRequest(direction);
            UIElement focused = Keyboard.FocusedElement as UIElement;
            if (focused != null)
            {
                focused.MoveFocus(request);
            }
            FocusNavigationDirection direction_ = FocusNavigationDirection.Right;
            TraversalRequest request_ = new TraversalRequest(direction_);
            focused = Keyboard.FocusedElement as UIElement;
            if (focused != null)
            {
                focused.MoveFocus(request_);
            }
        }

        /// <summary>
        ///     Raise the WindowInitialized event
        /// </summary>
        internal void OnWindowInitialized()
        {
            TaskDialogEventHandler eh;
            lock (this)
                eh = WindowInitialized;

            if (eh != null)
                eh(this, new TaskDialogEventArgs { Window = this.TaskDialogWindow });
        }

        /// <summary>
        ///     set the focus to the default button when the window is first shown
        /// </summary>
        /// <remarks>
        ///     Nasty Hack to set focus as setting the focus before the window is shown doesn't work
        /// </remarks>
        /// <param name="element"></param>
        private static void Focus(UIElement element)
        {
            ThreadPool.QueueUserWorkItem(
                delegate(object foo)
                {
                    int count = 10; // loop a maximum of 10 times
                    UIElement uiElement = (UIElement)foo;
                    IInputElement inputElement = null;
                    while (count-- > 0)
                    {
                        Thread.Sleep(10);   // wait a 1/10 of a second
                        uiElement.Dispatcher.Invoke(DispatcherPriority.Normal,
                                             (Action)delegate
                                                      {
                                                          // set focus on the UI thread
                                                          uiElement.Focus();
                                                          inputElement = Keyboard.Focus(uiElement);
                                                      });
                        if (ReferenceEquals(uiElement, inputElement))
                            break;

                    }
                }, element);
        }

        #endregion

        #region CLR Properties
        /// <summary>
        ///     the TaskDialogWindow for the control
        /// </summary>
        public TaskDialogWindow TaskDialogWindow { get; private set; }
        /// <summary>
        ///     Get or Set the TaskDialogWindow that is the Parent of the TaskDialog
        /// </summary>
        public Window ParentWindow { get; set; }
        /// <summary>
        ///     Get or Set the TaskDialogWindow Handle that is the Parent of the TaskDialog
        /// </summary>
        public IntPtr ParentWindowHandle { get; set; }
        /// <summary>
        ///     The result of a button click on the Task Dialog
        /// </summary>
        public TaskDialogResult Result { get; private set; }
        /// <summary>
        ///     Get or Set the SystemSound property
        /// </summary>
        public TaskDialogSound SystemSound { get; set; }

        /// <summary>
        ///     Get or Set the TopMost property
        /// </summary>
        public InteropWindowZOrder TopMost
        {
            get { return _topMost; }
            set
            {
                _topMost = value;
                if (TaskDialogWindow != null)
                    TaskDialogWindow.SetWindowTopMost(value);
            }
        }

        #region DefaultResult
        /// <summary>
        /// Gets or sets the DefaultResult property.
        /// </summary>
        public TaskDialogResult DefaultResult
        {
            get { return _defaultResult; }
            set
            {
                if (_defaultResult != value)
                {
                    _defaultResult = value;
                    OnDefaultResultChanged(value);
                }
            }
        }
        private void OnDefaultResultChanged(TaskDialogResult result)
        {
            switch (result)
            {
                case TaskDialogResult.None:
                    IsButton1Default = false;
                    IsButton2Default = false;
                    IsButton3Default = false;
                    break;

                case TaskDialogResult.Ok:
                case TaskDialogResult.Yes:
                    if (TaskDialogButton == TaskDialogButton.Custom)
                        return;

                    IsButton1Default = true;
                    break;

                case TaskDialogResult.Cancel:
                case TaskDialogResult.No:
                    if (TaskDialogButton == TaskDialogButton.Custom)
                        return;

                    if (TaskDialogButton == TaskDialogButton.YesNoCancel)
                        IsButton3Default = true;
                    else
                        IsButton2Default = true;
                    break;

                case TaskDialogResult.Button1:
                    if (TaskDialogButton != TaskDialogButton.Custom)
                        return;

                    IsButton1Default = true;
                    break;

                case TaskDialogResult.Button2:
                    if (TaskDialogButton != TaskDialogButton.Custom)
                        return;

                    IsButton2Default = true;
                    break;

                case TaskDialogResult.Button3:
                    if (TaskDialogButton != TaskDialogButton.Custom)
                        return;

                    IsButton3Default = true;
                    break;
            }
        }

        #endregion
        #endregion
        #region CommandBindings

        /// <summary>
        ///     Setup the command bindings for the 3 buttons
        /// </summary>
        private void SetUpButtonCommandBindings()
        {
            // Button1
            CommandBinding b = new CommandBinding(TaskDialogCommands.Button1);
            b.CanExecute += ((sender, e) => e.CanExecute = IsButton1Enabled);
            b.Executed += ((sender, e) =>
            {
                switch (TaskDialogButton)
                {
                    case TaskDialogButton.Ok:
                    case TaskDialogButton.OkCancel:
                        Result = TaskDialogResult.Ok;
                        break;

                    case TaskDialogButton.YesNo:
                    case TaskDialogButton.YesNoCancel:
                        Result = TaskDialogResult.Yes;
                        break;

                    case TaskDialogButton.Custom:
                        Result = TaskDialogResult.Button1;
                        break;
                }
                TaskDialogWindow.Close();
            });

            this.CommandBindings.Add(b);


            // Button2
            b = new CommandBinding(TaskDialogCommands.Button2);
            b.CanExecute += ((sender, e) => e.CanExecute = IsButton2Enabled);
            b.Executed += ((sender, e) =>
            {
                switch (TaskDialogButton)
                {
                    case TaskDialogButton.OkCancel:
                        Result = TaskDialogResult.Cancel;
                        break;

                    case TaskDialogButton.YesNo:
                    case TaskDialogButton.YesNoCancel:
                        Result = TaskDialogResult.No;
                        break;

                    case TaskDialogButton.Custom:
                        Result = TaskDialogResult.Button2;
                        break;
                }
                TaskDialogWindow.Close();
            });

            this.CommandBindings.Add(b);


            // Button3
            b = new CommandBinding(TaskDialogCommands.Button3);
            b.CanExecute += ((sender, e) => e.CanExecute = IsButton3Enabled);
            b.Executed += ((sender, e) =>
            {
                switch (TaskDialogButton)
                {
                    case TaskDialogButton.YesNoCancel:
                        Result = TaskDialogResult.Cancel;
                        break;

                    case TaskDialogButton.Custom:
                        Result = TaskDialogResult.Button3;
                        break;
                }
                TaskDialogWindow.Close();
            });

            this.CommandBindings.Add(b);
        }
        #endregion

        #region Dependency properties

        #region Header Properties
        protected override void OnHeaderTemplateSelectorChanged(DataTemplateSelector oldHeaderTemplateSelector, DataTemplateSelector newHeaderTemplateSelector)
        {
            base.OnHeaderTemplateSelectorChanged(oldHeaderTemplateSelector, newHeaderTemplateSelector);
            if (newHeaderTemplateSelector == null)
                HeaderTemplateSelector = new HeaderDataTemplateSelector();  // reset the selector
        }
        #region HeaderBackground

        /// <summary>
        /// HeaderBackground Dependency Property
        /// </summary>
        public static readonly DependencyProperty HeaderBackgroundProperty =
            DependencyProperty.Register("HeaderBackground", typeof(Brush), typeof(TaskDialog),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the HeaderBackground property 
        /// </summary>
        public Brush HeaderBackground
        {
            get { return (Brush)GetValue(HeaderBackgroundProperty); }
            set { SetValue(HeaderBackgroundProperty, value); }
        }

        #endregion
        #region HeaderForeground

        /// <summary>
        /// HeaderForeground Dependency Property
        /// </summary>
        public static readonly DependencyProperty HeaderForegroundProperty =
            DependencyProperty.Register("HeaderForeground", typeof(Brush), typeof(TaskDialog),
                new FrameworkPropertyMetadata(Brushes.Navy));

        /// <summary>
        /// Gets or sets the HeaderForeground property 
        /// </summary>
        public Brush HeaderForeground
        {
            get { return (Brush)GetValue(HeaderForegroundProperty); }
            set { SetValue(HeaderForegroundProperty, value); }
        }

        #endregion
        #region HeaderIcon

        /// <summary>
        /// HeaderIcon Dependency Property
        /// </summary>
        public static readonly DependencyProperty HeaderIconProperty =
            DependencyProperty.Register("HeaderIcon", typeof(TaskDialogIcon), typeof(TaskDialog),
                new FrameworkPropertyMetadata(TaskDialogIcon.None, new PropertyChangedCallback(OnHeaderIconChanged)));

        /// <summary>
        /// Gets or sets the HeaderIcon property.  This dependency property 
        /// indicates the icon to display on the task dialog.
        /// </summary>
        public TaskDialogIcon HeaderIcon
        {
            get { return (TaskDialogIcon)GetValue(HeaderIconProperty); }
            set { SetValue(HeaderIconProperty, value); }
        }

        /// <summary>
        /// Handles changes to the HeaderIcon property.
        /// </summary>
        private static void OnHeaderIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TaskDialog)d).OnHeaderIconChanged(e);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the HeaderIcon property.
        /// </summary>
        private void OnHeaderIconChanged(DependencyPropertyChangedEventArgs e)
        {
            string uri = null;
            switch ((TaskDialogIcon)e.NewValue)
            {
                case TaskDialogIcon.None:
                    SetHeaderIconImage(null);
                    HasHeaderIcon = false;
                    return;

                case TaskDialogIcon.Information:
                    uri = "pack://application:,,,/Sid.TaskDialog;Component/Images/Information.png";
                    break;
                case TaskDialogIcon.Warning:
                    uri = "pack://application:,,,/Sid.TaskDialog;Component/Images/Warning.png";
                    break;
                case TaskDialogIcon.Error:
                    uri = "pack://application:,,,/Sid.TaskDialog;Component/Images/Stop.png";
                    break;
                case TaskDialogIcon.Question:
                    uri = "pack://application:,,,/Sid.TaskDialog;Component/Images/Question.png";
                    break;
                case TaskDialogIcon.Shield:
                    uri = "pack://application:,,,/Sid.TaskDialog;Component/Images/Shield.png";
                    break;
            }
            if (uri != null)
            {
                BitmapImage bitmapImage = new BitmapImage(new Uri(uri));
                HasHeaderIcon = true;
                SetHeaderIconImage(bitmapImage);
            }
        }

        #region HeaderIconImage

        /// <summary>
        /// HeaderIconImage Read-Only Dependency Property
        /// </summary>
        private static readonly DependencyPropertyKey HeaderIconImagePropertyKey
            = DependencyProperty.RegisterReadOnly("HeaderIconImage", typeof(ImageSource), typeof(TaskDialog),
                new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty HeaderIconImageProperty
            = HeaderIconImagePropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the HeaderIconImage property. 
        /// </summary>
        public ImageSource HeaderIconImage
        {
            get { return (ImageSource)GetValue(HeaderIconImageProperty); }
        }

        /// <summary>
        /// Provides a secure method for setting the HeaderIconImage property.  
        /// </summary>
        /// <param name="value">The new value for the property.</param>
        private void SetHeaderIconImage(ImageSource value)
        {
            SetValue(HeaderIconImagePropertyKey, value);
        }

        #endregion
        #endregion
        #region HasHeaderIcon

        /// <summary>
        /// HasHeaderIcon Dependency Property
        /// </summary>
        public static readonly DependencyProperty HasHeaderIconProperty =
            DependencyProperty.Register("HasHeaderIcon", typeof(bool), typeof(TaskDialog),
                new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets or sets the HasHeaderIcon property. 
        /// </summary>
        public bool HasHeaderIcon
        {
            get { return (bool)GetValue(HasHeaderIconProperty); }
            set { SetValue(HasHeaderIconProperty, value); }
        }

        #endregion
        #endregion
        #region Content Properties
        protected override void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)
        {
            base.OnContentTemplateSelectorChanged(oldContentTemplateSelector, newContentTemplateSelector);
            if (newContentTemplateSelector == null)
                ContentTemplateSelector = new ContentDataTemplateSelector();    // reset the selector
        }
        #endregion
        #region Detail Properties
        #region Detail

        /// <summary>
        /// Detail Dependency Property
        /// </summary>
        public static readonly DependencyProperty DetailProperty =
            DependencyProperty.Register("Detail", typeof(object), typeof(TaskDialog),
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnDetailChanged)));

        private static void OnDetailChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TaskDialog)d).HasDetail = (e.NewValue != null);
        }

        /// <summary>
        /// Gets or sets the Detail property.  This dependency property 
        /// indicates the Detail text to render.
        /// </summary>
        public object Detail
        {
            get { return GetValue(DetailProperty); }
            set { SetValue(DetailProperty, value); }
        }

        #endregion
        #region DetailTemplate

        /// <summary>
        /// DetailTemplate Dependency Property
        /// </summary>
        public static readonly DependencyProperty DetailTemplateProperty =
            DependencyProperty.Register("DetailTemplate", typeof(DataTemplate), typeof(TaskDialog),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the DetailTemplate property.  This dependency property 
        /// indicates the Datatemplate for the Detail property.
        /// </summary>
        public DataTemplate DetailTemplate
        {
            get { return (DataTemplate)GetValue(DetailTemplateProperty); }
            set { SetValue(DetailTemplateProperty, value); }
        }

        #endregion
        #region DetailTemplateSelector

        /// <summary>
        /// DetailTemplateSelector Dependency Property
        /// </summary>
        public static readonly DependencyProperty DetailTemplateSelectorProperty =
            DependencyProperty.Register("DetailTemplateSelector", typeof(DataTemplateSelector), typeof(TaskDialog),
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnDetailTemplateSelectorChanged)));

        private static void OnDetailTemplateSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TaskDialog)d).OnDetailTemplateSelectorChanged((DataTemplateSelector)e.NewValue);
        }

        private void OnDetailTemplateSelectorChanged(DataTemplateSelector newDetailTemplateSelector)
        {
            if (newDetailTemplateSelector == null)
                DetailTemplateSelector = new DetailDataTemplateSelector(); // reset the selector
        }

        /// <summary>
        /// Gets or sets the DetailTemplateSelector property.  This dependency property 
        /// indicates the DataTemplateSelector for the Detail property.
        /// </summary>
        public DataTemplateSelector DetailTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(DetailTemplateSelectorProperty); }
            set { SetValue(DetailTemplateSelectorProperty, value); }
        }

        #endregion
        #region HasDetail

        /// <summary>
        /// HasDetail Dependency Property
        /// </summary>
        public static readonly DependencyProperty HasDetailProperty =
            DependencyProperty.Register("HasDetail", typeof(bool), typeof(TaskDialog),
                new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets or sets the HasDetail property.  This dependency property 
        /// indicateswhether the Task Dialog can expand to show more detail.
        /// </summary>
        public bool HasDetail
        {
            get { return (bool)GetValue(HasDetailProperty); }
            set { SetValue(HasDetailProperty, value); }
        }

        #endregion
        #endregion
        #region Footer Properties
        #region Footer

        /// <summary>
        /// Footer Dependency Property
        /// </summary>
        public static readonly DependencyProperty FooterProperty =
            DependencyProperty.Register("Footer", typeof(object), typeof(TaskDialog),
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnFooterChanged)));

        private static void OnFooterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TaskDialog)d).SetHasFooter(e.NewValue != null);
        }

        /// <summary>
        /// Gets or sets the Footer property.  This dependency property 
        /// indicates the Footer text to render.
        /// </summary>
        public object Footer
        {
            get { return GetValue(FooterProperty); }
            set { SetValue(FooterProperty, value); }
        }

        #endregion
        #region FooterTemplate

        /// <summary>
        /// FooterTemplate Dependency Property
        /// </summary>
        public static readonly DependencyProperty FooterTemplateProperty =
            DependencyProperty.Register("FooterTemplate", typeof(DataTemplate), typeof(TaskDialog),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the FooterTemplate property.  This dependency property 
        /// indicates the Datatemplate for the Footer.
        /// </summary>
        public DataTemplate FooterTemplate
        {
            get { return (DataTemplate)GetValue(FooterTemplateProperty); }
            set { SetValue(FooterTemplateProperty, value); }
        }

        #endregion
        #region FooterTemplateSelector

        /// <summary>
        /// FooterTemplateSelector Dependency Property
        /// </summary>
        public static readonly DependencyProperty FooterTemplateSelectorProperty =
            DependencyProperty.Register("FooterTemplateSelector", typeof(DataTemplateSelector), typeof(TaskDialog),
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnFooterTemplateSelectorChanged)));


        private static void OnFooterTemplateSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TaskDialog)d).OnFooterTemplateSelectorChanged((DataTemplateSelector)e.NewValue);
        }

        private void OnFooterTemplateSelectorChanged(DataTemplateSelector newFooterTemplateSelector)
        {
            if (newFooterTemplateSelector == null)
                FooterTemplateSelector = new FooterDataTemplateSelector(); // reset the selector
        }
        /// <summary>
        /// Gets or sets the FooterTemplateSelector property.  This dependency property 
        /// indicates the DataTmplateSelector for the Footer property.
        /// </summary>
        public DataTemplateSelector FooterTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(FooterTemplateSelectorProperty); }
            set { SetValue(FooterTemplateSelectorProperty, value); }
        }

        #endregion


        #region FooterIcon

        /// <summary>
        /// Footer HeaderIcon Dependency Property
        /// </summary>
        public static readonly DependencyProperty FooterIconProperty =
            DependencyProperty.Register("FooterIcon", typeof(TaskDialogIcon), typeof(TaskDialog),
                new FrameworkPropertyMetadata(TaskDialogIcon.None, new PropertyChangedCallback(OnFooterIconChanged)));

        /// <summary>
        /// Gets or sets the Footer HeaderIcon property.  This dependency property 
        /// indicates the Footer icon to display on the task dialog.
        /// </summary>
        public TaskDialogIcon FooterIcon
        {
            get { return (TaskDialogIcon)GetValue(FooterIconProperty); }
            set { SetValue(FooterIconProperty, value); }
        }

        /// <summary>
        /// Handles changes to the HeaderIcon property.
        /// </summary>
        private static void OnFooterIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TaskDialog)d).OnFooterIconChanged(e);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the Footer HeaderIcon property.
        /// </summary>
        private void OnFooterIconChanged(DependencyPropertyChangedEventArgs e)
        {
            string uri = null;
            switch ((TaskDialogIcon)e.NewValue)
            {
                case TaskDialogIcon.None:
                    SetFooterIconImage(null);
                    return;
                case TaskDialogIcon.Information:
                    uri = "pack://application:,,,/Sid.TaskDialog;Component/Images/Information.png";
                    break;
                case TaskDialogIcon.Warning:
                    uri = "pack://application:,,,/Sid.TaskDialog;Component/Images/Warning.png";
                    break;
                case TaskDialogIcon.Error:
                    uri = "pack://application:,,,/Sid.TaskDialog;Component/Images/Stop.png";
                    break;
                case TaskDialogIcon.Question:
                    uri = "pack://application:,,,/Sid.TaskDialog;Component/Images/Question.png";
                    break;
                case TaskDialogIcon.Shield:
                    uri = "pack://application:,,,/Sid.TaskDialog;Component/Images/Shield.png";
                    break;
            }
            if (uri != null)
            {
                BitmapImage bitmapImage = new BitmapImage(new Uri(uri));
                SetFooterIconImage(bitmapImage);
            }
        }

        #region FooterIconImage

        /// <summary>
        /// FooterIconImage Read-Only Dependency Property
        /// </summary>
        private static readonly DependencyPropertyKey FooterIconImagePropertyKey
            = DependencyProperty.RegisterReadOnly("FooterIconImage", typeof(ImageSource), typeof(TaskDialog),
                new FrameworkPropertyMetadata((ImageSource)null));

        public static readonly DependencyProperty FooterIconImageProperty
            = FooterIconImagePropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the FooterIconImage property.
        /// </summary>
        public ImageSource FooterIconImage
        {
            get { return (ImageSource)GetValue(FooterIconImageProperty); }
        }

        /// <summary>
        /// Provides a secure method for setting the FooterIconImage property.  
        /// </summary>
        /// <param name="value">The new value for the property.</param>
        private void SetFooterIconImage(ImageSource value)
        {
            SetValue(FooterIconImagePropertyKey, value);
        }

        #endregion

        #endregion
        #region HasFooter

        /// <summary>
        /// HasFooter Read-Only Dependency Property
        /// </summary>
        private static readonly DependencyPropertyKey HasFooterPropertyKey
            = DependencyProperty.RegisterReadOnly("HasFooter", typeof(bool), typeof(TaskDialog),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty HasFooterProperty
            = HasFooterPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the HasFooter property.
        /// </summary>
        public bool HasFooter
        {
            get { return (bool)GetValue(HasFooterProperty); }
        }

        /// <summary>
        /// Provides a secure method for setting the HasFooter property.  
        /// This dependency property indicates ....
        /// </summary>
        /// <param name="value">The new value for the property.</param>
        private void SetHasFooter(bool value)
        {
            SetValue(HasFooterPropertyKey, value);
        }

        #endregion
        #endregion

        #region Button Properties

        #region Button1Text

        /// <summary>
        /// Button1Text Dependency Property
        /// </summary>
        public static readonly DependencyProperty Button1TextProperty =
            DependencyProperty.Register("Button1Text", typeof(string), typeof(TaskDialog),
                new FrameworkPropertyMetadata("", OnButton1TextChanged, OnCoerceButton1Text));

        /// <summary>
        /// Gets or sets the Button1Text property.  This dependency property 
        /// indicates the text for Button 1.
        /// </summary>
        public string Button1Text
        {
            get { return (string)GetValue(Button1TextProperty); }
            set { SetValue(Button1TextProperty, value); }
        }

        /// <summary>
        /// Handles changes to the Button1Text property.
        /// </summary>
        private static void OnButton1TextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TaskDialog)d).OnButton1TextChanged();
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the Button1Text property.
        /// </summary>
        private void OnButton1TextChanged()
        {
            if (TaskDialogButton == TaskDialogButton.Custom)
            {
                if (Button1Text.Length > 0)
                    SetButton1Visibility(Visibility.Visible);
                else
                    SetButton1Visibility(Visibility.Collapsed);
                return;
            }
        }

        private static object OnCoerceButton1Text(DependencyObject d, object baseValue)
        {
            TaskDialog td = (TaskDialog)d;

            switch (td.TaskDialogButton)
            {
                case TaskDialogButton.Ok:
                case TaskDialogButton.OkCancel:
                    return "_Ok";

                case TaskDialogButton.YesNo:
                case TaskDialogButton.YesNoCancel:
                    return "_Yes";

                default:
                    return baseValue;
            }
        }

        #endregion
        #region Button2Text

        /// <summary>
        /// Button2Text Dependency Property
        /// </summary>
        public static readonly DependencyProperty Button2TextProperty =
            DependencyProperty.Register("Button2Text", typeof(string), typeof(TaskDialog),
                new FrameworkPropertyMetadata("", OnButton2TextChanged, OnCoerceButton2Text));

        /// <summary>
        /// Gets or sets the Button2Text property.  This dependency property 
        /// indicates the text for Button 1.
        /// </summary>
        public string Button2Text
        {
            get { return (string)GetValue(Button2TextProperty); }
            set { SetValue(Button2TextProperty, value); }
        }

        /// <summary>
        /// Handles changes to the Button2Text property.
        /// </summary>
        private static void OnButton2TextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TaskDialog)d).OnButton2TextChanged();
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the Button2Text property.
        /// </summary>
        private void OnButton2TextChanged()
        {
            if (TaskDialogButton == TaskDialogButton.Custom)
            {
                if (Button2Text.Length > 0)
                {
                    SetButton2Visibility(Visibility.Visible);
                    // make sure button1 is visible
                    if (Button1Text.Length == 0)
                        Button1Text = "???";
                }
                else
                    SetButton2Visibility(Visibility.Collapsed);
                return;
            }
        }

        private static object OnCoerceButton2Text(DependencyObject d, object baseValue)
        {
            TaskDialog td = (TaskDialog)d;

            switch (td.TaskDialogButton)
            {
                case TaskDialogButton.YesNo:
                case TaskDialogButton.YesNoCancel:
                    return "_No";

                case TaskDialogButton.OkCancel:
                    return "_Cancel";

                default:
                    return baseValue;
            }
        }

        #endregion
        #region Button3Text

        /// <summary>
        /// Button3Text Dependency Property
        /// </summary>
        public static readonly DependencyProperty Button3TextProperty =
            DependencyProperty.Register("Button3Text", typeof(string), typeof(TaskDialog),
                new FrameworkPropertyMetadata("", OnButton3TextChanged, OnCoerceButton3Text));


        /// <summary>
        /// Gets or sets the Button3Text property.  This dependency property 
        /// indicates the text for Button 1.
        /// </summary>
        public string Button3Text
        {
            get { return (string)GetValue(Button3TextProperty); }
            set { SetValue(Button3TextProperty, value); }
        }

        /// <summary>
        /// Handles changes to the Button3Text property.
        /// </summary>
        private static void OnButton3TextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TaskDialog)d).OnButton3TextChanged();
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the Button3Text property.
        /// </summary>
        private void OnButton3TextChanged()
        {
            if (TaskDialogButton == TaskDialogButton.Custom)
            {
                if (Button3Text.Length > 0)
                {
                    SetButton3Visibility(Visibility.Visible);
                    // make sure all the buttons are visible
                    if (Button1Text.Length == 0)
                        Button1Text = "???";

                    if (Button2Text.Length == 0)
                        Button2Text = "???";
                }
                else
                    SetButton3Visibility(Visibility.Collapsed);
                return;
            }
        }

        private static object OnCoerceButton3Text(DependencyObject d, object baseValue)
        {
            TaskDialog td = (TaskDialog)d;

            switch (td.TaskDialogButton)
            {
                case TaskDialogButton.YesNoCancel:
                    return "_Cancel";

                default:
                    return baseValue;
            }
        }

        #endregion

        #region Button1Visibility

        /// <summary>
        /// Button1Visibility Read-Only Dependency Property
        /// </summary>
        private static readonly DependencyPropertyKey Button1VisibilityPropertyKey
            = DependencyProperty.RegisterReadOnly("Button1Visibility", typeof(Visibility), typeof(TaskDialog),
                new FrameworkPropertyMetadata(Visibility.Collapsed));

        public static readonly DependencyProperty Button1VisibilityProperty
            = Button1VisibilityPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the Button1Visibility property.  This dependency property 
        /// indicates the visibility of button1.
        /// </summary>
        public Visibility Button1Visibility
        {
            get { return (Visibility)GetValue(Button1VisibilityProperty); }
        }

        /// <summary>
        /// Provides a secure method for setting the Button1Visibility property.  
        /// This dependency property indicates the visibility of button1.
        /// </summary>
        /// <param name="value">The new value for the property.</param>
        private void SetButton1Visibility(Visibility value)
        {
            SetValue(Button1VisibilityPropertyKey, value);
        }

        #endregion
        #region Button2Visibility

        /// <summary>
        /// Button2Visibility Read-Only Dependency Property
        /// </summary>
        private static readonly DependencyPropertyKey Button2VisibilityPropertyKey
            = DependencyProperty.RegisterReadOnly("Button2Visibility", typeof(Visibility), typeof(TaskDialog),
                new FrameworkPropertyMetadata(Visibility.Collapsed));

        public static readonly DependencyProperty Button2VisibilityProperty
            = Button2VisibilityPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the Button2Visibility property.  This dependency property 
        /// indicates the visibility of button2.
        /// </summary>
        public Visibility Button2Visibility
        {
            get { return (Visibility)GetValue(Button2VisibilityProperty); }
        }

        /// <summary>
        /// Provides a secure method for setting the Button2Visibility property.  
        /// This dependency property indicates the visibility of button2.
        /// </summary>
        /// <param name="value">The new value for the property.</param>
        private void SetButton2Visibility(Visibility value)
        {
            SetValue(Button2VisibilityPropertyKey, value);
        }

        #endregion
        #region Button3Visibility

        /// <summary>
        /// Button3Visibility Read-Only Dependency Property
        /// </summary>
        private static readonly DependencyPropertyKey Button3VisibilityPropertyKey
            = DependencyProperty.RegisterReadOnly("Button3Visibility", typeof(Visibility), typeof(TaskDialog),
                new FrameworkPropertyMetadata(Visibility.Collapsed));

        public static readonly DependencyProperty Button3VisibilityProperty
            = Button3VisibilityPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the Button3Visibility property.  This dependency property 
        /// indicates the visibility of Button3.
        /// </summary>
        public Visibility Button3Visibility
        {
            get { return (Visibility)GetValue(Button3VisibilityProperty); }
        }

        /// <summary>
        /// Provides a secure method for setting the Button3Visibility property.  
        /// This dependency property indicates the visibility of Button3.
        /// </summary>
        /// <param name="value">The new value for the property.</param>
        private void SetButton3Visibility(Visibility value)
        {
            SetValue(Button3VisibilityPropertyKey, value);
        }

        #endregion

        #region IsButton1Cancel

        /// <summary>
        /// IsButton1Cancel Dependency Property
        /// </summary>
        internal static readonly DependencyProperty IsButton1CancelProperty =
            DependencyProperty.Register("IsButton1Cancel", typeof(bool), typeof(TaskDialog),
                new FrameworkPropertyMetadata(false,
                    null, CoerceIsButton1CancelValue));

        /// <summary>
        /// Gets or sets the IsButton1Cancel property.  This dependency property 
        /// indicates if button 1 is the default button.
        /// </summary>
        public bool IsButton1Cancel
        {
            get { return (bool)GetValue(IsButton1CancelProperty); }
            set { SetValue(IsButton1CancelProperty, value); }
        }

        /// <summary>
        /// Coerces the IsButton1Cancel value.
        /// </summary>
        private static object CoerceIsButton1CancelValue(DependencyObject d, object value)
        {
            TaskDialog td = (TaskDialog)d;
            bool isdef = (bool)value;

            if (isdef)
            {
                td.IsButton2Cancel = false;
                td.IsButton3Cancel = false;
            }
            return value;
        }

        #endregion
        #region IsButton2Cancel

        /// <summary>
        /// IsButton2Cancel Dependency Property
        /// </summary>
        internal static readonly DependencyProperty IsButton2CancelProperty =
            DependencyProperty.Register("IsButton2Cancel", typeof(bool), typeof(TaskDialog),
                new FrameworkPropertyMetadata(false,
                    null, CoerceIsButton2CancelValue));

        /// <summary>
        /// Gets or sets the IsButton2Cancel property.  This dependency property 
        /// indicates if button 2 is the default button.
        /// </summary>
        public bool IsButton2Cancel
        {
            get { return (bool)GetValue(IsButton2CancelProperty); }
            set { SetValue(IsButton2CancelProperty, value); }
        }

        /// <summary>
        /// Coerces the IsButton2Cancel value.
        /// </summary>
        private static object CoerceIsButton2CancelValue(DependencyObject d, object value)
        {
            TaskDialog td = (TaskDialog)d;
            bool isdef = (bool)value;

            if (isdef)
            {
                td.IsButton1Cancel = false;
                td.IsButton3Cancel = false;
            }
            return value;
        }

        #endregion
        #region IsButton3Cancel

        /// <summary>
        /// IsButton3Cancel Dependency Property
        /// </summary>
        internal static readonly DependencyProperty IsButton3CancelProperty =
            DependencyProperty.Register("IsButton3Cancel", typeof(bool), typeof(TaskDialog),
                new FrameworkPropertyMetadata(false,
                    null, CoerceIsButton3CancelValue));

        /// <summary>
        /// Gets or sets the IsButton3Cancel property.  This dependency property 
        /// indicates if button 3 is the default button.
        /// </summary>
        public bool IsButton3Cancel
        {
            get { return (bool)GetValue(IsButton3CancelProperty); }
            set { SetValue(IsButton3CancelProperty, value); }
        }

        /// <summary>
        /// Coerces the IsButton3Cancel value.
        /// </summary>
        private static object CoerceIsButton3CancelValue(DependencyObject d, object value)
        {
            TaskDialog td = (TaskDialog)d;
            bool isdef = (bool)value;

            if (isdef)
            {
                td.IsButton1Cancel = false;
                td.IsButton2Cancel = false;
            }
            return value;
        }

        #endregion
        #region IsButton1Default

        /// <summary>
        /// IsButton1Default Dependency Property
        /// </summary>
        internal static readonly DependencyProperty IsButton1DefaultProperty =
            DependencyProperty.Register("IsButton1Default", typeof(bool), typeof(TaskDialog),
                new FrameworkPropertyMetadata(false,
                    null, CoerceIsButton1DefaultValue));

        /// <summary>
        /// Gets or sets the IsButton1Default property.  This dependency property 
        /// indicates if button 1 is the default button.
        /// </summary>
        internal bool IsButton1Default
        {
            get { return (bool)GetValue(IsButton1DefaultProperty); }
            set { SetValue(IsButton1DefaultProperty, value); }
        }

        /// <summary>
        /// Coerces the IsButton1Default value.
        /// </summary>
        private static object CoerceIsButton1DefaultValue(DependencyObject d, object value)
        {
            TaskDialog td = (TaskDialog)d;
            bool isdef = (bool)value;

            if (isdef)
            {
                td.IsButton2Default = false;
                td.IsButton3Default = false;
            }
            return value;
        }

        #endregion
        #region IsButton2Default

        /// <summary>
        /// IsButton2Default Dependency Property
        /// </summary>
        internal static readonly DependencyProperty IsButton2DefaultProperty =
            DependencyProperty.Register("IsButton2Default", typeof(bool), typeof(TaskDialog),
                new FrameworkPropertyMetadata(false,
                    null, CoerceIsButton2DefaultValue));

        /// <summary>
        /// Gets or sets the IsButton2Default property.  This dependency property 
        /// indicates if button 2 is the default button.
        /// </summary>
        internal bool IsButton2Default
        {
            get { return (bool)GetValue(IsButton2DefaultProperty); }
            set { SetValue(IsButton2DefaultProperty, value); }
        }

        /// <summary>
        /// Coerces the IsButton2Default value.
        /// </summary>
        private static object CoerceIsButton2DefaultValue(DependencyObject d, object value)
        {
            TaskDialog td = (TaskDialog)d;
            bool isdef = (bool)value;

            if (isdef)
            {
                td.IsButton1Default = false;
                td.IsButton3Default = false;
            }
            return value;
        }

        #endregion
        #region IsButton3Default

        /// <summary>
        /// IsButton3Default Dependency Property
        /// </summary>
        internal static readonly DependencyProperty IsButton3DefaultProperty =
            DependencyProperty.Register("IsButton3Default", typeof(bool), typeof(TaskDialog),
                new FrameworkPropertyMetadata(false,
                    null, CoerceIsButton3DefaultValue));

        /// <summary>
        /// Gets or sets the IsButton3Default property.  This dependency property 
        /// indicates if button 3 is the default button.
        /// </summary>
        internal bool IsButton3Default
        {
            get { return (bool)GetValue(IsButton3DefaultProperty); }
            set { SetValue(IsButton3DefaultProperty, value); }
        }

        /// <summary>
        /// Coerces the IsButton3Default value.
        /// </summary>
        private static object CoerceIsButton3DefaultValue(DependencyObject d, object value)
        {
            TaskDialog td = (TaskDialog)d;
            bool isdef = (bool)value;

            if (isdef)
            {
                td.IsButton1Default = false;
                td.IsButton2Default = false;
            }
            return value;
        }

        #endregion

        #region IsButton1Enabled

        /// <summary>
        /// IsButton1Enabled Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsButton1EnabledProperty =
            DependencyProperty.Register("IsButton1Enabled", typeof(bool), typeof(TaskDialog),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets the IsButton1Enabled property.  This dependency property 
        /// indicates the enabled state of button 1.
        /// </summary>
        public bool IsButton1Enabled
        {
            get { return (bool)GetValue(IsButton1EnabledProperty); }
            set { SetValue(IsButton1EnabledProperty, value); }
        }

        #endregion
        #region IsButton2Enabled

        /// <summary>
        /// IsButton2Enabled Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsButton2EnabledProperty =
            DependencyProperty.Register("IsButton2Enabled", typeof(bool), typeof(TaskDialog),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets the IsButton1Enabled property.  This dependency property 
        /// indicates the enabled state of button 2.
        /// </summary>
        public bool IsButton2Enabled
        {
            get { return (bool)GetValue(IsButton2EnabledProperty); }
            set { SetValue(IsButton2EnabledProperty, value); }
        }

        #endregion
        #region IsButton3Enabled

        /// <summary>
        /// IsButton3Enabled Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsButton3EnabledProperty =
            DependencyProperty.Register("IsButton3Enabled", typeof(bool), typeof(TaskDialog),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets the IsButton1Enabled property.  This dependency property 
        /// indicates the enabled state of button 3.
        /// </summary>
        public bool IsButton3Enabled
        {
            get { return (bool)GetValue(IsButton3EnabledProperty); }
            set { SetValue(IsButton3EnabledProperty, value); }
        }

        #endregion

        #endregion
        #region TaskDialogButton

        /// <summary>
        /// TaskDialogButton Dependency Property
        /// </summary>
        public static readonly DependencyProperty TaskDialogButtonsProperty =
            DependencyProperty.Register("TaskDialogButton", typeof(TaskDialogButton), typeof(TaskDialog),
                new FrameworkPropertyMetadata(TaskDialogButton.None,
                    new PropertyChangedCallback(OnTaskDialogButtonsChanged)));

        /// <summary>
        /// Gets or sets the TaskDialogButton property.  This dependency property 
        /// indicates the Buttons to show on the Task Dialog.
        /// </summary>
        public TaskDialogButton TaskDialogButton
        {
            get { return (TaskDialogButton)GetValue(TaskDialogButtonsProperty); }
            set { SetValue(TaskDialogButtonsProperty, value); }
        }

        /// <summary>
        /// Handles changes to the TaskDialogButton property.
        /// </summary>
        private static void OnTaskDialogButtonsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TaskDialog)d).OnTaskDialogButtonsChanged(e);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the TaskDialogButton property.
        /// </summary>
        private void OnTaskDialogButtonsChanged(DependencyPropertyChangedEventArgs e)
        {
            Result = TaskDialogResult.None;
            switch ((TaskDialogButton)e.NewValue)
            {
                case TaskDialogButton.Custom:
                    SetButton1Visibility(Button1Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed);
                    SetButton2Visibility(Button2Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed);
                    SetButton3Visibility(Button3Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed);
                    if (Button1Text.Length > 0)
                        DefaultResult = TaskDialogResult.Button1;
                    else if (Button2Text.Length > 0)
                        DefaultResult = TaskDialogResult.Button2;
                    else if (Button3Text.Length > 0)
                        DefaultResult = TaskDialogResult.Button3;
                    break;

                case TaskDialogButton.None:
                    SetButton1Visibility(Visibility.Collapsed);
                    SetButton2Visibility(Visibility.Collapsed);
                    SetButton3Visibility(Visibility.Collapsed);
                    break;

                case TaskDialogButton.Ok:
                    Button1Text = "_Ok";
                    SetButton1Visibility(Visibility.Visible);
                    SetButton2Visibility(Visibility.Collapsed);
                    SetButton3Visibility(Visibility.Collapsed);
                    DefaultResult = TaskDialogResult.Ok;
                    break;

                case TaskDialogButton.OkCancel:
                    Button1Text = "_Ok";
                    Button2Text = "_Cancel";
                    SetButton1Visibility(Visibility.Visible);
                    SetButton2Visibility(Visibility.Visible);
                    SetButton3Visibility(Visibility.Collapsed);
                    DefaultResult = TaskDialogResult.Cancel;
                    break;

                case TaskDialogButton.YesNo:
                    Button1Text = "_Yes";
                    Button2Text = "_No";
                    SetButton1Visibility(Visibility.Visible);
                    SetButton2Visibility(Visibility.Visible);
                    SetButton3Visibility(Visibility.Collapsed);
                    DefaultResult = TaskDialogResult.No;
                    break;

                case TaskDialogButton.YesNoCancel:
                    Button1Text = "_Yes";
                    Button2Text = "_No";
                    Button3Text = "_Cancel";
                    SetButton1Visibility(Visibility.Visible);
                    SetButton2Visibility(Visibility.Visible);
                    SetButton3Visibility(Visibility.Visible);
                    DefaultResult = TaskDialogResult.Cancel;
                    break;
            }
        }

        #endregion

        #region IsExpanded

        /// <summary>
        /// IsExpanded Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register("IsExpanded", typeof(bool), typeof(TaskDialog),
                new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsExpandedChanged)));

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TaskDialog td = (TaskDialog)d;
            if (td.ToggleButtonTexts == null) return;
            td.ToggleButtonText = (bool)e.NewValue ? td.ToggleButtonTexts.CollapseText : td.ToggleButtonTexts.ExpandText;
        }

        /// <summary>
        /// Gets or sets the IsExpanded property.  This dependency property 
        /// indicates the state of teh Collapsable text region.
        /// </summary>
        public bool IsExpanded
        {
            get { return (bool)GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        #endregion
        #region ToggleButtonText

        /// <summary>
        /// ToggleButtonText Dependency Property
        /// </summary>
        internal static readonly DependencyProperty ToggleButtonTextProperty =
            DependencyProperty.Register("ToggleButtonText", typeof(string), typeof(TaskDialog),
                new FrameworkPropertyMetadata(null));


        /// <summary>
        /// Gets or sets the ToggleButtonText property. 
        /// </summary>
        internal string ToggleButtonText
        {
            get { return (string)GetValue(ToggleButtonTextProperty); }
            set { SetValue(ToggleButtonTextProperty, value); }
        }

        #endregion
        #region ToggleButtonTexts

        /// <summary>
        /// ToggleButtonTexts Dependency Property
        /// </summary>
        public static readonly DependencyProperty ToggleButtonTextsProperty =
            DependencyProperty.Register("ToggleButtonTexts", typeof(TaskDialogToggleButtonTexts), typeof(TaskDialog),
                new FrameworkPropertyMetadata(null,
                    new PropertyChangedCallback(OnToggleButtonTextsChanged)));

        /// <summary>
        /// Gets or sets the ToggleButtonTexts property.  This dependency property 
        /// indicates the text captions for the toggle button when expanded and collapsed.
        /// </summary>
        public TaskDialogToggleButtonTexts ToggleButtonTexts
        {
            get { return (TaskDialogToggleButtonTexts)GetValue(ToggleButtonTextsProperty); }
            set { SetValue(ToggleButtonTextsProperty, value); }
        }

        /// <summary>
        /// Handles changes to the ToggleButtonTexts property.
        /// </summary>
        private static void OnToggleButtonTextsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TaskDialog)d).OnToggleButtonTextsChanged(e);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the ToggleButtonTexts property.
        /// </summary>
        private void OnToggleButtonTextsChanged(DependencyPropertyChangedEventArgs e)
        {
            TaskDialogToggleButtonTexts t = (TaskDialogToggleButtonTexts)e.NewValue;
            ToggleButtonText = IsExpanded ? t.CollapseText : t.ExpandText;
        }

        #endregion

        #endregion

        public void OnClosing()
        {
            if (TaskDialogWindow.IsCloseButtonEnabled)
            {
                if (Result == TaskDialogResult.None)
                    Result = TaskDialogResult.Cancel;
            }
        }

        public bool OnEscape()
        {
            if (IsButton1Cancel && Button1Visibility == Visibility.Visible
                || IsButton2Cancel && Button2Visibility == Visibility.Visible
                || IsButton3Cancel && Button3Visibility == Visibility.Visible
                || !TaskDialogWindow.IsCloseButtonEnabled)
            {
                return false;
            }
            
            TaskDialogWindow.Close();
            return true;
        }
    }
}
