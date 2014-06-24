using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.Composite.Regions;

using Microsoft.Practices.Unity;

namespace Tobi.Infrastructure.UI
{
    public static class RegionManagerCustomExtensions
    {
        /// <summary>
        /// manager.RegisterViewWithRegionInIndex(“MainRegion”, typeof(Views.HelloWorldView), 0);
        /// manager.RegisterViewWithRegionInIndex(“MainRegion”, typeof(Views.View1), 1);
        /// manager.RegisterViewWithRegionInIndex(“MainRegion”, typeof(Views.View2), 1);
        /// </summary>
        /// <param name="regionManager"></param>
        /// <param name="container"></param>
        /// <param name="regionName"></param>
        /// <param name="viewType"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static IRegionManager RegisterViewWithRegionInIndex(this IRegionManager regionManager, IUnityContainer container, string regionName, Type viewType, int index)
       {
           IRegion mainRegion = regionManager.Regions[regionName];
           int viewsAmount = mainRegion.Views.Count();
           if (index > viewsAmount)
           {
               throw new IndexOutOfRangeException("Tried to add a view to a region that does not have enough views.");
           }

           if (index < 0)
           {
               throw new IndexOutOfRangeException("Tried to add a view in a negative index.");
           }

           object activeView = null;

           if (mainRegion.ActiveViews.Count() == 1)
           {
               activeView = mainRegion.ActiveViews.First();
           }

           var regionViewRegistry = container.Resolve<IRegionViewRegistry>(); //ServiceLocator.Current.GetInstance<IRegionViewRegistry>(); ////using Microsoft.Practices.ServiceLocation;

           // Save reference to each view existing in the RegionManager after the index to insert.
           List<object> views = mainRegion.Views.SkipWhile((view, removeFrom) => removeFrom < index).ToList();

           //Remove elements from region that are after index to insert.
           for (int i = 0; i < views.Count; i++)
           {
               mainRegion.Remove(mainRegion.Views.ElementAt(index));
           }

           //Register view in index to insert.
           regionViewRegistry.RegisterViewWithRegion(regionName, viewType);

           // Adding previously removed views
           views.ForEach(view => mainRegion.Add(view));

           if (activeView != null)
           {
               mainRegion.Activate(activeView);
           }

           return regionManager;
       }
    }
}
