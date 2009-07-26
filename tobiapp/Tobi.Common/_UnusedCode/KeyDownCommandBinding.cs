using System;
using System.Windows;
using System.Windows.Input;

namespace Tobi.Common._UnusedCode
{
    public class CommandModelBase : ICommand {
        public void Execute(object parameter)
        {
            throw new NotImplementedException();
        }

        public bool CanExecute(object parameter)
        {
            throw new NotImplementedException();
        }

        public event EventHandler CanExecuteChanged;

        public ICommand Command
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public void OnQueryEnabled(object sender, CanExecuteRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void OnExecute(object sender, ExecutedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }

    public static class KeyDownCommandBinding
    {
        /// <summary>
        /// Command to execute.
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached("Command",
                                                typeof(CommandModelBase),
                                                typeof(KeyDownCommandBinding),
                                                new PropertyMetadata(new PropertyChangedCallback(OnCommandInvalidated)));

        /// <summary>
        /// Parameter to be passed to the command.
        /// </summary>
        public static readonly DependencyProperty ParameterProperty =
            DependencyProperty.RegisterAttached("Parameter",
                                                typeof(object),
                                                typeof(KeyDownCommandBinding),
                                                new PropertyMetadata(new PropertyChangedCallback(OnParameterInvalidated)));

        /// <summary>
        /// The key to be used as a trigger to execute the command.
        /// </summary>
        public static readonly DependencyProperty KeyProperty =
            DependencyProperty.RegisterAttached("Key",
                                                typeof(Key),
                                                typeof(KeyDownCommandBinding));

        /// <summary>
        /// Get the command to execute.
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        public static CommandModelBase GetCommand(DependencyObject sender)
        {
            return (CommandModelBase)sender.GetValue(CommandProperty);
        }

        /// <summary>
        /// Set the command to execute.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="command"></param>
        public static void SetCommand(DependencyObject sender, CommandModelBase command)
        {
            sender.SetValue(CommandProperty, command);
        }

        /// <summary>
        /// Get the parameter to pass to the command.
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        public static object GetParameter(DependencyObject sender)
        {
            return sender.GetValue(ParameterProperty);
        }

        /// <summary>
        /// Set the parameter to pass to the command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="parameter"></param>
        public static void SetParameter(DependencyObject sender, object parameter)
        {
            sender.SetValue(ParameterProperty, parameter);
        }

        /// <summary>
        /// Get the key to trigger the command.
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        public static Key GetKey(DependencyObject sender)
        {
            return (Key)sender.GetValue(KeyProperty);
        }

        /// <summary>
        /// Set the key which triggers the command.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="key"></param>
        public static void SetKey(DependencyObject sender, Key key)
        {
            sender.SetValue(KeyProperty, key);
        }

        /// <summary>
        /// When the command property is being set attach a listener for the
        /// key down event.  When the command is being unset (when the
        /// UIElement is unloaded for instance) remove the listener.
        /// </summary>
        /// <param name="dependencyObject"></param>
        /// <param name="e"></param>
        static void OnCommandInvalidated(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var element = (UIElement)dependencyObject;
            if (e.OldValue == null && e.NewValue != null)
            {
                element.AddHandler(UIElement.KeyDownEvent,
                                   new KeyEventHandler(OnKeyDown), true);
            }

            if (e.OldValue != null && e.NewValue == null)
            {
                element.RemoveHandler(UIElement.KeyDownEvent,
                                      new KeyEventHandler(OnKeyDown));
            }
        }

        /// <summary>
        /// When the parameter property is set update the command binding to
        /// include it.
        /// </summary>
        /// <param name="dependencyObject"></param>
        /// <param name="e"></param>
        static void OnParameterInvalidated(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var element = (UIElement)dependencyObject;
            element.CommandBindings.Clear();

            // Setup the binding
            var commandModel = e.NewValue as CommandModelBase;
            if (commandModel != null)
            {
                element.CommandBindings.Add(new CommandBinding(commandModel.Command,
                                                               commandModel.OnExecute, commandModel.OnQueryEnabled));
            }
        }

        /// <summary>
        /// When the trigger key is pressed on the element, check whether
        /// the command should execute and then execute it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void OnKeyDown(object sender, KeyEventArgs e)
        {
            var element = sender as UIElement;
            if (element == null) return;

            var triggerKey = (Key)element.GetValue(KeyProperty);

            if (e.Key != triggerKey)
            {
                return;
            }

            var cmdModel = (CommandModelBase)element.GetValue(CommandProperty);
            object parameter = element.GetValue(ParameterProperty);
            if (cmdModel.CanExecute(parameter))
            {
                cmdModel.Execute(parameter);
            }
            e.Handled = true;
        }
    }
}