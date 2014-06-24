using System;
using System.Runtime.Serialization;

namespace Sid.Windows.Controls
{
    public class TaskDialogException : Exception
    {
        public TaskDialogException()
        {
        }

        public TaskDialogException(string message) : base(message)
        {
        }

        public TaskDialogException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected TaskDialogException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
