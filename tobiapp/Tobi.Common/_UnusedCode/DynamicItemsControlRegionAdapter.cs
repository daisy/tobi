using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Practices.Composite.Presentation.Regions;
using Microsoft.Practices.Composite.Regions;

namespace Tobi.Common._UnusedCode
{
    /// <summary>
    /// See DanielVaughan.Calcium.Client.Modularity.RegionAdapters
    /// See http://msdn.microsoft.com/en-us/library/cc707884.aspx
    /// </summary>
    public class DynamicItemsControlRegionAdapter : RegionAdapterBase<ItemsControl>
    {
        public DynamicItemsControlRegionAdapter(IRegionBehaviorFactory regionBehaviorFactory)
            : base(regionBehaviorFactory)
        {
            /* Intentionally left blank. */
        }

        protected override void Adapt(IRegion region, ItemsControl regionTarget)
        {
            AddItems(regionTarget, region.ActiveViews.ToList());

            region.ActiveViews.CollectionChanged += delegate(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    AddItems(regionTarget, e.NewItems);
                }
                if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    RemoveItems(regionTarget, e.OldItems);
                }
            };
        }

        protected override IRegion CreateRegion()
        {
            IRegion region = new AllActiveRegion();
            return region;
        }

        void AddItems(ItemsControl control, IList newItems)
        {
            if (newItems == null)
            {
                return;
            }

            foreach (var item in newItems)
            {
                control.Items.Add(item);
            }

            var dependencyItem = control as DependencyObject;
            if (dependencyItem == null)
            {
                return;
            }
            RegionManager.SetRegionName(dependencyItem, RegionManager.GetRegionName(dependencyItem));
        }

        void RemoveItems(ItemsControl control, IList oldItems)
        {
            if (oldItems == null)
            {
                return;
            }

            foreach (var item in oldItems)
            {
                control.Items.Remove(item);
            }
        }
    }
}
