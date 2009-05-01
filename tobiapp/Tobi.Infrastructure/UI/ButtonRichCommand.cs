using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;

namespace Tobi.Infrastructure.UI
{
    public class ButtonRichCommand : Button
    {
        public static readonly DependencyProperty RichCommandProperty =
            DependencyProperty.Register("RichCommand",
                                        typeof(RichDelegateCommand<object>),
                                        typeof(ButtonRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnRichCommandChanged)));

        internal static void OnRichCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = d as ButtonBase;
            if (button == null)
            {
                return;
            }
            var command = e.NewValue as RichDelegateCommand<object>;
            if (command == null)
            {
                return;
            }

            ConfigureButtonFromCommand(button, command);
        }

        public static void ConfigureButtonFromCommand(ButtonBase button, RichDelegateCommand<object> command)
        {
            button.Command = command;

            button.ToolTip = command.LongDescription + (command.KeyGesture != null ? " [" + command.KeyGestureText + "]" : "");

            if (String.IsNullOrEmpty(command.ShortDescription))
            {
                button.Content = command.IconMedium;
            }
            else
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal };
                panel.Children.Add(new TextBlock(new Run(command.ShortDescription)));
                panel.Children.Add(command.IconMedium);
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

    public class ToggleButtonRichCommand : ToggleButton
    {
        public static readonly DependencyProperty RichCommandProperty =
            DependencyProperty.Register("RichCommand",
                                        typeof(RichDelegateCommand<object>),
                                        typeof(ToggleButtonRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnRichCommandChanged)));

        private static void OnRichCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ButtonRichCommand.OnRichCommandChanged(d, e);
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

    public class RepeatButtonRichCommand : RepeatButton
    {
        public static readonly DependencyProperty RichCommandProperty =
            DependencyProperty.Register("RichCommand",
                                        typeof(RichDelegateCommand<object>),
                                        typeof(RepeatButtonRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnRichCommandChanged)));

        private static void OnRichCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ButtonRichCommand.OnRichCommandChanged(d, e);
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

    public class TwoStateButtonRichCommand : Button
    {
        public static readonly DependencyProperty RichCommandOneProperty =
            DependencyProperty.Register("RichCommandOne",
                                        typeof(RichDelegateCommand<object>),
                                        typeof(TwoStateButtonRichCommand),
                                        new PropertyMetadata(new PropertyChangedCallback(OnRichCommandOneChanged)));

        private static void OnRichCommandOneChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ButtonRichCommand.OnRichCommandChanged(d, e);
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

        public static readonly DependencyProperty RichCommandActiveProperty =
            DependencyProperty.Register("RichCommandActive",
                                        typeof(Boolean),
                                        typeof(TwoStateButtonRichCommand),
                                        new PropertyMetadata(true, OnRichCommandActiveChanged));

        private static void OnRichCommandActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = d as TwoStateButtonRichCommand;
            if (button == null)
            {
                return;
            }
            var choice = (Boolean)e.NewValue;

            RichDelegateCommand<object> command = button.RichCommandOne;
            if (!choice)
            {
                command = button.RichCommandTwo;
            }
            ButtonRichCommand.ConfigureButtonFromCommand(button, command);
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
