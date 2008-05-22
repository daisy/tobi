using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using urakawa;
using urakawa.property.channel;
using urakawa.core;
using FlowDocumentXmlEditor.FlowDocumentExtraction.Html;

namespace FlowDocumentXmlEditor
{
    public class UrakawaHtmlFlowDocument : FlowDocument
    {
        private TreeNode mRootTreeNode;

        public TreeNode RootTreeNode { get { return mRootTreeNode; } }
        private Channel mTextChannel;
        public Channel TextChannel { get { return mTextChannel; } }

        public UrakawaHtmlFlowDocument(TreeNode root, Channel textCh)
        {
            mRootTreeNode = root;
            mTextChannel = textCh;
            LoadFromRoot();
        }

        public void LoadFromRoot()
        {
            Blocks.Clear();
            if (RootTreeNode != null)
            {
                HtmlBlockExtractionVisitor blockVisitor = new HtmlBlockExtractionVisitor(TextChannel);
                RootTreeNode.acceptDepthFirst(blockVisitor);
                Blocks.AddRange(blockVisitor.ExtractedBlocks);
            }
        }
    }
}
