using System.Windows;
using System.ComponentModel;

// See http://blogs.microsoft.co.il/blogs/tomershamam/archive/2009/04/14/wpf-commands-everywhere.aspx

namespace Tobi.Common._UnusedCode
{
    public class PropertyCommandTrigger : CommandTrigger
    {
        #region Dependency Properties

        #region Property Property

        /// <value>Identifies the Property dependency property</value>
        public static readonly DependencyProperty PropertyProperty =
            DependencyProperty.Register("Property", typeof(DependencyProperty), typeof(PropertyCommandTrigger),
                                        new FrameworkPropertyMetadata(null));

        /// <value>description for Property property</value>
        public DependencyProperty Property
        {
            get { return (DependencyProperty)GetValue(PropertyProperty); }
            set { SetValue(PropertyProperty, value); }
        }

        #endregion

        #region Value Property

        /// <value>Identifies the Value dependency property</value>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(string), typeof(PropertyCommandTrigger),
                                        new FrameworkPropertyMetadata(null));

        /// <value>description for Value property</value>
        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        #endregion

        #endregion

        protected override Freezable CreateInstanceCore()
        {
            return new PropertyCommandTrigger();
        }

        #region T Property

        /// <value>Identifies the T dependency property</value>
        public static readonly DependencyProperty TProperty =
            DependencyProperty.Register("T", typeof(object), typeof(PropertyCommandTrigger),
                                        new FrameworkPropertyMetadata(null, OnTChanged));

        /// <value>description for T property</value>
        public object T
        {
            get { return (object)GetValue(TProperty); }
            set { SetValue(TProperty, value); }
        }

        /// <summary>
        /// Invoked on T change.
        /// </summary>
        /// <param name="d">The object that was changed</param>
        /// <param name="e">Dependency property changed event arguments</param>
        static void OnTChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        #endregion

        protected override void InitializeCore(FrameworkElement source)
        {
            DependencyPropertyDescriptor descriptor = DependencyPropertyDescriptor.FromProperty(Property, source.GetType());
            descriptor.AddValueChanged(source, (s, e) =>
                                                   {
                                                       CommandParameter<object> parameter = new PropertyCommandParameter<object, object>(
                                                           CustomParameter, Property, source.GetValue(Property));

                                                       object value = Value;
                                                       if (descriptor.Converter.CanConvertFrom(typeof(string)))
                                                       {
                                                           value = descriptor.Converter.ConvertFromString(Value);
                                                       }

                                                       if (object.Equals(source.GetValue(Property), value))
                                                           ExecuteCommand(parameter);
                                                   });
        }
    }
}