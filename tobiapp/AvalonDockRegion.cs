using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Practices.Composite.Presentation.Regions;
using Tobi.Infrastructure;

namespace AvalonDock
{
    /// <summary>
    /// Region subclass that handles Selector views
    /// </summary>
    public class AvalonDockRegion : Region
    {
        private Selector m_selector;

        private readonly Dictionary<object, ManagedContent> m_viewToPaneMap = new Dictionary<object, ManagedContent>();

        public AvalonDockRegion()
        {
            Views.CollectionChanged += OnViewsCollectionChanged;
        }

        private void OnViewsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (m_selector != null)
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    ManagedContent managedContent = null;
                    object newView = e.NewItems[0];
                    string header = GetHeaderInfoFromView(newView);
                    if (m_selector is DockablePane)
                    {
                        managedContent = new DockableContent
                                             {
                                                 Name = Name,
                                                 Content = newView,
                                                 DockableStyle = DockableStyle.Document,
                                                 Title = header,
                                                 Background = null,
                                                 IsEnabled = true
                                             };
                    }
                    else if (m_selector is DocumentPane)
                    {
                        managedContent = new DocumentContent
                                             {
                                                 Name = Name,
                                                 Content = newView,
                                                 Title = header,
                                                 Background = null,
                                                 IsEnabled = true
                                             };
                        ((DocumentContent) managedContent).Closed += OnDocumentContentClosed;
                    }

                    if (managedContent != null)
                    {
                        m_viewToPaneMap.Add(newView, managedContent);
                        m_selector.Items.Add(managedContent);
                    }
                }
                else if (e.Action == NotifyCollectionChangedAction.Move)
                {
                }
                else if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                }
                else if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                }
                else if (e.Action == NotifyCollectionChangedAction.Replace)
                {
                }
            }
        }

        private void OnDocumentContentClosed(object sender, System.EventArgs e)
        {
            foreach (KeyValuePair<object, ManagedContent> pair in m_viewToPaneMap)
            {
                if (pair.Value == sender)
                {
                    Remove(pair.Key);
                    return;
                }
            }
        }

        public void Bind(UIElement content)
        {

            if (content is ContentControl)
            {
                foreach (var view in Views)
                {
                    ((ContentControl)content).Content = view;

                    m_viewToPaneMap.Clear();
                    m_selector = null;
                    break;
                }
            }
            else if (content is Selector)
            {
                m_viewToPaneMap.Clear();
                m_selector = content as Selector;
            }
        }

        public override void Activate(object view)
        {
            base.Activate(view);
            ManagedContent managedContent;
            if (m_viewToPaneMap.TryGetValue(view, out managedContent))
            {
                m_selector.SelectedItem = managedContent;
            }
        }

        protected virtual string GetHeaderInfoFromView(object view)
        {
            string ret = null;
            if (view is FrameworkElement)
            {
                object dc = (view as FrameworkElement).DataContext;
                if (dc is IHeaderInfoProvider)
                {
                    ret = (dc as IHeaderInfoProvider).HeaderInfo;
                }
            }
            return ret;
        }
    }
}
