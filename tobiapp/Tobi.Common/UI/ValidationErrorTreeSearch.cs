using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace Tobi.Common.UI
{
    public static class ValidationErrorTreeSearch
    {
#if DEBUG
        public static int CheckTreeWalking(DependencyObject parent, bool checkForValidationErrors, bool leafFirst, bool logicalInsteadOfVisualTreeScan)
        {
            IEnumerator<DependencyObject> enumerator;
            enumerator = checkForValidationErrors
                ? GetElementsWithErrors(parent, true, leafFirst, logicalInsteadOfVisualTreeScan).GetEnumerator()
                : VisualLogicalTreeWalkHelper.GetElements(parent, true, leafFirst, logicalInsteadOfVisualTreeScan).GetEnumerator();

            enumerator.MoveNext(); // first

            int size = 0;

            foreach (DependencyObject obj in checkForValidationErrors
                ? GetElementsWithErrors(parent, false, leafFirst, logicalInsteadOfVisualTreeScan)
                : VisualLogicalTreeWalkHelper.GetElements(parent, false, leafFirst, logicalInsteadOfVisualTreeScan))
            {
                size++;

                Debug.Assert(ReferenceEquals(obj, enumerator.Current), "Tree element differs !");

                enumerator.MoveNext();
            }

            return size;
        }
#endif

        public static IEnumerable<DependencyObject> GetElementsWithErrors(DependencyObject parent, bool nonRecursiveTreeParsingAlgorithm, bool leafFirst, bool logicalInsteadOfVisualTreeScan)
        {
            if (leafFirst)
            {
                if (nonRecursiveTreeParsingAlgorithm)
                    return FilterElementsWithErrors(VisualLogicalTreeWalkHelper.WalkLeafFirst_NonRecursive(parent, logicalInsteadOfVisualTreeScan));

                return FilterElementsWithErrors(VisualLogicalTreeWalkHelper.WalkLeafFirst_Recursive(parent, logicalInsteadOfVisualTreeScan));
            }

            if (nonRecursiveTreeParsingAlgorithm)
                return FilterElementsWithErrors(VisualLogicalTreeWalkHelper.WalkDepthFirst_NonRecursive(parent, logicalInsteadOfVisualTreeScan));

            return FilterElementsWithErrors(VisualLogicalTreeWalkHelper.WalkDepthFirst_Recursive(parent, logicalInsteadOfVisualTreeScan));
        }

        public static DependencyObject GetFirstElementWithErrors(DependencyObject parent, bool nonRecursiveTreeParsingAlgorithm, bool leafFirst, bool logicalInsteadOfVisualTreeScan)
        {
            if (leafFirst)
            {
                if (nonRecursiveTreeParsingAlgorithm)
                {
                    foreach (
                        DependencyObject errorObj in
                            FilterElementsWithErrors(VisualLogicalTreeWalkHelper.WalkLeafFirst_NonRecursive(parent, logicalInsteadOfVisualTreeScan))
                        )
                        return errorObj;
                }
                else
                {
                    foreach (
                        DependencyObject errorObj in
                            FilterElementsWithErrors(VisualLogicalTreeWalkHelper.WalkLeafFirst_Recursive(parent, logicalInsteadOfVisualTreeScan)))
                        return errorObj;
                }
            }
            else
            {
                if (nonRecursiveTreeParsingAlgorithm)
                {
                    foreach (
                        DependencyObject errorObj in
                            FilterElementsWithErrors(VisualLogicalTreeWalkHelper.WalkDepthFirst_NonRecursive(parent, logicalInsteadOfVisualTreeScan))
                        )
                        return errorObj;
                }
                else
                {
                    foreach (
                        DependencyObject errorObj in
                            FilterElementsWithErrors(VisualLogicalTreeWalkHelper.WalkDepthFirst_Recursive(parent, logicalInsteadOfVisualTreeScan)))
                        return errorObj;
                }
            }

            return null;
        }

        public static IEnumerable<DependencyObject> FilterElementsWithErrors(IEnumerable<DependencyObject> allChildren)
        {
            foreach (DependencyObject obj in allChildren)
                if (System.Windows.Controls.Validation.GetHasError(obj))
                    yield return obj;
        }
    }
}
