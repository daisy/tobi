using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using urakawa.core.visitor;
using urakawa.property.channel;
using System.Windows.Documents;
using urakawa.property.xml;
using urakawa.media;
using urakawa.core;

namespace FlowDocumentXmlEditor
{
    class HtmlInlineExtractionVisitor : ITreeNodeVisitor
    {

        public static Run GetRun(TreeNode node, Channel textCh)
        {
            if (textCh != null)
            {
                ChannelsProperty chProp = node.getProperty<ChannelsProperty>();
                if (chProp != null)
                {
                    TextMedia text = chProp.getMedia(textCh) as TextMedia;
                    if (text != null)
                    {
                        return new Run(text.getText());
                    }
                }
            }
            return null;
        }

        public HtmlInlineExtractionVisitor() :this(null)
        {
        }

        public HtmlInlineExtractionVisitor(Channel textCh)
        {
            mTextChannel = textCh;
        }

        public static string HtmlNS = "http://www.w3.org/1999/xhtml";

        private Channel mTextChannel;

        public Channel TextChannel { get { return mTextChannel; } }

        private List<Inline> mExtractedInlines = new List<Inline>();

        public List<Inline> ExtractedInlines { get { return mExtractedInlines; } }

        #region ITreeNodeVisitor Members

        public void postVisit(urakawa.core.TreeNode node)
        {
            //Do nothing in post
        }

        public bool preVisit(urakawa.core.TreeNode node)
        {
            Run thisRun = GetRun(node, TextChannel);
            XmlProperty xmlProp = node.getProperty<XmlProperty>();
            if (xmlProp != null)
            {
                if (xmlProp.getNamespaceUri() == HtmlNS)
                {
                    Span inline = new Span();
                    switch (xmlProp.getLocalName())
                    {
                        case "em":
                            inline = new Italic();
                            goto case "span";
                        case "strong":
                            inline = new Bold();
                            goto case "span";
                        case "a":
                            Hyperlink link = new Hyperlink();
                            XmlAttribute href = xmlProp.getAttribute("href", "");
                            if (href != null)
                            {
                                link.NavigateUri = new Uri(node.getPresentation().getRootUri(), href.getValue());
                            }
                            inline = link;
                            goto case "span";
                        case "span":
                            if (thisRun != null) inline.Inlines.Add(thisRun);
                            if (node.getChildCount() > 0)
                            {
                                HtmlInlineExtractionVisitor inlineVisitor = new HtmlInlineExtractionVisitor(TextChannel);
                                foreach (TreeNode child in node.getListOfChildren())
                                {
                                    child.acceptDepthFirst(inlineVisitor);
                                }
                                inline.Inlines.AddRange(inlineVisitor.ExtractedInlines);
                            }
                            if (inline.Inlines.Count == 0) inline.Inlines.Add(new Run(""));
                            mExtractedInlines.Add(inline);
                            return false;
                        default:

                            break;
                    }
                }
            }
            if (thisRun != null) mExtractedInlines.Add(thisRun);
            return true;
        }

        #endregion
    }
}
