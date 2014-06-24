using System;
using System.Linq;
using System.Windows.Input;

namespace Sid.Windows.Controls
{
    public class TaskDialogCommands
    {
        private static readonly RoutedUICommand Button1Command;
        private static readonly RoutedUICommand Button2Command;
        private static readonly RoutedUICommand Button3Command;

        static TaskDialogCommands()
        {
            Button1Command = new RoutedUICommand("Button1", "Button1Command", typeof(TaskDialogCommands));
            Button2Command = new RoutedUICommand("Button2", "Button2Command", typeof(TaskDialogCommands));
            Button3Command = new RoutedUICommand("Button3", "Button3Command", typeof(TaskDialogCommands));
        }

        public static RoutedUICommand Button1
        {
            get { return Button1Command; }
        }

        public static RoutedUICommand Button2
        {
            get { return Button2Command; }
        }

        public static RoutedUICommand Button3
        {
            get { return Button3Command; }
        }
    }
}
