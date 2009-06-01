using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Tobi.Infrastructure.Onyx.Reflection;

namespace Tobi.Infrastructure
{
    public interface INotifyDependentPropertyChanged
    {
        // key,value = parent_property_name, child_property_name, where child depends on parent.
        List<KeyValuePair<string, string>> DependentPropertyList { get; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class NotifyDependsOnAttribute : Attribute
    {
        public string DependsOn { get; set; }

        public NotifyDependsOnAttribute(string name)
        {
            DependsOn = name;
        }

        /*
        public NotifyDependsOnAttribute(System.Linq.Expressions.Expression<Func<string>> expression)
        {
            DependsOn = Reflect.GetMember(expression).Name;
        }

        public NotifyDependsOnAttribute(System.Linq.Expressions.Expression<Func<bool>> expression)
        {
            DependsOn = Reflect.GetMember(expression).Name;
        }

        public NotifyDependsOnAttribute(System.Linq.Expressions.Expression<Action> expression)
        {
            DependsOn = Reflect.GetMember(expression).Name;
        }*/

        public static void BuildDependentPropertyList(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            var obj_interface = (obj as INotifyDependentPropertyChanged);

            if (obj_interface == null)
            {
                throw new Exception(string.Format("Type {0} does not implement INotifyDependentPropertyChanged.", obj.GetType().Name));
            }

            obj_interface.DependentPropertyList.Clear();

            // Build the list of dependent properties.
            foreach (var property in obj.GetType().GetProperties())
            {
                // Find all of our attributes (may be multiple).
                var attributeArray = (NotifyDependsOnAttribute[])property.GetCustomAttributes(typeof(NotifyDependsOnAttribute), false);

                foreach (var attribute in attributeArray)
                {
                    obj_interface.DependentPropertyList.Add(new KeyValuePair<string, string>(attribute.DependsOn, property.Name));
                }
            }
        }
    }

    /// <summary>
    /// Base class for all ViewModels
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged, INotifyDependentPropertyChanged, IDisposable
    {
        protected Dispatcher Dispatcher { get; private set; }

        private readonly List<KeyValuePair<string, string>> m_DependentPropertyList;
        public List<KeyValuePair<string, string>> DependentPropertyList
        {
            get { return m_DependentPropertyList; }
        }

        protected ViewModelBase()
        {
#if DEBUG
            ThrowOnInvalidPropertyName = false;
#endif
            Dispatcher = Application.Current != null ? Application.Current.Dispatcher : Dispatcher.CurrentDispatcher;

            m_DependentPropertyList = new List<KeyValuePair<string, string>>();
            NotifyDependsOnAttribute.BuildDependentPropertyList(this);
        }

        #region Debug
#if DEBUG
        /// <summary>
        /// Warns the developer if this object does not have
        /// a public property with the specified name. This 
        /// method does not exist in a Release build.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(string propertyName)
        {
            //TypeDescriptor.GetProperties(this)[propertyName] == null
            if (!string.IsNullOrEmpty(propertyName) && GetType().GetProperty(propertyName) == null)
            {
                string msg = String.Format("Invalid property name ! ({0})", propertyName);

                if (ThrowOnInvalidPropertyName)
                {
                    throw new ArgumentException(msg, Reflect.GetField(() => propertyName).Name);
                }

                Debug.Fail(msg);
            }
        }

        /// <summary>
        /// Returns whether an exception is thrown, or if a Debug.Fail() is used
        /// when an invalid property name is passed to the VerifyPropertyName method.
        /// The default value is false, but subclasses used by unit tests might 
        /// override this property's getter to return true.
        /// </summary>
        protected virtual bool ThrowOnInvalidPropertyName { get; private set; }

        ~ViewModelBase()
        {
            string msg = string.Format("Finalized: ({0})", GetType().Name);
            Debug.WriteLine(msg);
        }

#endif // DEBUG
        #endregion Debug


        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            //PropertyChanged.Invoke(this, e);

            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
#if DEBUG
            VerifyPropertyName(propertyName);
#endif

            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

            foreach (var p in DependentPropertyList.Where(x => x.Key.Equals(propertyName)))
            {
                OnPropertyChanged(p.Value);
            }
        }

        protected void OnPropertyChanged<T>(System.Linq.Expressions.Expression<Func<T>> expression)
        {
            OnPropertyChanged(Reflect.GetProperty(expression).Name);
        }

        #endregion INotifyPropertyChanged

        #region IDisposable

        public void Dispose()
        {
#if DEBUG
            string msg = string.Format("Disposing: ({0})", GetType().Name);
            Debug.WriteLine(msg);
#endif
            Disposing();
        }

        protected virtual void Disposing()
        {
        }

        #endregion IDisposable
    }
}
