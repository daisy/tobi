using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Tobi.Common.Onyx.Reflection;

namespace Tobi.Common.MVVM
{
    public interface INotifyPropertyChangedEx : INotifyPropertyChanged
    {
        void DispatchPropertyChangedEvent(PropertyChangedEventArgs e);
    }

    public interface IPropertyChangedNotifyBase : INotifyPropertyChangedEx
    {
        void BindPropertyChangedToAction<T>(System.Linq.Expressions.Expression<Func<T>> expression, Action action);
    }

    /// <summary>
    /// Base implementation of INotifyPropertyChanged with automatic dependent property notification
    /// </summary>
    public class PropertyChangedNotifyBase : IPropertyChangedNotifyBase
    {
        private INotifyPropertyChangedEx m_ClassInstancePropertyHost;

        // key,value = parent_property_name, child_property_name, where child depends on parent.
        /*private readonly List<KeyValuePair<string, string>> m_DependentPropertyList;
        public List<KeyValuePair<string, string>> DependentPropertyList
        {
            get { return m_DependentPropertyList; }
        }*/
        private DependentPropsCache m_DependentPropsCache = new DependentPropsCache();

        public PropertyChangedNotifyBase(INotifyPropertyChangedEx propertySource)
        {
            //m_DependentPropertyList = new List<KeyValuePair<string, string>>();
            InitializeDependentProperties(propertySource);
        }

        public PropertyChangedNotifyBase()
        {
            InitializeDependentProperties(this);
        }

        public void InitializeDependentProperties(INotifyPropertyChangedEx obj)
        {
            m_ClassInstancePropertyHost = obj;

            //m_DependentPropertyList.Clear();
            m_DependentPropsCache.Flush();

            foreach (var property in m_ClassInstancePropertyHost.GetType().GetProperties())
            {
                var attributeArray = (NotifyDependsOnAttribute[])
                    property.GetCustomAttributes(typeof(NotifyDependsOnAttribute), false);

                foreach (var attribute in attributeArray)
                {
                    //m_DependentPropertyList.Add(new KeyValuePair<string, string>(attribute.DependsOn, property.Name));
#if DEBUG
                    VerifyPropertyName(attribute.DependsOn);
#endif
                    m_DependentPropsCache.Add(attribute.DependsOn, property.Name);
                }

                var attributeArrayFast = (NotifyDependsOnFastAttribute[])
                    property.GetCustomAttributes(typeof(NotifyDependsOnFastAttribute), false);

                foreach (var attribute in attributeArrayFast)
                {
                    FieldInfo fi = m_ClassInstancePropertyHost.GetType().GetField(attribute.DependencyPropertyArgs, BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Static);
                    if (fi == null || fi.FieldType != typeof(PropertyChangedEventArgs))
                    {
                        Debug.Fail("Not a backing field for PropertyChangedEventArgs ?");
                        continue;
                    }
                    var dependencyPropertyArgs = fi.GetValue(null) as PropertyChangedEventArgs;
                    if (dependencyPropertyArgs == null)
                    {
                        Debug.Fail("Backing field for PropertyChangedEventArgs does not have a value ?");
                        continue;
                    }

                    fi = m_ClassInstancePropertyHost.GetType().GetField(attribute.DependentPropertyArgs, BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Static);
                    if (fi == null || fi.FieldType != typeof(PropertyChangedEventArgs))
                    {
                        Debug.Fail("Not a backing field for PropertyChangedEventArgs ?");
                        continue;
                    }
                    var dependentPropertyArgs = fi.GetValue(null) as PropertyChangedEventArgs;
                    if (dependentPropertyArgs == null)
                    {
                        Debug.Fail("Backing field for PropertyChangedEventArgs does not have a value ?");
                        continue;
                    }

                    //m_DependentPropertyList.Add(new KeyValuePair<string, string>(attribute.DependsOn, property.Name));
#if DEBUG
                    VerifyPropertyName(dependencyPropertyArgs.PropertyName);
                    VerifyPropertyName(dependentPropertyArgs.PropertyName);
#endif
                    m_DependentPropsCache.Add(dependencyPropertyArgs, dependentPropertyArgs);
                }
            }
        }

        #region Debug
#if DEBUG

        List<String> m_missingClassProperties = new List<string>();

        /// <summary>
        /// Warns the developer if this object does not have
        /// a public property with the specified name. This 
        /// method does not exist in a Release build.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(string propertyName)
        {
            if (m_missingClassProperties.Contains(propertyName))
            {
                return;
            }

            //TypeDescriptor.GetProperties(m_ClassInstancePropertyHost)[propertyName] == null
            if (!string.IsNullOrEmpty(propertyName) &&
                m_ClassInstancePropertyHost.GetType().GetProperty(propertyName) == null)
            {
                m_missingClassProperties.Add(propertyName);

                string msg = String.Format("=== Invalid property name: ({0} / {1}) on {2}", propertyName, Reflect.GetField(() => propertyName).Name, m_ClassInstancePropertyHost.GetType().FullName);
                //Debug.Fail(msg);
                Debug.WriteLine(msg);
            }
        }

#endif // DEBUG
        #endregion Debug

        #region INotifyPropertyChanged

        //private Dictionary<String, PropertyChangedEventArgs> m_cachePropertyChangedEventArgs;
        private EventArgsCache m_EventArgsCache = new EventArgsCache();

        public event PropertyChangedEventHandler PropertyChanged;

        public void DispatchPropertyChangedEvent(PropertyChangedEventArgs e)
        {
            //m_ClassInstancePropertyHost.PropertyChanged.Invoke(m_ClassInstancePropertyHost, e);

            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(m_ClassInstancePropertyHost, e);
            }
        }

        public void RaisePropertyChanged(string propertyName)
        {
            /*
            if (m_cachePropertyChangedEventArgs == null)
            {
                m_cachePropertyChangedEventArgs = new Dictionary<String, PropertyChangedEventArgs>();
            }

            PropertyChangedEventArgs argz = null;
            if (m_cachePropertyChangedEventArgs.ContainsKey(propertyName))
            {
                argz = m_cachePropertyChangedEventArgs[propertyName];
            }
            else
            {
                argz = new PropertyChangedEventArgs(propertyName);
                m_cachePropertyChangedEventArgs.Add(propertyName, argz);
            }*/

#if DEBUG
            VerifyPropertyName(propertyName);
#endif

            PropertyChangedEventArgs argz = m_EventArgsCache.Handle(propertyName);

            m_ClassInstancePropertyHost.DispatchPropertyChangedEvent(argz);

            /*
            if (DependentPropertyList.Count <= 0) return;

            foreach (var p in DependentPropertyList.Where(x => x.Key.Equals(propertyName)))
            {
                RaisePropertyChanged(p.Value);
            }*/

            if (m_DependentPropsCache.IsEmpty)
            {
                return;
            }

            m_DependentPropsCache.Handle(argz.PropertyName, RaisePropertyChanged);
        }

        public void RaisePropertyChanged(PropertyChangedEventArgs argz)
        {
            m_ClassInstancePropertyHost.DispatchPropertyChangedEvent(argz);

            if (m_DependentPropsCache.IsEmpty)
            {
                return;
            }

            m_DependentPropsCache.Handle(argz, RaisePropertyChanged);
        }

        public void RaisePropertyChanged<T>(System.Linq.Expressions.Expression<Func<T>> expression)
        {
            string name = Reflect.GetProperty(expression).Name;
#if DEBUG
            Console.WriteLine("^^^^ PropertyChanged: " + name);
#endif
            RaisePropertyChanged(name);
        }

        #endregion INotifyPropertyChanged

        public void BindPropertyChangedToAction<T>(System.Linq.Expressions.Expression<Func<T>> expression, Action action)
        {
            PropertyChanged += ((sender, e) =>
                                    {
                                        if (e.PropertyName == Reflect.GetProperty(expression).Name)
                                        {
                                            action.Invoke();
                                        }
                                    });
        }
    }
}