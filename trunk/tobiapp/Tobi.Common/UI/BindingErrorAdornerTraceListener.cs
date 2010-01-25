using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace Tobi.Common.UI
{
    // See http://nickdarnell.wordpress.com/2010/01/21/wpf-how-can-i-debug-wpf-bindings-awesomely/
    public class BindingErrorAdornerTraceListener : DefaultTraceListener
    {
        private DispatcherOperation m_DispatcherOperation;

        private readonly bool m_LogicalInsteadOfVisualTreeScan;

        public BindingErrorAdornerTraceListener(bool logicalInsteadOfVisualTreeScan)
        {
            m_LogicalInsteadOfVisualTreeScan = logicalInsteadOfVisualTreeScan;
        }

        public override void TraceEvent(TraceEventCache eventCache,
            string source, TraceEventType eventType, int id,
            string format, params object[] args)
        {
            ReportAllErrors();
        }


        private void ReportAllErrors()
        {
            if (m_DispatcherOperation != null)
                return;

            m_DispatcherOperation = Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.DataBind,
                (Action)delegate()
                {
                    foreach (Window w in Application.Current.Windows)
                    {
                        IEnumerable<DependencyObject> enumeration = VisualLogicalTreeWalkHelper.GetElements(w, false, false, m_LogicalInsteadOfVisualTreeScan);
                        foreach (var element in enumeration)
                        {
                            checkErrors(element);
                        }
                    }

                    m_DispatcherOperation = null;
                });
        }

        private static void checkErrors(DependencyObject parent)
        {
            LocalValueEnumerator localValues = parent.GetLocalValueEnumerator();
            while (localValues.MoveNext())
            {
                LocalValueEntry entry = localValues.Current;
                if (BindingOperations.IsDataBound(parent, entry.Property))
                {
                    BindingExpression binding =
                        BindingOperations.GetBindingExpression(parent, entry.Property);

                    if (binding == null // Not possible because of IsDataBound() check, but we leave it here to remove the warning
                        || binding.DataItem == null)
                        continue;

                    if (binding.Status == BindingStatus.PathError)
                    {
                        var element = parent as FrameworkElement;
                        if (element != null)
                        {
                            var elementAdornerLayer = AdornerLayer.GetAdornerLayer(element);
                            if (elementAdornerLayer == null) continue;

                            Adorner[] adorners = elementAdornerLayer.GetAdorners(element);
                            if (adorners != null)
                            {
                                foreach (Adorner a in adorners)
                                {
                                    if (a is BindingErrorBorderAdorner)
                                        continue;
                                }
                            }

                            elementAdornerLayer.Add(new BindingErrorBorderAdorner(element));
                        }
                    }
                }
            }
        }
    }

    public class BindingErrorBorderAdorner : Adorner
    {
        public BindingErrorBorderAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
        }

        protected override void OnRender(DrawingContext dc)
        {
            var rect = new Rect(0, 0, AdornedElement.RenderSize.Width, AdornedElement.RenderSize.Height);
            var pen = new Pen(new SolidColorBrush(System.Windows.Media.Colors.OrangeRed), 3);

            dc.DrawRectangle(Brushes.Transparent, pen, rect);
        }
    }
}
