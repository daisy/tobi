using System;
using System.Windows.Data;
using System.ComponentModel;
using urakawa.media;

namespace FlowDocumentXmlEditor.FlowDocumentExtraction
{
    public class TextMediaBinding : Binding, INotifyPropertyChanged
    {
        public TextMediaBinding() : base("Text") { }


        private TextMedia mBoundTextMedia = null;

        public void RemoveDataModelListener() {
            if (mBoundTextMedia != null)
            {
                mBoundTextMedia.changed -= new EventHandler<urakawa.events.DataModelChangedEventArgs>(BoundTextMedia_changed);
            }
        }
        public TextMedia BoundTextMedia
        {
            get
            {
                return mBoundTextMedia;
            }
            set
            {
                RemoveDataModelListener();
              
                mBoundTextMedia = value;
                if (mBoundTextMedia != null)
                {
                    mBoundTextMedia.changed += new EventHandler<urakawa.events.DataModelChangedEventArgs>(BoundTextMedia_changed);
                }
                FirePropertyChanged("Text");
                Source = this;
            }
        }

        public string Text
        {
            get
            {
                if (BoundTextMedia != null) return BoundTextMedia.getText();
                return null;
            }
            set
            {
                if (BoundTextMedia != null && BoundTextMedia.getText() != value)
                {
                    BoundTextMedia.setText(value);
                }
            }
        }

        void BoundTextMedia_changed(object sender, urakawa.events.DataModelChangedEventArgs e)
        {
            FirePropertyChanged("Text");
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void FirePropertyChanged(string propName)
        {
            PropertyChangedEventHandler d = PropertyChanged;
            if (d != null) d(this, new PropertyChangedEventArgs(propName));
        }

        #endregion
    }
}
