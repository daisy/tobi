using System;
using System.Diagnostics;

namespace Tobi.Common.MVVM
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class NotifyDependsOnAttribute : Attribute
    {
        public string DependsOn { get; private set; }

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

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class NotifyDependsOnExAttribute : NotifyDependsOnAttribute
    {
        public Type DependsOnType { get; private set; }

        public NotifyDependsOnExAttribute(string name, Type typez) : base(name)
        {
            DependsOnType = typez;

#if DEBUG
            var propInfo = DependsOnType.GetProperty(DependsOn);
            if (propInfo == null)
            {
                Debug.Fail(string.Format("Property {0} not found on Type {1} !", DependsOn, DependsOnType.FullName));
            }
#endif //DEBUG
        }
    }


    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class NotifyDependsOnFastAttribute : Attribute
    {
        public string DependencyPropertyArgs { get; set; }
        public string DependentPropertyArgs { get; set; }

        public NotifyDependsOnFastAttribute(string dependency, string dependent)
        {
            DependencyPropertyArgs = dependency;
            DependentPropertyArgs = dependent;
        }
    }
}
