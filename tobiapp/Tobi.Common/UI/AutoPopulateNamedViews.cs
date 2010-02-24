using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Practices.Composite;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Presentation.Regions;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.ServiceLocation;


namespace Tobi.Common.UI
{
    public static class RegionManagerExtensionsAutoPopulateNamedViews
    {
        public static IRegionManager RegisterNamedViewWithRegion(this IRegionManager regionManager, string regionName, PreferredPositionNamedView content)
        {
            var regionViewRegistry = ServiceLocator.Current.GetInstance<IRegionNamedViewRegistry>();

            regionViewRegistry.RegisterNamedViewWithRegion(regionName, content);

            return regionManager;
        }
    }

    public struct PreferredPositionNamedView
    {
        public string m_viewName;

        public object m_viewInstance;
        public Func<object> m_viewInstanceProvider;

        public PreferredPosition m_viewPreferredPosition;
    }

    public class WeakDelegatesManager
    {
        private readonly List<DelegateReference> m_Listeners = new List<DelegateReference>();

        public void AddListener(Delegate listener)
        {
            m_Listeners.Add(new DelegateReference(listener, false));
        }

        public void RemoveListener(Delegate listener)
        {
            m_Listeners.RemoveAll(reference =>
            {
                //Remove the listener, and prune collected listeners
                Delegate target = reference.Target;
                return listener.Equals(target) || target == null;
            });
        }

        public void Raise(params object[] args)
        {
            m_Listeners.RemoveAll(listener => listener.Target == null);

            foreach (Delegate handler in m_Listeners.ToList().Select(listener => listener.Target).Where(listener => listener != null))
            {
                handler.DynamicInvoke(args);
            }
        }
    }


    public interface IRegionNamedViewRegistry
    {
        event EventHandler<NamedViewRegisteredEventArgs> ContentRegistered;

        IEnumerable<PreferredPositionNamedView> PullContents(string regionName);

        void RegisterNamedViewWithRegion(string regionName, PreferredPositionNamedView content);
    }

    public class RegionNamedViewRegistry : IRegionNamedViewRegistry
    {
        private readonly IServiceLocator locator;
        private readonly ListDictionary<string, PreferredPositionNamedView> m_RegisteredContent = new ListDictionary<string, PreferredPositionNamedView>();
        private readonly WeakDelegatesManager m_ContentRegisteredListeners = new WeakDelegatesManager();

        public RegionNamedViewRegistry(IServiceLocator m_Locator)
        {
            m_Locator = m_Locator;
        }

        public event EventHandler<NamedViewRegisteredEventArgs> ContentRegistered
        {
            add { m_ContentRegisteredListeners.AddListener(value); }
            remove { m_ContentRegisteredListeners.RemoveListener(value); }
        }

        public IEnumerable<PreferredPositionNamedView> PullContents(string regionName)
        {
            //List<NamedView> items = new List<NamedView>();

            foreach (PreferredPositionNamedView content in m_RegisteredContent[regionName])
            {
                yield return content;
                //items.Add(getContentDelegate());
            }

            m_RegisteredContent.Remove(regionName);

            //return items;
            yield break;
        }

        public void RegisterNamedViewWithRegion(string regionName, PreferredPositionNamedView content)
        {
            m_RegisteredContent.Add(regionName, content);
            OnContentRegistered(new NamedViewRegisteredEventArgs(regionName));
        }

        private void OnContentRegistered(NamedViewRegisteredEventArgs e)
        {
            try
            {
                m_ContentRegisteredListeners.Raise(this, e);
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
                    "Problem trying to register named view ! {0}, {1}", e.RegionName, rootException), ex.InnerException);
            }
        }
    }

    public class AutoPopulateRegionBehaviorNamedViews : RegionBehavior
    {
        public const string BehaviorKey = "AutoPopulateNamedViews";

        private readonly IRegionNamedViewRegistry m_RegionNamedViewRegistry;

        public AutoPopulateRegionBehaviorNamedViews(IRegionNamedViewRegistry regionViewRegistry)
        {
            m_RegionNamedViewRegistry = regionViewRegistry;
        }

        protected override void OnAttach()
        {
            if (string.IsNullOrEmpty(Region.Name))
            {
                Region.PropertyChanged += Region_PropertyChanged;
            }
            else
            {
                StartPopulatingContent();
            }
        }

        private void Region_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Name" && !string.IsNullOrEmpty(Region.Name))
            {
                Region.PropertyChanged -= Region_PropertyChanged;
                StartPopulatingContent();
            }
        }

        private void StartPopulatingContent()
        {
            Debug.Assert(!string.IsNullOrEmpty(Region.Name));

            foreach (PreferredPositionNamedView view in m_RegionNamedViewRegistry.PullContents(Region.Name))
            {
                AddNamedViewIntoRegion(view);
            }

            m_RegionNamedViewRegistry.ContentRegistered += OnNamedViewRegistered;
        }

        protected virtual void AddNamedViewIntoRegion(PreferredPositionNamedView viewToAdd)
        {
            if (Region is PreferredPositionRegion)
            {
                ((PreferredPositionRegion)Region).Add(viewToAdd, false);
            }
            else
            {
                Region.Add((viewToAdd.m_viewInstance ?? viewToAdd.m_viewInstanceProvider()), viewToAdd.m_viewName);
            }

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers", Justification = "This has to be public in order to work with weak references in partial trust or Silverlight environments.")]
        public virtual void OnNamedViewRegistered(object sender, NamedViewRegisteredEventArgs e)
        {
            if (e.RegionName == Region.Name)
            {
                foreach (PreferredPositionNamedView view in m_RegionNamedViewRegistry.PullContents(Region.Name))
                {
                    AddNamedViewIntoRegion(view);
                }

                //AddNamedViewIntoRegion(e.Content);
            }
        }
    }

    public class NamedViewRegisteredEventArgs : EventArgs
    {
        public NamedViewRegisteredEventArgs(string regionName) //, NamedView content)
        {
            //Content = content;
            RegionName = regionName;
        }

        public readonly string RegionName;
        //public readonly NamedView Content;
    }

    public enum PreferredPosition
    {
        Any = -1,
        First = 0, // DEFAULT
        Last = 100
    }

    public class PreferredPositionItemMetadata : ItemMetadata
    {
        public static readonly DependencyProperty PreferredPositionProperty =
               DependencyProperty.Register("PreferredPosition", typeof(PreferredPosition), typeof(PreferredPositionItemMetadata), null);

        public PreferredPosition PreferredPosition
        {
            get { return (PreferredPosition)GetValue(PreferredPositionProperty); }
            set { SetValue(PreferredPositionProperty, value); }
        }

        public PreferredPositionItemMetadata(object item)
            : base(item)
        {
        }
    }

    public class PreferredPositionRegion : Region
    {
        public IEnumerable<object> GetViewsWithNamePrefix(string viewNamePrefix)
        {
            // just to check the parameter and throw the correct exception message obtained from the superclass resources.
            object dummy = GetView(viewNamePrefix);

            return ItemMetadataCollection.Where(x => x.Name.StartsWith(viewNamePrefix)).Select(metadata => metadata.Item);
        }

        public virtual IRegionManager Add(PreferredPositionNamedView namedview, bool createRegionManagerScope)
        {
            IRegionManager manager = createRegionManagerScope ? this.RegionManager.CreateRegionManager() : this.RegionManager;
            this.InnerAdd(namedview, manager);
            return manager;
        }

        private void InnerAdd(PreferredPositionNamedView namedview, IRegionManager scopedRegionManager)
        {
            object view = (namedview.m_viewInstance ?? namedview.m_viewInstanceProvider());

            if (this.ItemMetadataCollection.FirstOrDefault(x => x.Item == view) != null)
            {
                throw new InvalidOperationException("RegionViewExistsException");
            }

            PreferredPositionItemMetadata itemMetadata = new PreferredPositionItemMetadata(view);
            if (!string.IsNullOrEmpty(namedview.m_viewName))
            {
                if (this.ItemMetadataCollection.FirstOrDefault(x => x.Name == namedview.m_viewName) != null)
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "RegionViewNameExistsException: {0}", namedview.m_viewName));
                }
                itemMetadata.Name = namedview.m_viewName;
            }

            itemMetadata.PreferredPosition = namedview.m_viewPreferredPosition;

            DependencyObject dependencyObject = view as DependencyObject;

            if (dependencyObject != null)
            {
                Microsoft.Practices.Composite.Presentation.Regions.RegionManager.SetRegionManager(dependencyObject, scopedRegionManager);
            }

            switch (itemMetadata.PreferredPosition)
            {
                case PreferredPosition.First:
                    {
                        var lastItemWithFirstPreferredPosition = ItemMetadataCollection.LastOrDefault(item =>
                        {
                            var positionedItem = item as PreferredPositionItemMetadata;
                            if (positionedItem == null)
                            {
                                Debug.Fail(@"This should never happen !! (not a PreferredPositionItemMetadata in PreferredPositionRegion)");
                                return false;
                            }
                            return positionedItem.PreferredPosition == PreferredPosition.First;
                        });

                        if (lastItemWithFirstPreferredPosition == null)
                        {
                            if (ItemMetadataCollection.Count == 0)
                            {
                                if (!(itemMetadata.Item is Separator))
                                {
                                    ItemMetadataCollection.Add(itemMetadata);
                                }
                            }
                            else
                            {
                                //ItemMetadataCollection.Insert(0, itemMetadata);

                                var backup = new List<ItemMetadata>(ItemMetadataCollection.Count);
                                while (ItemMetadataCollection.Count > 0)
                                {
                                    var toRemove = ItemMetadataCollection.First();
                                    ItemMetadataCollection.Remove(toRemove);
                                    backup.Add(toRemove);
                                }
                                if (!(itemMetadata.Item is Separator))
                                {
                                    ItemMetadataCollection.Add(itemMetadata);
                                }
                                ItemMetadataCollection.AddRange(backup);
                            }
                        }
                        else
                        {
                            int index = ItemMetadataCollection.IndexOf(lastItemWithFirstPreferredPosition);
                            if (index == ItemMetadataCollection.Count - 1)
                            {
                                ItemMetadataCollection.Add(itemMetadata);
                            }
                            else
                            {
                                //ItemMetadataCollection.Insert(index + 1, itemMetadata);

                                var nToKeep = index + 1;
                                var nToRemove = ItemMetadataCollection.Count - nToKeep;
                                var backup = new List<ItemMetadata>(nToRemove);
                                backup.AddRange(ItemMetadataCollection.Skip(nToKeep));

                                for (int i = 1; i <= nToRemove; i++)
                                {
                                    ItemMetadataCollection.RemoveAt(ItemMetadataCollection.Count - 1);
                                }

                                ItemMetadataCollection.Add(itemMetadata);
                                ItemMetadataCollection.AddRange(backup);
                            }
                        }

                        break;
                    }
                case PreferredPosition.Last:
                    {
                        var lastItemWithLastPreferredPosition = ItemMetadataCollection.LastOrDefault(item =>
                        {
                            var positionedItem = item as PreferredPositionItemMetadata;
                            if (positionedItem == null)
                            {
                                Debug.Fail(@"This should never happen !! (not a PreferredPositionItemMetadata in PreferredPositionRegion)");
                                return false;
                            }
                            return positionedItem.PreferredPosition == PreferredPosition.Last;
                        });

                        if (lastItemWithLastPreferredPosition == null)
                        {
                            ItemMetadataCollection.Add(itemMetadata);
                        }
                        else
                        {
                            int index = ItemMetadataCollection.IndexOf(lastItemWithLastPreferredPosition);
                            if (index == ItemMetadataCollection.Count - 1)
                            {
                                ItemMetadataCollection.Add(itemMetadata);
                            }
                            else
                            {
                                //ItemMetadataCollection.Insert(index + 1, itemMetadata);

                                var nToKeep = index + 1;
                                var nToRemove = ItemMetadataCollection.Count - nToKeep;
                                var backup = new List<ItemMetadata>(nToRemove);
                                backup.AddRange(ItemMetadataCollection.Skip(nToKeep));

                                for (int i = 1; i <= nToRemove; i++)
                                {
                                    ItemMetadataCollection.RemoveAt(ItemMetadataCollection.Count - 1);
                                }

                                ItemMetadataCollection.Add(itemMetadata);
                                ItemMetadataCollection.AddRange(backup);
                            }
                        }

                        break;
                    }
                case PreferredPosition.Any:
                    {
                        var firstItemWithLastPreferredPosition = ItemMetadataCollection.FirstOrDefault(item =>
                        {
                            var positionedItem = item as PreferredPositionItemMetadata;
                            if (positionedItem == null)
                            {
                                Debug.Fail(@"This should never happen !! (not a PreferredPositionItemMetadata in PreferredPositionRegion)");
                                return false;
                            }
                            return positionedItem.PreferredPosition == PreferredPosition.Last;
                        });

                        if (firstItemWithLastPreferredPosition == null)
                        {
                            ItemMetadataCollection.Add(itemMetadata);
                        }
                        else
                        {
                            int index = ItemMetadataCollection.IndexOf(firstItemWithLastPreferredPosition);
                            ItemMetadataCollection.Insert(index, itemMetadata);
                        }

                        break;
                    }
            }
        }
    }

    public class PreferredPositionAllActiveRegion : PreferredPositionRegion
    {
        public override IViewsCollection ActiveViews
        {
            get { return Views; }
        }

        public override void Deactivate(object view)
        {
            throw new InvalidOperationException("DeactiveNotPossibleException");
        }
    }

    public class PreferredPositionItemsControlRegionAdapter : ItemsControlRegionAdapter
    {
        public PreferredPositionItemsControlRegionAdapter(IRegionBehaviorFactory regionBehaviorFactory)
            : base(regionBehaviorFactory)
        {
        }

        protected override IRegion CreateRegion()
        {
            return new PreferredPositionAllActiveRegion();
        }
    }
}
