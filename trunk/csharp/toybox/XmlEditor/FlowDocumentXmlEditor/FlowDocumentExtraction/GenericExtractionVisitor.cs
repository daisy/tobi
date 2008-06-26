using urakawa.core.visitor;
using urakawa.core;
using urakawa.property.channel;
using System.Windows.Documents;
using urakawa.media;
using urakawa.property.xml;
using System.Windows.Controls;
using System.Windows;

namespace FlowDocumentXmlEditor.FlowDocumentExtraction
{
    public abstract class GenericExtractionVisitor : ITreeNodeVisitor
    {
        private Channel mTextChannel;
        public Channel TextChannel { get { return mTextChannel; } }

        protected GenericExtractionVisitor(Channel textCh)
        {
            mTextChannel = textCh;
        }

        public Inline GetTextInline(TreeNode node)
        {
            if (TextChannel != null)
            {
                ChannelsProperty chProp = node.getProperty<ChannelsProperty>();
                if (chProp != null)
                {
                    TextMedia text = chProp.getMedia(TextChannel) as TextMedia;
                    if (text != null)
                    {
                        TextBox tb = new TextBox();
                        tb.BorderThickness = new Thickness(1);
                        
                        TextMediaBinding binding = new TextMediaBinding();
                        binding.BoundTextMedia = text;
                        binding.Mode = System.Windows.Data.BindingMode.TwoWay;
                        tb.SetBinding(TextBox.TextProperty, binding);
                        tb.Text = text.getText();
                        tb.Focusable = true;
                        InlineUIContainer res = new InlineUIContainer(tb);
                        return res;
                    }
                }
            }
            return null;
        }

        #region ITreeNodeVisitor Members

        public void postVisit(TreeNode node)
        {
            //Do nothing
        }

        public virtual bool preVisit(TreeNode node)
        {
            Inline nodeRun = GetTextInline(node);
            XmlProperty xmlProp = node.getProperty<XmlProperty>();
            if (xmlProp!=null)
            {
                if (!HandleXmlElement(xmlProp, node, nodeRun)) return false;
            }
            if (nodeRun!=null) HandleNodeRun(nodeRun);
            return true;
        }

        #endregion

        protected abstract bool HandleXmlElement(XmlProperty xmlProp, TreeNode node, Inline nodeRun);

        protected abstract void HandleNodeRun(Inline nodeRun);
    }
}
