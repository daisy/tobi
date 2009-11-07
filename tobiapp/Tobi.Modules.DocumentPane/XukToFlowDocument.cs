using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using urakawa.core;
using urakawa.media;
using urakawa.media.data.audio;
using urakawa.property.channel;
using urakawa.property.xml;
using urakawa.xuk;

namespace Tobi.Modules.DocumentPane
{
    public class XukToFlowDocument
    {
        protected ILoggerFacade Logger { private set; get; }
        protected IEventAggregator EventAggregator { private set; get; }

        public XukToFlowDocument(ILoggerFacade logger, IEventAggregator aggregator,
            DelegateOnMouseUpFlowDoc delegateOnMouseUpFlowDoc,
            DelegateOnMouseDownTextElementWithNode delegateOnMouseDownTextElementWithNode,
            DelegateOnRequestNavigate delegateOnRequestNavigate,
            DelegateAddIdLinkTarget delegateAddIdLinkTarget)
        {
            Logger = logger;
            EventAggregator = aggregator;

            m_DelegateOnMouseUpFlowDoc = delegateOnMouseUpFlowDoc;
            m_DelegateOnMouseDownTextElementWithNode = delegateOnMouseDownTextElementWithNode;
            m_DelegateOnRequestNavigate = delegateOnRequestNavigate;
            m_DelegateAddIdLinkTarget = delegateAddIdLinkTarget;
        }

        public delegate void DelegateAddIdLinkTarget(string name, TextElement data);
        private DelegateAddIdLinkTarget m_DelegateAddIdLinkTarget;

        private FlowDocument m_FlowDoc;
        private TreeNode m_TreeNode;

        public void Convert(TreeNode node, FlowDocument flowDoc)
        {
            m_TreeNode = node;
            
            m_FlowDoc = flowDoc;

            walkBookTreeAndGenerateFlowDocument(m_TreeNode, null);

            //m_FlowDoc.MouseUp += OnMouseUpFlowDoc;

        }

        private int m_currentTD;
        private bool m_firstTR;
        private int m_currentROWGROUP;
        List<TableCell> m_cellsToExpand = new List<TableCell>();

        private delegate void DelegateSectionInitializer(Section secstion);
        private delegate void DelegateFigureInitializer(Figure fig);
        private delegate void DelegateFloaterInitializer(Floater floater);
        private delegate void DelegateSpanInitializer(Span span);
        private delegate void DelegateParagraphInitializer(Paragraph para);

        public delegate void DelegateOnMouseUpFlowDoc();
        private DelegateOnMouseUpFlowDoc m_DelegateOnMouseUpFlowDoc;
        private void OnMouseUpFlowDoc(object sender, MouseButtonEventArgs e)
        {
            //e.Handled = true;
            m_DelegateOnMouseUpFlowDoc();
        }

        public delegate void DelegateOnMouseDownTextElementWithNode(TextElement textElem);
        private DelegateOnMouseDownTextElementWithNode m_DelegateOnMouseDownTextElementWithNode;

        private void OnMouseDownTextElementWithNode(object sender, MouseButtonEventArgs e)
        {
            //e.Handled = true;
            m_DelegateOnMouseDownTextElementWithNode((TextElement)sender);
        }

        private void OnMouseDownTextElementWithNodeAndAudio(object sender, MouseButtonEventArgs e)
        {
            //e.Handled = true;
            m_DelegateOnMouseDownTextElementWithNode((TextElement)sender);
        }

        public delegate void DelegateOnRequestNavigate(Uri uri);
        private DelegateOnRequestNavigate m_DelegateOnRequestNavigate;

        private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            //e.Handled = true;
            m_DelegateOnRequestNavigate(e.Uri);
        }

        private void formatCaptionCell(TableCell cell)
        {
            cell.BorderBrush = Brushes.BlueViolet;
            cell.BorderThickness = new Thickness(2);
            cell.Background = Brushes.LightYellow;
            cell.Foreground = Brushes.Navy;
        }

        private void formatListHeader(Paragraph data)
        {
            data.BorderBrush = Brushes.Blue;
            data.BorderThickness = new Thickness(2.0);
            data.Padding = new Thickness(2.0);
            data.FontWeight = FontWeights.Bold;
            data.FontSize = m_FlowDoc.FontSize * 1.2;
            data.Background = Brushes.Lavender;
            data.Foreground = Brushes.DarkSlateBlue;
        }

        private void formatPageNumberAndSetId(TreeNode node, TextElement data)
        {
            setTag(data, node);

            XmlProperty xmlProp = node.GetProperty<XmlProperty>();
            XmlAttribute attr = xmlProp.GetAttribute("id");

            string pageID = null;

            if (attr != null &&
                !String.IsNullOrEmpty(attr.Value))
            {
                pageID = attr.Value;
            }
            else
            {
                string innerText = node.GetTextMediaFlattened();
                if (!string.IsNullOrEmpty(innerText))
                {
                    pageID = generatePageId(innerText);
                }
            }

            if (!string.IsNullOrEmpty(pageID))
            {
                data.Name = IdToName(pageID);
                data.ToolTip = data.Name;

                //Logger.Log("-- PublishEvent [PageFoundByFlowDocumentParserEvent] DocumentPaneView.PageFoundByFlowDocumentParserEvent (" + data.Name + ")", Category.Debug, Priority.Medium);

                EventAggregator.GetEvent<PageFoundByFlowDocumentParserEvent>().Publish(data);
            }
        }

        private void formatPageNumberAndSetId_Span(TreeNode node, Span data)
        {
            //data.BorderBrush = Brushes.Orange;
            //data.BorderThickness = new Thickness(2.0);
            //data.Padding = new Thickness(2.0);
            data.FontWeight = FontWeights.Bold;
            data.FontSize = m_FlowDoc.FontSize * 1.2;
            data.Background = Brushes.Orange;
            data.Foreground = Brushes.DarkOrange;

            formatPageNumberAndSetId(node, data);
        }

        private void formatPageNumberAndSetId_Para(TreeNode node, Paragraph data)
        {
            data.BorderBrush = Brushes.Orange;
            data.BorderThickness = new Thickness(2.0);
            data.Padding = new Thickness(2.0);
            data.FontWeight = FontWeights.Bold;
            data.FontSize = m_FlowDoc.FontSize * 1.2;
            data.Background = Brushes.LightYellow;
            data.Foreground = Brushes.DarkOrange;

            formatPageNumberAndSetId(node, data);
        }

        private string generatePageId(string pageText)
        {
            return "id_tobipage_" + pageText.Replace(" ", "_");
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
            if (lastBlock != null && lastBlock is Section)
            {
                Block lastBlock2 = ((Section)lastBlock).Blocks.LastBlock;
                if (lastBlock2 != null && lastBlock2 is Paragraph && lastBlock2.Tag != null && lastBlock2.Tag is bool && ((bool)lastBlock2.Tag))
                {
                    ((Paragraph)lastBlock2).Inlines.Add(data);
                }
                else
                {
                    Paragraph para = new Paragraph(data);
                    para.Tag = true;
                    ((Section)lastBlock).Blocks.Add(para);
                }
            }
            else if (lastBlock != null && lastBlock is Paragraph && lastBlock.Tag != null && lastBlock.Tag is bool && ((bool)lastBlock.Tag))
            {
                ((Paragraph)lastBlock).Inlines.Add(data);
            }
            else
            {
                Paragraph para = new Paragraph(data);
                para.Tag = true;
                blocks.Add(new Section(para));
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

        private void setTag(TextElement data, TreeNode node)
        {
            data.Tag = node;
            data.Foreground = Brushes.Red;

            ManagedAudioMedia media = node.GetManagedAudioMedia();
            if (media != null)
            {
                data.Foreground = Brushes.Black;
                //data.Background = Brushes.LightGoldenrodYellow;
                data.Cursor = Cursors.Hand;
                data.MouseDown += OnMouseDownTextElementWithNodeAndAudio;
                return;
            }

            SequenceMedia seqManagedAudioMedia = node.GetManagedAudioSequenceMedia();
            if (seqManagedAudioMedia != null)
            {
                data.Foreground = Brushes.Black;
                //data.Background = Brushes.LightGoldenrodYellow;
                data.Cursor = Cursors.Cross;
                data.MouseDown += OnMouseDownTextElementWithNodeAndAudio;
                return;
            }

            TreeNode ancerstor = node.GetFirstAncestorWithManagedAudio();
            if (ancerstor != null)
            {
                data.Foreground = Brushes.Black;
                //data.Background = Brushes.LightGoldenrodYellow;
                data.Cursor = Cursors.SizeAll;
                data.MouseDown += OnMouseDownTextElementWithNodeAndAudio;
                return;
            }

            if (node.GetTextMedia() != null)
            {
                data.Foreground = Brushes.DarkGray;
                //data.Background = Brushes.LimeGreen;
                data.Cursor = Cursors.Pen;
                data.MouseDown += OnMouseDownTextElementWithNode;
            }
        }

        public static string IdToName(string id)
        {
            return id.Replace("-", "_DaSh_");
        }

        public static string NameToId(string name)
        {
            return name.Replace("_DaSh_", "-");
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_th_td(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia)
        {
            if (parent is Table)
            {
                m_currentTD++;
                TableCell data = new TableCell();
                setTag(data, node);

                data.BorderBrush = Brushes.LightGray;
                data.BorderThickness = new Thickness(1.0);

                TableRowGroup trg = ((Table)parent).RowGroups[m_currentROWGROUP];

                if (trg.Tag != null && trg.Tag is TreeNode)
                {
                    QualifiedName qn = ((TreeNode)trg.Tag).GetXmlElementQName();
                    if (qn != null)
                    {
                        if (qn.LocalName == "thead")
                        {
                            data.Background = Brushes.LightGreen;
                            data.FontWeight = FontWeights.Heavy;
                        }
                        if (qn.LocalName == "tfoot")
                        {
                            data.Background = Brushes.LightBlue;
                        }
                    }
                }
                if (qname.LocalName == "th")
                {
                    data.FontWeight = FontWeights.Heavy;
                }

                TableRowCollection trc = trg.Rows;
                trc[trc.Count - 1].Cells.Add(data);

                if (m_currentTD > ((Table)parent).Columns.Count)
                {
                    ((Table)parent).Columns.Add(new TableColumn());
                }

                XmlProperty xmlProp = node.GetProperty<XmlProperty>();
                XmlAttribute attr = xmlProp.GetAttribute("colspan");

                if (attr != null && !String.IsNullOrEmpty(attr.Value))
                {
                    data.ColumnSpan = int.Parse(attr.Value);
                }

                if (node.Children.Count == 0)
                {
                    if (textMedia == null || String.IsNullOrEmpty(textMedia.Text))
                    {
                        // ignore empty list item
                    }
                    else
                    {
                        data.Blocks.Add(new Paragraph(new Run(textMedia.Text)));
                    }

                    return parent;
                }
                //assumption based on the caller: when node.Children.Count != 0 then textMedia.Text == null
                else
                {
                    Section section = new Section();
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

        private TextElement walkBookTreeAndGenerateFlowDocument_Paragraph(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia, DelegateParagraphInitializer initializer)
        {
            Paragraph data = new Paragraph();
            setTag(data, node);

            if (initializer != null)
            {
                initializer(data);
            }

            if (node.Children.Count == 0)
            {
                if (textMedia == null || String.IsNullOrEmpty(textMedia.Text))
                {
                    data.Inlines.Add(new LineBreak());
                }
                else
                {
                    data.Inlines.Add(new Run(textMedia.Text));
                }

                addBlock(parent, data);
                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia.Text == null
            else
            {
                addBlock(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_underline_u(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia)
        {
            Underline data = new Underline();
            setTag(data, node);

            if (node.Children.Count == 0)
            {
                if (textMedia == null || String.IsNullOrEmpty(textMedia.Text))
                {
                    // ignore empty underline
                }
                else
                {
                    data.Inlines.Add(new Run(textMedia.Text));
                    addInline(parent, data);
                }

                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia.Text == null
            else
            {
                addInline(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_strong_b(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia)
        {
            Bold data = new Bold();
            setTag(data, node);

            if (node.Children.Count == 0)
            {
                if (textMedia == null || String.IsNullOrEmpty(textMedia.Text))
                {
                    // ignore empty bold
                }
                else
                {
                    data.Inlines.Add(new Run(textMedia.Text));
                    addInline(parent, data);
                }

                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia.Text == null
            else
            {
                addInline(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_em_i(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia)
        {
            Italic data = new Italic();
            setTag(data, node);

            if (node.Children.Count == 0)
            {
                if (textMedia == null || String.IsNullOrEmpty(textMedia.Text))
                {
                    // ignore empty italic
                }
                else
                {
                    data.Inlines.Add(new Run(textMedia.Text));
                    addInline(parent, data);
                }

                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia.Text == null
            else
            {
                addInline(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_list_dl(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia)
        {
            List data = new List();
            setTag(data, node);

            if (node.Children.Count == 0)
            {
                //ignore empty list
                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia.Text == null
            else
            {
                addBlock(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_table(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia)
        {
            m_cellsToExpand.Clear();
            m_currentTD = 0;

            Table data = new Table();
            setTag(data, node);

            data.CellSpacing = 4.0;
            data.BorderBrush = Brushes.Brown;
            data.BorderThickness = new Thickness(1.0);

            m_currentROWGROUP = -1;
            m_firstTR = false;

            if (node.Children.Count == 0)
            {
                //ignore empty table
                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia.Text == null
            else
            {
                addBlock(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_li_dd_dt(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia)
        {
            if (!(parent is List))
            {
#if DEBUG
                Debugger.Break();
#endif
                throw new Exception("list item not in List ??");
            }
            ListItem data = new ListItem();
            setTag(data, node);

            if (node.Children.Count == 0)
            {
                if (textMedia == null || String.IsNullOrEmpty(textMedia.Text))
                {
                    // ignore empty list item
                }
                else
                {
                    Paragraph para = new Paragraph(new Run(textMedia.Text));
                    data.Blocks.Add(para);
                    ((List)parent).ListItems.Add(data);

                    if (qname.LocalName == "pagenum")
                    {
                        data.Tag = null;
                        formatPageNumberAndSetId_Para(node, para);
                    }
                    else if (qname.LocalName == "hd")
                    {
                        data.Tag = null;
                        setTag(para, node);
                        formatListHeader(para);
                    }
                }

                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia.Text == null
            else
            {
                ((List)parent).ListItems.Add(data);
                if (qname.LocalName == "pagenum")
                {
                    data.Tag = null;
                    Paragraph para = new Paragraph();
                    formatPageNumberAndSetId_Para(node, para);
                    data.Blocks.Add(para);
                    return para;
                }
                else if (qname.LocalName == "hd")
                {
                    data.Tag = null;
                    Paragraph para = new Paragraph();
                    setTag(para, node);
                    formatListHeader(para);
                    data.Blocks.Add(para);
                    return para;
                }
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_tr_tbody_thead_tfoot_caption_pagenum(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia)
        {
            if (node.Children.Count == 0)
            {
                if (parent is Table)
                {
                    if ((qname.LocalName == "pagenum" || qname.LocalName == "caption")
                        && textMedia != null && !string.IsNullOrEmpty(textMedia.Text))
                    {
                        m_currentTD = 0;

                        TableRowGroup rowGroup = new TableRowGroup();

                        ((Table)parent).RowGroups.Add(rowGroup);
                        m_currentROWGROUP++;

                        TableRow data = new TableRow();
                        ((Table)parent).RowGroups[m_currentROWGROUP].Rows.Add(data);
                        Paragraph para = new Paragraph(new Run(textMedia.Text));
                        TableCell cell = new TableCell(para);

                        if (qname.LocalName == "caption")
                        {
                            setTag(para, node);
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
            //assumption based on the caller: when node.Children.Count != 0 then textMedia.Text == null
            else
            {
                if (parent is Table)
                {
                    if (qname.LocalName == "pagenum" || qname.LocalName == "caption")
                    {
                        m_currentTD = 0;

                        TableRowGroup rowGroup = new TableRowGroup();

                        ((Table)parent).RowGroups.Add(rowGroup);
                        m_currentROWGROUP++;

                        TableRow row = new TableRow();
                        ((Table)parent).RowGroups[m_currentROWGROUP].Rows.Add(row);
                        Paragraph para = new Paragraph();
                        TableCell cell = new TableCell(para);

                        if (qname.LocalName == "caption")
                        {
                            setTag(para, node);
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
                    else if (qname.LocalName == "thead"
                        || qname.LocalName == "tbody"
                        || qname.LocalName == "tfoot")
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
                            QualifiedName qnameParent = node.Parent.GetXmlElementQName();
                            if (qnameParent != null && qnameParent.LocalName == "table")
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

        private TextElement walkBookTreeAndGenerateFlowDocument_anchor_a(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia)
        {
            Hyperlink data = new Hyperlink();
            setTag(data, node);

            XmlProperty xmlProp = node.GetProperty<XmlProperty>();
            XmlAttribute attr = xmlProp.GetAttribute("href");

            if (attr != null && !String.IsNullOrEmpty(attr.Value))
            {
                data.NavigateUri = new Uri(attr.Value, UriKind.RelativeOrAbsolute);
                data.RequestNavigate += new RequestNavigateEventHandler(OnRequestNavigate);
                data.ToolTip = data.NavigateUri.ToString();
            }

            if (node.Children.Count == 0)
            {
                if (textMedia == null || String.IsNullOrEmpty(textMedia.Text))
                {
                    if (attr != null && !String.IsNullOrEmpty(attr.Value))
                    {
                        data.Inlines.Add(new Run(attr.Value));
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
                    data.Inlines.Add(new Run(textMedia.Text));
                    addInline(parent, data);
                }

                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia.Text == null
            else
            {
                addInline(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_annoref_noteref(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia)
        {
            Hyperlink data = new Hyperlink();
            setTag(data, node);

            data.FontSize = m_FlowDoc.FontSize / 1.2;
            data.FontWeight = FontWeights.Bold;
            data.Background = Brushes.LightSkyBlue;
            data.Foreground = Brushes.Blue;

            XmlProperty xmlProp = node.GetProperty<XmlProperty>();
            XmlAttribute attr = xmlProp.GetAttribute("idref");

            if (attr != null && !String.IsNullOrEmpty(attr.Value))
            {
                data.NavigateUri = new Uri((attr.Value.StartsWith("#") ? "" : "#") + attr.Value, UriKind.Relative);
                data.RequestNavigate += new RequestNavigateEventHandler(OnRequestNavigate);
            }
            else
            {
                //ignore: no link
            }

            if (node.Children.Count == 0)
            {
                if (textMedia == null || String.IsNullOrEmpty(textMedia.Text))
                {
                    data.Inlines.Add(new Run("..."));
                    addInline(parent, data);
                }
                else
                {
                    data.Inlines.Add(new Run(textMedia.Text));
                    addInline(parent, data);
                }

                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia.Text == null
            else
            {
                addInline(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_Span(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia, DelegateSpanInitializer initializer)
        {
            Span data = new Span();
            setTag(data, node);

            if (initializer != null)
            {
                initializer(data);
            }

            if (node.Children.Count == 0)
            {
                if (textMedia == null || String.IsNullOrEmpty(textMedia.Text))
                {
                    data.Inlines.Add(new Run("..."));
                    addInline(parent, data);
                }
                else
                {
                    var run = new Run(textMedia.Text);
                    if (qname.LocalName == "sup")
                    {
                        run.SetValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Superscript);
                    }
                    else if (qname.LocalName == "sub")
                    {
                        run.SetValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Subscript);
                    }
                    data.Inlines.Add(run);
                    addInline(parent, data);
                }

                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia.Text == null
            else
            {
                addInline(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_Floater(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia, DelegateFloaterInitializer initializer)
        {
            Floater data = new Floater();
            setTag(data, node);

            if (initializer != null)
            {
                initializer(data);
            }

            if (node.Children.Count == 0)
            {
                if (textMedia == null || String.IsNullOrEmpty(textMedia.Text))
                {
                    data.Blocks.Add(new Paragraph(new LineBreak()));
                }
                else
                {
                    data.Blocks.Add(new Paragraph(new Run(textMedia.Text)));
                }

                addInline(parent, data);
                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia.Text == null
            else
            {
                addInline(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_Figure(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia, DelegateFigureInitializer initializer)
        {
            Figure data = new Figure();
            setTag(data, node);

            if (initializer != null)
            {
                initializer(data);
            }

            if (node.Children.Count == 0)
            {
                if (textMedia == null || String.IsNullOrEmpty(textMedia.Text))
                {
                    data.Blocks.Add(new Paragraph(new LineBreak()));
                }
                else
                {
                    data.Blocks.Add(new Paragraph(new Run(textMedia.Text)));
                }

                addInline(parent, data);
                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia.Text == null
            else
            {
                addInline(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_Section(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia, DelegateSectionInitializer initializer)
        {
            Section data = new Section();
            setTag(data, node);

            if (initializer != null)
            {
                initializer(data);
            }

            if (node.Children.Count == 0)
            {
                if (textMedia == null || String.IsNullOrEmpty(textMedia.Text))
                {
                    data.Blocks.Add(new Paragraph(new LineBreak()));
                }
                else
                {
                    data.Blocks.Add(new Paragraph(new Run(textMedia.Text)));
                }

                addBlock(parent, data);
                return parent;
            }
            //assumption based on the caller: when node.Children.Count != 0 then textMedia.Text == null
            else
            {
                addBlock(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_img(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia)
        {
            if (node.Children.Count != 0 || textMedia != null && !String.IsNullOrEmpty(textMedia.Text))
            {
#if DEBUG
                Debugger.Break();
#endif
                throw new Exception("Node has children or text exists when processing image ??");
            }

            XmlProperty xmlProp = node.GetProperty<XmlProperty>();
            XmlAttribute srcAttr = xmlProp.GetAttribute("src");

            if (srcAttr == null) return parent;

            Image image = new Image();

            if (srcAttr.Value.StartsWith("http://"))
            {
                try
                {
                    //image.Source = new BitmapImage(new Uri(srcAttr.Value, UriKind.Absolute));

                    string imagePath = new Uri(srcAttr.Value, UriKind.Absolute).AbsolutePath;
                    imagePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(imagePath));

                    //string batchFile = Path.ChangeExtension(Path.GetTempFileName(), "bat");

                    WebClient webClient = new WebClient();
                    webClient.Proxy = null;
                    webClient.DownloadFile(srcAttr.Value, imagePath);

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.None;
                    bitmap.EndInit();

                    //bitmap.Freeze(); COM Exception !!
                    image.Source = bitmap;
                }
                catch (Exception)
                {
                    return parent;
                }
            }
            else
            {
                //http://blogs.msdn.com/yangxind/archive/2006/11/09/don-t-use-net-system-uri-unescapedatastring-in-url-decoding.aspx

                string dirPath = Path.GetDirectoryName(m_TreeNode.Presentation.RootUri.LocalPath);
                string fullImagePath = Path.Combine(dirPath, Uri.UnescapeDataString(srcAttr.Value));

                try
                {
                    //BitmapImage bitmap = new BitmapImage(new Uri(fullImagePath, UriKind.Absolute)); // { CacheOption = BitmapCacheOption.OnLoad };

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(fullImagePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.None;
                    bitmap.EndInit();

                    bitmap.Freeze();
                    image.Source = bitmap;
                }
                catch (Exception)
                {
                    return parent;
                }
            }

            image.Stretch = Stretch.Uniform;
            image.StretchDirection = StretchDirection.Both;

            if (image.Source is BitmapSource)
            {
                BitmapSource bitmap = (BitmapSource)image.Source;
                int ph = bitmap.PixelHeight;
                int pw = bitmap.PixelWidth;
                double dpix = bitmap.DpiX;
                double dpiy = bitmap.DpiY;
                image.Width = ph;
                image.Height = pw;
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

            image.MinWidth = image.Width;
            image.MinHeight = image.Height;
            //image.MaxWidth = image.Width;
            //image.MaxHeight = image.Height;

            //Floater floater = new Floater();
            //floater.Blocks.Add(img);
            //floater.Width = image.Width;
            //addInline(parent, floater);

            //Figure figure = new Figure(img);
            //figure.Width = image.Width;
            //addInline(parent, figure);

            bool parentHasBlocks = parent is TableCell
                                   || parent is Section
                                   || parent is Floater
                                   || parent is Figure
                                   || parent is ListItem;

            if (parentHasBlocks)
            {
                BlockUIContainer img = new BlockUIContainer(image);

                //img.BorderBrush = Brushes.RoyalBlue;
                //img.BorderThickness = new Thickness(2.0);

                setTag(img, node);

                addBlock(parent, img);


                XmlAttribute altAttr = xmlProp.GetAttribute("alt");

                if (altAttr != null && !string.IsNullOrEmpty(altAttr.Value))
                {
                    image.ToolTip = altAttr.Value;
                    Paragraph paraAlt = new Paragraph(new Run("ALT: " + altAttr.Value));
                    paraAlt.BorderBrush = Brushes.CadetBlue;
                    paraAlt.BorderThickness = new Thickness(1.0);
                    paraAlt.FontSize = m_FlowDoc.FontSize / 1.2;
                    addBlock(parent, paraAlt);
                }
            }
            else
            {
                InlineUIContainer img = new InlineUIContainer(image);

                setTag(img, node);

                addInline(parent, img);
            }


            return parent;

            /*
                WebClient webClient = new WebClient();
                fullImagePath = srcAttr.Value;
                byte[] imageContent = webClient.DownloadData(srcAttr.Value);
                Stream stream = new MemoryStream(imageContent);
                 */
            /*
             * stream = new FileStream(fullImagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
             * 
             * 
            if (fullImagePath.EndsWith(".jpg") || fullImagePath.EndsWith(".jpeg"))
            {
                BitmapDecoder dec = new JpegBitmapDecoder(stream, BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.OnLoad);
                image.Source = dec.Frames[0];
            }
            else if (fullImagePath.EndsWith(".png"))
            {
                BitmapDecoder dec = new PngBitmapDecoder(stream, BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.OnLoad);
                image.Source = dec.Frames[0];
            }
            else if (fullImagePath.EndsWith(".bmp"))
            {
                BitmapDecoder dec = new BmpBitmapDecoder(stream, BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.OnLoad);
                image.Source = dec.Frames[0];
            }
            else if (fullImagePath.EndsWith(".gif"))
            {
                BitmapDecoder dec = new GifBitmapDecoder(stream, BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.OnLoad);
                image.Source = dec.Frames[0];
            }
             */
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia)
        {
            if (qname == null)
            {
                //assumption based on the caller: node.Children.Count == 0 && textMedia != null
                if (textMedia.Text.Length == 0)
                {
                    return parent;
                }

                Run data = new Run(textMedia.Text);
                setTag(data, node);
                addInline(parent, data);

                return parent;
            }

            if (qname.NamespaceUri.Length == 0
                || qname.NamespaceUri == m_TreeNode.Presentation.PropertyFactory.DefaultXmlNamespaceUri)
            {
                // node.Children.Count ?
                // String.IsNullOrEmpty(textMedia.Text) ?

                switch (qname.LocalName)
                {
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
                            return walkBookTreeAndGenerateFlowDocument_Section(node, parent, qname, textMedia, null);
                        }
                    case "frontmatter":
                    case "rearmatter":
                        {
                            return walkBookTreeAndGenerateFlowDocument_Section(node, parent, qname, textMedia,
                                data =>
                                {
                                    data.BorderBrush = Brushes.GreenYellow;
                                    data.BorderThickness = new Thickness(2.0);
                                    data.Padding = new Thickness(4.0);
                                }
                                );
                        }
                    case "blockquote":
                        {
                            return walkBookTreeAndGenerateFlowDocument_Section(node, parent, qname, textMedia,
                                data =>
                                {
                                    data.BorderBrush = Brushes.Olive;
                                    data.BorderThickness = new Thickness(2.0);
                                    data.Padding = new Thickness(2.0);
                                    data.Margin = new Thickness(4.0);
                                }
                                );
                        }
                    case "note":
                    case "annotation":
                        {
                            return walkBookTreeAndGenerateFlowDocument_Section(node, parent, qname, textMedia,
                                data =>
                                {
                                    data.BorderBrush = Brushes.LightSkyBlue;
                                    data.BorderThickness = new Thickness(2.0);
                                    data.Padding = new Thickness(2.0);
                                    data.FontSize = m_FlowDoc.FontSize / 1.2;

                                    XmlProperty xmlProp = node.GetProperty<XmlProperty>();
                                    XmlAttribute attr = xmlProp.GetAttribute("id");

                                    if (attr != null && !String.IsNullOrEmpty(attr.Value))
                                    {
                                        data.Name = IdToName(attr.Value);
                                        data.ToolTip = data.Name;
                                        m_DelegateAddIdLinkTarget(data.Name, data);
                                    }
                                }
                                );
                        }
                    case "noteref":
                    case "annoref":
                        {
                            return walkBookTreeAndGenerateFlowDocument_annoref_noteref(node, parent, qname, textMedia);
                        }
                    case "caption":
                        {
                            if (parent is Table)
                            {
                                return walkBookTreeAndGenerateFlowDocument_tr_tbody_thead_tfoot_caption_pagenum(node, parent, qname, textMedia);
                            }
                            else
                            {
                                return walkBookTreeAndGenerateFlowDocument_Paragraph(node, parent, qname, textMedia,
                                                 data =>
                                                 {
                                                     data.BorderBrush =
                                                         Brushes.Green;
                                                     data.BorderThickness =
                                                         new Thickness(1.0);
                                                     data.Padding =
                                                         new Thickness(2.0);
                                                     data.FontWeight =
                                                         FontWeights.Light;
                                                     data.FontSize =
                                                         m_FlowDoc.FontSize / 1.2;
                                                     data.Foreground =
                                                         Brushes.DarkGreen;
                                                 });
                            }
                        }
                    case "h1":
                    case "hd":
                        {
                            if (qname.LocalName == "hd" && parent is List)
                            {
                                return walkBookTreeAndGenerateFlowDocument_li_dd_dt(node, parent, qname, textMedia);
                            }
                            return walkBookTreeAndGenerateFlowDocument_Section(node, parent, qname, textMedia,
                                data =>
                                {
                                    data.FontSize = m_FlowDoc.FontSize * 2;
                                    data.FontWeight = FontWeights.Heavy;
                                });
                        }
                    case "h2":
                        {
                            return walkBookTreeAndGenerateFlowDocument_Section(node, parent, qname, textMedia,
                                data =>
                                {
                                    data.FontSize = m_FlowDoc.FontSize * 1.5;
                                    data.FontWeight = FontWeights.Heavy;
                                });
                        }
                    case "h3":
                    case "h4":
                    case "h5":
                    case "h6":
                        {
                            return walkBookTreeAndGenerateFlowDocument_Section(node, parent, qname, textMedia,
                                data =>
                                {
                                    data.FontSize = m_FlowDoc.FontSize * 1.2;
                                    data.FontWeight = FontWeights.Heavy;
                                });
                        }
                    case "doctitle":
                    case "docauthor":
                    case "covertitle":
                        {
                            return walkBookTreeAndGenerateFlowDocument_Paragraph(node, parent, qname, textMedia,
                                data =>
                                {
                                    data.FontSize = m_FlowDoc.FontSize * 1.2;
                                    data.FontWeight = FontWeights.Heavy;
                                    data.Foreground = Brushes.Navy;
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
                                return walkBookTreeAndGenerateFlowDocument_tr_tbody_thead_tfoot_caption_pagenum(node, parent, qname, textMedia);
                            }
                            if (parent is List)
                            {
                                return walkBookTreeAndGenerateFlowDocument_li_dd_dt(node, parent, qname, textMedia);
                            }
                            if (parent == null || parent is TableCell || parent is Section || parent is Floater || parent is Figure || parent is ListItem)
                            {
                                return walkBookTreeAndGenerateFlowDocument_Paragraph(node, parent, qname, textMedia, delegatePageNumPara);
                            }
                            if (parent is Paragraph || parent is Span)
                            {
                                return walkBookTreeAndGenerateFlowDocument_Span(node, parent, qname, textMedia, delegatePageNumSpan);
                            }

                            Debug.Fail("Page pagenum cannot be added due to incompatible FlowDocument structure !");
                            break;
                        }
                    case "imggroup":
                        {
                            return walkBookTreeAndGenerateFlowDocument_Section(node, parent, qname, textMedia,
                                data =>
                                {
                                    data.BorderBrush = Brushes.LightSalmon;
                                    data.BorderThickness = new Thickness(0.5);
                                    data.Padding = new Thickness(2.0);
                                });
                        }
                    case "sidebar":
                        {
                            return walkBookTreeAndGenerateFlowDocument_Floater(node, parent, qname, textMedia,
                                data =>
                                {
                                    data.BorderBrush = Brushes.MediumSlateBlue;
                                    data.BorderThickness = new Thickness(2.0);
                                    data.Padding = new Thickness(2.0);
                                });
                        }
                    case "img":
                        {
                            return walkBookTreeAndGenerateFlowDocument_img(node, parent, qname, textMedia);
                        }
                    case "th":
                    case "td":
                        {
                            return walkBookTreeAndGenerateFlowDocument_th_td(node, parent, qname, textMedia);
                        }
                    case "br":
                        {
                            LineBreak data = new LineBreak();
                            setTag(data, node);
                            addInline(parent, data);
                            return parent;
                        }
                    case "em":
                    case "i":
                        {
                            return walkBookTreeAndGenerateFlowDocument_em_i(node, parent, qname, textMedia);
                        }
                    case "strong":
                    case "b":
                        {
                            return walkBookTreeAndGenerateFlowDocument_strong_b(node, parent, qname, textMedia);
                        }
                    case "underline":
                    case "u":
                        {
                            return walkBookTreeAndGenerateFlowDocument_underline_u(node, parent, qname, textMedia);
                        }
                    case "anchor":
                    case "a":
                        {
                            return walkBookTreeAndGenerateFlowDocument_anchor_a(node, parent, qname, textMedia);
                        }
                    case "table":
                        {
                            return walkBookTreeAndGenerateFlowDocument_table(node, parent, qname, textMedia);
                        }
                    case "tr":
                    case "thead":
                    case "tfoot":
                    case "tbody":
                        {
                            return walkBookTreeAndGenerateFlowDocument_tr_tbody_thead_tfoot_caption_pagenum(node, parent, qname, textMedia);
                        }
                    case "list":
                    case "ul":
                    case "ol":
                    case "dl":
                        {
                            return walkBookTreeAndGenerateFlowDocument_list_dl(node, parent, qname, textMedia);
                        }
                    case "dt":
                    case "dd":
                    case "li":
                        {
                            return walkBookTreeAndGenerateFlowDocument_li_dd_dt(node, parent, qname, textMedia);
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
                            return walkBookTreeAndGenerateFlowDocument_Span(node, parent, qname, textMedia, null);
                        }
                    case "sup":
                    case "sub":
                        {
                            return walkBookTreeAndGenerateFlowDocument_Span(node, parent, qname, textMedia, null);
                        }
                    case "ruby":
                        {
                            return walkBookTreeAndGenerateFlowDocument_Span(node, parent, qname, textMedia,
                                data =>
                                {
                                    data.Background = Brushes.BlanchedAlmond;
                                }
                                );
                        }
                    case "rb":
                        {
                            return walkBookTreeAndGenerateFlowDocument_Span(node, parent, qname, textMedia,
                                data =>
                                {
                                    data.Inlines.Add(new Run(" "));
                                    data.TextDecorations = TextDecorations.OverLine;
                                });

                        }
                    case "rt":
                        {
                            return walkBookTreeAndGenerateFlowDocument_Span(node, parent, qname, textMedia,
                                data =>
                                {
                                    //var converter = new FontFamilyConverter();
                                    //data.FontFamily = (FontFamily)converter.ConvertFrom("Meiryo");

                                    //data.FontFamily = new FontFamily("Meiryo");

                                    data.Typography.Variants = FontVariants.Subscript;

                                    var xmlProp = node.GetProperty<XmlProperty>();
                                    if (xmlProp != null)
                                    {
                                        var attr = xmlProp.GetAttribute("rbspan");

                                        if (attr != null && !String.IsNullOrEmpty(attr.Value))
                                        {
                                            data.TextDecorations = TextDecorations.Underline;
                                            return;
                                        }
                                    }
                                    data.Inlines.Add(new Run(" "));
                                }
                                );
                        }
                    case "acronym":
                        {
                            return walkBookTreeAndGenerateFlowDocument_Span(node, parent, qname, textMedia,
                                data =>
                                {
                                    XmlProperty xmlProp = node.GetProperty<XmlProperty>();
                                    if (xmlProp == null) return;

                                    XmlAttribute attr = xmlProp.GetAttribute("pronounce");
                                    if (attr == null) return;

                                    if (!String.IsNullOrEmpty(attr.Value))
                                    {
                                        data.ToolTip = "pronounce = " + attr.Value;
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
                            return walkBookTreeAndGenerateFlowDocument_Paragraph(node, parent, qname, textMedia, null);
                        }
                    case "lic":
                    case "prodnote":
                    case "div":
                    case "samp":
                    case "poem":
                    case "linegroup":
                    case "code":
                    case "book":
                    case "address":
                    case "epigraph":
                        {
                            return walkBookTreeAndGenerateFlowDocument_Section(node, parent, qname, textMedia, null);
                        }
                    case "col":
                    case "colgroup":
                        {
                            System.Diagnostics.Debug.Fail(String.Format("DTBook element not yet supported [{0}]", qname.LocalName));
                            break;
                        }
                    case "hr":
                        {
                            Console.WriteLine("XUK to FlowDocument converter: ignoring HR markup.");
                            break;
                        }
                    default:
                        {
                            System.Diagnostics.Debug.Fail(String.Format("Unknown DTBook element ! [{0}]", qname.LocalName));
                            break;
                        }
                }
            }
            else
            {
                //System.Diagnostics.Debug.Fail(String.Format("Unknown element namespace in DTBook ! [{0}]", qname.NamespaceUri));
            }

            return parent;
        }

        private void walkBookTreeAndGenerateFlowDocument(TreeNode node, TextElement parent)
        {
            TextElement parentNext = parent;

            QualifiedName qname = node.GetXmlElementQName();
            AbstractTextMedia textMedia = node.GetTextMedia();

            if (node.Children.Count == 0)
            {
                if (qname == null)
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
                        parentNext = walkBookTreeAndGenerateFlowDocument_(node, parent, qname, textMedia);
                    }
                }
                else //childCount == 0 && qname != null
                {
                    if (textMedia == null)
                    {
                        //parentNext = generateFlowDocument_NoChild_Xml_NoText(node, parent, qname);
                        parentNext = walkBookTreeAndGenerateFlowDocument_(node, parent, qname, textMedia);
                    }
                    else //childCount == 0 && qname != null && textMedia != null
                    {
                        //parentNext = generateFlowDocument_NoChild_Xml_Text(node, parent, qname, textMedia);
                        parentNext = walkBookTreeAndGenerateFlowDocument_(node, parent, qname, textMedia);
                    }
                }
            }
            else //childCount != 0
            {
                if (qname == null)
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
                        //parentNext = generateFlowDocument_Child_Xml_NoText(node, parent, qname);
                        parentNext = walkBookTreeAndGenerateFlowDocument_(node, parent, qname, textMedia);
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
                    walkBookTreeAndGenerateFlowDocument(node.Children.Get(i), parentNext);
                }

                if (qname.LocalName == "table")
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
                            QualifiedName qn = ((TreeNode)trg.Tag).GetXmlElementQName();
                            if (qn != null)
                            {
                                switch (qn.LocalName)
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
                                                QualifiedName qun = ((TreeNode)trgFirst.Tag).GetXmlElementQName();
                                                if (qun != null && qun.LocalName == "caption")
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
    }
}
