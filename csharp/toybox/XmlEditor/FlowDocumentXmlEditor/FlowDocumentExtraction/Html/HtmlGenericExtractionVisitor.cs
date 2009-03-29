using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using urakawa.property.xml;
using urakawa.core;
using System.Windows.Documents;
using urakawa.property.channel;

namespace FlowDocumentXmlEditor.FlowDocumentExtraction.Html
{
    public abstract class HtmlGenericExtractionVisitor : GenericExtractionVisitor
    {
        public HtmlGenericExtractionVisitor(Channel textCh) : base(textCh) { }

        public static string HtmlNamespace = "http://www.w3.org/1999/xhtml";
        protected override bool HandleXmlElement(XmlProperty xmlProp, TreeNode node, Inline nodeRun)
        {
            if (xmlProp.NamespaceUri == HtmlNamespace)
            {
                return HandleHtmlElement(xmlProp, node, nodeRun);
            }
            return true;
        }

        protected abstract bool HandleHtmlElement(XmlProperty xmlProp, TreeNode node, Inline nodeRun);

        protected void HandleInlines(InlineCollection destInlineColl, TreeNode node, Inline nodeRun)
        {
            if (nodeRun != null) destInlineColl.Add(nodeRun);
            if (node.ChildCount > 0)
            {
                HtmlInlineExtractionVisitor inlineVisitor = new HtmlInlineExtractionVisitor(TextChannel);
                foreach (TreeNode child in node.ListOfChildren)
                {
                    child.AcceptDepthFirst(inlineVisitor);
                }
                destInlineColl.AddRange(inlineVisitor.ExtractedInlines);
            }
        }

        protected void HandleBlocks(BlockCollection destBlockColl, TreeNode node, Inline nodeRun)
        {
            if (nodeRun != null) destBlockColl.Add(new Paragraph(nodeRun));
            if (node.ChildCount > 0)
            {
                HtmlBlockExtractionVisitor blockVisitor = new HtmlBlockExtractionVisitor(TextChannel);
                foreach (TreeNode child in node.ListOfChildren)
                {
                    child.AcceptDepthFirst(blockVisitor);
                }
                destBlockColl.AddRange(blockVisitor.ExtractedBlocks);
            }
        }
    }
}
