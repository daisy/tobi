using System.ComponentModel;
using System.Windows.Data;
using urakawa.events;
using urakawa.media;

namespace Tobi.Common._UnusedCode
{
    public class BindableRunTextMedia : BindableRun
    {
        public TextMedia TextMedia
        {
            get;
            set;
        }

        public BindableRunTextMedia(TextMedia tmedia)
        {
            TextMedia = tmedia;

            var binding = new TextMediaBinding
                              {
                                  BoundTextMedia = TextMedia,
                                  Mode = BindingMode.TwoWay
                              };
            SetBinding(TextProperty, binding);
        }

        public override void InvalidateBinding()
        {
            BindingExpression bindExpr = GetBindingExpression(TextProperty);
            if (bindExpr != null)
            {
                var bind = bindExpr.ParentBinding as TextMediaBinding;
                if (bind != null)
                {
                    bind.RemoveDataModelListener();
                }
            }
        }
    }

    public class TextMediaBinding : Binding, INotifyPropertyChanged
    {
        public TextMediaBinding() : base("Text") { }

        private TextMedia m_BoundTextMedia;

        public void RemoveDataModelListener()
        {
            if (m_BoundTextMedia != null)
            {
                m_BoundTextMedia.Changed -= OnBoundTextMediaChanged;
            }
        }
        public TextMedia BoundTextMedia
        {
            get
            {
                return m_BoundTextMedia;
            }
            set
            {
                RemoveDataModelListener();

                m_BoundTextMedia = value;
                if (m_BoundTextMedia != null)
                {
                    m_BoundTextMedia.Changed += OnBoundTextMediaChanged;
                }
                FirePropertyChanged("Text");
                Source = this;
            }
        }

        public string Text
        {
            get
            {
                if (BoundTextMedia != null) return BoundTextMedia.Text;
                return null;
            }
            set
            {
                if (BoundTextMedia != null && BoundTextMedia.Text != value)
                {
                    BoundTextMedia.Text = value;
                }
            }
        }

        private void OnBoundTextMediaChanged(object sender, DataModelChangedEventArgs e)
        {
            FirePropertyChanged("Text");
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void FirePropertyChanged(string propName)
        {
            PropertyChangedEventHandler d = PropertyChanged;
            if (d != null) d(this, new PropertyChangedEventArgs(propName));
        }

        #endregion INotifyPropertyChanged
    }
}