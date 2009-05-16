using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Tobi.Infrastructure.Commanding;

namespace Tobi.Infrastructure.UI
{
    public interface IInputBindingManager
    {
        bool AddInputBinding(InputBinding inputBinding);
        void RemoveInputBinding(InputBinding inputBinding);
    }

    /// <summary>
    /// //////////////////////////////////////////
    /// </summary>
    public class ButtonRichCommand : Button
    {
        public static readonly DependencyProperty RichCommandProperty =
            DependencyProperty.Register("RichCommand",
                                        typeof(RichDelegateCommand<object>),
                                        typeof(ButtonRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnRichCommandChanged)));

        private static void OnRichCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = d as ButtonRichCommand;
            if (button == null)
            {
                return;
            }
            var command = e.NewValue as RichDelegateCommand<object>;
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

        public static void ConfigureButtonFromCommand(ButtonBase button, RichDelegateCommand<object> command, bool showTextLabel)
        {
            button.Command = command;

            button.ToolTip = command.LongDescription + (!String.IsNullOrEmpty(command.KeyGestureText) ? " " + command.KeyGestureText + " " : "");

            Image image = command.IconMedium;
            image.Margin = new Thickness(2, 2, 2, 2);

            if (!showTextLabel || String.IsNullOrEmpty(command.ShortDescription))
            {
                button.Content = image;
            }
            else
            {
                var panel = new StackPanel
                                {
                                    Orientation = Orientation.Horizontal
                                };
                panel.Children.Add(image);
                var tb = new Label
                             {
                                 VerticalAlignment = VerticalAlignment.Center,
                                 Content = command.ShortDescription,
                                 //Margin = new Thickness(8, 0, 0, 0)
                             };

                //tb.Content = new Run(UserInterfaceStrings.EscapeMnemonic(command.ShortDescription));

                panel.Children.Add(tb);
                button.Content = panel;
            }
        }


        public RichDelegateCommand<object> RichCommand
        {
            get
            {
                return (RichDelegateCommand<object>)GetValue(RichCommandProperty);
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
                                        typeof(RichDelegateCommand<object>),
                                        typeof(ToggleButtonRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnRichCommandChanged)));

        private static void OnRichCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = d as ToggleButtonRichCommand;
            if (button == null)
            {
                return;
            }
            var command = e.NewValue as RichDelegateCommand<object>;
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
        public RichDelegateCommand<object> RichCommand
        {
            get
            {
                return (RichDelegateCommand<object>)GetValue(RichCommandProperty);
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
                                        typeof(RichDelegateCommand<object>),
                                        typeof(RepeatButtonRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnRichCommandChanged)));

        private static void OnRichCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = d as RepeatButtonRichCommand;
            if (button == null)
            {
                return;
            }
            var command = e.NewValue as RichDelegateCommand<object>;
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
        public RichDelegateCommand<object> RichCommand
        {
            get
            {
                return (RichDelegateCommand<object>)GetValue(RichCommandProperty);
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
                                        typeof(RichDelegateCommand<object>),
                                        typeof(TwoStateToggleButtonRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnRichCommandOneChanged)));

        private static void OnRichCommandOneChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //ButtonRichCommand.OnRichCommandChanged(d, e);
        }

        public RichDelegateCommand<object> RichCommandOne
        {
            get
            {
                return (RichDelegateCommand<object>)GetValue(RichCommandOneProperty);
            }
            set
            {
                SetValue(RichCommandOneProperty, value);
            }
        }

        public static readonly DependencyProperty RichCommandTwoProperty =
            DependencyProperty.Register("RichCommandTwo",
                                        typeof(RichDelegateCommand<object>),
                                        typeof(TwoStateToggleButtonRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnRichCommandTwoChanged)));

        private static void OnRichCommandTwoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //ButtonRichCommand.OnRichCommandChanged(d, e);
        }

        public RichDelegateCommand<object> RichCommandTwo
        {
            get
            {
                return (RichDelegateCommand<object>)GetValue(RichCommandTwoProperty);
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

            RichDelegateCommand<object> command = button.RichCommandOne;

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
                                        typeof(RichDelegateCommand<object>),
                                        typeof(TwoStateButtonRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnRichCommandOneChanged)));

        private static void OnRichCommandOneChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //ButtonRichCommand.OnRichCommandChanged(d, e);
        }

        public RichDelegateCommand<object> RichCommandOne
        {
            get
            {
                return (RichDelegateCommand<object>)GetValue(RichCommandOneProperty);
            }
            set
            {
                SetValue(RichCommandOneProperty, value);
            }
        }

        public static readonly DependencyProperty RichCommandTwoProperty =
            DependencyProperty.Register("RichCommandTwo",
                                        typeof(RichDelegateCommand<object>),
                                        typeof(TwoStateButtonRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnRichCommandTwoChanged)));

        private static void OnRichCommandTwoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //ButtonRichCommand.OnRichCommandChanged(d, e);
        }

        public RichDelegateCommand<object> RichCommandTwo
        {
            get
            {
                return (RichDelegateCommand<object>)GetValue(RichCommandTwoProperty);
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

            RichDelegateCommand<object> command = button.RichCommandOne;

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
