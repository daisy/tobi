using System;
using System.ComponentModel;
using System.Diagnostics;
using Tobi.Infrastructure.Onyx.Reflection;

namespace Tobi.Infrastructure
{
    public interface INotifyPropertyChangedEx : INotifyPropertyChanged
    {
        void RaisePropertyChanged(PropertyChangedEventArgs e);
    }

    /// <summary>
    /// Base implementation of INotifyPropertyChanged with automatic dependent property notification
    /// </summary>
    public class PropertyChangedNotifyBase : INotifyPropertyChangedEx
    {
        private INotifyPropertyChangedEx m_ClassInstancePropertyHost;

        // key,value = parent_property_name, child_property_name, where child depends on parent.
        /*private readonly List<KeyValuePair<string, string>> m_DependentPropertyList;
        public List<KeyValuePair<string, string>> DependentPropertyList
        {
            get { return m_DependentPropertyList; }
        }*/
        private DependentPropsCache m_DependentPropsCache = new DependentPropsCache();

        public PropertyChangedNotifyBase()
        {
            //m_DependentPropertyList = new List<KeyValuePair<string, string>>();
            InitializeDependentProperties(this);
        }

        public void InitializeDependentProperties(INotifyPropertyChangedEx obj)
        {
            m_ClassInstancePropertyHost = obj;

            //m_DependentPropertyList.Clear();
            m_DependentPropsCache.Flush();

            foreach (var property in obj.GetType().GetProperties())
            {
                var attributeArray = (NotifyDependsOnAttribute[])property.GetCustomAttributes(
                                                typeof(NotifyDependsOnAttribute), false);

                foreach (var attribute in attributeArray)
                {
                    //m_DependentPropertyList.Add(new KeyValuePair<string, string>(attribute.DependsOn, property.Name));
                    m_DependentPropsCache.Add(attribute.DependsOn, property.Name);
                }
            }
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
            //TypeDescriptor.GetProperties(m_ClassInstancePropertyHost)[propertyName] == null
            if (!string.IsNullOrEmpty(propertyName) &&
                m_ClassInstancePropertyHost.GetType().GetProperty(propertyName) == null)
            {
                string msg = String.Format("Invalid property name ! ({0} / {1})", propertyName, Reflect.GetField(() => propertyName).Name);
                Debug.Fail(msg);
            }
        }

#endif // DEBUG
        #endregion Debug

        #region INotifyPropertyChanged

        //private Dictionary<String, PropertyChangedEventArgs> m_cachePropertyChangedEventArgs;
        private EventArgsCache m_EventArgsCache = new EventArgsCache();

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(PropertyChangedEventArgs e)
        {
            //m_ClassInstancePropertyHost.PropertyChanged.Invoke(m_ClassInstancePropertyHost, e);

            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(m_ClassInstancePropertyHost, e);
            }
        }

        private void RaisePropertyChanged(string propertyName)
        {
#if DEBUG
            VerifyPropertyName(propertyName);
#endif
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

            PropertyChangedEventArgs argz = m_EventArgsCache.Handle(propertyName);
            
            m_ClassInstancePropertyHost.RaisePropertyChanged(argz);

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

            m_DependentPropsCache.Handle(propertyName, RaisePropertyChanged);
        }

        public void OnPropertyChanged<T>(System.Linq.Expressions.Expression<Func<T>> expression)
        {
            RaisePropertyChanged(Reflect.GetProperty(expression).Name);
        }

        #endregion INotifyPropertyChanged
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
    }

    public class DependentPropsCache
    {
        private CacheItem m_First;

        protected class CacheItem
        {
            public CacheData data;
            public CacheItem nextItem;
        }

        protected struct CacheData
        {
            public string dependency;
            public string dependent;
        }

        public void Handle(string dependency, Action<string> action)
        {
            if (IsEmpty)
            {
                return;
            }

            CacheItem current = m_First;
            do
            {
                if (current.data.dependency == dependency)
                {
                    action(current.data.dependent);
                }
                current = current.nextItem;
            } while (current != null);
        }

        public bool IsEmpty
        {
            get
            {
                return m_First == null;
            }
        }

        public void Flush()
        {
            m_First = null;
        }

        public void Add(string dependency, string dependent)
        {
            var item = new CacheItem()
            {
                data = new CacheData()
                {
                    dependency = dependency,
                    dependent = dependent
                },
                nextItem = null
            };

            if (m_First == null)
            {
                m_First = item;
                return;
            }

            CacheItem last = m_First;
            while (last.nextItem != null)
            {
                last = last.nextItem;
            }
            last.nextItem = item;
        }
    } 
    public class EventArgsCache
    {
        private CacheItem m_First;

        protected class CacheItem
        {
            public CacheData data;
            public CacheItem nextItem;
        }

        protected struct CacheData
        {
            public string propertyName;
            public PropertyChangedEventArgs argz;
        }

        public PropertyChangedEventArgs Handle(string propertyName)
        {
            PropertyChangedEventArgs argz = find(propertyName);

            if (argz == null)
            {
                argz = new PropertyChangedEventArgs(propertyName);
                add(propertyName, argz);
            }

            return argz;
        }

        private void add(string propertyName, PropertyChangedEventArgs argz)
        {
            var item = new CacheItem()
            {
                data = new CacheData()
                {
                    propertyName = propertyName,
                    argz = argz
                },
                nextItem = null
            };

            if (m_First == null)
            {
                m_First = item;
                return;
            }

            CacheItem last = m_First;
            while (last.nextItem != null)
            {
                last = last.nextItem;
            }
            last.nextItem = item;
        }

        private PropertyChangedEventArgs find(string propertyName)
        {
            if (m_First == null)
            {
                return null;
            }

            CacheItem current = m_First;
            do
            {
                if (current.data.propertyName == propertyName)
                {
                    return current.data.argz;
                }
                current = current.nextItem;
            } while (current != null);

            return null;
        }
    } 
}
