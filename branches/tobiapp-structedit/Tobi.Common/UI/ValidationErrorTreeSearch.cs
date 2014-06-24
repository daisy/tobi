using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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
            {
                if (System.Windows.Controls.Validation.GetHasError(obj)
                    || CheckAllValidationErrors(obj))
                    yield return obj;
            }
        }

        public static bool CheckAllValidationErrors(DependencyObject dependencyObject, DependencyProperty dependencyProperty)
        {
            bool isValid = true;

            if (BindingOperations.IsDataBound(dependencyObject, dependencyProperty))
            {
                Binding binding = BindingOperations.GetBinding(dependencyObject, dependencyProperty);
                MultiBinding multiBinding = null;
                if (binding == null)
                {
                    multiBinding = BindingOperations.GetMultiBinding(dependencyObject, dependencyProperty);
                    if (multiBinding == null) return true;
                }

                if (binding != null && binding.ValidationRules.Count > 0
                    ||
                    multiBinding != null && multiBinding.ValidationRules.Count > 0)
                {
                    BindingExpression bindingExpression = BindingOperations.GetBindingExpression(dependencyObject, dependencyProperty);
                    MultiBindingExpression multiBindingExpression = null;
                    if (bindingExpression == null)
                    {
                        multiBindingExpression = BindingOperations.GetMultiBindingExpression(dependencyObject, dependencyProperty);
                        if (multiBindingExpression == null) return true;
                    }

                    BindingExpressionBase expression = bindingExpression ?? (BindingExpressionBase)multiBindingExpression;

                    switch (binding != null ? binding.Mode : multiBinding.Mode)
                    {
                        case BindingMode.OneTime:
                        case BindingMode.OneWay:
                            expression.UpdateTarget();
                            break;
                        default:
                            expression.UpdateSource();
                            break;
                    }

                    if (expression.HasError)
                        isValid = false;
                }


                // Another method:
                foreach (ValidationRule rule in (binding != null ? binding.ValidationRules : multiBinding.ValidationRules))
                {
                    ValidationResult result = rule.Validate(dependencyObject.GetValue(dependencyProperty), null);
                    if (!result.IsValid)
                    {
                        BindingExpression bindingExpression = BindingOperations.GetBindingExpression(dependencyObject, dependencyProperty);
                        MultiBindingExpression multiBindingExpression = null;
                        if (bindingExpression == null)
                        {
                            multiBindingExpression = BindingOperations.GetMultiBindingExpression(dependencyObject, dependencyProperty);
                            if (multiBindingExpression == null) continue;
                        }

                        BindingExpressionBase expression = bindingExpression ?? (BindingExpressionBase) multiBindingExpression;

                        System.Windows.Controls.Validation.MarkInvalid(expression, new ValidationError(rule, expression, result.ErrorContent, null));
                        isValid = false;
                    }
                }
            }

            return isValid;
        }

        public static bool CheckAllValidationErrors(DependencyObject dependencyObject)
        {
            bool isValid = true;

            // The local values do not work with DataTemplates (but should show attached properties)...
            LocalValueEnumerator localValues = dependencyObject.GetLocalValueEnumerator();
            while (localValues.MoveNext())
            {
                LocalValueEntry entry = localValues.Current;
                isValid = CheckAllValidationErrors(dependencyObject, entry.Property);
            }

            //...we introspect the type using reflection to figure-out additional DPs that did not surface through the local values.
            foreach (DependencyProperty dependencyProperty in GetDependencyProperties(dependencyObject.GetType()))
            {
                isValid = CheckAllValidationErrors(dependencyObject, dependencyProperty);
            }

            return isValid;
        }

        private static Dictionary<Type, List<DependencyProperty>> TypeDependencyPropertiesCache = new Dictionary<Type, List<DependencyProperty>>();

        public static List<DependencyProperty> GetDependencyProperties(Type t)
        {
            if (TypeDependencyPropertiesCache.ContainsKey(t))
                return TypeDependencyPropertiesCache[t];

            FieldInfo[] properties = t.GetFields(
                BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            var dps = new List<DependencyProperty>();
            foreach (FieldInfo field in properties)
                if (field.FieldType == typeof(DependencyProperty))
                    dps.Add((DependencyProperty)field.GetValue(null));

            TypeDependencyPropertiesCache.Add(t, dps);
            return dps;
        }
    }
}
