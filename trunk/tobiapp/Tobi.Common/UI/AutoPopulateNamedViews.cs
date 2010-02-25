using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Practices.Composite;
using Microsoft.Practices.Composite.Presentation.Regions;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.ServiceLocation;
using Tobi.Common.MVVM.Command;


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


    public enum PreferredPosition : byte
    {
        First, Any, Last
    }

    public struct PreferredPositionNamedView
    {
        public string m_viewName;

        public object m_viewInstance;
        public Func<object> m_viewInstanceProvider;

        public PreferredPosition m_viewPreferredPosition;
    }

    public interface IRegionNamedViewRegistry
    {
        event EventHandler<EventArgsx<string>> ContentRegistered; //NamedViewRegisteredEventArgs

        IEnumerable<PreferredPositionNamedView> PullContents(string regionName);

        void RegisterNamedViewWithRegion(string regionName, PreferredPositionNamedView content);
    }

    public class RegionNamedViewRegistry : IRegionNamedViewRegistry
    {
        private readonly IServiceLocator locator;
        private readonly ListDictionary<string, PreferredPositionNamedView> m_RegisteredContent = new ListDictionary<string, PreferredPositionNamedView>();

        //private List<WeakReference<EventHandler<EventArgsx<string>>>> m_ContentRegisteredHandlers;
        private readonly WeakDelegatesManager m_ContentRegisteredListeners = new WeakDelegatesManager();

        public RegionNamedViewRegistry(IServiceLocator m_Locator)
        {
            m_Locator = m_Locator;
        }

        public event EventHandler<EventArgsx<string>> ContentRegistered //NamedViewRegisteredEventArgs
        {
            add
            {
                //WeakReferencedEventHandlerHelper.AddWeakReferenceHandler<EventArgsx<string>>(ref m_ContentRegisteredHandlers, value, 2);
                m_ContentRegisteredListeners.AddListener(value);
            }
            remove
            {
                //WeakReferencedEventHandlerHelper.RemoveWeakReferenceHandler<EventArgsx<string>>(m_ContentRegisteredHandlers, value);
                m_ContentRegisteredListeners.RemoveListener(value);
            }
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
            OnContentRegistered(new EventArgsx<string>(regionName)); //NamedViewRegisteredEventArgs
        }

        private void OnContentRegistered(EventArgsx<string> e) //NamedViewRegisteredEventArgs
        {
            try
            {
                //WeakReferencedEventHandlerHelper.CallWeakReferenceHandlers_WithDispatchCheck<EventArgsx<string>>(m_ContentRegisteredHandlers, this, e);
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
                    "Problem trying to register named view ! {0}, {1}", e.PayLoad, rootException), ex.InnerException);
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
        public virtual void OnNamedViewRegistered(object sender, EventArgsx<string> e) //NamedViewRegisteredEventArgs e)
        {
            if (Region.Name == e.PayLoad) //e.RegionName
            {
                foreach (PreferredPositionNamedView view in m_RegionNamedViewRegistry.PullContents(Region.Name))
                {
                    AddNamedViewIntoRegion(view);
                }

                //AddNamedViewIntoRegion(e.Content);
            }
        }
    }

    //public class NamedViewRegisteredEventArgs : EventArgs
    //{
    //    public NamedViewRegisteredEventArgs(string regionName) //, NamedView content)
    //    {
    //        //Content = content;
    //        RegionName = regionName;
    //    }

    //    public readonly string RegionName;
    //    //public readonly NamedView Content;
    //}

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
    public class PreferredPositionRegion : Region
    {
#if DEBUG
        public static readonly bool MARK_PREFERRED_POS = true; // change this to false to avoid information overload in DEBUG mode.
#endif
        private int addIndex = 0;

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
#if DEBUG
            if (MARK_PREFERRED_POS) markWithAddIndex(itemMetadata);
#endif
            int initialCount = ItemMetadataCollection.Count;

            bool isSeparator = itemMetadata.Item is Separator
#if DEBUG
 || !(itemMetadata.Item is MenuItemRichCommand) && itemMetadata.Item is MenuItem
#endif
;

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

                        // No First in the current list of views for this region
                        if (lastItemWithFirstPreferredPosition == null)
                        {
                            if (isSeparator) break; // the following views will be inserted as the very first ones, no need for separation

                            if (ItemMetadataCollection.Count == 0)
                            {
                                ItemMetadataCollection.Add(itemMetadata);
#if DEBUG
                                ItemMetadataCollectionCheckPreferredPositions();
#endif
                            }
                            else
                            {
                                //ItemMetadataCollection.Insert(0, itemMetadata);
                                //ItemMetadataCollectionInsert(0, itemMetadata);

                                var backup = new List<ItemMetadata>(initialCount);
                                while (ItemMetadataCollection.Count > 0)
                                {
                                    var toRemove = ItemMetadataCollection.First();
                                    ItemMetadataCollection.Remove(toRemove);
                                    backup.Add(toRemove);
                                }

                                ItemMetadataCollection.Add(itemMetadata);
                                ItemMetadataCollection.AddRange(backup);

#if DEBUG
                                ItemMetadataCollectionCheckPreferredPositions();
#endif
                            }

                            Debug.Assert(ItemMetadataCollection.Count == initialCount + 1);
                        }
                        else // we found the last view with position == First
                        {
                            int index = ItemMetadataCollection.IndexOf(lastItemWithFirstPreferredPosition);

                            // All the current views are First, so we append the new view (note: could be a Separator)
                            // i.e. the first view to be registered as First is guaranteed to take precedence over the ones that come after.
                            if (index == ItemMetadataCollection.Count - 1)
                            {
                                ItemMetadataCollection.Add(itemMetadata);

#if DEBUG
                                ItemMetadataCollectionCheckPreferredPositions();
#endif
                            }
                            else // there are Any or Last views after the last First one.
                            {
                                //ItemMetadataCollection.Insert(index + 1, itemMetadata);

                                ItemMetadataCollectionInsert(index + 1, itemMetadata);

#if DEBUG
                                ItemMetadataCollectionCheckPreferredPositions();
#endif
                            }

                            Debug.Assert(ItemMetadataCollection.Count == initialCount + 1);
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

                        // No Last in the current list of views for this region
                        if (lastItemWithLastPreferredPosition == null)
                        {
                            ItemMetadataCollection.Add(itemMetadata); // could be a separator

#if DEBUG
                            ItemMetadataCollectionCheckPreferredPositions();
#endif
                        }
                        else // we found the last view with position == Last
                        {
                            int index = ItemMetadataCollection.IndexOf(lastItemWithLastPreferredPosition);
                            if (index == ItemMetadataCollection.Count - 1)
                            {
                                ItemMetadataCollection.Add(itemMetadata); // could be a separator

#if DEBUG
                                ItemMetadataCollectionCheckPreferredPositions();
#endif
                            }
                            else
                            {
                                Debug.Fail("No First or Any should follow a Last !!");

                                ////ItemMetadataCollection.Insert(index + 1, itemMetadata);
                                //ItemMetadataCollectionInsert(index + 1, itemMetadata);
                            }
                        }

                        Debug.Assert(ItemMetadataCollection.Count == initialCount + 1);

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

                        // No Last in the current list of views for this region
                        if (firstItemWithLastPreferredPosition == null)
                        {
                            ItemMetadataCollection.Add(itemMetadata); // could be a separator

#if DEBUG
                            ItemMetadataCollectionCheckPreferredPositions();
#endif
                        }
                        else
                        {
                            int index = ItemMetadataCollection.IndexOf(firstItemWithLastPreferredPosition);

                            // ItemMetadataCollection.Insert(index, itemMetadata);
                            ItemMetadataCollectionInsert(index, itemMetadata);

#if DEBUG
                            ItemMetadataCollectionCheckPreferredPositions();
#endif
                        }

                        break;
                    }
            }
        }

        [Conditional("DEBUG")]
        private void markWithAddIndex(ItemMetadata itemMetadata)
        {
            addIndex++;

            if (itemMetadata.Item is TwoStateMenuItemRichCommand_DataContextWrapper)
            {
                var item = (TwoStateMenuItemRichCommand_DataContextWrapper)itemMetadata.Item;
                item.RichCommandOne.ShortDescription = addIndex + ") " +
                                                       item.RichCommandOne.ShortDescription;
                item.RichCommandTwo.ShortDescription = addIndex + ") " +
                                                       item.RichCommandTwo.ShortDescription;
            }
            else if (itemMetadata.Item is RichDelegateCommand)
            {
                var item = (RichDelegateCommand)itemMetadata.Item;
                item.ShortDescription = addIndex + ") " +
                                                       item.ShortDescription;
            }
            else if (itemMetadata.Item is MenuItemRichCommand)
            {
                var item = (MenuItemRichCommand)itemMetadata.Item;
                item.Header = addIndex + ") " +
                                                       item.Header;
            }
        }

        [Conditional("DEBUG")]
        private void ItemMetadataCollectionCheckPreferredPositions()
        {
            PreferredPosition currentPreferredPosition = PreferredPosition.First;
            bool first = true;
            foreach (var itemMetadata in ItemMetadataCollection)
            {
                var prefPos = ((PreferredPositionItemMetadata)itemMetadata).PreferredPosition;

                if (first) currentPreferredPosition = prefPos;
                first = false;

                if (prefPos < currentPreferredPosition)
                    Debug.Fail("Corrupted PreferredPosition order !!");

                if (prefPos > currentPreferredPosition)
                    currentPreferredPosition = prefPos;
            }
        }

        private void ItemMetadataCollectionInsert(int index, PreferredPositionItemMetadata itemMetadata)
        {
            var nToKeep = index; // left
            var nToRemove = ItemMetadataCollection.Count - nToKeep; // right

            Debug.Assert(ItemMetadataCollection.Count == nToRemove + nToKeep);

            var backup = new List<ItemMetadata>(nToRemove);
            backup.AddRange(ItemMetadataCollection.Skip(nToKeep)); // save the left part

            for (int i = 1; i <= nToRemove; i++) // remove the right part
            {
                ItemMetadataCollection.RemoveAt(ItemMetadataCollection.Count - 1); // remove last view
            }

            ItemMetadataCollection.Add(itemMetadata); // append after the left part
            ItemMetadataCollection.AddRange(backup); // restore the right part
        }
    }
}
