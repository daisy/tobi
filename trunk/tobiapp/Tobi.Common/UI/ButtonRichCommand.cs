using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;

namespace Tobi.Common.UI
{

    /// <summary>
    /// //////////////////////////////////////////
    /// </summary>
    public class ButtonRichCommand : Button
    {
        //public ButtonRichCommand()
        //{
        //    PreviewKeyDown += OnPreviewKeyDownUp_;
        //    PreviewKeyUp += OnPreviewKeyDownUp_;
        //}

        //protected void OnPreviewKeyDownUp_(object sender, KeyEventArgs e)
        //{
        //    if (e.Key == Key.Space)
        //    {
        //        //RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        //        e.Handled = true;
        //    }
        //}

        public static readonly DependencyProperty RichCommandProperty =
            DependencyProperty.Register("RichCommand",
                                        typeof(RichDelegateCommand),
                                        typeof(ButtonRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnRichCommandChanged)));

        private static void OnRichCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = d as ButtonRichCommand;
            if (button == null)
            {
                return;
            }
            var command = e.NewValue as RichDelegateCommand;
            if (command == null)
            {
                return;
            }

            ConfigureButtonFromCommand(button, command, button.ShowTextLabel);
        }

        public static readonly DependencyProperty ShowTextLabelProperty =
            DependencyProperty.Register("ShowTextLabel",
                                        typeof(bool),
                                        typeof(ButtonRichCommand),
                                        new PropertyMetadata(false));

        public bool ShowTextLabel
        {
            get
            {
                return (bool)GetValue(ShowTextLabelProperty);
            }
            set
            {
                SetValue(ShowTextLabelProperty, value);
            }
        }

        public static void SetRichCommand(RichDelegateCommand command, ButtonBase button, bool showTextLabel, EventHandler dataChangedEventCallback)
        {
            if (button.Command == command)
                return;

            if (button.Command != null
                && button.Command is RichDelegateCommand
                && ((RichDelegateCommand)button.Command).DataChangedHasHandlers)
            {
                ((RichDelegateCommand)button.Command).DataChanged -= dataChangedEventCallback;
            }

            button.Command = command;

            RefreshButtonFromItsRichCommand(button, showTextLabel);

            command.DataChanged += dataChangedEventCallback;
        }

        public void SetRichCommand(RichDelegateCommand command)
        {
            SetRichCommand(command, this, ShowTextLabel, OnCommandDataChanged);

            //if (Command == command)
            //    return;

            //if (Command != null
            //    && Command is RichDelegateCommand
            //    && ((RichDelegateCommand)Command).DataChangedHasHandlers)
            //{
            //    ((RichDelegateCommand)Command).DataChanged -= OnCommandDataChanged;
            //}

            //Command = command;

            //RefreshButtonFromItsRichCommand(this, ShowTextLabel);

            //command.DataChanged += OnCommandDataChanged;
        }

        private void OnCommandDataChanged(object sender, EventArgs e)
        {
            var command = sender as RichDelegateCommand;
            if (command == null)
                return;

            if (command != Command)
            {
#if DEBUG
                Debugger.Break();
#endif
                return;
            }

            RefreshButtonFromItsRichCommand(this, ShowTextLabel);
        }

        public static void ConfigureButtonFromCommand(ButtonBase button, RichDelegateCommand command, bool showTextLabel)
        {
            if (button is ButtonRichCommand)
            {
                ((ButtonRichCommand)button).SetRichCommand(command);
            }
            else if (button is TwoStateButtonRichCommand)
            {
                ((TwoStateButtonRichCommand)button).SetRichCommand(command);
            }
            else if (button is RepeatButtonRichCommand)
            {
                ((RepeatButtonRichCommand)button).SetRichCommand(command);
            }
            else
            {
                button.Command = command;
                RefreshButtonFromItsRichCommand(button, showTextLabel);
#if DEBUG
                Debugger.Break();
#endif
            }
        }

        public bool UseSmallerIcon { get; set; }

        public static void RefreshButtonFromItsRichCommand(ButtonBase button, bool showTextLabel)
        {
            var command = button.Command as RichDelegateCommand;
            if (command == null)
            {
#if DEBUG
                Debugger.Break();
#endif
                return;
            }

            button.ToolTip = command.LongDescription +
                             (!String.IsNullOrEmpty(command.KeyGestureText) ? " " + command.KeyGestureText + " " : "");

            button.SetValue(AutomationProperties.NameProperty, UserInterfaceStrings.EscapeMnemonic(command.ShortDescription) + " / " + button.ToolTip);
            //button.SetValue(AutomationProperties.HelpTextProperty, command.ShortDescription);

            if (command.HasIcon && (!showTextLabel || String.IsNullOrEmpty(command.ShortDescription)))
            {
                var iconProvider = command.IconProviderNotShared;

                //button.Content = image;
                iconProvider.IconMargin_Medium = new Thickness(2, 2, 2, 2);

                var richButt = button as ButtonRichCommand;

                var binding = new Binding
                                  {
                                      Mode = BindingMode.OneWay,
                                      Source = iconProvider,
                                      Path = new PropertyPath(
                                          richButt != null && richButt.UseSmallerIcon ? PropertyChangedNotifyBase.GetMemberName(() => iconProvider.IconSmall) : PropertyChangedNotifyBase.GetMemberName(() => iconProvider.IconMedium)
                                          )
                                  };

                var expr = button.SetBinding(Button.ContentProperty, binding);
            }
            else
            {
                if (button.Tag is ImageAndTextPlaceholder)
                {
                    object currentImageContent = ((ImageAndTextPlaceholder)button.Tag).m_ImageHost.Content;
                    if (currentImageContent is Image)
                    {
                        var image = currentImageContent as Image;
                        ((ImageAndTextPlaceholder)button.Tag).m_Command.IconProviderDispose(image);
                    }

                    if (command.HasIcon)
                    {
                        var iconProvider = command.IconProviderNotShared;

                        var binding = new Binding
                                          {
                                              Mode = BindingMode.OneWay,
                                              Source = iconProvider,
                                              Path =
                                                  new PropertyPath(
                                                  PropertyChangedNotifyBase.GetMemberName(
                                                      () => iconProvider.IconMedium))
                                          };
                        var bindingExpressionBase_ =
                            ((ImageAndTextPlaceholder)button.Tag).m_ImageHost.SetBinding(
                                ContentControl.ContentProperty, binding);
                    }

                    ((ImageAndTextPlaceholder)button.Tag).m_TextHost.Content = command.ShortDescription;
                    button.ToolTip = command.LongDescription;

                    button.SetValue(AutomationProperties.NameProperty, UserInterfaceStrings.EscapeMnemonic(command.ShortDescription) + " / " + button.ToolTip);
                    //button.SetValue(AutomationProperties.HelpTextProperty, command.ShortDescription);

                    ((ImageAndTextPlaceholder)button.Tag).m_Command = command;
                }
                else
                {
                    button.Content = null;

                    var panel = new StackPanel
                                    {
                                        Orientation = Orientation.Horizontal
                                    };

                    var imageHost = new ContentControl { Focusable = false };

                    if (command.HasIcon)
                    {
                        var iconProvider = command.IconProviderNotShared;

                        //Image image = command.IconProvider.IconMedium;
                        iconProvider.IconMargin_Medium = new Thickness(2, 2, 2, 2);

                        var binding = new Binding
                                          {
                                              Mode = BindingMode.OneWay,
                                              Source = iconProvider,
                                              Path =
                                                  new PropertyPath(
                                                  PropertyChangedNotifyBase.GetMemberName(
                                                      () => iconProvider.IconMedium))
                                          };

                        var bindingExpressionBase = imageHost.SetBinding(ContentControl.ContentProperty, binding);
                    }

                    panel.Children.Add(imageHost);

                    var tb = new Label
                                 {
                                     VerticalAlignment = VerticalAlignment.Center,
                                     Content = command.ShortDescription,
                                     //Margin = new Thickness(8, 0, 0, 0)
                                 };

                    //tb.Content = new Run(UserInterfaceStrings.EscapeMnemonic(command.ShortDescription));

                    panel.Children.Add(tb);
                    button.Content = panel;

                    button.ToolTip = command.LongDescription;

                    button.SetValue(AutomationProperties.NameProperty, UserInterfaceStrings.EscapeMnemonic(command.ShortDescription) + " / " + button.ToolTip);
                    //button.SetValue(AutomationProperties.HelpTextProperty, command.ShortDescription);

                    button.Tag = new ImageAndTextPlaceholder(imageHost, tb)
                                     {
                                         m_Command = command
                                     };
                }
            }
        }

        private sealed class ImageAndTextPlaceholder
        {
            public ImageAndTextPlaceholder(ContentControl imageHost, Label textHost)
            {
                m_ImageHost = imageHost;
                m_TextHost = textHost;
            }

            public readonly ContentControl m_ImageHost;
            public readonly Label m_TextHost;

            public RichDelegateCommand m_Command;
        }

        public RichDelegateCommand RichCommand
        {
            get
            {
                return (RichDelegateCommand)GetValue(RichCommandProperty);
            }
            set
            {
                SetValue(RichCommandProperty, value);
            }
        }
    }

    /// <summary>
    /// //////////////////////////////////////////
    /// </summary>
    //public class ToggleButtonRichCommand : ToggleButton
    //{
    //    public static readonly DependencyProperty RichCommandProperty =
    //        DependencyProperty.Register("RichCommand",
    //                                    typeof(RichDelegateCommand),
    //                                    typeof(ToggleButtonRichCommand),
    //                                    new PropertyMetadata(new PropertyChangedCallback(OnRichCommandChanged)));

    //    private static void OnRichCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //    {
    //        var button = d as ToggleButtonRichCommand;
    //        if (button == null)
    //        {
    //            return;
    //        }
    //        var command = e.NewValue as RichDelegateCommand;
    //        if (command == null)
    //        {
    //            return;
    //        }

    //        ButtonRichCommand.ConfigureButtonFromCommand(button, command, button.ShowTextLabel);
    //    }

    //    public static readonly DependencyProperty ShowTextLabelProperty =
    //        DependencyProperty.Register("ShowTextLabel",
    //                                    typeof(bool),
    //                                    typeof(ToggleButtonRichCommand),
    //                                    new PropertyMetadata(false));

    //    public bool ShowTextLabel
    //    {
    //        get
    //        {
    //            return (bool)GetValue(ShowTextLabelProperty);
    //        }
    //        set
    //        {
    //            SetValue(ShowTextLabelProperty, value);
    //        }
    //    }
    //    public RichDelegateCommand RichCommand
    //    {
    //        get
    //        {
    //            return (RichDelegateCommand)GetValue(RichCommandProperty);
    //        }
    //        set
    //        {
    //            SetValue(RichCommandProperty, value);
    //        }
    //    }
    //}

    /// <summary>
    /// //////////////////////////////////////////
    /// </summary>
    public class RepeatButtonRichCommand : RepeatButton
    {
        public static readonly DependencyProperty RichCommandProperty =
            DependencyProperty.Register("RichCommand",
                                        typeof(RichDelegateCommand),
                                        typeof(RepeatButtonRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnRichCommandChanged)));

        private static void OnRichCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = d as RepeatButtonRichCommand;
            if (button == null)
            {
                return;
            }
            var command = e.NewValue as RichDelegateCommand;
            if (command == null)
            {
                return;
            }

            ButtonRichCommand.ConfigureButtonFromCommand(button, command, button.ShowTextLabel);
        }

        public static readonly DependencyProperty ShowTextLabelProperty =
            DependencyProperty.Register("ShowTextLabel",
                                        typeof(bool),
                                        typeof(RepeatButtonRichCommand),
                                        new PropertyMetadata(false));

        public bool ShowTextLabel
        {
            get
            {
                return (bool)GetValue(ShowTextLabelProperty);
            }
            set
            {
                SetValue(ShowTextLabelProperty, value);
            }
        }
        public RichDelegateCommand RichCommand
        {
            get
            {
                return (RichDelegateCommand)GetValue(RichCommandProperty);
            }
            set
            {
                SetValue(RichCommandProperty, value);
            }
        }

        public void SetRichCommand(RichDelegateCommand command)
        {
            ButtonRichCommand.SetRichCommand(command, this, ShowTextLabel, OnCommandDataChanged);
        }

        private void OnCommandDataChanged(object sender, EventArgs e)
        {
            var command = sender as RichDelegateCommand;
            if (command == null)
                return;

            if (command != Command)
            {
#if DEBUG
                Debugger.Break();
#endif
                return;
            }

            ButtonRichCommand.RefreshButtonFromItsRichCommand(this, ShowTextLabel);
        }
    }

    /// <summary>
    /// //////////////////////////////////////////
    /// </summary>
    //public class TwoStateToggleButtonRichCommand : ToggleButton
    //{
    //    public static readonly DependencyProperty InputBindingManagerProperty =
    //        DependencyProperty.Register("InputBindingManager",
    //                                    typeof(IInputBindingManager),
    //                                    typeof(TwoStateToggleButtonRichCommand),
    //                                    new PropertyMetadata(new PropertyChangedCallback(OnInputBindingManagerChanged)));

    //    private static void OnInputBindingManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //    {
    //        ;
    //    }

    //    public IInputBindingManager InputBindingManager
    //    {
    //        get
    //        {
    //            return (IInputBindingManager)GetValue(InputBindingManagerProperty);
    //        }
    //        set
    //        {
    //            SetValue(InputBindingManagerProperty, value);
    //        }
    //    }


    //    public static readonly DependencyProperty RichCommandOneProperty =
    //        DependencyProperty.Register("RichCommandOne",
    //                                    typeof(RichDelegateCommand),
    //                                    typeof(TwoStateToggleButtonRichCommand),
    //                                    new PropertyMetadata(new PropertyChangedCallback(OnRichCommandOneChanged)));

    //    private static void OnRichCommandOneChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //    {
    //        //ButtonRichCommand.OnRichCommandChanged(d, e);
    //    }

    //    public RichDelegateCommand RichCommandOne
    //    {
    //        get
    //        {
    //            return (RichDelegateCommand)GetValue(RichCommandOneProperty);
    //        }
    //        set
    //        {
    //            SetValue(RichCommandOneProperty, value);
    //        }
    //    }

    //    public static readonly DependencyProperty RichCommandTwoProperty =
    //        DependencyProperty.Register("RichCommandTwo",
    //                                    typeof(RichDelegateCommand),
    //                                    typeof(TwoStateToggleButtonRichCommand),
    //                                    new PropertyMetadata(new PropertyChangedCallback(OnRichCommandTwoChanged)));

    //    private static void OnRichCommandTwoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //    {
    //        //ButtonRichCommand.OnRichCommandChanged(d, e);
    //    }

    //    public RichDelegateCommand RichCommandTwo
    //    {
    //        get
    //        {
    //            return (RichDelegateCommand)GetValue(RichCommandTwoProperty);
    //        }
    //        set
    //        {
    //            SetValue(RichCommandTwoProperty, value);
    //        }
    //    }

    //    public static readonly DependencyProperty ShowTextLabelProperty =
    //        DependencyProperty.Register("ShowTextLabel",
    //                                    typeof(bool),
    //                                    typeof(TwoStateToggleButtonRichCommand),
    //                                    new PropertyMetadata(false));

    //    public bool ShowTextLabel
    //    {
    //        get
    //        {
    //            return (bool)GetValue(ShowTextLabelProperty);
    //        }
    //        set
    //        {
    //            SetValue(ShowTextLabelProperty, value);
    //        }
    //    }
    //    public static readonly DependencyProperty RichCommandActiveProperty =
    //        DependencyProperty.Register("RichCommandActive",
    //                                    typeof(Boolean),
    //                                    typeof(TwoStateToggleButtonRichCommand),
    //                                    new PropertyMetadata(true, OnRichCommandActiveChanged));

    //    private static void OnRichCommandActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //    {
    //        var button = d as TwoStateToggleButtonRichCommand;
    //        if (button == null)
    //        {
    //            return;
    //        }
    //        var choice = (Boolean)e.NewValue;

    //        RichDelegateCommand command = button.RichCommandOne;

    //        if (command.KeyGesture == null && button.RichCommandTwo.KeyGesture != null)
    //        {
    //            command.KeyGestureText = button.RichCommandTwo.KeyGestureText;
    //        }

    //        if (button.InputBindingManager != null
    //            && command.KeyGesture != null
    //                && command.KeyGesture.Equals(button.RichCommandTwo.KeyGesture))
    //        {
    //            button.InputBindingManager.RemoveInputBinding(button.RichCommandTwo.KeyBinding);
    //            button.InputBindingManager.AddInputBinding(command.KeyBinding);
    //        }

    //        if (!choice)
    //        {
    //            command = button.RichCommandTwo;

    //            if (command.KeyGesture == null && button.RichCommandOne.KeyGesture != null)
    //            {
    //                command.KeyGestureText = button.RichCommandOne.KeyGestureText;
    //            }

    //            if (button.InputBindingManager != null
    //               && command.KeyGesture != null
    //               && command.KeyGesture.Equals(button.RichCommandOne.KeyGesture))
    //            {
    //                button.InputBindingManager.RemoveInputBinding(button.RichCommandOne.KeyBinding);
    //                button.InputBindingManager.AddInputBinding(command.KeyBinding);
    //            }
    //        }

    //        ButtonRichCommand.ConfigureButtonFromCommand(button, command, button.ShowTextLabel);
    //    }

    //    /// <summary>
    //    /// True => RichCommandOne (default one)
    //    /// False => RichCommandTwo (alternative one)
    //    /// </summary>
    //    public Boolean RichCommandActive
    //    {
    //        get
    //        {
    //            return (Boolean)GetValue(RichCommandActiveProperty);
    //        }
    //        set
    //        {
    //            SetValue(RichCommandActiveProperty, value);
    //        }
    //    }
    //}

    /// <summary>
    /// //////////////////////////////////////////
    /// </summary>
    public class TwoStateButtonRichCommand : Button
    {
        public static readonly DependencyProperty InputBindingManagerProperty =
            DependencyProperty.Register("InputBindingManager",
                                        typeof(IInputBindingManager),
                                        typeof(TwoStateButtonRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnInputBindingManagerChanged)));

        private static void OnInputBindingManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ;
        }

        public IInputBindingManager InputBindingManager
        {
            get
            {
                return (IInputBindingManager)GetValue(InputBindingManagerProperty);
            }
            set
            {
                SetValue(InputBindingManagerProperty, value);
            }
        }


        public static readonly DependencyProperty RichCommandOneProperty =
            DependencyProperty.Register("RichCommandOne",
                                        typeof(RichDelegateCommand),
                                        typeof(TwoStateButtonRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnRichCommandOneChanged)));

        private static void OnRichCommandOneChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //ButtonRichCommand.OnRichCommandChanged(d, e);
        }

        public RichDelegateCommand RichCommandOne
        {
            get
            {
                return (RichDelegateCommand)GetValue(RichCommandOneProperty);
            }
            set
            {
                SetValue(RichCommandOneProperty, value);
            }
        }

        public static readonly DependencyProperty RichCommandTwoProperty =
            DependencyProperty.Register("RichCommandTwo",
                                        typeof(RichDelegateCommand),
                                        typeof(TwoStateButtonRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnRichCommandTwoChanged)));

        private static void OnRichCommandTwoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //ButtonRichCommand.OnRichCommandChanged(d, e);
        }

        public RichDelegateCommand RichCommandTwo
        {
            get
            {
                return (RichDelegateCommand)GetValue(RichCommandTwoProperty);
            }
            set
            {
                SetValue(RichCommandTwoProperty, value);
            }
        }

        public static readonly DependencyProperty ShowTextLabelProperty =
            DependencyProperty.Register("ShowTextLabel",
                                        typeof(bool),
                                        typeof(TwoStateButtonRichCommand),
                                        new PropertyMetadata(false));

        public bool ShowTextLabel
        {
            get
            {
                return (bool)GetValue(ShowTextLabelProperty);
            }
            set
            {
                SetValue(ShowTextLabelProperty, value);
            }
        }

        public static readonly DependencyProperty RichCommandActiveProperty =
            DependencyProperty.Register("RichCommandActive",
                                        typeof(Boolean),
                                        typeof(TwoStateButtonRichCommand),
                                        new PropertyMetadata(true, OnRichCommandActiveChanged));

        public static void OnRichCommandActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = d as TwoStateButtonRichCommand;
            if (button == null)
            {
                return;
            }
            var choice = (Boolean)e.NewValue;

            //RichDelegateCommand command = button.RichCommandOne;

            //if (command.KeyGesture == null && button.RichCommandTwo.KeyGesture != null)
            //{
            //    command.KeyGestureText = button.RichCommandTwo.KeyGestureText;
            //}


            if (button.InputBindingManager != null)
            {
                if (choice)
                {
                    if (true
                        //&& KeyGestureString.AreEqual(command.KeyGesture, button.RichCommandTwo.KeyGesture)
                        //&& command.KeyGesture.Equals(button.RichCommandTwo.KeyGesture)
                        )
                    {
                        if (button.RichCommandTwo.KeyGesture != null)
                            button.InputBindingManager.RemoveInputBinding(button.RichCommandTwo.KeyBinding);

                        if (button.RichCommandOne.KeyGesture != null)
                            button.InputBindingManager.AddInputBinding(button.RichCommandOne.KeyBinding);
                    }
                }
                else
                {
                    //command = button.RichCommandTwo;

                    //if (command.KeyGesture == null && button.RichCommandOne.KeyGesture != null)
                    //{
                    //    command.KeyGestureText = button.RichCommandOne.KeyGestureText;
                    //}

                    if (true
                        //&& KeyGestureString.AreEqual(command.KeyGesture, button.RichCommandOne.KeyGesture)
                        //&& command.KeyGesture.Equals(button.RichCommandOne.KeyGesture)
                        )
                    {
                        if (button.RichCommandOne.KeyGesture != null)
                            button.InputBindingManager.RemoveInputBinding(button.RichCommandOne.KeyBinding);

                        if (button.RichCommandTwo.KeyGesture != null)
                            button.InputBindingManager.AddInputBinding(button.RichCommandTwo.KeyBinding);
                    }
                }
            }

            ButtonRichCommand.ConfigureButtonFromCommand(button,
                choice ? button.RichCommandOne : button.RichCommandTwo,
                button.ShowTextLabel);
        }

        /// <summary>
        /// True => RichCommandOne (default one)
        /// False => RichCommandTwo (alternative one)
        /// </summary>
        public Boolean RichCommandActive
        {
            get
            {
                return (Boolean)GetValue(RichCommandActiveProperty);
            }
            set
            {
                SetValue(RichCommandActiveProperty, value);
            }
        }

        public void SetRichCommand(RichDelegateCommand command)
        {
            ButtonRichCommand.SetRichCommand(command, this, ShowTextLabel, OnCommandDataChanged);
        }

        private void OnCommandDataChanged(object sender, EventArgs e)
        {
            var command = sender as RichDelegateCommand;
            if (command == null)
                return;

            if (command != Command)
            {
#if DEBUG
                Debugger.Break();
#endif
                return;
            }

            ButtonRichCommand.RefreshButtonFromItsRichCommand(this, ShowTextLabel);
        }
    }
}
