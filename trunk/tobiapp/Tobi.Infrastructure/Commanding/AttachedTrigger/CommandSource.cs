using System.Windows;

//See http://blogs.microsoft.co.il/blogs/tomershamam/archive/2009/04/14/wpf-commands-everywhere.aspx

namespace Tobi.Infrastructure.Commanding.AttachedTrigger
{
    public static class CommandSource
    {
        #region Attached Properties

        #region Trigger

        public static ICommandTrigger GetTrigger(FrameworkElement source)
        {
            return (ICommandTrigger)source.GetValue(TriggerProperty);
        }

        public static void SetTrigger(FrameworkElement source, ICommandTrigger value)
        {
            source.SetValue(TriggerProperty, value);
        }

        // Using a DependencyProperty as the backing store for Trigger.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TriggerProperty =
            DependencyProperty.RegisterAttached(
                "Trigger",
                typeof(ICommandTrigger),
                typeof(CommandSource),
                new UIPropertyMetadata(
                    null,
                    TriggerPropertyChanged));

        private static void TriggerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement element = d as FrameworkElement;

            ICommandTrigger commandTrigger = e.NewValue as ICommandTrigger;
            if (commandTrigger != null)
            {
                commandTrigger.Initialize(element);
            }
        }

        #endregion

        #endregion
    }
}