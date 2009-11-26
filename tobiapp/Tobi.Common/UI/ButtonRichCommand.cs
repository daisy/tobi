using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;

namespace Tobi.Common.UI
{

    /// <summary>
    /// //////////////////////////////////////////
    /// </summary>
    public class ButtonRichCommand : Button
    {
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

        public static void ConfigureButtonFromCommand(ButtonBase button, RichDelegateCommand command, bool showTextLabel)
        {
            button.Command = command;

            button.ToolTip = command.LongDescription +
                             (!String.IsNullOrEmpty(command.KeyGestureText) ? " " + command.KeyGestureText + " " : "");

            button.SetValue(AutomationProperties.NameProperty, button.ToolTip);
            //button.SetValue(AutomationProperties.HelpTextProperty, command.ShortDescription);

            if (command.IconProvider != null && (!showTextLabel || String.IsNullOrEmpty(command.ShortDescription)))
            {
                //button.Content = image;
                command.IconProvider.IconMargin_Medium = new Thickness(2, 2, 2, 2);

                var binding = new Binding
                                  {
                                      Mode = BindingMode.OneWay,
                                      Source = command.IconProvider,
                                      Path = new PropertyPath(PropertyChangedNotifyBase.GetMemberName(() => command.IconProvider.IconLarge))
                                  };

                var expr = button.SetBinding(Button.ContentProperty, binding);
            }
            else
            {
                if (button.Tag is ImageAndTextPlaceholder)
                {
                    if (command.IconProvider != null)
                    {
                        var binding = new Binding
                                          {
                                              Mode = BindingMode.OneWay,
                                              Source = command.IconProvider,
                                              Path =
                                                  new PropertyPath(
                                                  PropertyChangedNotifyBase.GetMemberName(
                                                      () => command.IconProvider.IconLarge))
                                          };
                        var bindingExpressionBase_ =
                            ((ImageAndTextPlaceholder) button.Tag).m_ImageHost.SetBinding(
                                ContentControl.ContentProperty, binding);
                    }

                    ((ImageAndTextPlaceholder)button.Tag).m_TextHost.Content = command.ShortDescription;
                    button.ToolTip = command.LongDescription;

                    button.SetValue(AutomationProperties.NameProperty, button.ToolTip);
                    //button.SetValue(AutomationProperties.HelpTextProperty, command.ShortDescription);
                }
                else
                {
                    button.Content = null;

                    var panel = new StackPanel
                                    {
                                        Orientation = Orientation.Horizontal
                                    };

                    var imageHost = new ContentControl { Focusable = false };

                    if (command.IconProvider != null)
                    {
                        //Image image = command.IconProvider.IconMedium;
                        command.IconProvider.IconMargin_Medium = new Thickness(2, 2, 2, 2);

                        var binding = new Binding
                                          {
                                              Mode = BindingMode.OneWay,
                                              Source = command.IconProvider,
                                              Path =
                                                  new PropertyPath(
                                                  PropertyChangedNotifyBase.GetMemberName(
                                                      () => command.IconProvider.IconLarge))
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

                    button.SetValue(AutomationProperties.NameProperty, button.ToolTip);
                    //button.SetValue(AutomationProperties.HelpTextProperty, command.ShortDescription);

                    button.Tag = new ImageAndTextPlaceholder
                                     {
                                         m_ImageHost = imageHost,
                                         m_TextHost = tb
                                     };
                }
            }
        }

        private struct ImageAndTextPlaceholder
        {
            public ContentControl m_ImageHost;
            public Label m_TextHost;
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
    public class ToggleButtonRichCommand : ToggleButton
    {
        public static readonly DependencyProperty RichCommandProperty =
            DependencyProperty.Register("RichCommand",
                                        typeof(RichDelegateCommand),
                                        typeof(ToggleButtonRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnRichCommandChanged)));

        private static void OnRichCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = d as ToggleButtonRichCommand;
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
                                        typeof(ToggleButtonRichCommand),
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
    }

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
    }

    /// <summary>
    /// //////////////////////////////////////////
    /// </summary>
    public class TwoStateToggleButtonRichCommand : ToggleButton
    {
        public static readonly DependencyProperty InputBindingManagerProperty =
            DependencyProperty.Register("InputBindingManager",
                                        typeof(IInputBindingManager),
                                        typeof(TwoStateToggleButtonRichCommand),
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
                                        typeof(TwoStateToggleButtonRichCommand),
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
                                        typeof(TwoStateToggleButtonRichCommand),
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
                                        typeof(TwoStateToggleButtonRichCommand),
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
                                        typeof(TwoStateToggleButtonRichCommand),
                                        new PropertyMetadata(true, OnRichCommandActiveChanged));

        private static void OnRichCommandActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = d as TwoStateToggleButtonRichCommand;
            if (button == null)
            {
                return;
            }
            var choice = (Boolean)e.NewValue;

            RichDelegateCommand command = button.RichCommandOne;

            if (command.KeyGesture == null && button.RichCommandTwo.KeyGesture != null)
            {
                command.KeyGestureText = button.RichCommandTwo.KeyGestureText;
            }

            if (button.InputBindingManager != null
                && command.KeyGesture != null
                    && command.KeyGesture.Equals(button.RichCommandTwo.KeyGesture))
            {
                button.InputBindingManager.RemoveInputBinding(button.RichCommandTwo.KeyBinding);
                button.InputBindingManager.AddInputBinding(command.KeyBinding);
            }

            if (!choice)
            {
                command = button.RichCommandTwo;

                if (command.KeyGesture == null && button.RichCommandOne.KeyGesture != null)
                {
                    command.KeyGestureText = button.RichCommandOne.KeyGestureText;
                }

                if (button.InputBindingManager != null
                   && command.KeyGesture != null
                   && command.KeyGesture.Equals(button.RichCommandOne.KeyGesture))
                {
                    button.InputBindingManager.RemoveInputBinding(button.RichCommandOne.KeyBinding);
                    button.InputBindingManager.AddInputBinding(command.KeyBinding);
                }
            }

            ButtonRichCommand.ConfigureButtonFromCommand(button, command, button.ShowTextLabel);
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
    }

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

            RichDelegateCommand command = button.RichCommandOne;

            if (command.KeyGesture == null && button.RichCommandTwo.KeyGesture != null)
            {
                command.KeyGestureText = button.RichCommandTwo.KeyGestureText;
            }

            if (command.KeyGesture != null
                    && command.KeyGesture.Equals(button.RichCommandTwo.KeyGesture)
                    && button.InputBindingManager != null)
            {
                button.InputBindingManager.RemoveInputBinding(button.RichCommandTwo.KeyBinding);
                button.InputBindingManager.AddInputBinding(command.KeyBinding);
            }

            if (!choice)
            {
                command = button.RichCommandTwo;

                if (command.KeyGesture == null && button.RichCommandOne.KeyGesture != null)
                {
                    command.KeyGestureText = button.RichCommandOne.KeyGestureText;
                }

                if (command.KeyGesture != null
                   && command.KeyGesture.Equals(button.RichCommandOne.KeyGesture)
                   && button.InputBindingManager != null)
                {
                    button.InputBindingManager.RemoveInputBinding(button.RichCommandOne.KeyBinding);
                    button.InputBindingManager.AddInputBinding(command.KeyBinding);
                }
            }

            ButtonRichCommand.ConfigureButtonFromCommand(button, command, button.ShowTextLabel);
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
    }
}
