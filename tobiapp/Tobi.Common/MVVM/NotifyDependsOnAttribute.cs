using System;
using System.ComponentModel;

namespace Tobi.Common.MVVM
{
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
