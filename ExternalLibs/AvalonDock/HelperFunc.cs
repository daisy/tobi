using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace AvalonDock
{
    internal static class HelperFunc
    {
        public static bool AreVeryClose(double v1, double v2)
        {
            if (Math.Abs(v1 - v2) < 0.000001)
                return true;
            
            return false;
        }

        public static bool IsLessThen(double v1, double v2)
        {
            if (AreVeryClose(v1, v2))
                return false;

            return v1 < v2;
        }

        public static Point PointToScreenWithoutFlowDirection(FrameworkElement element, Point point)
        { 
            if (FrameworkElement.GetFlowDirection(element) == FlowDirection.RightToLeft)
            {
                Point leftToRightPoint = new Point(
                    element.ActualWidth - point.X,
                    point.Y);
                return element.PointToScreenDPI(leftToRightPoint);
            }

            return element.PointToScreenDPI(point);
        }

        public static T FindVisualAncestor<T>(this DependencyObject obj, bool includeThis) where T : DependencyObject
        {
            if (!includeThis)
                obj = VisualTreeHelper.GetParent(obj);

            while (obj != null && (!(obj is T)))
            {
                obj = VisualTreeHelper.GetParent(obj);
            }

            return obj as T;
        }

        public static bool IsLogicalChildContained<T>(this DependencyObject obj) where T : DependencyObject
        {
            foreach (object child in LogicalTreeHelper.GetChildren(obj))
            {
                if (child is T)
                    return true;

                if (child is DependencyObject)
                {

                    bool res = (child as DependencyObject).IsLogicalChildContained<T>();
                    if (res)
                        return true;
                }
            }

            return false;
        }

        public static T GetLogicalChildContained<T>(this DependencyObject obj) where T : DependencyObject
        {
            foreach (object child in LogicalTreeHelper.GetChildren(obj))
            {
                if (child is T)
                    return child as T;

                if (child is DependencyObject)
                {

                    T childFound = (child as DependencyObject).GetLogicalChildContained<T>();
                    if (childFound != null)
                        return childFound;
                }
            }

            return null;
        }

        public static DockablePane FindChildDockablePane(this DockingManager manager, AnchorStyle desideredAnchor)
        {
            foreach (UIElement childObject in LogicalTreeHelper.GetChildren(manager))
            {
                DockablePane foundPane = FindChildDockablePane(childObject, desideredAnchor);
                if (foundPane != null)
                    return foundPane;
            }

            return null;
        }

        static DockablePane FindChildDockablePane(UIElement parent, AnchorStyle desideredAnchor)
        {
            if (parent is DockablePane && ((DockablePane)parent).Anchor == desideredAnchor)
                return parent as DockablePane;

            if (parent is ResizingPanel)
            {
                foreach (UIElement childObject in ((ResizingPanel)parent).Children)
                {
                    DockablePane foundPane = FindChildDockablePane(childObject, desideredAnchor);
                    if (foundPane != null)
                        return foundPane;
                }
            }

            return null;
        }


        public static Point PointToScreenDPI(this Visual visual, Point pt)
        {
            Point resultPt = visual.PointToScreen(pt);
            return TransformToDeviceDPI(visual, resultPt);
        }

        public static Point TransformToDeviceDPI(this Visual visual, Point pt)
        {
            Matrix m = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice;
            return new Point(pt.X / m.M11, pt.Y /m.M22);
        }
    }
}
