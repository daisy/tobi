using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Tobi.Common.UI
{
    public static class VisualLogicalTreeWalkHelper
    {
        public static IEnumerable<T> FindObjectsInLogicalTreeWithMatchingType<T>(
            DependencyObject parent, Func<T, bool> filterCondition)
            where T : DependencyObject
        {
            if (parent is T)
            {
                if (filterCondition == null
                    || filterCondition((T)parent))
                {
                    yield return (T)parent;
                }
            }

            foreach (object obj in LogicalTreeHelper.GetChildren(parent))
            {
                var child = obj as DependencyObject;
                if (child == null) continue;

                var childOfChild = FindObjectInLogicalTreeWithMatchingType<T>(child, filterCondition);
                if (childOfChild != null)
                {
                    yield return childOfChild;
                }
            }

            yield break;
        }

        public static T FindObjectInLogicalTreeWithMatchingType<T>(
            DependencyObject parent, Func<T, bool> filterCondition)
            where T : DependencyObject
        {
            if (parent is T)
            {
                if (filterCondition == null
                    || filterCondition((T)parent))
                {
                    return (T)parent;
                }
            }

            foreach (object obj in LogicalTreeHelper.GetChildren(parent))
            {
                var child = obj as DependencyObject;
                if (child == null) continue;

                var childOfChild = FindObjectInLogicalTreeWithMatchingType<T>(child, filterCondition);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }

        public static IEnumerable<T> FindObjectsInVisualTreeWithMatchingType<T>(
            DependencyObject parent, Func<T, bool> filterCondition)
            where T : DependencyObject
        {
            if (parent is T)
            {
                if (filterCondition == null
                    || filterCondition((T)parent))
                {
                    yield return (T)parent;
                }
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                var childOfChild = FindObjectInVisualTreeWithMatchingType<T>(child, filterCondition);
                if (childOfChild != null)
                {
                    yield return childOfChild;
                }
            }

            yield break;
        }
        public static T FindObjectInVisualTreeWithMatchingType<T>(
            DependencyObject parent, Func<T, bool> filterCondition)
            where T : DependencyObject
        {
            if (parent is T)
            {
                if (filterCondition == null
                    || filterCondition((T)parent))
                {
                    return (T)parent;
                }
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                var childOfChild = FindObjectInVisualTreeWithMatchingType<T>(child, filterCondition);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }

        public static IEnumerable<DependencyObject> GetElements(DependencyObject parent, bool nonRecursiveTreeParsingAlgorithm, bool leafFirst, bool logicalInsteadOfVisualTreeScan)
        {
            if (leafFirst)
            {
                if (nonRecursiveTreeParsingAlgorithm)
                    return WalkLeafFirst_NonRecursive(parent, logicalInsteadOfVisualTreeScan);

                return WalkLeafFirst_Recursive(parent, logicalInsteadOfVisualTreeScan);
            }

            if (nonRecursiveTreeParsingAlgorithm)
                return WalkDepthFirst_NonRecursive(parent, logicalInsteadOfVisualTreeScan);

            return WalkDepthFirst_Recursive(parent, logicalInsteadOfVisualTreeScan);
        }

        public static IEnumerable<DependencyObject> WalkDepthFirst_Recursive(DependencyObject root, bool logicalInsteadOfVisualTreeScan)
        {
            yield return root;

            if (logicalInsteadOfVisualTreeScan)
            {
                DependencyObject child;
                foreach (object c in LogicalTreeHelper.GetChildren(root))
                    if (null != (child = c as DependencyObject))
                        foreach (DependencyObject subChild in WalkDepthFirst_Recursive(child, true))
                            yield return subChild;
            }
            else
            {
                for (int i = 0; i != VisualTreeHelper.GetChildrenCount(root); ++i)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(root, i);
                    foreach (DependencyObject subChild in WalkDepthFirst_Recursive(child, false))
                        yield return subChild;
                }
            }
        }

        public static IEnumerable<DependencyObject> WalkDepthFirst_NonRecursive(DependencyObject root, bool logicalInsteadOfVisualTreeScan)
        {
            var work = new Stack<DependencyObject>();
            work.Push(root);

            while (work.Count > 0)
            {
                DependencyObject current = work.Pop();
                yield return current;

                var children = new List<DependencyObject>();

                if (logicalInsteadOfVisualTreeScan)
                {
                    foreach (object c in LogicalTreeHelper.GetChildren(current))
                    {
                        var child = c as DependencyObject;
                        if (child != null)
                            children.Add(child);
                    }
                }
                else
                {
                    for (int i = 0; i != VisualTreeHelper.GetChildrenCount(current); ++i)
                    {
                        DependencyObject child = VisualTreeHelper.GetChild(current, i);
                        children.Add(child);
                    }
                }


                for (int i = children.Count - 1; i >= 0; i--)
                    work.Push(children[i]);
            }
        }

        public static IEnumerable<DependencyObject> WalkLeafFirst_Recursive(DependencyObject root, bool logicalInsteadOfVisualTreeScan)
        {
            if (logicalInsteadOfVisualTreeScan)
            {
                DependencyObject child;
                foreach (object c in LogicalTreeHelper.GetChildren(root))
                    if (null != (child = c as DependencyObject))
                        foreach (DependencyObject subChild in WalkLeafFirst_Recursive(child, true))
                            yield return subChild;

            }
            else
            {
                for (int i = 0; i != VisualTreeHelper.GetChildrenCount(root); ++i)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(root, i);
                    foreach (DependencyObject subChild in WalkLeafFirst_Recursive(child, false))
                        yield return subChild;
                }
            }

            yield return root;
        }

        public static IEnumerable<DependencyObject> WalkLeafFirst_NonRecursive(DependencyObject root, bool logicalInsteadOfVisualTreeScan)
        {
            var work = new Stack<KeyValuePair<DependencyObject, bool>>();

            work.Push(new KeyValuePair<DependencyObject, bool>(root, false));

            while (work.Count > 0)
            {
                KeyValuePair<DependencyObject, bool> current = work.Pop();
                if (current.Value)
                    yield return current.Key;
                else
                {
                    work.Push(new KeyValuePair<DependencyObject, bool>(current.Key, true));

                    var children = new List<DependencyObject>();

                    if (logicalInsteadOfVisualTreeScan)
                    {
                        foreach (object c in LogicalTreeHelper.GetChildren(current.Key))
                        {
                            var child = c as DependencyObject;
                            if (child != null)
                                children.Add(child);
                        }
                    }
                    else
                    {
                        for (int i = 0; i != VisualTreeHelper.GetChildrenCount(current.Key); ++i)
                        {
                            DependencyObject child = VisualTreeHelper.GetChild(current.Key, i);
                            children.Add(child);
                        }
                    }

                    for (int i = children.Count - 1; i >= 0; i--)
                        work.Push(new KeyValuePair<DependencyObject, bool>(children[i], false));
                }
            }
        }
    }
}
