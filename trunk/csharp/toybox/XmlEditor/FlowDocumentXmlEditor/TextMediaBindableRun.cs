using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using urakawa.media;
using FlowDocumentXmlEditor.FlowDocumentExtraction;

namespace FlowDocumentXmlEditor
{
    public class TextMediaBindableRun : BindableRun
    {
        private TextMedia mTextMedia = null;

        public TextMedia TextMedia
        {
            get
            {
                return mTextMedia;
            }
            set { mTextMedia = value; }
        }
        public TextMediaBindableRun(TextMedia tmedia)
        {
            TextMedia = tmedia;

            TextMediaBinding binding = new TextMediaBinding();
            binding.BoundTextMedia = TextMedia;
            binding.Mode = System.Windows.Data.BindingMode.TwoWay;
            SetBinding(TextProperty, binding);
        }
    }
}
