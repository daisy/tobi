using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Microsoft.Practices.Composite.Wpf.Commands;

namespace Tobi.Infrastructure
{
    public class GlobalCommands
    {
        //public static RoutedUICommand ChangeStatusTextCommand;

        //public static CompositeCommand TobiCompositeCommand = new CompositeCommand();

        static GlobalCommands()
        {
            //var changeStatusTextInputs = new InputGestureCollection { new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift) };
            //ChangeStatusTextCommand = new RoutedUICommand("Change Status Text", "ChangeStatusText", typeof(GlobalCommands), changeStatusTextInputs);


            /*
            CommandBinding binding = new CommandBinding();

            binding.Command = ChangeStatusTextCommand;

            binding.Executed += new ExecutedRoutedEventHandler(binding_Executed);

            CommandManager.RegisterClassCommandBinding(typeof(GlobalCommands), binding);
             */
        }
    }
}
