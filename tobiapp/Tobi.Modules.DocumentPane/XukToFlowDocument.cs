using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.UI;
using Tobi.Common.UI.XAML;
using urakawa.core;
using urakawa.data;
using urakawa.exception;
using urakawa.media;
using urakawa.media.data.audio;
using urakawa.media.data.image;
using urakawa.media.data.video;
using urakawa.media.timing;
using urakawa.property.xml;
using urakawa.xuk;
using Colors = System.Windows.Media.Colors;

namespace Tobi.Plugin.DocumentPane
{
    //public class RunEx : Run
    //{
    //    private readonly DocumentPaneView m_DocumentPaneView;

    //    public RunEx(DocumentPaneView documentPaneView, string str)
    //        : base(str)
    //    {
    //        m_DocumentPaneView = documentPaneView;
    //    }

    //    protected override void OnMouseDown(MouseButtonEventArgs e)
    //    {
    //        m_DocumentPaneView.OnTextElementMouseDown(this, e);
    //    }
    //    protected override void OnMouseUp(MouseButtonEventArgs e)
    //    {
    //        m_DocumentPaneView.OnTextElementMouseUp(this, e);
    //    }
    //}
    //public class BlockUIContainerEx : BlockUIContainer
    //{
    //    private readonly DocumentPaneView m_DocumentPaneView;

    //    public BlockUIContainerEx(DocumentPaneView documentPaneView)
    //    {
    //        m_DocumentPaneView = documentPaneView;
    //    }

    //    protected override void OnMouseDown(MouseButtonEventArgs e)
    //    {
    //        m_DocumentPaneView.OnTextElementMouseDown(this, e);
    //    }
    //    protected override void OnMouseUp(MouseButtonEventArgs e)
    //    {
    //        m_DocumentPaneView.OnTextElementMouseUp(this, e);
    //    }
    //}
    public delegate void DelegateOnMouseDownTextElementWithNode(TextElement textElem);
    public delegate void DelegateOnRequestNavigate(Uri uri);
    public delegate void DelegateAddIdLinkTarget(string name, TextElement data);
    public delegate void DelegateAddIdLinkSource(string name, TextElement data);

    public partial class XukToFlowDocument : DualCancellableProgressReporter
    {
        private DocumentPaneView m_DocumentPaneView;

        //private Stopwatch m_StopWatch;

        private int m_percentageProgress = 0;
        public override void DoWork()
        {
            m_totalAudioDuration = Time.Zero;
            m_nTreeNode = 0;

            m_percentageProgress = -1;
            reportProgress(m_percentageProgress, Tobi_Plugin_DocumentPane_Lang.ConvertingXukToFlowDocument);

            //if (m_StopWatch != null) m_StopWatch.Stop();
            //m_StopWatch = null;

            try
            {
                walkBookTreeAndGenerateFlowDocument(m_TreeNode, null);
            }
            catch (ProgressCancelledException ex)
            {
                return;
            }
            //finally
            //{
            //    if (m_StopWatch != null) m_StopWatch.Stop();
            //    m_StopWatch = null;
            //}

            EventAggregator.GetEvent<TotalAudioDurationComputedByFlowDocumentParserEvent>().Publish(m_totalAudioDuration);
        }

        protected readonly ILoggerFacade Logger;
        protected readonly IEventAggregator EventAggregator;
        protected readonly IShellView ShellView;

        private long m_nTreeNode;
        private static Time m_totalAudioDuration;

        private static int COUNT = 0;
        ~XukToFlowDocument()
        {
            int debug = COUNT;
            //#if DEBUG
            //            Debugger.Break();
            //#endif
        }

        public XukToFlowDocument(DocumentPaneView documentPaneView, TreeNode node, FlowDocument flowDocument,
            ILoggerFacade logger, IEventAggregator aggregator, IShellView shellView
            //DelegateOnMouseUpFlowDoc delegateOnMouseUpFlowDoc,
            //DelegateOnMouseDownTextElementWithNode delegateOnMouseDownTextElementWithNode,
            //DelegateOnRequestNavigate delegateOnRequestNavigate,
            //DelegateAddIdLinkTarget delegateAddIdLinkTarget,
            //DelegateAddIdLinkSource delegateAddIdLinkSource
            )
        {
            m_DocumentPaneView = documentPaneView;

            COUNT++;


            m_FlowDoc = flowDocument;

            m_TreeNode = node;

            Logger = logger;
            EventAggregator = aggregator;
            ShellView = shellView;

            //m_DelegateOnMouseUpFlowDoc = delegateOnMouseUpFlowDoc;
            //m_DelegateOnMouseDownTextElementWithNode = delegateOnMouseDownTextElementWithNode;
            //m_DelegateOnRequestNavigate = delegateOnRequestNavigate;

            //m_DelegateAddIdLinkTarget = delegateAddIdLinkTarget;
            //m_DelegateAddIdLinkSource = delegateAddIdLinkSource;
        }

        //private DelegateAddIdLinkTarget m_DelegateAddIdLinkTarget;

        //private DelegateAddIdLinkSource m_DelegateAddIdLinkSource;

        public readonly FlowDocument m_FlowDoc = new FlowDocument();

        private TreeNode m_TreeNode;


        private int m_currentTD;
        private bool m_firstTR;
        private int m_currentROWGROUP;
        List<TableCell> m_cellsToExpand = new List<TableCell>();

        private delegate void DelegateSectionInitializer(Section secstion);
        private delegate void DelegateFigureInitializer(Figure fig);
        private delegate void DelegateFloaterInitializer(Floater floater);
        private delegate void DelegateSpanInitializer(Span span);
        private delegate void DelegateParagraphInitializer(Paragraph para);

        //public delegate void DelegateOnMouseUpFlowDoc();
        //private DelegateOnMouseUpFlowDoc m_DelegateOnMouseUpFlowDoc;
        //private void OnMouseUpFlowDoc(object sender, MouseButtonEventArgs e)
        //{
        //    //e.Handled = true;
        //    m_DelegateOnMouseUpFlowDoc();
        //}

        //private DelegateOnMouseDownTextElementWithNode m_DelegateOnMouseDownTextElementWithNode;

        //private void OnMouseDownTextElementWithNode(object sender, MouseButtonEventArgs e)
        //{
        //    //var src = e.Source;
        //    //var obj = FindVisualTreeRoot((DependencyObject) src);

        //    //e.Handled = true;
        //    m_DelegateOnMouseDownTextElementWithNode((TextElement)sender);
        //}

        //private void OnMouseDownTextElementWithNodeAndAudio(object sender, MouseButtonEventArgs e)
        //{
        //    //e.Handled = true;
        //    m_DelegateOnMouseDownTextElementWithNode((TextElement)sender);
        //}

        //private DelegateOnRequestNavigate m_DelegateOnRequestNavigate;

        //private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        //{
        //    //e.Handled = true;
        //    m_DelegateOnRequestNavigate(e.Uri);
        //}


        //DependencyObject FindVisualTreeRoot(DependencyObject initial)
        //{
        //    DependencyObject current = initial;
        //    DependencyObject result = initial;

        //    while (current != null)
        //    {
        //        result = current;
        //        if (current is ContainerVisual)
        //        {
        //            current = VisualTreeHelper.GetParent(current);
        //        }
        //        else if (current is Visual) // || current is Visual3D)
        //        {
        //            current = VisualTreeHelper.GetParent(current);
        //        }
        //        else
        //        {
        //            // If we're in Logical Land then we must walk 
        //            // up the logical tree until we find a 
        //            // Visual/Visual3D to get us back to Visual Land.
        //            current = LogicalTreeHelper.GetParent(current);
        //        }
        //    }

        //    return result;
        //}

        private void formatCaptionCell(TableCell cell)
        {
            //cell.BorderBrush = Brushes.BlueViolet;
            cell.BorderThickness = new Thickness(2);
            //cell.Background = Brushes.LightYellow;
            //cell.Foreground = Brushes.Navy;
        }

        private void formatListHeader(Paragraph data)
        {
            //data.BorderBrush = Brushes.Blue;
            data.BorderThickness = new Thickness(2.0);
            data.Padding = new Thickness(2.0);
            data.FontWeight = FontWeights.Bold;
            data.FontSize = m_FlowDoc.FontSize * 1.2;
            //data.Background = Brushes.Lavender;
            //data.Foreground = Brushes.DarkSlateBlue;
        }

        private void formatPageNumberAndSetId(TreeNode node, TextElement data)
        {
            //setTag(data, node);

            string id = node.GetXmlElementId();

            string pageID = null;

            if (!string.IsNullOrEmpty(id))
            {
                pageID = id;
            }
            else
            {
                TreeNode.StringChunkRange range = node.GetTextFlattened_();
                if (range != null && range.First != null && !string.IsNullOrEmpty(range.First.Str))
                {
                    StringBuilder strBuilder = new StringBuilder(range.GetLength());
                    TreeNode.ConcatStringChunks(range, -1, strBuilder);

                    strBuilder.Replace(" ", "_");
                    strBuilder.Insert(0, "id_tobipage_");

                    pageID = strBuilder.ToString();
                }
            }

            if (!string.IsNullOrEmpty(pageID))
            {
                //string name = IdToName(pageID);
                data.ToolTip = pageID;

                //Logger.Log("-- PublishEvent [PageFoundByFlowDocumentParserEvent] DocumentPaneView.PageFoundByFlowDocumentParserEvent (" + data.Name + ")", Category.Debug, Priority.Medium);

                EventAggregator.GetEvent<PageFoundByFlowDocumentParserEvent>().Publish(node);
            }
        }


        private void SetBorderAndBackColorBasedOnTreeNodeTag(TextElement data)
        {
            SetBorderAndBackColorBasedOnTreeNodeTag(m_DocumentPaneView, data);
        }

        public static void SetBorderAndBackColorBasedOnTreeNodeTag(DocumentPaneView documentPaneView, TextElement data)
        {
            data.Background = null; // Brushes.Transparent; // SystemColors.WindowBrush;

            if (data is Block)
            {
                ((Block)data).BorderBrush = ColorBrushCache.Get(Colors.Transparent);
                //((Block)data).BorderBrush = null;
            }

            if (data.Tag == null || !(data.Tag is TreeNode))
            {
                //data.Foreground = null; // m_ColorBrushCache.Get(Settings.Default.Document_Color_Font_NoAudio);
                return;
            }

            var treeNode = (TreeNode)data.Tag;

            if (!treeNode.HasXmlProperty) return;

            string localName = treeNode.GetXmlElementLocalName();

            if (localName == "pagenum")
            {
                data.Background = ColorBrushCache.Get(Settings.Default.Document_Color_PageNum_Back);
            }
            else if (localName == "a" || localName == "anchor"
                || localName == "annoref" || localName == "noteref")
            {
                data.Background = ColorBrushCache.Get(Settings.Default.Document_Color_Hyperlink_Back);
            }
            else if (localName == "th" || localName == "td")
            {
                DebugFix.Assert(data is TableCell);
                if (data is TableCell)
                {
                    ((TableCell)data).BorderBrush = ColorBrushCache.Get(Settings.Default.Document_Color_Font_Audio);
                }
            }
            else if (localName == "sidebar")
            {
                DebugFix.Assert(data is Section);
                if (data is Section)
                {
                    ((Section)data).BorderBrush = ColorBrushCache.Get(Settings.Default.Document_Color_Font_Audio);
                }
            }
            else if (localName == "imggroup"
                 || localName == "doctitle"
                 || localName == "docauthor"
                 || localName == "covertitle"
                 || localName == "caption"
                 || localName == "note"
                 || localName == "annotation"
                 || localName == "blockquote"
                 || localName == "table"
                )
            {
                DebugFix.Assert(data is Block);
                if (data is Block)
                {
                    ((Block)data).BorderBrush = ColorBrushCache.Get(Settings.Default.Document_Color_Font_NoAudio);
                }
            }
        }

        private void formatPageNumberAndSetId_Span(TreeNode node, Span data)
        {
            //data.BorderBrush = Brushes.Orange;
            //data.BorderThickness = new Thickness(2.0);
            //data.Padding = new Thickness(2.0);
            data.FontWeight = FontWeights.Bold;
            data.FontSize = m_FlowDoc.FontSize * 1.2;
#if DEBUG
            DebugFix.Assert(data.Tag != null);
            DebugFix.Assert(data.Tag is TreeNode);
            DebugFix.Assert(node == data.Tag);

            DebugFix.Assert(node.HasXmlProperty && node.GetXmlElementLocalName() == "pagenum");
#endif
            SetBorderAndBackColorBasedOnTreeNodeTag(data);


            //Brush brushBack = m_ColorBrushCache.Get(Settings.Default.Document_Color_PageNum_Back);
            ////Brush brushFont = m_ColorBrushCache.Get(Settings.Default.Document_Color_PageNum_Font);

            //data.Background = brushBack;
            ////data.Foreground = brushFont;

            formatPageNumberAndSetId(node, data);
        }

        private void formatPageNumberAndSetId_Para(TreeNode node, Paragraph data)
        {
            //data.BorderBrush = Brushes.Orange;
            //data.BorderThickness = new Thickness(2.0);
            data.Padding = new Thickness(2.0);
            data.FontWeight = FontWeights.Bold;
            data.FontSize = m_FlowDoc.FontSize * 1.2;
#if DEBUG
            DebugFix.Assert(data.Tag != null);
            DebugFix.Assert(data.Tag is TreeNode);
            DebugFix.Assert(node == data.Tag);

            DebugFix.Assert(node.HasXmlProperty && node.GetXmlElementLocalName() == "pagenum");
#endif
            SetBorderAndBackColorBasedOnTreeNodeTag(data);


            //Brush brushBack = m_ColorBrushCache.Get(Settings.Default.Document_Color_PageNum_Back);
            ////Brush brushFont = m_ColorBrushCache.Get(Settings.Default.Document_Color_PageNum_Font);

            //data.Background = brushBack;
            ////data.Foreground = brushFont;

            formatPageNumberAndSetId(node, data);
        }


        private void addInline(TextElement parent, Inline data)
        {
            if (parent == null)
            {
                addInlineInBlocks(data, m_FlowDoc.Blocks);
            }
            else if (parent is Paragraph)
            {
                ((Paragraph)parent).Inlines.Add(data);
            }
            else if (parent is Span)
            {
                ((Span)parent).Inlines.Add(data);
            }
            else if (parent is TableCell)
            {
                addInlineInBlocks(data, ((TableCell)parent).Blocks);
            }
            else if (parent is Section)
            {
                addInlineInBlocks(data, ((Section)parent).Blocks);
            }
            else if (parent is Floater)
            {
                addInlineInBlocks(data, ((Floater)parent).Blocks);
            }
            else if (parent is Figure)
            {
                addInlineInBlocks(data, ((Figure)parent).Blocks);
            }
            else if (parent is ListItem)
            {
                addInlineInBlocks(data, ((ListItem)parent).Blocks);
            }
            else
            {
#if DEBUG
                Debugger.Break();
#endif
                throw new Exception("The given parent TextElement is not valid in this context.");
            }
        }

        private void addInlineInBlocks(Inline data, BlockCollection blocks)
        {
            Block lastBlock = blocks.LastBlock;
            if (lastBlock != null && lastBlock is Section && lastBlock.Tag != null && lastBlock.Tag is bool && ((bool)lastBlock.Tag))
            {
                Block lastBlock2 = ((Section)lastBlock).Blocks.LastBlock;
                if (lastBlock2 != null && lastBlock2 is Paragraph && lastBlock2.Tag != null && lastBlock2.Tag is bool && ((bool)lastBlock2.Tag))
                {
                    ((Paragraph)lastBlock2).Inlines.Add(data);
                }
                else
                {
                    var para = new Paragraph(data) { Tag = true };
                    ((Section)lastBlock).Blocks.Add(para);
                }
            }
            else if (lastBlock != null && lastBlock is Paragraph && lastBlock.Tag != null && lastBlock.Tag is bool && ((bool)lastBlock.Tag))
            {
                ((Paragraph)lastBlock).Inlines.Add(data);
            }
            else
            {
                var para = new Paragraph(data) { Tag = true };
                var section = new Section(para) { Tag = true };
                blocks.Add(section);
            }
        }

        private void addBlock(TextElement parent, Block data)
        {
            if (parent == null)
            {
                m_FlowDoc.Blocks.Add(data);
            }
            else if (parent is TableCell)
            {
                ((TableCell)parent).Blocks.Add(data);
            }
            else if (parent is Section)
            {
                ((Section)parent).Blocks.Add(data);
            }
            else if (parent is Floater)
            {
                ((Floater)parent).Blocks.Add(data);
            }
            else if (parent is Figure)
            {
                ((Figure)parent).Blocks.Add(data);
            }
            else if (parent is ListItem)
            {
                ((ListItem)parent).Blocks.Add(data);
            }
            else
            {
#if DEBUG
                Debugger.Break();
#endif
                throw new Exception("The given parent TextElement is not valid in this context.");
            }
        }

        private void SetForegroundColorAndCursorBasedOnTreeNodeTag(TextElement data, bool updateTotalDuration)
        {
            SetForegroundColorAndCursorBasedOnTreeNodeTag(m_DocumentPaneView, data, updateTotalDuration);
        }


        public static void SetForegroundColorAndCursorBasedOnTreeNodeTag(DocumentPaneView documentPaneView, TextElement data, bool updateTotalDuration)
        {
            DebugFix.Assert(data.Tag is TreeNode);
            var node = (TreeNode)data.Tag;

            ManagedAudioMedia media = node.GetManagedAudioMedia();
            if (media != null)
            {
                if (updateTotalDuration)
                {
                    m_totalAudioDuration.Add(media.Duration);
                }

                Brush brushFontAudio = ColorBrushCache.Get(Settings.Default.Document_Color_Font_Audio);

                data.Foreground = brushFontAudio;
                //data.Cursor = Cursors.Hand;

                return;
            }

#if ENABLE_SEQ_MEDIA

            SequenceMedia seqManagedAudioMedia = node.GetManagedAudioSequenceMedia();
            if (seqManagedAudioMedia != null)
            {
                Debug.Fail("SequenceMedia is normally removed at import time...have you tried re-importing the DAISY book ?");

                Brush brushFontAudio = ColorBrushCache.Get(Settings.Default.Document_Color_Font_Audio);

                data.Foreground = brushFontAudio;
                data.Cursor = Cursors.Cross;

                return;
            }
            
#endif //ENABLE_SEQ_MEDIA

            TreeNode ancerstor = node.GetFirstAncestorWithManagedAudio();
            if (ancerstor != null)
            {
                Brush brushFontAudio = ColorBrushCache.Get(Settings.Default.Document_Color_Font_Audio);

                data.Foreground = brushFontAudio;
                //data.Cursor = Cursors.SizeAll;

                return;
            }

            if (bTreeNodeNeedsAudio(node))
            {
                Brush brushFontNoAudio = ColorBrushCache.Get(Settings.Default.Document_Color_Font_NoAudio);

                data.Foreground = brushFontNoAudio;
                //data.Cursor = Cursors.Pen;
            }

            //#if DEBUG
            //            Debugger.Break();
            //#endif
        }

        public static bool bTreeNodeNeedsAudio(TreeNode node)
        {
            if (node.GetTextMedia() != null)
            {
                DebugFix.Assert(node.Children.Count == 0);
                return true;
            }

            if (node.HasXmlProperty)
            {
                string localName = node.GetXmlElementLocalName();
                if (localName.Equals("img", StringComparison.OrdinalIgnoreCase)
                     || localName.Equals("video", StringComparison.OrdinalIgnoreCase)
                    )
                {
                    DebugFix.Assert(node.Children.Count == 0);
                    return true;
                }
            }

            return false;
        }

        private void setTag(TextElement data, TreeNode node)
        {
            DebugFix.Assert(data.Tag == null);

            if (data is Block)
            {
                setTextDirection(node, null, null, (Block)data);
            }

            data.Tag = node;
            node.Tag = data;
            //data.Foreground = Brushes.Red; // default is normally overriden

            if (m_FlowDoc.ContextMenu == null)
            {
                m_FlowDoc.ContextMenu = (ContextMenu)m_DocumentPaneView.Resources["DocContextMenu"];
            }

            if (data is Block)
            {
                ((Block)data).BorderThickness = new Thickness(1.0);
                ((Block)data).BorderBrush = ColorBrushCache.Get(Colors.Transparent);
            }

            SetForegroundColorAndCursorBasedOnTreeNodeTag(data, true);

            if (bTreeNodeNeedsAudio(node))
            {
                if (!node.HasOrInheritsAudio())
                {
                    EventAggregator.GetEvent<NoAudioContentFoundByFlowDocumentParserEvent>().Publish(node);
                }

                data.Cursor = Cursors.Hand;
                data.MouseEnter += m_DocumentPaneView.OnTextElementMouseEnter;
            }
        }

        //public static string IdToName(string id)
        //{
        //    return id.Replace("-", "_DaSh_");
        //}

        //public static string NameToId(string name)
        //{
        //    return name.Replace("_DaSh_", "-");
        //}

        private TextElement walkBookTreeAndGenerateFlowDocument_th_td(TreeNode node, TextElement parent, string textMedia)
        {
            if (parent is Table)
            {
                m_currentTD++;
                TableCell data = new TableCell();
                setTag(data, node);

                data.BorderThickness = new Thickness(1.0);

                //data.BorderBrush = m_ColorBrushCache.Get(Settings.Default.Document_Color_Font_Audio);
#if DEBUG
                DebugFix.Assert(data.Tag != null);
                DebugFix.Assert(data.Tag is TreeNode);
                DebugFix.Assert(node == data.Tag);

                DebugFix.Assert(node.HasXmlProperty &&
                    (node.GetXmlElementLocalName() == "td"
                    || node.GetXmlElementLocalName() == "th")
                    );
#endif
                SetBorderAndBackColorBasedOnTreeNodeTag(data);

                TableRowGroup trg = ((Table)parent).RowGroups[m_currentROWGROUP];

                if (trg.Tag != null && trg.Tag is TreeNode)
                {
                    if (((TreeNode)trg.Tag).HasXmlProperty)
                    {
                        string localName = ((TreeNode)trg.Tag).GetXmlElementLocalName();
                        if (localName == "thead")
                        {
                            //data.Background = Brushes.LightGreen;
                            data.FontWeight = FontWeights.Heavy;
                        }
                        if (localName == "tfoot")
                        {
                            //data.Background = Brushes.LightBlue;
                        }
                    }
                }
                if (node.GetXmlElementLocalName() == "th")
                {
                    data.FontWeight = FontWeights.Heavy;
                }

                TableRowCollection trc = trg.Rows;
                trc[trc.Count - 1].Cells.Add(data);

                if (m_currentTD > ((Table)parent).Columns.Count)
                {
                    ((Table)parent).Columns.Add(new TableColumn());
                }

                XmlProperty xmlProp = node.GetXmlProperty();
                XmlAttribute attr = xmlProp.GetAttribute("colspan");

                if (attr != null && !String.IsNullOrEmpty(attr.Value))
                {
                    data.ColumnSpan = int.Parse(attr.Value);
                }

                if (node.Children.Count == 0)
                {
                    if (textMedia == null || String.IsNullOrEmpty(textMedia))
                    {
                        // ignore empty list item
                    }
                    else
                    {
                        var run = new Run(textMedia);
                        setTextDirection(node, null, run, null);
                        var para = new Paragraph(run);
                        data.Blocks.Add(para);
                    }

                    return parent;
                }
                //assumption based on the caller: when node.Children.Count != 0 then textMedia == null
                else
                {
                    var section = new Section();
                    data.Blocks.Add(section);
                    return section;
                }
            }
            else
            {
#if DEBUG
                Debugger.Break();
#endif
                throw new Exception("Trying to add TableCell btu parent is not Table ??");
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_Paragraph(TreeNode node, TextElement parent, string textMedia, DelegateParagraphInitializer initializer)
        {
            Paragraph data = new Paragraph();
            setTag(data, node);

            if (initializer != null)
            {
                initializer(data);
            }

            if (node.Children.Count == 0)
            {
                if (textMedia == null || String.IsNullOrEmpty(textMedia))
                {
                    data.Inlines.Add(new LineBreak());
                }
                else
                {
                    var run = new Run(textMedia);
                    setTextDirection(node, null, run, null);
                    data.Inlines.Add(run);
                }

                addBlock(parent, data);
                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia == null
            else
            {
                addBlock(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_underline_u(TreeNode node, TextElement parent, string textMedia)
        {
            Underline data = new Underline();
            setTag(data, node);

            if (node.Children.Count == 0)
            {
                if (textMedia == null || String.IsNullOrEmpty(textMedia))
                {
                    // ignore empty underline
                }
                else
                {
                    var run = new Run(textMedia);
                    setTextDirection(node, null, run, null);
                    data.Inlines.Add(run);
                    addInline(parent, data);
                }

                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia == null
            else
            {
                addInline(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_strong_b(TreeNode node, TextElement parent, string textMedia)
        {
            Bold data = new Bold();
            setTag(data, node);

            if (node.Children.Count == 0)
            {
                if (textMedia == null || String.IsNullOrEmpty(textMedia))
                {
                    // ignore empty bold
                }
                else
                {
                    var run = new Run(textMedia);
                    setTextDirection(node, null, run, null);
                    data.Inlines.Add(run);
                    addInline(parent, data);
                }

                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia == null
            else
            {
                addInline(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_em_i(TreeNode node, TextElement parent, string textMedia)
        {
            Italic data = new Italic();
            setTag(data, node);

            if (node.Children.Count == 0)
            {
                if (textMedia == null || String.IsNullOrEmpty(textMedia))
                {
                    // ignore empty italic
                }
                else
                {
                    var run = new Run(textMedia);
                    setTextDirection(node, null, run, null);
                    data.Inlines.Add(run);
                    addInline(parent, data);
                }

                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia == null
            else
            {
                addInline(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_list_dl(TreeNode node, TextElement parent, string textMedia)
        {
            List data = new List();
            setTag(data, node);

            if (node.Children.Count == 0)
            {
                //ignore empty list
                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia == null
            else
            {
                addBlock(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_table(TreeNode node, TextElement parent, string textMedia)
        {
            m_cellsToExpand.Clear();
            m_currentTD = 0;

            Table data = new Table();
            setTag(data, node);

            data.CellSpacing = 4.0;

            data.BorderThickness = new Thickness(1.0);

            //data.BorderBrush = m_ColorBrushCache.Get(Settings.Default.Document_Color_Font_NoAudio);
#if DEBUG
            DebugFix.Assert(data.Tag != null);
            DebugFix.Assert(data.Tag is TreeNode);
            DebugFix.Assert(node == data.Tag);

            DebugFix.Assert(node.HasXmlProperty && node.GetXmlElementLocalName() == "table");
#endif
            SetBorderAndBackColorBasedOnTreeNodeTag(data);

            m_currentROWGROUP = -1;
            m_firstTR = false;

            if (node.Children.Count == 0)
            {
                //ignore empty table
                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia == null
            else
            {
                addBlock(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_li_dd_dt(TreeNode node, TextElement parent, string textMedia)
        {
            if (!(parent is List))
            {
#if DEBUG
                Debugger.Break();
#endif
                throw new Exception("list item not in List ??");
            }
            var data = new ListItem();

            if (node.Children.Count == 0)
            {
                if (textMedia == null || String.IsNullOrEmpty(textMedia))
                {
                    setTag(data, node);
                    // ignore empty list item
                }
                else
                {
                    var run = new Run(textMedia);
                    setTextDirection(node, null, run, null);
                    var para = new Paragraph(run);
                    data.Blocks.Add(para);
                    ((List)parent).ListItems.Add(data);

                    string localName = node.GetXmlElementLocalName();

                    if (localName == "pagenum")
                    {
                        //data.Tag = null;
                        setTag(para, node);
                        formatPageNumberAndSetId_Para(node, para);
                    }
                    else if (localName == "hd")
                    {
                        //data.Tag = null;
                        setTag(para, node);
                        formatListHeader(para);
                    }
                    else
                    {
                        setTag(data, node);
                    }
                }

                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia == null
            else
            {
                string localName = node.GetXmlElementLocalName();

                ((List)parent).ListItems.Add(data);
                if (localName == "pagenum")
                {
                    //data.Tag = null;
                    var para = new Paragraph();
                    setTag(para, node);
                    formatPageNumberAndSetId_Para(node, para);
                    data.Blocks.Add(para);
                    return para;
                }
                else if (localName == "hd")
                {
                    //data.Tag = null;
                    var para = new Paragraph();
                    setTag(para, node);
                    formatListHeader(para);
                    data.Blocks.Add(para);
                    return para;
                }
                else
                {
                    setTag(data, node);
                }
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_tr_tbody_thead_tfoot_caption_pagenum(TreeNode node, TextElement parent, string textMedia)
        {
            string localName = node.GetXmlElementLocalName();

            if (node.Children.Count == 0)
            {
                if (parent is Table)
                {
                    if ((localName == "pagenum" || localName == "caption")
                        && textMedia != null && !string.IsNullOrEmpty(textMedia))
                    {
                        m_currentTD = 0;

                        TableRowGroup rowGroup = new TableRowGroup();

                        ((Table)parent).RowGroups.Add(rowGroup);
                        m_currentROWGROUP++;

                        TableRow data = new TableRow();
                        ((Table)parent).RowGroups[m_currentROWGROUP].Rows.Add(data);
                        var run = new Run(textMedia);
                        setTextDirection(node, null, run, null);
                        Paragraph para = new Paragraph(run);
                        TableCell cell = new TableCell(para);

                        setTag(para, node);

                        if (localName == "caption")
                        {
                            formatCaptionCell(cell);
                        }
                        else
                        {
                            formatPageNumberAndSetId_Para(node, para);
                        }

                        cell.ColumnSpan = 1;
                        m_cellsToExpand.Add(cell);

                        data.Cells.Add(cell);

                        m_firstTR = false;
                        return parent;
                    }
                    else
                    {
                        //ignore empty row
                        return parent;
                    }
                }
                else
                {
#if DEBUG
                    Debugger.Break();
#endif
                    throw new Exception("table row not in Table ??");
                }
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia == null
            else
            {
                if (parent is Table)
                {
                    if (localName == "pagenum" || localName == "caption")
                    {
                        m_currentTD = 0;

                        TableRowGroup rowGroup = new TableRowGroup();

                        ((Table)parent).RowGroups.Add(rowGroup);
                        m_currentROWGROUP++;

                        TableRow row = new TableRow();
                        ((Table)parent).RowGroups[m_currentROWGROUP].Rows.Add(row);
                        Paragraph para = new Paragraph();
                        TableCell cell = new TableCell(para);

                        setTag(para, node);

                        if (localName == "caption")
                        {
                            formatCaptionCell(cell);
                        }
                        else
                        {
                            formatPageNumberAndSetId_Para(node, para);
                        }

                        cell.ColumnSpan = 1;
                        m_cellsToExpand.Add(cell);

                        row.Cells.Add(cell);

                        m_firstTR = false;
                        return para;
                    }
                    else if (localName == "thead"
                        || localName == "tbody"
                        || localName == "tfoot")
                    {
                        TableRowGroup rowGroup = new TableRowGroup();
                        setTag(rowGroup, node);

                        ((Table)parent).RowGroups.Add(rowGroup);
                        m_currentROWGROUP++;
                        m_firstTR = false;
                        return parent;
                    }
                    else
                    {
                        m_currentTD = 0;

                        if (node.Parent != null)
                        {
                            if (node.Parent.HasXmlProperty && node.Parent.GetXmlElementLocalName() == "table")
                            {
                                if (!m_firstTR)
                                {
                                    ((Table)parent).RowGroups.Add(new TableRowGroup());
                                    m_currentROWGROUP++;

                                    m_firstTR = true;
                                }
                            }
                            else
                            {
                                m_firstTR = false;
                            }
                        }

                        if (((Table)parent).RowGroups.Count == 0)
                        {
                            ((Table)parent).RowGroups.Add(new TableRowGroup());
                            m_currentROWGROUP = 0;
                        }

                        TableRow data = new TableRow();
                        setTag(data, node);

                        ((Table)parent).RowGroups[m_currentROWGROUP].Rows.Add(data);

                        return parent;
                    }
                }
                else
                {
#if DEBUG
                    Debugger.Break();
#endif
                    throw new Exception("table row not in Table ??");
                }
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_anchor_a(TreeNode node, TextElement parent, string textMedia)
        {
            //Hyperlink data = new Hyperlink();
            var data = new Underline();
            setTag(data, node);
#if DEBUG
            DebugFix.Assert(data.Tag != null);
            DebugFix.Assert(data.Tag is TreeNode);
            DebugFix.Assert(node == data.Tag);

            DebugFix.Assert(node.HasXmlProperty &&
                (node.GetXmlElementLocalName() == "a"
                || node.GetXmlElementLocalName() == "anchor")
                );
#endif
            SetBorderAndBackColorBasedOnTreeNodeTag(data);

            //data.Background = m_ColorBrushCache.Get(Settings.Default.Document_Color_Hyperlink_Back);
            ////data.Foreground = Brushes.Blue;

            XmlProperty xmlProp = node.GetXmlProperty();
            XmlAttribute attr = xmlProp.GetAttribute("href");
            if (attr != null)
            {
                if (!String.IsNullOrEmpty(attr.Value))
                {
                    var uri = new Uri(attr.Value, UriKind.RelativeOrAbsolute);
                    //removed to avoid swallowing the mouse click
                    //data.NavigateUri = uri;
                    //data.RequestNavigate += new RequestNavigateEventHandler(OnRequestNavigate);
                    data.ToolTip = uri.ToString();
                }
            }
            else
            {
                attr = xmlProp.GetAttribute("name");
                if (attr != null)
                {
                    if (!String.IsNullOrEmpty(attr.Value))
                    {
                        data.ToolTip = attr.Value;
                    }
                }
                else
                {
                    string id = node.GetXmlElementId();

                    if (!String.IsNullOrEmpty(id))
                    {
                        data.ToolTip = id;
                    }
                }
            }

            if (node.Children.Count == 0)
            {
                if (textMedia == null || String.IsNullOrEmpty(textMedia))
                {
                    if (attr != null && !String.IsNullOrEmpty(attr.Value))
                    {
                        var run = new Run(attr.Value);
                        setTextDirection(node, null, run, null);
                        data.Inlines.Add(run);
                        addInline(parent, data);
                    }
                    else
                    {
                        // otherwise ignore empty link
                    }
                    return parent;
                }
                else
                {
                    var run = new Run(textMedia);
                    setTextDirection(node, null, run, null);
                    data.Inlines.Add(run);
                    addInline(parent, data);
                }

                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia == null
            else
            {
                addInline(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_annoref_noteref(TreeNode node, TextElement parent, string textMedia)
        {
            var data = new Hyperlink();
            setTag(data, node);

            data.Focusable = false;
            //data.PreviewGotKeyboardFocus += new KeyboardFocusChangedEventHandler((o, e) => e.Handled = true);

            data.FontSize = m_FlowDoc.FontSize / 1.2;
            data.FontWeight = FontWeights.Bold;
#if DEBUG
            DebugFix.Assert(data.Tag != null);
            DebugFix.Assert(data.Tag is TreeNode);
            DebugFix.Assert(node == data.Tag);

            DebugFix.Assert(node.HasXmlProperty &&
                (node.GetXmlElementLocalName() == "annoref"
                || node.GetXmlElementLocalName() == "noteref")
                );
#endif
            SetBorderAndBackColorBasedOnTreeNodeTag(data);

            //data.Background = m_ColorBrushCache.Get(Settings.Default.Document_Color_Hyperlink_Back);
            ////data.Foreground = Brushes.Blue;

            XmlProperty xmlProp = node.GetXmlProperty();
            XmlAttribute attr = xmlProp.GetAttribute("idref");

            if (attr != null && !String.IsNullOrEmpty(attr.Value) && attr.Value.Length > 1)
            {
                string id = attr.Value.StartsWith("#") ? attr.Value.Substring(1) : attr.Value;
                data.NavigateUri = new Uri("#" + id, UriKind.Relative);

                //// Now using LostMouseCapture data.RequestNavigate += m_DocumentPaneView.OnTextElementRequestNavigate;

                //data.MouseDown += m_DocumentPaneView.OnTextElementHyperLinkMouseDown;

                data.ToolTip = data.NavigateUri.ToString();
                //string name = IdToName(id);

                data.GotKeyboardFocus += m_DocumentPaneView.OnHyperLinkGotKeyboardFocus;

                m_DocumentPaneView.AddIdLinkSource(id, data);
            }
            else
            {
                //ignore: no link
            }

            if (node.Children.Count == 0)
            {
                if (textMedia == null || String.IsNullOrEmpty(textMedia))
                {
                    var run = new Run("...");
                    setTextDirection(node, null, run, null);
                    data.Inlines.Add(run);
                    addInline(parent, data);
                }
                else
                {
                    //var span = new Span();
                    var run = new Run(textMedia);
                    setTextDirection(node, null, run, null);
                    //span.Inlines.Add(run);

                    data.Inlines.Add(run);
                    addInline(parent, data);
                }

                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia == null
            else
            {
                addInline(parent, data);
                return data;
            }
        }


        private TextElement walkBookTreeAndGenerateFlowDocument_Span(TreeNode node, TextElement parent, string textMedia, DelegateSpanInitializer initializer)
        {
            Span data = new Span();
            setTag(data, node);

            if (initializer != null)
            {
                initializer(data);
            }

            if (node.Children.Count == 0)
            {
                if (String.IsNullOrEmpty(textMedia))
                {
                    var run = new Run("...");
                    setTextDirection(node, null, run, null);
                    data.Inlines.Add(run);
                    addInline(parent, data);
                }
                else
                {
                    string localName = node.GetXmlElementLocalName();
                    var run = new Run(textMedia);
                    setTextDirection(node, null, run, null);
                    if (localName == "sup")
                    {
                        run.SetValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Superscript);
                    }
                    else if (localName == "sub")
                    {
                        run.SetValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Subscript);
                    }
                    data.Inlines.Add(run);
                    addInline(parent, data);
                }

                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia == null
            else
            {
                addInline(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_Floater(TreeNode node, TextElement parent, string textMedia, DelegateFloaterInitializer initializer)
        {
            Floater data = new Floater();
            setTag(data, node);

            if (initializer != null)
            {
                initializer(data);
            }

            if (node.Children.Count == 0)
            {
                if (textMedia == null || String.IsNullOrEmpty(textMedia))
                {
                    data.Blocks.Add(new Paragraph(new LineBreak()));
                }
                else
                {
                    var run = new Run(textMedia);
                    setTextDirection(node, null, run, null);
                    data.Blocks.Add(new Paragraph(run));
                }

                addInline(parent, data);
                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia == null
            else
            {
                addInline(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_Figure(TreeNode node, TextElement parent, string textMedia, DelegateFigureInitializer initializer)
        {
            Figure data = new Figure();
            setTag(data, node);

            if (initializer != null)
            {
                initializer(data);
            }

            if (node.Children.Count == 0)
            {
                if (textMedia == null || String.IsNullOrEmpty(textMedia))
                {
                    data.Blocks.Add(new Paragraph(new LineBreak()));
                }
                else
                {
                    var run = new Run(textMedia);
                    setTextDirection(node, null, run, null);
                    data.Blocks.Add(new Paragraph(run));
                }

                addInline(parent, data);
                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia == null
            else
            {
                addInline(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_Section(TreeNode node, TextElement parent, string textMedia, DelegateSectionInitializer initializer)
        {
            Section data = new Section();
            setTag(data, node);

            if (initializer != null)
            {
                initializer(data);
            }

            if (node.Children.Count == 0)
            {
                if (textMedia == null || String.IsNullOrEmpty(textMedia))
                {
                    data.Blocks.Add(new Paragraph(new LineBreak()));
                }
                else
                {
                    var run = new Run(textMedia);
                    setTextDirection(node, null, run, null);
                    data.Blocks.Add(new Paragraph(run));
                }

                addBlock(parent, data);
                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia == null
            else
            {
                addBlock(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_img(TreeNode node, TextElement parent, string textMedia)
        {
            if (node.Children.Count != 0 || textMedia != null && !String.IsNullOrEmpty(textMedia))
            {
#if DEBUG
                Debugger.Break();
#endif
                throw new Exception("Node has children or text exists when processing image ??");
            }



            XmlProperty xmlProp = node.GetXmlProperty();

            AbstractImageMedia imgMedia = node.GetImageMedia();
            var imgMedia_ext = imgMedia as ExternalImageMedia;
            var imgMedia_man = imgMedia as ManagedImageMedia;

            string dirPath = Path.GetDirectoryName(m_TreeNode.Presentation.RootUri.LocalPath);

            string imagePath = null;

            if (imgMedia_ext != null)
            {

#if DEBUG
                Debugger.Break();
#endif //DEBUG

                //http://blogs.msdn.com/yangxind/archive/2006/11/09/don-t-use-net-system-uri-unescapedatastring-in-url-decoding.aspx

                imagePath = Path.Combine(dirPath, Uri.UnescapeDataString(imgMedia_ext.Src));
            }
            else if (imgMedia_man != null)
            {
#if DEBUG
                XmlAttribute srcAttr = xmlProp.GetAttribute("src");

                DebugFix.Assert(imgMedia_man.ImageMediaData.OriginalRelativePath == srcAttr.Value);
#endif //DEBUG
                var fileDataProv = imgMedia_man.ImageMediaData.DataProvider as FileDataProvider;

                if (fileDataProv != null)
                {
                    imagePath = fileDataProv.DataFileFullPath;
                }
            }

            if (imagePath == null || FileDataProvider.isHTTPFile(imagePath))
            {
#if DEBUG
                Debugger.Break();
#endif //DEBUG
                return parent;
            }






            //if (!FileDataProvider.isHTTPFile(imagePath))

            Image image = new Image();

            ImageSource imageSource = AutoGreyableImage.GetSVGOrBitmapImageSource(imagePath);
            if (imageSource == null)
            {
                Console.WriteLine(@"Problem trying to load image: [" + imagePath + @"]");
#if DEBUG
                Debugger.Break();
#endif //DEBUG

                VisualBrush brush = ShellView.LoadGnomeNeuIcon("Neu_emblem-important");
                RenderTargetBitmap bitmap = AutoGreyableImage.CreateFromVectorGraphics(brush, 100, 100);

                image.Source = bitmap;
            }
            else
            {
                image.Source = imageSource;
            }

            if (image.Source.CanFreeze)
            {
                image.Source.Freeze();
            }

            if (image.Source is BitmapSource)
            {
                BitmapSource bitmap = (BitmapSource)image.Source;
                int ph = bitmap.PixelHeight;
                int pw = bitmap.PixelWidth;
                double dpix = bitmap.DpiX;
                double dpiy = bitmap.DpiY;
                //image.Width = pw;
                //image.Height = ph;
            }

            XmlAttribute srcW = xmlProp.GetAttribute("width");
            if (srcW != null)
            {
                double ww = Double.Parse(srcW.Value);
                image.Width = ww;
            }
            XmlAttribute srcH = xmlProp.GetAttribute("height");
            if (srcH != null)
            {
                double hh = Double.Parse(srcH.Value);
                image.Height = hh;
            }

            image.HorizontalAlignment = HorizontalAlignment.Center;
            image.VerticalAlignment = VerticalAlignment.Top;

            image.Stretch = Stretch.Uniform;
            image.StretchDirection = StretchDirection.DownOnly;

            //image.MinWidth = image.Width;
            //image.MinHeight = image.Height;
            //image.MaxWidth = image.Width;
            //image.MaxHeight = image.Height;

            //Floater floater = new Floater();
            //floater.Blocks.Add(img);
            //floater.Width = image.Width;
            //addInline(parent, floater);

            //Figure figure = new Figure(img);
            //figure.Width = image.Width;
            //addInline(parent, figure);

            string imgAlt = null;

            XmlAttribute altAttr = xmlProp.GetAttribute("alt");
            if (altAttr != null)
            {
                imgAlt = altAttr.Value;
            }

            if (!string.IsNullOrEmpty(imgAlt))
            {
                image.ToolTip = imgAlt;
            }

            bool parentHasBlocks = parent is TableCell
                                   || parent is Section
                                   || parent is Floater
                                   || parent is Figure
                                   || parent is ListItem;

            var imagePanel = new StackPanel();
            imagePanel.Orientation = Orientation.Vertical;
            //imagePanel.LastChildFill = true;
            if (!string.IsNullOrEmpty(imgAlt))
            {
                var run = new Run(imgAlt);
                var tb = new TextBlock(run)
                             {
                                 HorizontalAlignment = HorizontalAlignment.Center,
                                 TextWrapping = TextWrapping.Wrap
                             };

                setTextDirection(node, tb, run, null);
                imagePanel.Children.Add(tb);
            }
            imagePanel.Children.Add(image);

            imagePanel.HorizontalAlignment = HorizontalAlignment.Center;
            imagePanel.VerticalAlignment = VerticalAlignment.Top;

            //imagePanel.Width = image.Width;
            //imagePanel.MaxWidth = image.Width;

            //imagePanel.Height = image.Height;
            //imagePanel.MaxHeight = image.Height;

            if (parentHasBlocks)
            {
                var img = new BlockUIContainer(imagePanel);

                //img.LineStackingStrategy = LineStackingStrategy.MaxHeight;

                //img.BorderBrush = Brushes.RoyalBlue;
                //img.BorderThickness = new Thickness(2.0);

                setTag(img, node);

                addBlock(parent, img);

                //if (!string.IsNullOrEmpty(imgAlt))
                //{
                //    Paragraph paraAlt = new Paragraph(new Run("(" + imgAlt + ")"));
                //    paraAlt.BorderBrush = Brushes.CadetBlue;
                //    paraAlt.BorderThickness = new Thickness(1.0);
                //    paraAlt.FontSize = m_FlowDoc.FontSize / 1.2;
                //    addBlock(parent, paraAlt);
                //}
            }
            else
            {
                var img = new InlineUIContainer(imagePanel);

                setTag(img, node);

                addInline(parent, img);
            }


            return parent;
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_(TreeNode node, TextElement parent, string textMedia)
        {
            if (!node.HasXmlProperty)
            {
                //assumption based on the caller: node.Children.Count == 0 && textMedia != null
                if (textMedia.Length == 0)
                {
#if DEBUG
                    Debugger.Break();
#endif
                    return parent;
                }

                var data = new Run(textMedia);
                setTextDirection(node, null, data, null);
                setTag(data, node);
                addInline(parent, data);

                return parent;
            }

            // node.Children.Count ?
            // String.IsNullOrEmpty(textMedia) ?

            string localName = node.GetXmlElementLocalName();

            switch (localName)
            {
                case "math":
                    {
                        string xmlFragment = node.GetXmlFragment();
                        return null;
                    }
                case "p":
                case "level":
                case "level1":
                case "level2":
                case "level3":
                case "level4":
                case "level5":
                case "level6":
                case "bodymatter":
                    {
                        return walkBookTreeAndGenerateFlowDocument_Section(node, parent, textMedia, null);
                    }
                case "frontmatter":
                case "rearmatter":
                    {
                        return walkBookTreeAndGenerateFlowDocument_Section(node, parent, textMedia,
                            data =>
                            {
                                //data.BorderBrush = Brushes.GreenYellow;
                                data.BorderThickness = new Thickness(2.0);
                                data.Padding = new Thickness(4.0);
                            }
                            );
                    }
                case "blockquote":
                    {
                        return walkBookTreeAndGenerateFlowDocument_Section(node, parent, textMedia,
                            data =>
                            {
                                data.BorderThickness = new Thickness(2.0);
                                data.Padding = new Thickness(2.0);
                                data.Margin = new Thickness(4.0);

                                //data.BorderBrush = m_ColorBrushCache.Get(Settings.Default.Document_Color_Font_NoAudio);
#if DEBUG
                                DebugFix.Assert(data.Tag != null);
                                DebugFix.Assert(data.Tag is TreeNode);
                                DebugFix.Assert(node == data.Tag);

                                DebugFix.Assert(node.HasXmlProperty && node.GetXmlElementLocalName() == "blockquote");
#endif
                                SetBorderAndBackColorBasedOnTreeNodeTag(data);

                            }
                            );
                    }
                case "note":
                case "annotation":
                    {
                        return walkBookTreeAndGenerateFlowDocument_Section(node, parent, textMedia,
                            data =>
                            {
                                data.BorderThickness = new Thickness(2.0);
                                data.Padding = new Thickness(2.0);
                                data.FontSize = m_FlowDoc.FontSize / 1.2;

                                //data.BorderBrush = m_ColorBrushCache.Get(Settings.Default.Document_Color_Font_NoAudio);
#if DEBUG
                                DebugFix.Assert(data.Tag != null);
                                DebugFix.Assert(data.Tag is TreeNode);
                                DebugFix.Assert(node == data.Tag);

                                DebugFix.Assert(node.HasXmlProperty &&
                                    (node.GetXmlElementLocalName() == "annotation"
                                    || node.GetXmlElementLocalName() == "note")
                                    );
#endif
                                SetBorderAndBackColorBasedOnTreeNodeTag(data);

                                string id = node.GetXmlElementId();
                                if (!string.IsNullOrEmpty(id))
                                {
                                    //string name = IdToName(attr.Value);
                                    data.ToolTip = id;

                                    m_DocumentPaneView.AddIdLinkTarget(id, data);
                                }
                            }
                            );
                    }
                case "noteref":
                case "annoref":
                    {
                        return walkBookTreeAndGenerateFlowDocument_annoref_noteref(node, parent, textMedia);
                    }
                case "caption":
                    {
                        if (parent is Table)
                        {
                            return walkBookTreeAndGenerateFlowDocument_tr_tbody_thead_tfoot_caption_pagenum(node, parent, textMedia);
                        }
                        else
                        {
                            return walkBookTreeAndGenerateFlowDocument_Section(node, parent, textMedia,
                             data =>
                             {
                                 data.BorderThickness = new Thickness(1.0);
                                 data.Padding = new Thickness(2.0);
                                 data.FontWeight = FontWeights.Light;
                                 data.FontSize = m_FlowDoc.FontSize / 1.2;
                                 data.TextAlignment = TextAlignment.Center;
                                 //data.Foreground = Brushes.DarkGreen;

                                 //data.BorderBrush = m_ColorBrushCache.Get(Settings.Default.Document_Color_Font_NoAudio);
#if DEBUG
                                 DebugFix.Assert(data.Tag != null);
                                 DebugFix.Assert(data.Tag is TreeNode);
                                 DebugFix.Assert(node == data.Tag);

                                 DebugFix.Assert(node.HasXmlProperty && node.GetXmlElementLocalName() == "caption");

#endif
                                 SetBorderAndBackColorBasedOnTreeNodeTag(data);
                             });
                        }
                    }
                case "h1":
                case "levelhd":
                case "hd":
                    {
                        if (localName == "hd" && parent is List)
                        {
                            return walkBookTreeAndGenerateFlowDocument_li_dd_dt(node, parent, textMedia);
                        }
                        return walkBookTreeAndGenerateFlowDocument_Section(node, parent, textMedia,
                            data =>
                            {
                                data.FontSize = m_FlowDoc.FontSize * 1.5;
                                data.FontWeight = FontWeights.Heavy;
                            });
                    }
                case "h2":
                    {
                        return walkBookTreeAndGenerateFlowDocument_Section(node, parent, textMedia,
                            data =>
                            {
                                data.FontSize = m_FlowDoc.FontSize * 1.25;
                                data.FontWeight = FontWeights.Heavy;
                            });
                    }
                case "h3":
                case "h4":
                case "h5":
                case "h6":
                    {
                        return walkBookTreeAndGenerateFlowDocument_Section(node, parent, textMedia,
                            data =>
                            {
                                data.FontSize = m_FlowDoc.FontSize * 1.15;
                                data.FontWeight = FontWeights.Heavy;
                            });
                    }
                case "doctitle":
                case "docauthor":
                case "covertitle":
                    {
                        return walkBookTreeAndGenerateFlowDocument_Paragraph(node, parent, textMedia,
                            data =>
                            {
                                data.BorderThickness = new Thickness(1.0);
                                data.FontSize = m_FlowDoc.FontSize * 1.3;
                                data.FontWeight = FontWeights.Heavy;
                                //data.Foreground = Brushes.Navy;

                                //data.BorderBrush = m_ColorBrushCache.Get(Settings.Default.Document_Color_Font_NoAudio);
#if DEBUG
                                DebugFix.Assert(data.Tag != null);
                                DebugFix.Assert(data.Tag is TreeNode);
                                DebugFix.Assert(node == data.Tag);

                                DebugFix.Assert(node.HasXmlProperty &&
                                    (node.GetXmlElementLocalName() == "doctitle"
                                    || node.GetXmlElementLocalName() == "docauthor"
                                    || node.GetXmlElementLocalName() == "covertitle")
                                    );
#endif
                                SetBorderAndBackColorBasedOnTreeNodeTag(data);
                            });
                    }
                case "pagenum":
                    {
                        DelegateParagraphInitializer delegatePageNumPara =
                                data =>
                                {
                                    formatPageNumberAndSetId_Para(node, data);
                                };
                        DelegateSpanInitializer delegatePageNumSpan =
                                data =>
                                {
                                    formatPageNumberAndSetId_Span(node, data);
                                };
                        if (parent is Table)
                        {
                            return walkBookTreeAndGenerateFlowDocument_tr_tbody_thead_tfoot_caption_pagenum(node, parent, textMedia);
                        }
                        if (parent is List)
                        {
                            return walkBookTreeAndGenerateFlowDocument_li_dd_dt(node, parent, textMedia);
                        }
                        if (parent == null || parent is TableCell || parent is Section || parent is Floater || parent is Figure || parent is ListItem)
                        {
                            return walkBookTreeAndGenerateFlowDocument_Paragraph(node, parent, textMedia, delegatePageNumPara);
                        }
                        if (parent is Paragraph || parent is Span)
                        {
                            return walkBookTreeAndGenerateFlowDocument_Span(node, parent, textMedia, delegatePageNumSpan);
                        }

                        Debug.Fail("Page pagenum cannot be added due to incompatible FlowDocument structure !");
                        break;
                    }
                case "imggroup":
                    {
                        return walkBookTreeAndGenerateFlowDocument_Section(node, parent, textMedia,
                            data =>
                            {
                                data.BorderThickness = new Thickness(0.5);
                                data.Padding = new Thickness(2.0);

                                //data.BorderBrush = m_ColorBrushCache.Get(Settings.Default.Document_Color_Font_NoAudio);
#if DEBUG
                                DebugFix.Assert(data.Tag != null);
                                DebugFix.Assert(data.Tag is TreeNode);
                                DebugFix.Assert(node == data.Tag);

                                DebugFix.Assert(node.HasXmlProperty && node.GetXmlElementLocalName() == "imggroup");

#endif
                                SetBorderAndBackColorBasedOnTreeNodeTag(data);
                            });
                    }
                case "sidebar":
                    {
                        return walkBookTreeAndGenerateFlowDocument_Section(node, parent, textMedia,
                            data =>
                            {
                                data.BorderThickness = new Thickness(2.0);
                                data.Padding = new Thickness(2.0);

                                //data.BorderBrush = m_ColorBrushCache.Get(Settings.Default.Document_Color_Font_NoAudio);
#if DEBUG
                                DebugFix.Assert(data.Tag != null);
                                DebugFix.Assert(data.Tag is TreeNode);
                                DebugFix.Assert(node == data.Tag);

                                DebugFix.Assert(node.HasXmlProperty && node.GetXmlElementLocalName() == "sidebar");

#endif
                                SetBorderAndBackColorBasedOnTreeNodeTag(data);
                            });
                    }
                case "img":
                    {
                        return walkBookTreeAndGenerateFlowDocument_img(node, parent, textMedia);
                    }
                case "video":
                    {
                        return walkBookTreeAndGenerateFlowDocument_video(node, parent, textMedia);
                    }
                case "th":
                case "td":
                    {
                        return walkBookTreeAndGenerateFlowDocument_th_td(node, parent, textMedia);
                    }
                case "br":
                    {
                        var data = new LineBreak();
                        setTag(data, node);
                        addInline(parent, data);
                        return parent;
                    }
                case "em":
                case "i":
                    {
                        return walkBookTreeAndGenerateFlowDocument_em_i(node, parent, textMedia);
                    }
                case "strong":
                case "b":
                    {
                        return walkBookTreeAndGenerateFlowDocument_strong_b(node, parent, textMedia);
                    }
                case "underline":
                case "u":
                    {
                        return walkBookTreeAndGenerateFlowDocument_underline_u(node, parent, textMedia);
                    }
                case "anchor":
                case "a":
                    {
                        return walkBookTreeAndGenerateFlowDocument_anchor_a(node, parent, textMedia);
                    }
                case "table":
                    {
                        return walkBookTreeAndGenerateFlowDocument_table(node, parent, textMedia);
                    }
                case "tr":
                case "thead":
                case "tfoot":
                case "tbody":
                    {
                        return walkBookTreeAndGenerateFlowDocument_tr_tbody_thead_tfoot_caption_pagenum(node, parent, textMedia);
                    }
                case "list":
                case "ul":
                case "ol":
                case "dl":
                    {
                        return walkBookTreeAndGenerateFlowDocument_list_dl(node, parent, textMedia);
                    }
                case "dt":
                case "dd":
                case "li":
                    {
                        return walkBookTreeAndGenerateFlowDocument_li_dd_dt(node, parent, textMedia);
                    }
                case "span":
                case "linenum":
                case "sent":
                case "w":
                case "cite":
                case "author":
                case "bdo":
                case "kbd":
                case "dfn":
                case "abbr":
                case "q":
                case "rbc":
                case "rtc":
                case "rp":
                    {
                        return walkBookTreeAndGenerateFlowDocument_Span(node, parent, textMedia, null);
                    }
                case "sup":
                case "sub":
                    {
                        return walkBookTreeAndGenerateFlowDocument_Span(node, parent, textMedia, null);
                    }
                case "ruby":
                    {
                        return walkBookTreeAndGenerateFlowDocument_Span(node, parent, textMedia,
                            data =>
                            {
                                //data.Background = Brushes.BlanchedAlmond;
                            }
                            );
                    }
                case "rb":
                    {
                        return walkBookTreeAndGenerateFlowDocument_Span(node, parent, textMedia,
                            data =>
                            {
                                var run = new Run(" ");
                                setTextDirection(node, null, run, null);
                                data.Inlines.Add(run);
                                data.TextDecorations = TextDecorations.OverLine;
                            });

                    }
                case "rt":
                    {
                        return walkBookTreeAndGenerateFlowDocument_Span(node, parent, textMedia,
                            data =>
                            {
                                //var converter = new FontFamilyConverter();
                                //data.FontFamily = (FontFamily)converter.ConvertFrom("Meiryo");

                                //data.FontFamily = new FontFamily("Meiryo");

                                data.Typography.Variants = FontVariants.Subscript;

                                var xmlProp = node.GetXmlProperty();
                                if (xmlProp != null)
                                {
                                    var attr = xmlProp.GetAttribute("rbspan");

                                    if (attr != null && !String.IsNullOrEmpty(attr.Value))
                                    {
                                        data.TextDecorations = TextDecorations.Underline;
                                        return;
                                    }
                                }
                                var run = new Run(" ");
                                setTextDirection(node, null, run, null);
                                data.Inlines.Add(run);
                            }
                            );
                    }
                case "acronym":
                    {
                        return walkBookTreeAndGenerateFlowDocument_Span(node, parent, textMedia,
                            data =>
                            {
                                XmlProperty xmlProp = node.GetXmlProperty();
                                if (xmlProp == null) return;

                                XmlAttribute attr = xmlProp.GetAttribute("title");
                                if (attr == null) return;

                                if (!String.IsNullOrEmpty(attr.Value))
                                {
                                    data.ToolTip = attr.Value;
                                }
                            }
                            );
                    }
                case "line":
                case "dateline":
                case "bridgehead":
                case "byline":
                case "title":
                    {
                        return walkBookTreeAndGenerateFlowDocument_Paragraph(node, parent, textMedia, null);
                    }
                case "lic":
                case "prodnote":
                case "div":
                case "samp":
                case "poem":
                case "linegroup":
                case "code":
                case "book":
                case "body":
                case "address":
                case "epigraph":
                    {
                        return walkBookTreeAndGenerateFlowDocument_Section(node, parent, textMedia, null);
                    }
                case "col":
                case "colgroup":
                    {
                        Debug.Fail(String.Format(@"DTBook element not yet supported [{0}]", localName));
                        break;
                    }
                case "hr":
                    {
                        Console.WriteLine(@"XUK to FlowDocument converter: ignoring HR markup.");
                        break;
                    }
                default:
                    {
                        if (false
                            //&&
                            //(
                            //string.IsNullOrEmpty(qname.NamespaceUri)
                            //|| qname.NamespaceUri == m_TreeNode.Presentation.PropertyFactory.DefaultXmlNamespaceUri
                            //)
                            )
                        {
                            Debug.Fail(String.Format("Unknown DTBook / HTML element ! [{0}]", localName));
                            break;
                        }
                        else
                        {
                            return walkBookTreeAndGenerateFlowDocument_Section(node, parent, textMedia,
                                data =>
                                {
                                    data.BorderBrush = Brushes.Red;
                                    data.BorderThickness = new Thickness(1.0);
                                    data.Padding = new Thickness(4.0);
                                }
                                );
                            //System.Diagnostics.Debug.Fail(String.Format("Unknown element namespace in DTBook ! [{0}]".NamespaceUri));
                        }
                    }
            }

            return parent;
        }

        private Stopwatch m_stopwatch;
        private void walkBookTreeAndGenerateFlowDocument(TreeNode node, TextElement parent)
        {
            m_nTreeNode++;

            if (node.IsMarked)
            {
                EventAggregator.GetEvent<MarkedTreeNodeFoundByFlowDocumentParserEvent>().Publish(node);
            }

            //if (node.HasAlternateContentProperty)
            //{
            //    EventAggregator.GetEvent<DescribedTreeNodeFoundByFlowDocumentParserEvent>().Publish(node);
            //}

            if (node.Parent == null &&
                node.GetTextDirectionality() //node.TextDirectionality
                == TreeNode.TextDirection.RTL
                )
            {
                m_FlowDoc.FlowDirection = FlowDirection.RightToLeft;
            }

            if (node.HasXmlProperty
                && node.GetXmlElementLocalName().Equals("img", StringComparison.OrdinalIgnoreCase))
            {
                EventAggregator.GetEvent<DescribableTreeNodeFoundByFlowDocumentParserEvent>().Publish(node);
            }

            TextElement parentNext = parent;

            AbstractTextMedia textMedia_ = node.GetTextMedia();
            string textMedia = textMedia_ == null ? null : textMedia_.Text;

            if (node.Children.Count == 0)
            {
                if (!node.HasXmlProperty)
                {
                    if (textMedia == null)
                    {
#if DEBUG
                        Debugger.Break();
#endif
                        throw new Exception("The given TreeNode has no children, has no XmlProperty, and has no TextMedia.");
                    }
                    else //childCount == 0 && qname == null && textMedia != null
                    {
                        //parentNext = generateFlowDocument_NoChild_NoXml_Text(node, parent, textMedia);
                        parentNext = walkBookTreeAndGenerateFlowDocument_(node, parent, textMedia);
                    }
                }
                else //childCount == 0 && qname != null
                {
                    if (textMedia == null)
                    {
                        //parentNext = generateFlowDocument_NoChild_Xml_NoText(node, parent);
                        parentNext = walkBookTreeAndGenerateFlowDocument_(node, parent, textMedia);
                    }
                    else //childCount == 0 && qname != null && textMedia != null
                    {
                        //parentNext = generateFlowDocument_NoChild_Xml_Text(node, parent, textMedia);
                        parentNext = walkBookTreeAndGenerateFlowDocument_(node, parent, textMedia);
                    }
                }
            }
            else //childCount != 0
            {
                if (!node.HasXmlProperty)
                {
                    if (textMedia == null)
                    {
#if DEBUG
                        Debugger.Break();
#endif
                        throw new Exception("The given TreeNode has children, has no XmlProperty, and has no TextMedia.");
                    }
                    else //childCount != 0 && qname == null && textMedia != null
                    {
#if DEBUG
                        Debugger.Break();
#endif
                        throw new Exception("The given TreeNode has children, has no XmlProperty, and has TextMedia.");
                    }
                }
                else //childCount != 0 && qname != null
                {
                    if (textMedia == null)
                    {
                        //parentNext = generateFlowDocument_Child_Xml_NoText(node, parent);
                        parentNext = walkBookTreeAndGenerateFlowDocument_(node, parent, textMedia);
                    }
                    else //childCount != 0 && qname != null && textMedia != null
                    {
#if DEBUG
                        Debugger.Break();
#endif
                        throw new Exception("The given TreeNode has children, has XmlProperty, and has TextMedia.");
                    }
                }

                for (int i = 0; i < node.Children.Count; i++)
                {
                    if (RequestCancellation)
                    {
                        throw new ProgressCancelledException(@"dummy");
                    }
                    if (node.Presentation.XukedInTreeNodes <= 0)
                    {
                        m_percentageProgress = -1;
                    }
                    else
                    {
                        m_percentageProgress = (int)(100 * m_nTreeNode / node.Presentation.XukedInTreeNodes);
                        if (m_percentageProgress > 100)
                            m_percentageProgress = -1;
                    }

                    if (m_stopwatch == null || m_stopwatch.ElapsedMilliseconds >= 500)
                    {
                        if (m_stopwatch != null)
                        {
                            m_stopwatch.Stop();
                        }

                        string str = Tobi_Plugin_DocumentPane_Lang.ConvertingXukToFlowDocument + " ["
                                     +
                                     (node.Presentation.XukedInTreeNodes <= 0
                                          ? m_nTreeNode.ToString()
                                          : m_nTreeNode + "/" + node.Presentation.XukedInTreeNodes)
                                     + "]";
                        //Console.WriteLine(str);
                        reportProgress(m_percentageProgress, str);

                        if (m_stopwatch == null)
                        {
                            m_stopwatch = new Stopwatch();
                        }
                        else
                        {
                            m_stopwatch.Reset();
                        }
                        m_stopwatch.Start();
                    }

                    if (parentNext != null)
                    {
                        walkBookTreeAndGenerateFlowDocument(node.Children.Get(i), parentNext);
                    }
                }

                string localName = node.GetXmlElementLocalName();
                if (parentNext != null
                    && !string.IsNullOrEmpty(localName)
                    && localName.Equals("table", StringComparison.OrdinalIgnoreCase))
                {
                    int n = ((Table)parentNext).Columns.Count;
                    foreach (TableCell cell in m_cellsToExpand)
                    {
                        cell.ColumnSpan = n;
                    }
                    m_cellsToExpand.Clear();

                    TableRowGroupCollection trgc = ((Table)parentNext).RowGroups;

                    for (int index = 0; index < trgc.Count; )
                    {
                        TableRowGroup trg = trgc[index];


                        if (trg.Tag != null && trg.Tag is TreeNode)
                        {
                            if (((TreeNode)trg.Tag).HasXmlProperty)
                            {
                                switch (((TreeNode)trg.Tag).GetXmlElementLocalName())
                                {
                                    case "caption":
                                        {
                                            if (index == 0)
                                            {
                                                index++;
                                                break;
                                            }
                                            trgc.Remove(trg);
                                            trgc.Insert(0, trg);
                                            index++;
                                            break;
                                        }
                                    case "thead":
                                        {
                                            if (index == 0)
                                            {
                                                index++;
                                                break;
                                            }
                                            TableRowGroup trgFirst = trgc[0];

                                            if (trgFirst.Tag != null && trgFirst.Tag is TreeNode)
                                            {
                                                if (((TreeNode)trgFirst.Tag).HasXmlProperty && ((TreeNode)trgFirst.Tag).GetXmlElementLocalName() == "caption")
                                                {
                                                    if (index == 1)
                                                    {
                                                        index++;
                                                        break;
                                                    }
                                                    trgc.Remove(trg);
                                                    trgc.Insert(1, trg);
                                                    index++;
                                                    break;
                                                }
                                            }

                                            trgc.Remove(trg);
                                            trgc.Insert(0, trg);
                                            index++;
                                            break;
                                        }
                                    case "tfoot":
                                        {
                                            if (index == (trgc.Count - 1))
                                            {
                                                index++;
                                                break;
                                            }
                                            trgc.Remove(trg);
                                            trgc.Add(trg);
                                            break;
                                        }
                                    default:
                                        {
                                            index++;
                                            break;
                                        }
                                }
                            }
                        }
                        else
                        {
                            index++;
                        }
                    }
                }
            }
        }

        private FlowDirection setTextDirection(TreeNode.TextDirection dir, FrameworkElement el, Inline il, Block bl)
        {
            FlowDirection direction = FlowDirection.LeftToRight;

            if (dir == TreeNode.TextDirection.RTL)
            {
                direction = FlowDirection.RightToLeft;
                if (il != null)
                {
#if false && DEBUG
                    il.Background = ColorBrushCache.Get(Colors.Yellow);
#endif //DEBUG
                }
                if (bl != null)
                {
                    //bl.TextAlignment = TextAlignment.Right;
#if false && DEBUG
                    bl.Background = ColorBrushCache.Get(Colors.Red);
#endif //DEBUG
                }
                if (el != null)
                {
                    //el.HorizontalAlignment = HorizontalAlignment.Right;
                }
            }
            else if (dir == TreeNode.TextDirection.LTR)
            {
                direction = FlowDirection.LeftToRight;
            }
            else //TreeNode.TextDirection.Unsure
            {
                direction = FlowDirection.LeftToRight;
            }

            if (il != null)
            {
                il.FlowDirection = direction;
            }
            if (bl != null)
            {
                bl.FlowDirection = direction;
            }
            if (el != null)
            {
                el.FlowDirection = direction;
            }

            return direction;
        }

        private FlowDirection setTextDirection(TreeNode.StringChunk strChunk, FrameworkElement el, Inline il, Block bl)
        {
            if (strChunk == null) return FlowDirection.LeftToRight;

            TreeNode.TextDirection dir = strChunk.Direction;
            return setTextDirection(dir, el, il, bl);
        }
        //private FlowDirection setTextDirection(TreeNode.StringChunkRange strChunkRange, FrameworkElement el)
        //{
        //    if (strChunkRange == null) return;

        //    return setTextDirection(strChunkRange.First, el);
        //}
        private FlowDirection setTextDirection(TreeNode node, FrameworkElement el, Inline il, Block bl)
        {
            TreeNode.StringChunkRange strChunkRange = node.GetTextFlattened_();
            if (strChunkRange != null)
            {
                return setTextDirection(strChunkRange.First, el, il, bl);
            }
            else
            {
                // no text happens with empty P paragraphs!
                //#if DEBUG
                //                Debugger.Break();
                //#endif //DEBUG

                // COSTLY (walks parent chain in tree)
                TreeNode.TextDirection dir = node.GetTextDirectionality();
                return setTextDirection(dir, el, il, bl);
            }
        }
    }
}
