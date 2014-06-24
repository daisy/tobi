using System;

namespace Sid.Windows.Controls
{
    public delegate void TaskDialogEventHandler(object sender, TaskDialogEventArgs e);
    public class TaskDialogEventArgs : EventArgs
    {
        public TaskDialogWindow Window { get; internal set; }
    }
}
