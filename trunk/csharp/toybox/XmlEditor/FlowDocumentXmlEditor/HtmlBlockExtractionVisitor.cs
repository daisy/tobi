using System.Collections.Generic;
using urakawa.core.visitor;
using System.Windows.Documents;
using urakawa.property.xml;
using urakawa.property.channel;
using urakawa.media;
using urakawa.core;

namespace FlowDocumentXmlEditor
{
    public class HtmlBlockExtractionVisitor : ITreeNodeVisitor
    {
        public HtmlBlockExtractionVisitor() :this(null)
        {
        }

        public HtmlBlockExtractionVisitor(Channel textCh)
        {
            mTextChannel = textCh;
        }

        public static string HtmlNS = "http://www.w3.org/1999/xhtml";

        private Channel mTextChannel;

        public Channel TextChannel { get { return mTextChannel; } }

        private List<Block> mExtractedBlocks = new List<Block>();

        public List<Block> ExtractedBlocks
        {
            get { return mExtractedBlocks; }
        }

        #region ITreeNodeVisitor Members

        public void postVisit(TreeNode node)
        {
            //Do nothing in post
        }

        public bool preVisit(TreeNode node)
        {
            Run thisRun = HtmlInlineExtractionVisitor.GetRun(node, TextChannel);
            XmlProperty xmlProp = node.getProperty<XmlProperty>();
            if (xmlProp != null)
            {
                if (xmlProp.getNamespaceUri() == HtmlNS)
                {
                    switch (xmlProp.getLocalName())
                    {
                        case "p":
                            Paragraph newPara = new Paragraph();
                            if (thisRun != null) newPara.Inlines.Add(thisRun);
                            if (node.getChildCount() > 0)
                            {
                                HtmlInlineExtractionVisitor inlineVisitor = new HtmlInlineExtractionVisitor(TextChannel);
                                foreach (TreeNode child in node.getListOfChildren())
                                {
                                    child.acceptDepthFirst(inlineVisitor);
                                }
                                newPara.Inlines.AddRange(inlineVisitor.ExtractedInlines);
                            }
                            if (newPara.Inlines.Count == 0) newPara.Inlines.Add(new Run(""));
                            mExtractedBlocks.Add(newPara);
                            return false;
                        default:
                            break;
                    }
                }
            }
            if (thisRun != null) mExtractedBlocks.Add(new Paragraph(thisRun));
            return true;
        }

        #endregion
    }
}
