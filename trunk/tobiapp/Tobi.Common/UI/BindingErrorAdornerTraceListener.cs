using System;
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
                        CheckForBindingErrors(w);
                    }

                    m_DispatcherOperation = null;
                });
        }

        public static void CheckForBindingErrors(DependencyObject parent)
        {
            LocalValueEnumerator localValues = parent.GetLocalValueEnumerator();
            while (localValues.MoveNext())
            {
                LocalValueEntry entry = localValues.Current;
                if (BindingOperations.IsDataBound(parent, entry.Property))
                {
                    BindingExpression binding =
                        BindingOperations.GetBindingExpression(parent, entry.Property);

                    if (binding.DataItem == null)
                        continue;

                    if (binding.Status == BindingStatus.PathError)
                    {
                        // Found binding error
                        FrameworkElement element = parent as FrameworkElement;
                        if (element != null)
                        {
                            var elementAdornerLayer = AdornerLayer.GetAdornerLayer(element);

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

            for (int i = 0; i != VisualTreeHelper.GetChildrenCount(parent); ++i)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                CheckForBindingErrors(child);
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
