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
    /// Inspired/copied from http://msdn.microsoft.com/en-us/library/cc707884.aspx
    /// </summary>
    public class ToolBarTrayRegionAdapter : RegionAdapterBase<ToolBarTray>
    {
        public ToolBarTrayRegionAdapter(IRegionBehaviorFactory regionBehaviorFactory)
            : base(regionBehaviorFactory)
        {
            // Nothing else to do here.
        }

        protected override void Adapt(IRegion region, ToolBarTray regionTarget)
        {
            addItems(regionTarget, region.ActiveViews.ToList());

            region.ActiveViews.CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs e)
                                                        {
                                                            if (e.Action == NotifyCollectionChangedAction.Add)
                                                            {
                                                                addItems(regionTarget, e.NewItems);
                                                            }
                                                            if (e.Action == NotifyCollectionChangedAction.Remove)
                                                            {
                                                                removeItems(regionTarget, e.OldItems);
                                                            }
                                                        };
        }

        protected override IRegion CreateRegion()
        {
            return new AllActiveRegion();
        }

        private static void addItems(ToolBarTray control, IEnumerable newItems)
        {
            if (newItems == null)
            {
                return;
            }

            foreach (var item in newItems.OfType<ToolBar>())
            {
                control.ToolBars.Add(item);
            }

            var dependencyItem = control as DependencyObject;
            if (dependencyItem == null)
            {
                return;
            }

            RegionManager.SetRegionName(dependencyItem, RegionManager.GetRegionName(dependencyItem));
        }

        private static void removeItems(ToolBarTray control, IEnumerable oldItems)
        {
            if (oldItems == null)
            {
                return;
            }

            foreach (var item in oldItems.OfType<ToolBar>())
            {
                control.ToolBars.Remove(item);
            }
        }
    }
}