using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;

namespace Tobi.Infrastructure.UI
{
    /*
     * Example of use:
     * 
     * foreach (Run curRun in LogicalTreeHelperHelper.GetChildren<Run>(flowDocumentReader.Document, true)) 
     *          curRun.MouseDown += new MouseButtonEventHandler(curRun_MouseDown);
     */
    public static class LogicalTreeHelperHelper
    {
        public static IEnumerable GetChildren(DependencyObject obj, Boolean allChildrenInHierarchy)
        {
            if (!allChildrenInHierarchy)
            {
                return LogicalTreeHelper.GetChildren(obj);
            }
            var children = new List<object>();
            GetAllChildren(obj, children);
            return children;
        }

        private static void GetAllChildren(DependencyObject obj, ICollection<object> children)
        {
            foreach (object child in LogicalTreeHelper.GetChildren(obj))
            {
                children.Add(child);

                if (child is DependencyObject)
                {
                    GetAllChildren((DependencyObject)child, children);
                }
            }
        }

        public static IEnumerable<T> GetChildren<T>(DependencyObject obj, Boolean allChildrenInHierarchy)
        {
            foreach (object child in GetChildren(obj, allChildrenInHierarchy))
            {
                if (child is T)
                {
                    yield return (T) child;
                }
            }
        }
    }
}
