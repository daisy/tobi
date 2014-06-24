using System.Windows;
using Microsoft.Practices.Composite.Presentation.Regions;

namespace Tobi.Common.UI
{
    public class ClearChildViewsRegionBehavior : RegionBehavior
    {
        public const string BehaviorKey = @"ClearChildViews";

        protected override void OnAttach()
        {
            Region.PropertyChanged += Region_PropertyChanged;
        }

        void Region_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == @"RegionManager")
            {
                if (Region.RegionManager == null)
                {
                    foreach (object view in Region.Views)
                    {
                        DependencyObject dependencyObject = view as DependencyObject;
                        if (dependencyObject != null)
                        {
                            dependencyObject.ClearValue(RegionManager.RegionManagerProperty);
                        }
                    }
                }
            }
        }
    }
}
