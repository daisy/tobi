using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Sid.Windows.Controls
{
    public class TaskDialogCommandButton : ButtonBase
    {
        static TaskDialogCommandButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TaskDialogCommandButton), new FrameworkPropertyMetadata(typeof(TaskDialogCommandButton)));
        }

        #region Header

        /// <summary>
        /// Header Dependency Property
        /// </summary>
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(object), typeof(TaskDialogCommandButton),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the Header property.
        /// </summary>
        public object Header
        {
            get { return GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        #endregion


        
    }
}
