using System;
using System.Windows;

// See http://blogs.microsoft.co.il/blogs/tomershamam/archive/2009/04/14/wpf-commands-everywhere.aspx

namespace Tobi.Infrastructure.Commanding.AttachedTrigger
{
    public class CommandParameter<TCustomParameter>
    {
        public TCustomParameter CustomParameter { get; private set; }

        protected CommandParameter(TCustomParameter customParameter)
        {
            this.CustomParameter = customParameter;
        }

        public static CommandParameter<TCustomParameter> Cast(object parameter)
        {
            var parameterToCast = parameter as CommandParameter<object>;
            if (parameterToCast == null)
            {
                throw new InvalidCastException(string.Format("Failed to case {0} to {1}",
                                                             parameter.GetType(), typeof(CommandParameter<object>)));
            }

            var castedParameter = new CommandParameter<TCustomParameter>(
                (TCustomParameter)parameterToCast.CustomParameter);

            return castedParameter;
        }
    }

    public class EventCommandParameter<TCustomParameter, TEventArgs> : CommandParameter<TCustomParameter>
        where TEventArgs : RoutedEventArgs
    {
        public RoutedEvent RoutedEvent { get; private set; }
        public TEventArgs EventArgs { get; private set; }

        public EventCommandParameter(
            TCustomParameter customParameter,
            RoutedEvent routedEvent,
            TEventArgs eventArgs) : base(customParameter)
        {
            this.RoutedEvent = routedEvent;
            this.EventArgs = eventArgs;
        }

        public static EventCommandParameter<TCustomParameter, TEventArgs> Cast(object parameter)
        {
            var parameterToCast = parameter as EventCommandParameter<object, RoutedEventArgs>;
            if (parameterToCast == null)
            {
                throw new InvalidCastException(string.Format("Failed to case {0} to {1}",
                                                             parameter.GetType(), typeof(EventCommandParameter<object, RoutedEventArgs>)));
            }

            var castedParameter = new EventCommandParameter<TCustomParameter, TEventArgs>(
                (TCustomParameter)parameterToCast.CustomParameter,
                parameterToCast.RoutedEvent,
                (TEventArgs)parameterToCast.EventArgs);

            return castedParameter;
        }
    }

    public class PropertyCommandParameter<TCustomParameter, TValue> : CommandParameter<TCustomParameter>
    {
        public DependencyProperty Property { get; private set; }
        public TValue Value { get; private set; }

        public PropertyCommandParameter(
            TCustomParameter customParameter,
            DependencyProperty property,
            TValue value) : base(customParameter)
        {
            this.Property = property;
            this.Value = value;
        }

        public static PropertyCommandParameter<TCustomParameter, TValue> Cast(object parameter)
        {
            var parameterToCast = parameter as PropertyCommandParameter<object, object>;
            if (parameterToCast == null)
            {
                throw new InvalidCastException(string.Format("Failed to case {0} to {1}",
                                                             parameter.GetType(), typeof(PropertyCommandParameter<object, object>)));
            }

            var castedParameter = new PropertyCommandParameter<TCustomParameter, TValue>(
                (TCustomParameter)parameterToCast.CustomParameter,
                parameterToCast.Property,
                (TValue)parameterToCast.Value);

            return castedParameter;
        }
    }
}