using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml;
using DTbookToXuk;
using Microsoft.Win32;
using urakawa;
using urakawa.core;
using urakawa.media;
using urakawa.navigation;
using urakawa.property.xml;
using urakawa.xuk;
using XmlAttribute = urakawa.property.xml.XmlAttribute;

namespace WpfDtbookTest
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : INotifyPropertyChanged
    {
        public HeadingsNavigator TOC
        {
            get
            {
                return m_HeadingsNavigator;
            }
        }
        public class HeadingTreeNodeWrapper
        {
            private TreeNode m_TreeNode;
            private TreeNode m_TreeNodeHeading;
            private HeadingsNavigator m_navigator;
            private ObservableCollection<HeadingTreeNodeWrapper> m_children;

            public TreeNode WrappedTreeNode_Level
            {
                get
                {
                    return m_TreeNode;
                }
            }
            public TreeNode WrappedTreeNode_LevelHeading
            {
                get
                {
                    return m_TreeNodeHeading;
                }
            }

            public HeadingTreeNodeWrapper(HeadingsNavigator navigator, TreeNode node)
            {
                m_TreeNode = node;
                m_navigator = navigator;
            }
            public string Title
            {
                get
                {
                    if (m_TreeNodeHeading == null && m_TreeNode.ChildCount > 0)
                    {
                        TreeNode node = m_TreeNode.GetChild(0);
                        QualifiedName qname = node.GetXmlElementQName();
                        if (qname != null && (qname.LocalName == "hd"
                            || qname.LocalName == "h1"
                            || qname.LocalName == "h2"
                            || qname.LocalName == "h3"
                            || qname.LocalName == "h4"
                            || qname.LocalName == "h5"
                            || qname.LocalName == "h6"
                            ))
                        {
                            m_TreeNodeHeading = node;
                        }
                    }
                    string str = (m_TreeNodeHeading != null ? m_TreeNodeHeading.GetTextMediaFlattened() : "??" + m_TreeNode.GetXmlElementQName().LocalName);
                    return str;
                }
            }
            public ObservableCollection<HeadingTreeNodeWrapper> Children
            {
                get
                {
                    if (m_children == null)
                    {
                        m_children = new ObservableCollection<HeadingTreeNodeWrapper>();

                        int n = m_navigator.GetChildCount(m_TreeNode);
                        for (int index = 0; index < n; index++)
                        {
                            TreeNode node = m_navigator.GetChild(m_TreeNode, index);
                            m_children.Add(new HeadingTreeNodeWrapper(m_navigator, node));
                        }
                    }
                    return m_children;
                }
            }
        }

        private void OnHeadingSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            HeadingTreeNodeWrapper node = TreeView.SelectedItem as HeadingTreeNodeWrapper;
            if (node != null)
            {
                BringIntoViewAndHighlight((node.WrappedTreeNode_LevelHeading ?? node.WrappedTreeNode_Level));
            }
        }

        public class HeadingsNavigator : AbstractFilterNavigator
        {
            public HeadingsNavigator(Project project)
            {
                m_Project = project;
            }
            private ObservableCollection<HeadingTreeNodeWrapper> m_roots;
            private readonly Project m_Project;

            public ObservableCollection<HeadingTreeNodeWrapper> Roots
            {
                get
                {
                    if (m_roots == null)
                    {
                        m_roots = new ObservableCollection<HeadingTreeNodeWrapper>();
                        TreeNode presentationRootNode = m_Project.GetPresentation(0).RootNode;
                        int n = GetChildCount(presentationRootNode);
                        for (int index = 0; index < n; index++)
                        {
                            TreeNode node = GetChild(presentationRootNode, index);
                            m_roots.Add(new HeadingTreeNodeWrapper(this, node));
                        }
                    }
                    return m_roots;
                }
            }

            //treeView.SelectedNode.EnsureVisible();

            public override bool IsIncluded(TreeNode node)
            {
                QualifiedName qname = node.GetXmlElementQName();
                return qname != null &&
                    (qname.LocalName == "level1"
                    || qname.LocalName == "level"
                    || qname.LocalName == "level2"
                    || qname.LocalName == "level3"
                    || qname.LocalName == "level4"
                    || qname.LocalName == "level5"
                    || qname.LocalName == "level6"
                    );
            }
        }
        public class Page
        {
            public Page(TextElement textElement)
            {
                TextElement = textElement;
            }

            public TextElement TextElement
            {
                get;
                private set;
            }
            public string Id
            {
                get
                {
                    return TextElement.Name;
                }
            }
            public string Name
            {
                get
                {
                    if (TextElement is Paragraph)
                    {
                        return extractString((Paragraph)TextElement);
                    }
                    return "??";
                }
            }
        }

        private Page GetPage(string id)
        {
            foreach (Page page in Pages)
            {
                if (page.Id == id) return page;
            }
            return null;
        }

        protected static string extractString(Paragraph para)
        {
            StringBuilder str = new StringBuilder();
            foreach (Inline inline in para.Inlines)
            {
                if (inline is Run)
                {
                    str.Append(((Run)inline).Text);
                }
                else if (inline is Span)
                {
                    str.Append(extractString((Span)inline));
                }
            }
            return str.ToString();
        }

        protected static string extractString(Span span)
        {
            StringBuilder str = new StringBuilder();
            foreach (Inline inline in span.Inlines)
            {
                if (inline is Run)
                {
                    str.Append(((Run)inline).Text);
                }
                else if (inline is Span)
                {
                    str.Append(extractString((Span)inline));
                }
            }
            return str.ToString();
        }

        delegate void DelegateSectionInitializer(Section secstion);
        delegate void DelegateFigureInitializer(Figure fig);
        delegate void DelegateFloaterInitializer(Floater floater);
        delegate void DelegateSpanInitializer(Span span);
        delegate void DelegateParagraphInitializer(Paragraph para);

        private string m_FilePath;
        private FlowDocument m_FlowDoc;
        private Project m_XukProject;

        private int m_currentTD;
        private bool m_firstTR;
        private int m_currentROWGROUP;
        List<TableCell> m_cellsToExpand = new List<TableCell>();

        private Dictionary<string, TextElement> m_idLinkTargets;
        //private Dictionary<string, TextElement> m_idPageMarkers;

        private TextElement m_lastHighlighted;
        private Brush m_lastHighlighted_Background;
        private Brush m_lastHighlighted_Foreground;


        private ObservableCollection<Page> _Pages = new ObservableCollection<Page>();
        private HeadingsNavigator m_HeadingsNavigator;

        public ObservableCollection<Page> Pages
        {
            get
            {
                return _Pages;
            }
        }

        public Window1()
        {
            InitializeComponent();
            DataContext = this;
        }
        public string FilePath
        {
            get
            {
                return m_FilePath;
            }
            set
            {
                if (m_FilePath == value) return;
                m_FilePath = value;
                OnPropertyChanged("FilePath");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        private void OnOpenFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.FileName = "dtbook"; // Default file name
            dlg.DefaultExt = ".xml"; // Default file extension
            dlg.Filter = "DTBOOK documents (.xml)|*.xml;*.opf;*.ncx";
            bool? result = dlg.ShowDialog();
            if (result == false)
            {
                return;
            }

            FilePath = dlg.FileName;

            var uri = new Uri(FilePath);
            DTBooktoXukConversion converter = new DTBooktoXukConversion(uri);

            m_XukProject = converter.Project;

            m_HeadingsNavigator = new HeadingsNavigator(m_XukProject);

            TreeView.DataContext = TOC;

            if (m_idLinkTargets != null)
            {
                m_idLinkTargets.Clear();
            }
            m_idLinkTargets = new Dictionary<string, TextElement>();
            Pages.Clear();

            m_lastHighlighted = null;

            FlowDocument flowDoc = createFlowDocumentFromXuk();
            flowDoc.IsEnabled = true;
            flowDoc.IsHyphenationEnabled = false;
            flowDoc.IsOptimalParagraphEnabled = false;
            flowDoc.ColumnWidth = Double.PositiveInfinity;
            flowDoc.IsColumnWidthFlexible = false;
            flowDoc.TextAlignment = TextAlignment.Left;

            FlowDocReader.Zoom = 120;
            FlowDocReader.Document = flowDoc;

            /*
            string dirPath = Path.GetDirectoryName(FilePath);
            string fullPath = Path.Combine(dirPath, "FlowDocument.xaml");

            using (FileStream stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                try
                {
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Encoding = Encoding.UTF8;
                    settings.NewLineHandling = NewLineHandling.Replace;
                    settings.NewLineChars = "\n";
                    settings.Indent = true;
                    settings.IndentChars = "\t";
                    settings.NewLineOnAttributes = true;

                    XmlWriter xmlWriter = XmlWriter.Create(stream, settings);

                    XamlWriter.Save(m_FlowDoc, xmlWriter);
                }
                finally
                {
                    stream.Close();
                }
            }*/

        }

        private FlowDocument createFlowDocumentFromXuk()
        {
            TreeNode root = m_XukProject.GetPresentation(0).RootNode;
            TreeNode nodeBook = getTreeNodeWithXmlElementName(root, "book");
            if (nodeBook == null)
            {
                return null;
            }

            m_FlowDoc = new FlowDocument();
            m_FlowDoc.Blocks.Clear();

            walkBookTreeAndGenerateFlowDocument(nodeBook, null);

            return m_FlowDoc;
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_img(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia)
        {
            if (node.ChildCount != 0 || textMedia != null && !String.IsNullOrEmpty(textMedia.Text))
            {
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
                    image.Source = new BitmapImage(new Uri(srcAttr.Value, UriKind.Absolute));
                }
                catch (Exception)
                {
                    return parent;
                }
            }
            else
            {
                //http://blogs.msdn.com/yangxind/archive/2006/11/09/don-t-use-net-system-uri-unescapedatastring-in-url-decoding.aspx

                string dirPath = Path.GetDirectoryName(FilePath);
                string fullImagePath = Path.Combine(dirPath, Uri.UnescapeDataString(srcAttr.Value));

                try
                {
                    image.Source = new BitmapImage(new Uri(fullImagePath));
                }
                catch (Exception)
                {
                    return parent;
                }
            }

            image.Stretch = Stretch.Uniform;

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

            //BlockUIContainer img = new BlockUIContainer(image);
            //addBlock(parent, img);

            InlineUIContainer img = new InlineUIContainer(image);
            img.Tag = node;

            addInline(parent, img);



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

        private TextElement walkBookTreeAndGenerateFlowDocument_th_td(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia)
        {
            if (parent is Table)
            {
                m_currentTD++;
                TableCell data = new TableCell();
                data.Tag = node;

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

                if (node.ChildCount == 0)
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
                //assumption based on the caller: when node.ChildCount != 0 then textMedia.Text == null
                else
                {
                    Section section = new Section();
                    data.Blocks.Add(section);
                    return section;
                }
            }
            else
            {
                throw new Exception("Trying to add TableCell btu parent is not Table ??");
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_Paragraph(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia, DelegateParagraphInitializer initializer)
        {
            Paragraph data = new Paragraph();
            data.Tag = node;

            if (initializer != null)
            {
                initializer(data);
            }

            if (node.ChildCount == 0)
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
            //assumption based on the caller: when node.ChildCount != 0 then textMedia.Text == null
            else
            {
                addBlock(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_underline_u(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia)
        {
            Underline data = new Underline();
            data.Tag = node;

            if (node.ChildCount == 0)
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
            //assumption based on the caller: when node.ChildCount != 0 then textMedia.Text == null
            else
            {
                addInline(parent, data);
                return data;
            }
        }
        private TextElement walkBookTreeAndGenerateFlowDocument_strong_b(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia)
        {
            Bold data = new Bold();
            data.Tag = node;

            if (node.ChildCount == 0)
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
            //assumption based on the caller: when node.ChildCount != 0 then textMedia.Text == null
            else
            {
                addInline(parent, data);
                return data;
            }
        }
        private TextElement walkBookTreeAndGenerateFlowDocument_em_i(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia)
        {
            Italic data = new Italic();
            data.Tag = node;

            if (node.ChildCount == 0)
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
            //assumption based on the caller: when node.ChildCount != 0 then textMedia.Text == null
            else
            {
                addInline(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_list_dl(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia)
        {
            List data = new List();
            data.Tag = node;

            if (node.ChildCount == 0)
            {
                //ignore empty list
                return parent;
            }
            //assumption based on the caller: when node.ChildCount != 0 then textMedia.Text == null
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
            data.Tag = node;

            data.CellSpacing = 4.0;
            data.BorderBrush = Brushes.Brown;
            data.BorderThickness = new Thickness(1.0);

            m_currentROWGROUP = -1;
            m_firstTR = false;

            if (node.ChildCount == 0)
            {
                //ignore empty table
                return parent;
            }
            //assumption based on the caller: when node.ChildCount != 0 then textMedia.Text == null
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
                throw new Exception("list item not in List ??");
            }
            ListItem data = new ListItem();
            data.Tag = node;

            if (node.ChildCount == 0)
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
                        formatPageNumberAndSetId(node, para);
                    }
                }

                return parent;
            }
            //assumption based on the caller: when node.ChildCount != 0 then textMedia.Text == null
            else
            {
                ((List)parent).ListItems.Add(data);
                if (qname.LocalName == "pagenum")
                {
                    Paragraph para = new Paragraph();
                    formatPageNumberAndSetId(node, para);
                    data.Blocks.Add(para);
                    return para;
                }
                return data;
            }
        }
        private void formatCaptionCell(TableCell cell)
        {
            cell.BorderBrush = Brushes.BlueViolet;
            cell.BorderThickness = new Thickness(2);
            cell.Background = Brushes.LightYellow;
            cell.Foreground = Brushes.Navy;
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_tr_tbody_thead_tfoot_caption_pagenum(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia)
        {
            if (node.ChildCount == 0)
            {
                if (parent is Table)
                {
                    if ((qname.LocalName == "pagenum" || qname.LocalName == "caption")
                        && textMedia != null && !string.IsNullOrEmpty(textMedia.Text))
                    {
                        m_currentTD = 0;

                        TableRowGroup rowGroup = new TableRowGroup();
                        rowGroup.Tag = node;

                        ((Table)parent).RowGroups.Add(rowGroup);
                        m_currentROWGROUP++;

                        TableRow data = new TableRow();
                        ((Table)parent).RowGroups[m_currentROWGROUP].Rows.Add(data);
                        Paragraph para = new Paragraph(new Run(textMedia.Text));
                        TableCell cell = new TableCell(para);

                        if (qname.LocalName == "caption")
                        {
                            formatCaptionCell(cell);
                        }
                        else
                        {
                            formatPageNumberAndSetId(node, para);
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
                    throw new Exception("table row not in Table ??");
                }
            }
            //assumption based on the caller: when node.ChildCount != 0 then textMedia.Text == null
            else
            {
                if (parent is Table)
                {
                    if (qname.LocalName == "pagenum" || qname.LocalName == "caption")
                    {
                        m_currentTD = 0;

                        TableRowGroup rowGroup = new TableRowGroup();
                        rowGroup.Tag = node;

                        ((Table)parent).RowGroups.Add(rowGroup);
                        m_currentROWGROUP++;

                        TableRow row = new TableRow();
                        ((Table)parent).RowGroups[m_currentROWGROUP].Rows.Add(row);
                        Paragraph para = new Paragraph();
                        TableCell cell = new TableCell(para);

                        if (qname.LocalName == "caption")
                        {
                            formatCaptionCell(cell);
                        }
                        else
                        {
                            formatPageNumberAndSetId(node, para);
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
                        rowGroup.Tag = node;

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
                        data.Tag = node;

                        ((Table)parent).RowGroups[m_currentROWGROUP].Rows.Add(data);

                        return parent;
                    }
                }
                else
                {
                    throw new Exception("table row not in Table ??");
                }
            }
        }
        private TextElement walkBookTreeAndGenerateFlowDocument_anchor_a(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia)
        {
            Hyperlink data = new Hyperlink();
            data.Tag = node;

            XmlProperty xmlProp = node.GetProperty<XmlProperty>();
            XmlAttribute attr = xmlProp.GetAttribute("href");

            if (attr != null && !String.IsNullOrEmpty(attr.Value))
            {
                data.NavigateUri = new Uri(attr.Value, UriKind.RelativeOrAbsolute);
                data.RequestNavigate += new RequestNavigateEventHandler(OnRequestNavigate);
                data.ToolTip = data.NavigateUri.ToString();
            }

            if (node.ChildCount == 0)
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
            //assumption based on the caller: when node.ChildCount != 0 then textMedia.Text == null
            else
            {
                addInline(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_annoref_noteref(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia)
        {
            Hyperlink data = new Hyperlink();
            data.Tag = node;

            data.FontSize = m_FlowDoc.FontSize / 1.2;
            data.FontWeight = FontWeights.Bold;
            data.Background = Brushes.LightSkyBlue;
            data.Foreground = Brushes.Blue;

            XmlProperty xmlProp = node.GetProperty<XmlProperty>();
            XmlAttribute attr = xmlProp.GetAttribute("idref");

            if (attr != null && !String.IsNullOrEmpty(attr.Value))
            {
                data.NavigateUri = new Uri("#" + attr.Value, UriKind.Relative);
                data.RequestNavigate += new RequestNavigateEventHandler(OnRequestNavigate);
            }
            else
            {
                //ignore: no link
            }

            if (node.ChildCount == 0)
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
            //assumption based on the caller: when node.ChildCount != 0 then textMedia.Text == null
            else
            {
                addInline(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_Span(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia, DelegateSpanInitializer initializer)
        {
            Span data = new Span();
            data.Tag = node;

            if (initializer != null)
            {
                initializer(data);
            }

            if (node.ChildCount == 0)
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
            //assumption based on the caller: when node.ChildCount != 0 then textMedia.Text == null
            else
            {
                addInline(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_Floater(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia, DelegateFloaterInitializer initializer)
        {
            Floater data = new Floater();
            data.Tag = node;

            if (initializer != null)
            {
                initializer(data);
            }

            if (node.ChildCount == 0)
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
            //assumption based on the caller: when node.ChildCount != 0 then textMedia.Text == null
            else
            {
                addInline(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_Figure(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia, DelegateFigureInitializer initializer)
        {
            Figure data = new Figure();
            data.Tag = node;

            if (initializer != null)
            {
                initializer(data);
            }

            if (node.ChildCount == 0)
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
            //assumption based on the caller: when node.ChildCount != 0 then textMedia.Text == null
            else
            {
                addInline(parent, data);
                return data;
            }
        }

        private TextElement walkBookTreeAndGenerateFlowDocument_Section(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia, DelegateSectionInitializer initializer)
        {
            Section data = new Section();
            data.Tag = node;

            if (initializer != null)
            {
                initializer(data);
            }

            if (node.ChildCount == 0)
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
            //assumption based on the caller: when node.ChildCount != 0 then textMedia.Text == null
            else
            {
                addBlock(parent, data);
                return data;
            }
        }
        private TextElement walkBookTreeAndGenerateFlowDocument_(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia)
        {
            if (qname == null)
            {
                //assumption based on the caller: node.ChildCount == 0 && textMedia != null
                if (textMedia.Text.Length == 0)
                {
                    return parent;
                }

                Run data = new Run(textMedia.Text);
                data.Tag = node;
                addInline(parent, data);

                return parent;
            }

            if (qname.NamespaceUri.Length == 0
                || qname.NamespaceUri == m_XukProject.GetPresentation(0).PropertyFactory.DefaultXmlNamespaceUri)
            {
                // node.ChildCount ?
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
                                        data.Name = attr.Value;
                                        m_idLinkTargets.Add(data.Name, data);
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
                            return walkBookTreeAndGenerateFlowDocument_Paragraph(node, parent, qname, textMedia,
                                data =>
                                {
                                    data.FontSize = m_FlowDoc.FontSize * 2;
                                    data.FontWeight = FontWeights.Heavy;
                                });
                        }
                    case "h2":
                        {
                            return walkBookTreeAndGenerateFlowDocument_Paragraph(node, parent, qname, textMedia,
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
                            return walkBookTreeAndGenerateFlowDocument_Paragraph(node, parent, qname, textMedia,
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
                            DelegateParagraphInitializer delegatePageNum =
                                    data =>
                                    {
                                        formatPageNumberAndSetId(node, data);
                                    };
                            if (parent is Table)
                            {
                                return walkBookTreeAndGenerateFlowDocument_tr_tbody_thead_tfoot_caption_pagenum(node, parent, qname, textMedia);
                            }
                            if (parent is List)
                            {
                                return walkBookTreeAndGenerateFlowDocument_li_dd_dt(node, parent, qname, textMedia);
                            }
                            else
                            {
                                return walkBookTreeAndGenerateFlowDocument_Paragraph(node, parent, qname, textMedia, delegatePageNum);
                            }
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
                            data.Tag = node;
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
                    case "sup":
                    case "sub":
                    case "bdo":
                    case "kbd":
                    case "dfn":
                    case "abbr":
                        {
                            return walkBookTreeAndGenerateFlowDocument_Span(node, parent, qname, textMedia, null);
                        }
                    case "acronym":
                        {
                            return walkBookTreeAndGenerateFlowDocument_Span(node, parent, qname, textMedia,
                                data =>
                                {
                                    XmlProperty xmlProp = node.GetProperty<XmlProperty>();
                                    XmlAttribute attr = xmlProp.GetAttribute("pronounce");

                                    if (attr != null && !String.IsNullOrEmpty(attr.Value))
                                    {
                                        data.ToolTip = "pronounce = " + attr.Value;
                                    }
                                }
                                );
                        }
                    case "q":
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

        private void formatPageNumberAndSetId(TreeNode node, Paragraph data)
        {
            data.BorderBrush = Brushes.Orange;
            data.BorderThickness = new Thickness(2.0);
            data.Padding = new Thickness(2.0);
            data.FontWeight = FontWeights.Bold;
            data.FontSize = m_FlowDoc.FontSize * 1.2;
            data.Background = Brushes.LightYellow;
            data.Foreground = Brushes.DarkOrange;

            XmlProperty xmlProp = node.GetProperty<XmlProperty>();
            XmlAttribute attr = xmlProp.GetAttribute("id");

            if (attr != null &&
                !String.IsNullOrEmpty(attr.Value))
            {
                data.Name = attr.Value;
                Pages.Add(new Page(data));
            }
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
                throw new Exception("The given parent TextElement is not valid in this context.");
            }
        }
        private void walkBookTreeAndGenerateFlowDocument(TreeNode node, TextElement parent)
        {
            TextElement parentNext = parent;

            QualifiedName qname = node.GetXmlElementQName();
            AbstractTextMedia textMedia = node.GetTextMedia();

            if (node.ChildCount == 0)
            {
                if (qname == null)
                {
                    if (textMedia == null)
                    {
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
                        throw new Exception("The given TreeNode has children, has no XmlProperty, and has no TextMedia.");
                    }
                    else //childCount != 0 && qname == null && textMedia != null
                    {
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
                        throw new Exception("The given TreeNode has children, has XmlProperty, and has TextMedia.");
                    }
                }

                for (int i = 0; i < node.ChildCount; i++)
                {
                    walkBookTreeAndGenerateFlowDocument(node.GetChild(i), parentNext);
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

        private TreeNode getTreeNodeWithXmlElementName(TreeNode node, string elemName)
        {
            QualifiedName qname = node.GetXmlElementQName();
            if (qname != null && qname.LocalName == elemName) return node;

            for (int i = 0; i < node.ChildCount; i++)
            {
                TreeNode child = getTreeNodeWithXmlElementName(node.GetChild(i), elemName);
                if (child != null)
                {
                    return child;
                }
            }
            return null;
        }

        private void OnPageSelected(object sender, SelectionChangedEventArgs e)
        {
            Page page = ListView.SelectedItem as Page;
            if (page != null)
            {
                BringIntoViewAndHighlight(page.Id);
            }
        }
        private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (e.Uri.ToString().StartsWith("#"))
            {
                string id = e.Uri.ToString().Substring(1);
                BringIntoViewAndHighlight(id);
            }
        }

        private void BringIntoViewAndHighlight(TreeNode node)
        {
            TextElement textElement = FindTextElement(node);
            if (textElement != null)
            {
                BringIntoViewAndHighlight(textElement);
            }
        }

        private TextElement FindTextElement(TreeNode node, InlineCollection ic)
        {
            foreach (Inline inline in ic)
            {
                if (inline is Figure)
                {
                    TextElement te = FindTextElement(node, (Figure)inline);
                    if (te != null) return te;
                }
                else if (inline is Floater)
                {
                    TextElement te = FindTextElement(node, (Floater)inline);
                    if (te != null) return te;
                }
                else if (inline is Run)
                {
                    TextElement te = FindTextElement(node, (Run)inline);
                    if (te != null) return te;
                }
                else if (inline is LineBreak)
                {
                    TextElement te = FindTextElement(node, (LineBreak)inline);
                    if (te != null) return te;
                }
            }

            return null;
        }

        private TextElement FindTextElement(TreeNode node, TableCellCollection tcc)
        {
            foreach (TableCell tc in tcc)
            {
                TextElement te = FindTextElement(node, tc);
                if (te != null) return te;
            }
            return null;
        }
        private TextElement FindTextElement(TreeNode node, TableRowCollection trc)
        {
            foreach (TableRow tr in trc)
            {
                TextElement te = FindTextElement(node, tr);
                if (te != null) return te;
            }
            return null;
        }
        private TextElement FindTextElement(TreeNode node, TableRowGroupCollection trgc)
        {
            foreach (TableRowGroup trg in trgc)
            {
                TextElement te = FindTextElement(node, trg);
                if (te != null) return te;
            }
            return null;
        }
        private TextElement FindTextElement(TreeNode node, ListItemCollection lic)
        {
            foreach (ListItem li in lic)
            {
                TextElement te = FindTextElement(node, li);
                if (te != null) return te;
            }
            return null;
        }

        private TextElement FindTextElement(TreeNode node, BlockCollection bc)
        {
            foreach (Block block in bc)
            {
                if (block is Section)
                {
                    TextElement te = FindTextElement(node, (Section)block);
                    if (te != null) return te;
                }
                else if (block is Paragraph)
                {
                    TextElement te = FindTextElement(node, (Paragraph)block);
                    if (te != null) return te;
                }
                else if (block is List)
                {
                    TextElement te = FindTextElement(node, (List)block);
                    if (te != null) return te;
                }
                else if (block is Table)
                {
                    TextElement te = FindTextElement(node, (Table)block);
                    if (te != null) return te;
                }
            }

            return null;
        }

        private TextElement FindTextElement(TreeNode node, TableCell tc)
        {
            if (tc.Tag == node) return tc;
            return FindTextElement(node, tc.Blocks);
        }

        private TextElement FindTextElement(TreeNode node, Run r)
        {
            if (r.Tag == node) return r;
            return null;
        }
        private TextElement FindTextElement(TreeNode node, LineBreak lb)
        {
            if (lb.Tag == node) return lb;
            return null;
        }
        private TextElement FindTextElement(TreeNode node, Floater f)
        {
            if (f.Tag == node) return f;
            return FindTextElement(node, f.Blocks);
        }
        private TextElement FindTextElement(TreeNode node, Figure f)
        {
            if (f.Tag == node) return f;
            return FindTextElement(node, f.Blocks);
        }
        private TextElement FindTextElement(TreeNode node, TableRow tr)
        {
            if (tr.Tag == node) return tr;
            return FindTextElement(node, tr.Cells);
        }
        private TextElement FindTextElement(TreeNode node, TableRowGroup trg)
        {
            if (trg.Tag == node) return trg;
            return FindTextElement(node, trg.Rows);
        }
        private TextElement FindTextElement(TreeNode node, ListItem li)
        {
            if (li.Tag == node) return li;
            return FindTextElement(node, li.Blocks);
        }
        private TextElement FindTextElement(TreeNode node)
        {
            return FindTextElement(node, m_FlowDoc.Blocks);
        }
        private TextElement FindTextElement(TreeNode node, Section section)
        {
            if (section.Tag == node) return section;
            return FindTextElement(node, section.Blocks);
        }
        private TextElement FindTextElement(TreeNode node, Paragraph para)
        {
            if (para.Tag == node) return para;
            return FindTextElement(node, para.Inlines);
        }
        private TextElement FindTextElement(TreeNode node, List list)
        {
            if (list.Tag == node) return list;
            return FindTextElement(node, list.ListItems);
        }
        private TextElement FindTextElement(TreeNode node, Table table)
        {
            if (table.Tag == node) return table;
            return FindTextElement(node, table.RowGroups);
        }


        private void BringIntoViewAndHighlight(TextElement textElement)
        {
            textElement.BringIntoView();
            if (m_lastHighlighted != null)
            {
                m_lastHighlighted.Background = m_lastHighlighted_Background;
                m_lastHighlighted.Foreground = m_lastHighlighted_Foreground;
            }
            m_lastHighlighted = textElement;

            m_lastHighlighted_Background = m_lastHighlighted.Background;
            m_lastHighlighted.Background = Brushes.Yellow;

            m_lastHighlighted_Foreground = m_lastHighlighted.Foreground;
            m_lastHighlighted.Foreground = Brushes.Red;
        }

        private void BringIntoViewAndHighlight(string id)
        {
            TextElement textElement = null;
            if (m_idLinkTargets.ContainsKey(id))
            {
                textElement = m_idLinkTargets[id];
            }
            else
            {
                Page page = GetPage(id);
                if (page != null)
                {
                    textElement = page.TextElement;
                }
            }
            if (textElement == null)
            {
                textElement = m_FlowDoc.FindName(id) as TextElement;
            }
            if (textElement != null)
            {
                BringIntoViewAndHighlight(textElement);
            }
        }
    }
}
