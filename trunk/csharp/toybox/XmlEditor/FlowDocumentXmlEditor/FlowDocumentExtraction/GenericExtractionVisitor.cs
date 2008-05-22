using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using urakawa.core.visitor;
using urakawa.core;
using urakawa.property.channel;
using System.Windows.Documents;
using urakawa.media;
using urakawa.property.xml;

namespace FlowDocumentXmlEditor.FlowDocumentExtraction
{
    public abstract class GenericExtractionVisitor : ITreeNodeVisitor
    {
        private Channel mTextChannel;
        public Channel TextChannel { get { return mTextChannel; } }

        public GenericExtractionVisitor(Channel textCh)
        {
            mTextChannel = textCh;
        }

        public Run GetRun(TreeNode node)
        {
            if (TextChannel != null)
            {
                ChannelsProperty chProp = node.getProperty<ChannelsProperty>();
                if (chProp != null)
                {
                    TextMedia text = chProp.getMedia(TextChannel) as TextMedia;
                    if (text != null)
                    {
                        return new Run(text.getText());
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
            Run nodeRun = GetRun(node);
            XmlProperty xmlProp = node.getProperty<XmlProperty>();
            if (xmlProp!=null)
            {
                if (!HandleXmlElement(xmlProp, node, nodeRun)) return false;
            }
            if (nodeRun!=null) HandleNodeRun(nodeRun);
            return true;
        }

        #endregion

        protected abstract bool HandleXmlElement(XmlProperty xmlProp, TreeNode node, Run nodeRun);

        protected abstract void HandleNodeRun(Run nodeRun);
    }
}
