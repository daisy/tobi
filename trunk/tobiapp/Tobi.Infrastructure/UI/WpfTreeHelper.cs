using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Tobi.Infrastructure.UI
{
    /*
     * Example of use:
     * 
     * foreach (Run curRun in WpfTreeHelper.GetChildren<Run>(flowDocumentReader.Document, true)) 
     *          curRun.MouseDown += new MouseButtonEventHandler(curRun_MouseDown);
     */
    public static class WpfTreeHelper
    {
        #region find parent

        /// <summary>
        /// Finds a parent of a given item on the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="child">A direct or indirect child of the
        /// queried item.</param>
        /// <returns>The first parent item that matches the submitted
        /// type parameter. If not matching item can be found, a null
        /// reference is being returned.</returns>
        public static T TryFindParent<T>(this DependencyObject child)
            where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = GetParentObject(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                //use recursion to proceed with next level
                return TryFindParent<T>(parentObject);
            }
        }

        /// <summary>
        /// This method is an alternative to WPF's
        /// <see cref="VisualTreeHelper.GetParent"/> method, which also
        /// supports content elements. Keep in mind that for content element,
        /// this method falls back to the logical tree of the element!
        /// </summary>
        /// <param name="child">The item to be processed.</param>
        /// <returns>The submitted item's parent, if available. Otherwise
        /// null.</returns>
        public static DependencyObject GetParentObject(this DependencyObject child)
        {
            if (child == null) return null;
            ContentElement contentElement = child as ContentElement;

            if (contentElement != null)
            {
                DependencyObject parent = ContentOperations.GetParent(contentElement);
                if (parent != null) return parent;

                FrameworkContentElement fce = contentElement as FrameworkContentElement;
                return fce != null ? fce.Parent : null;
            }

            //if it's not a ContentElement, rely on VisualTreeHelper
            return VisualTreeHelper.GetParent(child);
        }

        #endregion

        #region find children

        /// <summary>
        /// Analyzes both visual and logical tree in order to find all elements of a given
        /// type that are descendants of the <paramref name="source"/> item.
        /// </summary>
        /// <typeparam name="T">The type of the queried items.</typeparam>
        /// <param name="source">The root element that marks the source of the search. If the
        /// source is already of the requested type, it will not be included in the result.</param>
        /// <returns>All descendants of <paramref name="source"/> that match the requested type.</returns>
        public static IEnumerable<T> FindChildren<T>(this DependencyObject source) where T : DependencyObject
        {
            if (source != null)
            {
                var childs = GetChildObjects(source);
                foreach (DependencyObject child in childs)
                {
                    //analyze if children match the requested type
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    //recurse tree
                    foreach (T descendant in FindChildren<T>(child))
                    {
                        yield return descendant;
                    }
                }
            }
        }


        /// <summary>
        /// This method is an alternative to WPF's
        /// <see cref="VisualTreeHelper.GetChild"/> method, which also
        /// supports content elements. Keep in mind that for content elements,
        /// this method falls back to the logical tree of the element.
        /// </summary>
        /// <param name="parent">The item to be processed.</param>
        /// <returns>The submitted item's child elements, if available.</returns>
        public static IEnumerable<DependencyObject> GetChildObjects(this DependencyObject parent)
        {
            if (parent == null) yield break;
            ContentElement contentElement = parent as ContentElement;

            if (contentElement != null)
            {
                //use the logical tree for content elements
                foreach (object obj in LogicalTreeHelper.GetChildren(contentElement))
                {
                    var depObj = obj as DependencyObject;
                    if (depObj != null) yield return (DependencyObject)obj;
                }
            }
            else
            {
                //use the visual tree per default
                int count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < count; i++)
                {
                    yield return VisualTreeHelper.GetChild(parent, i);
                }
            }
        }

        #endregion

        #region find from point

        /// <summary>
        /// Tries to locate a given item within the visual tree,
        /// starting with the dependency object at a given position. 
        /// </summary>
        /// <typeparam name="T">The type of the element to be found
        /// on the visual tree of the element at the given location.</typeparam>
        /// <param name="reference">The main element which is used to perform
        /// hit testing.</param>
        /// <param name="point">The position to be evaluated on the origin.</param>
        public static T TryFindFromPoint<T>(UIElement reference, Point point)
            where T : DependencyObject
        {
            DependencyObject element = reference.InputHitTest(point) as DependencyObject;

            if (element == null) return null;
            else if (element is T) return (T)element;
            else return TryFindParent<T>(element);
        }

        #endregion

        /// <summary>
        /// Looks up the first object in the <see cref="System.Windows.Controls.Control.Template"/>
        /// of the control and returns this.
        /// </summary>
        /// <param name="name">The name of the object to look for.</param>
        /// <param name="control">The control to look in.</param>
        /// <returns></returns>
        public static object FindChildInTemplate(string name, Control control)
        {
            if (control != null && name != null)
            {
                ControlTemplate iTemplate = control.Template;
                if (control.IsLoaded == true && iTemplate != null)
                    return iTemplate.FindName(name, control);
            }
            return null;
        }

        /// <summary>
        /// Searches up the visual tree starting at the specified object untill it finds the first object of the specified type or null.
        /// </summary>
        /// <param name="start">The object to start walking up the tree.</param>
        /// <typeparam name="T">The type to look for</typeparam>
        public static T FindInTree<T>(DependencyObject start) where T : class
        {
            while (start != null && !(start is T))
                start = VisualTreeHelper.GetParent(start);
            return start as T;
        }


        /// <summary>
        /// Searches up the Logical tree starting at the specified object untill it finds an object of the specified type or null.
        /// </summary>
        /// <param name="aStart">The object to start walking up the tree.</param>
        /// <param name="aType">The type to look for.</param>
        /// <returns></returns>
        public static T FindInLTree<T>(DependencyObject aStart) where T : class
        {
            while (aStart != null && !(aStart is T))
            {
                aStart = LogicalTreeHelper.GetParent(aStart);
            }
            return aStart as T;
        }

        /// <summary>
        /// Searches up the visual tree starting at the specified point untill it finds an object that has a dataContext of the specified
        /// type.
        /// </summary>
        /// <typeparam name="T">The data type to look for.</typeparam>
        /// <param name="start">The UI element to start looking at.</param>
        /// <returns>The DataContext object that was found.</returns>
        public static T FindInTreeAsDataContext<T>(DependencyObject aStart) where T : class
        {
            while (aStart != null && (aStart is FrameworkElement && !(((FrameworkElement)aStart).DataContext is T)))
            {
                aStart = VisualTreeHelper.GetParent(aStart);
            }
            if (aStart is FrameworkElement)
                return ((FrameworkElement)aStart).DataContext as T;
            else
                return null;
        }

        /// <summary>
        /// Looks up the Panel used by the ItemsControl
        /// </summary>
        /// <remarks>
        /// This function searches all the children of the 
        /// </remarks>
        /// <param name="list"></param>
        /// <returns></returns>
        public static Panel FindPanelFor(ItemsControl list)
        {
            return FindPanelForInternal(list);
        }

        /// <summary>
        /// Recursive part of <see cref="App.FindInPanelFor"/>.
        /// </summary>
        /// <param name="item">The item who's children to walk through in search of the first panel that is an ItemsHost.</param>
        /// <returns></returns>
        private static Panel FindPanelForInternal(DependencyObject item)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(item); i++)
            {
                Panel iPanel = VisualTreeHelper.GetChild(item, i) as Panel;
                if (iPanel != null && iPanel.IsItemsHost == true)
                    return iPanel;
                else
                {
                    Panel iRes = FindPanelForInternal(VisualTreeHelper.GetChild(item, i));
                    if (iRes != null)
                        return iRes;
                }
            }
            return null;
        }
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
