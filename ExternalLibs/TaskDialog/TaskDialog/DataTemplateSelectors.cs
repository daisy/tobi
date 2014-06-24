using System.Windows;
using System.Windows.Controls;

namespace Sid.Windows.Controls
{
    /// <summary>
    ///     For Text Content, returns a built-in template to use, otherwise returns null
    /// </summary>
    abstract class TextDataTemplateSelector : DataTemplateSelector
    {
        private readonly string _templateName;

        protected TextDataTemplateSelector(string templateName)
        {
            _templateName = templateName;
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            // if the container has a Template defined, just return it.
            ContentPresenter cp = container as ContentPresenter;
            if (cp != null && cp.ContentTemplate != null) 
                return cp.ContentTemplate;

            // either return the built-in template for a string, or return null 
            if (item is string)
            {
                ComponentResourceKey key = new ComponentResourceKey(typeof(TaskDialog), _templateName);
                DataTemplate t = Application.Current.TryFindResource(key) as DataTemplate;

                return t;
            }
            return null;
        }
    }

    class HeaderDataTemplateSelector : TextDataTemplateSelector
    {
        public HeaderDataTemplateSelector()
            : base("headerDataTemplate")
        { }
    }

    class ContentDataTemplateSelector : TextDataTemplateSelector
    {
        public ContentDataTemplateSelector()
            : base("contentDataTemplate")
        { }
    }

    class DetailDataTemplateSelector : TextDataTemplateSelector
    {
        public DetailDataTemplateSelector()
            : base("detailDataTemplate")
        { }
    }
    class FooterDataTemplateSelector : TextDataTemplateSelector
    {
        public FooterDataTemplateSelector()
            : base("footerDataTemplate")
        { }
    }

}
