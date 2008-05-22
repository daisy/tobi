using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using urakawa.property.channel;
using System.Windows.Documents;
using urakawa.property.xml;
using urakawa.core;

namespace FlowDocumentXmlEditor.FlowDocumentExtraction.Html
{
    public class HtmlInlineExtractionVisitor : HtmlGenericExtractionVisitor
    {
        private List<Inline> mExtractedInlines = new List<Inline>();
        public List<Inline> ExtractedInlines { get { return mExtractedInlines; } }


        public HtmlInlineExtractionVisitor(Channel textCh) : base(textCh) { }

        protected override bool HandleHtmlElement(XmlProperty xmlProp, TreeNode node, Run nodeRun)
        {
            switch (xmlProp.getLocalName())
            {
                case "em":
                    Italic newItalic = new Italic();
                    HandleInlines(newItalic.Inlines, node, nodeRun);
                    ExtractedInlines.Add(newItalic);
                    return false;
                case "strong":
                    Bold newBold = new Bold();
                    HandleInlines(newBold.Inlines, node, nodeRun);
                    ExtractedInlines.Add(newBold);
                    return false;
                case "span":
                    Span newSpan = new Span();
                    HandleInlines(newSpan.Inlines, node, nodeRun);
                    ExtractedInlines.Add(newSpan);
                    return false;
                case "a":
                    Hyperlink link = new Hyperlink();
                    XmlAttribute href = xmlProp.getAttribute("href", "");
                    if (href != null)
                    {
                        link.NavigateUri = new Uri(node.getPresentation().getRootUri(), href.getValue());
                    }
                    HandleInlines(link.Inlines, node, nodeRun);
                    ExtractedInlines.Add(link);
                    return false;
                default:
                    return true;
            }
        }

        protected override void HandleNodeRun(Run nodeRun)
        {
            ExtractedInlines.Add(nodeRun);
        }
    }
}
