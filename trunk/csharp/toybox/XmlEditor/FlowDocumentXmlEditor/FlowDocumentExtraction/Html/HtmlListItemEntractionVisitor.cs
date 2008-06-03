using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using urakawa.property.xml;
using urakawa.core;
using urakawa.property.channel;

namespace FlowDocumentXmlEditor.FlowDocumentExtraction.Html
{
    public class HtmlListItemEntractionVisitor : HtmlGenericExtractionVisitor
    {
        public HtmlListItemEntractionVisitor(Channel textCh) : base(textCh) { }

        private List<ListItem> mExtractedListItems = new List<ListItem>();
        public List<ListItem> ExtractedListItems { get { return mExtractedListItems; } }

        protected override bool HandleHtmlElement(XmlProperty xmlProp, TreeNode node, Inline nodeRun)
        {
            switch (xmlProp.getLocalName())
            {
                case "li":
                    ListItem newItem = new ListItem();
                    if (HtmlBlockExtractionVisitor.ContainsHtmlBlockChild(node))
                    {
                        HandleBlocks(newItem.Blocks, node, nodeRun);
                    }
                    else
                    {
                        Paragraph para = new Paragraph();
                        HandleInlines(para.Inlines, node, nodeRun);
                        newItem.Blocks.Add(para);
                    }
                    ExtractedListItems.Add(newItem);
                    return false;
                default:
                    break;
            }
            return true;
        }

        protected override void HandleNodeRun(Inline nodeRun)
        {
            ExtractedListItems.Add(new ListItem(new Paragraph(nodeRun)));
        }
    }
}
