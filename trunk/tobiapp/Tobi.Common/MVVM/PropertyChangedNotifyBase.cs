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
                    VerifyPropertyName(attribute.DependsOn,
                        (
                        attribute.GetType() == typeof(NotifyDependsOnExAttribute)
                        ? ((NotifyDependsOnExAttribute)attribute).DependsOnType
                        : m_ClassInstancePropertyHost.GetType()
                        )
                        );
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
                    VerifyPropertyName(dependencyPropertyArgs.PropertyName, m_ClassInstancePropertyHost.GetType());
                    VerifyPropertyName(dependentPropertyArgs.PropertyName, m_ClassInstancePropertyHost.GetType());
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
        //[DebuggerStepThrough]
        [Conditional("DEBUG")]
        public void VerifyPropertyName(string propertyName, Type typez)
        {
            if (m_missingClassProperties.Contains(propertyName))
            {
                return;
            }

            //Console.WriteLine(@"^^^^ PropertyChanged: " + propertyName);

            //TypeDescriptor.GetProperties(m_ClassInstancePropertyHost)[propertyName] == null
            if (!string.IsNullOrEmpty(propertyName) &&
                typez.GetProperty(propertyName) == null)
            {
                m_missingClassProperties.Add(propertyName);

                string msg = String.Format(@"=== Invalid property name: ({0}) on {1}",
                    propertyName,
                    typez.FullName);

                Console.WriteLine(msg);
                Debug.Fail(msg);
            }
        }

#endif // DEBUG
        #endregion Debug

        #region INotifyPropertyChanged

        //private Dictionary<String, PropertyChangedEventArgs> m_cachePropertyChangedEventArgs;
        private static EventArgsCache m_EventArgsCache = new EventArgsCache();
        private static readonly Object m_EventArgsCache_LOCK = new object();

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

        private void RaisePropertyChanged(string propertyName)
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

            //#if DEBUG
            //            VerifyPropertyName(propertyName);
            //#endif

            PropertyChangedEventArgs argz;
            lock (m_EventArgsCache_LOCK)
            {
                argz = m_EventArgsCache.Handle(propertyName);
            }

            Debug.Assert(propertyName == argz.PropertyName);

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

            foreach (string argzBump in m_DependentPropsCache.Handle(argz.PropertyName, RaisePropertyChanged))
            {
                if (argzBump != null
                    && (PresentationTraceSources.DataBindingSource.Switch.Level == SourceLevels.All
                        || PresentationTraceSources.DataBindingSource.Switch.Level >= SourceLevels.Error))
                {
#if false && DEBUG
                    Console.WriteLine(@"^^^^ RaisePropertyChanged BUMP (STRING): " + propertyName + @" --> " + argzBump);
#endif
                }
            }
        }

        public void RaisePropertyChanged(PropertyChangedEventArgs argz)
        {
            m_ClassInstancePropertyHost.DispatchPropertyChangedEvent(argz);

            if (m_DependentPropsCache.IsEmpty)
            {
                return;
            }

            foreach (PropertyChangedEventArgs argzBump in m_DependentPropsCache.Handle(argz, RaisePropertyChanged))
            {
                if (argzBump != null
                    && (PresentationTraceSources.DataBindingSource.Switch.Level == SourceLevels.All
                        || PresentationTraceSources.DataBindingSource.Switch.Level >= SourceLevels.Error))
                {
#if false && DEBUG
                    Console.WriteLine(@"^^^^ RaisePropertyChanged BUMP (PROP): " + argz.PropertyName + @" --> " + argzBump.PropertyName);
#endif
                }
            }
        }


        public static string GetMemberName<T>(System.Linq.Expressions.Expression<Func<T>> expression)
        {
            return Reflect.GetProperty(expression).Name;
        }

        public void RaisePropertyChanged<T>(System.Linq.Expressions.Expression<Func<T>> expression)
        {
            //var propertyType = typeof (T);
            string name = GetMemberName(expression);
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
#if DEBUG
                Console.WriteLine(@"^^^^ PropertyChangedEventArgs: " + propertyName);
#endif
                argz = new PropertyChangedEventArgs(propertyName);
                add(propertyName, argz);
            }

            return argz;
        }

        private void add(string propertyName, PropertyChangedEventArgs argz)
        {
            var item = new CacheItem
            {
                data = new CacheData
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
            public string dependencyPropertyName;
            public PropertyChangedEventArgs dependencyPropertyChangeEventArgs;

            public string dependentPropertyName;
            public PropertyChangedEventArgs dependentPropertyChangeEventArgs;
        }

        public IEnumerable<PropertyChangedEventArgs> Handle(PropertyChangedEventArgs dependency, Action<PropertyChangedEventArgs> action)
        {
            if (IsEmpty)
            {
                yield break;
            }

            CacheItem current = m_First;
            do
            {
                if (current.data.dependencyPropertyChangeEventArgs != null
                     && current.data.dependencyPropertyChangeEventArgs.PropertyName == dependency.PropertyName)
                {
                    action(current.data.dependentPropertyChangeEventArgs);

                    yield return current.data.dependentPropertyChangeEventArgs;
                }

                current = current.nextItem;
            } while (current != null);

            yield break;
        }

        public IEnumerable<string> Handle(string dependency, Action<string> action)
        {
            if (IsEmpty)
            {
                yield break;
            }

            CacheItem current = m_First;
            do
            {
                if (!string.IsNullOrEmpty(current.data.dependencyPropertyName)
                    && current.data.dependencyPropertyName == dependency)
                {
                    action(current.data.dependentPropertyName);

                    yield return current.data.dependentPropertyName;
                }
                current = current.nextItem;
            } while (current != null);

            yield break;
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

        private void add(CacheItem item)
        {
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

        public void Add(string dependency, string dependent)
        {
            var item = new CacheItem
                           {
                               data = new CacheData
                                          {
                                              dependencyPropertyName = dependency,
                                              dependentPropertyName = dependent
                                          },
                               nextItem = null
                           };

            add(item);
        }

        public void Add(PropertyChangedEventArgs dependency, PropertyChangedEventArgs dependent)
        {
            var item = new CacheItem
                           {
                               data = new CacheData
                                          {
                                              dependencyPropertyChangeEventArgs = dependency,
                                              dependentPropertyChangeEventArgs = dependent
                                          },
                               nextItem = null
                           };

            add(item);
        }
    }
}