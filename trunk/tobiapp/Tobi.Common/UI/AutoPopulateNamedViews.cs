using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Practices.Composite;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Presentation.Regions;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.ServiceLocation;


namespace Tobi.Common.UI
{
    public static class RegionManagerExtensionsAutoPopulateNamedViews
    {
        public static IRegionManager RegisterNamedViewWithRegion(this IRegionManager regionManager, string regionName, Func<NamedView> getContentDelegate)
        {
            var regionViewRegistry = ServiceLocator.Current.GetInstance<IRegionNamedViewRegistry>();

            regionViewRegistry.RegisterNamedViewWithRegion(regionName, getContentDelegate);

            return regionManager;
        }
    }

    public struct NamedView
    {
        public string m_viewName;
        public Func<object> m_viewProvider;
    }

    public class WeakDelegatesManager
    {
        private readonly List<DelegateReference> listeners = new List<DelegateReference>();

        public void AddListener(Delegate listener)
        {
            this.listeners.Add(new DelegateReference(listener, false));
        }

        public void RemoveListener(Delegate listener)
        {
            this.listeners.RemoveAll(reference =>
            {
                //Remove the listener, and prune collected listeners
                Delegate target = reference.Target;
                return listener.Equals(target) || target == null;
            });
        }

        public void Raise(params object[] args)
        {
            this.listeners.RemoveAll(listener => listener.Target == null);

            foreach (Delegate handler in this.listeners.ToList().Select(listener => listener.Target).Where(listener => listener != null))
            {
                handler.DynamicInvoke(args);
            }
        }
    }


    public interface IRegionNamedViewRegistry
    {
        event EventHandler<NamedViewRegisteredEventArgs> ContentRegistered;

        IEnumerable<NamedView> GetContents(string regionName);

        void RegisterNamedViewWithRegion(string regionName, Func<NamedView> getContentDelegate);
    }

    public class RegionNamedViewRegistry : IRegionNamedViewRegistry
    {
        private readonly IServiceLocator locator;
        private readonly ListDictionary<string, Func<NamedView>> registeredContent = new ListDictionary<string, Func<NamedView>>();
        private readonly WeakDelegatesManager contentRegisteredListeners = new WeakDelegatesManager();

        public RegionNamedViewRegistry(IServiceLocator locator)
        {
            this.locator = locator;
        }

        public event EventHandler<NamedViewRegisteredEventArgs> ContentRegistered
        {
            add { this.contentRegisteredListeners.AddListener(value); }
            remove { this.contentRegisteredListeners.RemoveListener(value); }
        }

        public IEnumerable<NamedView> GetContents(string regionName)
        {
            List<NamedView> items = new List<NamedView>();
            foreach (Func<NamedView> getContentDelegate in this.registeredContent[regionName])
            {
                items.Add(getContentDelegate());
            }

            return items;
        }

        public void RegisterNamedViewWithRegion(string regionName, Func<NamedView> getContentDelegate)
        {
            this.registeredContent.Add(regionName, getContentDelegate);
            this.OnContentRegistered(new NamedViewRegisteredEventArgs(regionName, getContentDelegate));
        }

        protected virtual object CreateInstance(Type type)
        {
            return this.locator.GetInstance(type);
        }

        private void OnContentRegistered(NamedViewRegisteredEventArgs e)
        {
            try
            {
                this.contentRegisteredListeners.Raise(this, e);
            }
            catch (TargetInvocationException ex)
            {
                Exception rootException;
                if (ex.InnerException != null)
                {
                    rootException = ex.InnerException.GetRootException();
                }
                else
                {
                    rootException = ex.GetRootException();
                }

                throw new ViewRegistrationException(string.Format(CultureInfo.CurrentCulture,
                    "Problem trying to register named view !", e.RegionName, rootException), ex.InnerException);
            }
        }
    }

    public class AutoPopulateRegionBehaviorNamedViews : RegionBehavior
    {
        public const string BehaviorKey = "AutoPopulateNamedViews";

        private readonly IRegionNamedViewRegistry m_RegionNamedViewRegistry;

        public AutoPopulateRegionBehaviorNamedViews(IRegionNamedViewRegistry regionViewRegistry)
        {
            this.m_RegionNamedViewRegistry = regionViewRegistry;
        }

        protected override void OnAttach()
        {
            if (string.IsNullOrEmpty(this.Region.Name))
            {
                this.Region.PropertyChanged += this.Region_PropertyChanged;
            }
            else
            {
                this.StartPopulatingContent();
            }
        }

        private void StartPopulatingContent()
        {
            foreach (NamedView view in this.CreateNamedViewsToAutoPopulate())
            {
                AddNamedViewIntoRegion(view);
            }

            this.m_RegionNamedViewRegistry.ContentRegistered += this.OnNamedViewRegistered;
        }

        protected virtual IEnumerable<NamedView> CreateNamedViewsToAutoPopulate()
        {
            return this.m_RegionNamedViewRegistry.GetContents(this.Region.Name);
        }

        protected virtual void AddNamedViewIntoRegion(NamedView viewToAdd)
        {
            this.Region.Add(viewToAdd.m_viewProvider(), viewToAdd.m_viewName);
        }

        private void Region_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Name" && !string.IsNullOrEmpty(this.Region.Name))
            {
                this.Region.PropertyChanged -= this.Region_PropertyChanged;
                this.StartPopulatingContent();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers", Justification = "This has to be public in order to work with weak references in partial trust or Silverlight environments.")]
        public virtual void OnNamedViewRegistered(object sender, NamedViewRegisteredEventArgs e)
        {
            if (e.RegionName == this.Region.Name)
            {
                AddNamedViewIntoRegion(e.GetNamedView());
            }
        }
    }

    public class NamedViewRegisteredEventArgs : EventArgs
    {
        public NamedViewRegisteredEventArgs(string regionName, Func<NamedView> getViewDelegate)
        {
            this.GetNamedView = getViewDelegate;
            this.RegionName = regionName;
        }

        public string RegionName { get; private set; }

        public Func<NamedView> GetNamedView { get; private set; }
    }
}
