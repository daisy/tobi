using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;

namespace Tobi.Infrastructure.UI
{
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

            button.Command = command;

            button.ToolTip = command.LongDescription;

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
            get { return (RichDelegateCommand<object>)GetValue(RichCommandProperty); }
            set
            {
                SetValue(RichCommandProperty, value);
            }
        }
    }
}
