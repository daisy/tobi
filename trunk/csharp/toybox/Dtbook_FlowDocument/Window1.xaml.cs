using System;
using System.Collections.Generic;
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
        private string m_FilePath;
        private FlowDocument m_FlowDoc;
        private Project m_XukProject;
        private int m_currentTD;
        private Dictionary<string,TextElement> m_idLinkTargets;
        private TextElement m_lastHighlighted;

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

        private void OnOpenFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.FileName = "dtbook"; // Default file name
            dlg.DefaultExt = ".xml"; // Default file extension
            dlg.Filter = "DTBOOK documents (.xml)|*.xml;*.opf";
            bool? result = dlg.ShowDialog();
            if (result == false)
            {
                return;
            }

            FilePath = dlg.FileName;

            var uri = new Uri(FilePath);
            DTBooktoXukConversion converter = new DTBooktoXukConversion(uri);

            m_XukProject = converter.Project;

            if (m_idLinkTargets != null)
            {
                m_idLinkTargets.Clear();
            }
            m_idLinkTargets = new Dictionary<string, TextElement>();
            m_lastHighlighted = null;

            FlowDocument flowDoc = createFlowDocumentFromXuk();
            flowDoc.IsEnabled = true;
            flowDoc.IsHyphenationEnabled = false;
            flowDoc.IsOptimalParagraphEnabled = false;
            flowDoc.ColumnWidth = Double.PositiveInfinity;
            flowDoc.IsColumnWidthFlexible = false;
            flowDoc.TextAlignment = TextAlignment.Left;
            FlowDocReader.Document = flowDoc;



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
            }
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
                        parentNext = generateFlowDocument_NoChild_NoXml_Text(node, parent, textMedia);
                    }
                }
                else //childCount == 0 && qname != null
                {
                    if (textMedia == null)
                    {
                        parentNext = generateFlowDocument_NoChild_Xml_NoText(node, parent, qname);
                    }
                    else //childCount == 0 && qname != null && textMedia != null
                    {
                        parentNext = generateFlowDocument_NoChild_Xml_Text(node, parent, qname, textMedia);
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
                        parentNext = generateFlowDocument_Child_Xml_NoText(node, parent, qname);
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
            }
        }

        private TextElement generateFlowDocument_Child_Xml_NoText(TreeNode node, TextElement parent, QualifiedName qname)
        {
            if (qname.LocalName == "book"
                || qname.LocalName == "frontmatter"
                || qname.LocalName == "bodymatter"
                || qname.LocalName == "rearmatter"
                || qname.LocalName == "level"
                || qname.LocalName == "level1"
                || qname.LocalName == "level2"
                || qname.LocalName == "level3"
                || qname.LocalName == "level4"
                || qname.LocalName == "level5"
                || qname.LocalName == "level6"
                || qname.LocalName == "address"
                || qname.LocalName == "epigraph"
                || qname.LocalName == "prodnote"
                || qname.LocalName == "blockquote"
                || qname.LocalName == "note"
                || qname.LocalName == "annotation"
                || qname.LocalName == "lic"
                || qname.LocalName == "linegroup"
                || qname.LocalName == "bridgehead"
                || qname.LocalName == "code"
                || qname.LocalName == "dateline"
                || qname.LocalName == "samp"
                || qname.LocalName == "line"
                || qname.LocalName == "col"
                || qname.LocalName == "colgroup"
                || qname.LocalName == "thead"
                || qname.LocalName == "tfoot"
                || qname.LocalName == "tbody"
                || qname.LocalName == "th"
                || qname.LocalName == "poem")
            {
                Section data = new Section();
                setStyles(qname, data);

                pushIdIfNecessary(data, qname, node);

                if (parent == null)
                {
                    m_FlowDoc.Blocks.Add(data);
                }
                else
                {
                    if (parent is Section)
                    {
                        ((Section)parent).Blocks.Add(data);
                    }
                    else if (parent is TableCell)
                    {
                        ((TableCell)parent).Blocks.Add(data);
                    }
                    else if (parent is Floater)
                    {
                        ((Floater)parent).Blocks.Add(data);
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
                return data;
            }
            else if (qname.LocalName == "p"
                || qname.LocalName == "doctitle"
                || qname.LocalName == "docauthor"
                || qname.LocalName == "covertitle"
                || qname.LocalName == "div"
                || qname.LocalName == "h1"
                || qname.LocalName == "h2"
                || qname.LocalName == "h3"
                || qname.LocalName == "h4"
                || qname.LocalName == "h4"
                || qname.LocalName == "h5"
                || qname.LocalName == "h6"
                || qname.LocalName == "hd"
                || qname.LocalName == "pagenum"
                || qname.LocalName == "caption"
                || qname.LocalName == "noteref"
                || qname.LocalName == "annoref")
            {
                Paragraph data = new Paragraph();
                setStyles(qname, data);


                prependInnerLinkIfNecessary(data, qname, node);


                if (parent is List)
                {
                    ((List)parent).ListItems.Add(new ListItem(data));
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
                else if (parent is ListItem)
                {
                    ((ListItem)parent).Blocks.Add(data);
                }
                else
                {
                    throw new Exception("The given parent TextElement is not valid in this context.");
                }
                return data;
            }
            else if (qname.LocalName == "imggroup"
                || qname.LocalName == "sidebar")
            {
                Floater data = new Floater();
                setStyles(qname, data);

                if (parent is Section)
                {
                    ((Section)parent).Blocks.Add(new Paragraph(data));
                }
                else if (parent is TableCell)
                {
                    ((TableCell)parent).Blocks.Add(new Paragraph(data));
                }
                else if (parent is Paragraph)
                {
                    ((Paragraph)parent).Inlines.Add(data);
                }
                else if (parent is Span)
                {
                    ((Span)parent).Inlines.Add(data);
                }
                else
                {
                    throw new Exception("The given parent TextElement is not valid in this context.");
                }
                return data;
            }
            else if (qname.LocalName == "span"
                || qname.LocalName == "linenum"
                || qname.LocalName == "sent"
                || qname.LocalName == "w"
                || qname.LocalName == "strong"
                || qname.LocalName == "b"
                || qname.LocalName == "em"
                || qname.LocalName == "italic"
                || qname.LocalName == "i"
                || qname.LocalName == "underline"
                || qname.LocalName == "u"
                || qname.LocalName == "cite"
                || qname.LocalName == "author"
                || qname.LocalName == "byline"
                || qname.LocalName == "title"
                || qname.LocalName == "sup"
                || qname.LocalName == "sub"
                || qname.LocalName == "bdo"
                || qname.LocalName == "img"
                || qname.LocalName == "a"
                || qname.LocalName == "kbd"
                || qname.LocalName == "dfn"
                || qname.LocalName == "abbr"
                || qname.LocalName == "acronym"
                || qname.LocalName == "q")
            {
                Span data = new Span();
                setStyles(qname, data);

                if (parent is Paragraph)
                {
                    ((Paragraph)parent).Inlines.Add(data);
                }
                else if (parent is TableCell)
                {
                    ((TableCell)parent).Blocks.Add(new Paragraph(data));
                }
                else if (parent is Span)
                {
                    ((Span)parent).Inlines.Add(data);
                }
                else
                {
                    throw new Exception("The given parent TextElement is not valid in this context.");
                }
                return data;
            }
            else if (qname.LocalName == "list"
                || qname.LocalName == "dl")
            {
                List data = new List();
                setStyles(qname, data);

                if (parent is Section)
                {
                    ((Section)parent).Blocks.Add(data);
                }
                else if (parent is TableCell)
                {
                    ((TableCell)parent).Blocks.Add(data);
                }
                else if (parent is Floater)
                {
                    ((Floater)parent).Blocks.Add(data);
                }
                else if (parent is ListItem)
                {
                    ((ListItem)parent).Blocks.Add(data);
                }
                else
                {
                    throw new Exception("The given parent TextElement is not valid in this context.");
                }
                return data;
            }
            else if (qname.LocalName == "table")
            {
                m_currentTD = 0;
                Table data = new Table();
                data.RowGroups.Add(new TableRowGroup());

                setStyles(qname, data);

                if (parent is Section)
                {
                    ((Section)parent).Blocks.Add(data);
                }
                else if (parent is TableCell)
                {
                    ((TableCell)parent).Blocks.Add(data);
                }
                else if (parent is Floater)
                {
                    ((Floater)parent).Blocks.Add(data);
                }
                else if (parent is ListItem)
                {
                    ((ListItem)parent).Blocks.Add(data);
                }
                else
                {
                    throw new Exception("The given parent TextElement is not valid in this context.");
                }
                return data;
            }
            else if (qname.LocalName == "tr")
            {
                if (parent is Table)
                {
                    m_currentTD = 0;
                    TableRow data = new TableRow();
                    ((Table)parent).RowGroups[0].Rows.Add(data);

                    setStyles(qname, data);
                    return parent;
                }
            }
            else if (qname.LocalName == "td")
            {
                if (parent is Table)
                {
                    m_currentTD++;
                    TableCell data = new TableCell();
                    TableRowCollection trc = ((Table) parent).RowGroups[0].Rows;
                    trc[trc.Count-1].Cells.Add(data);

                    if (m_currentTD > ((Table) parent).Columns.Count)
                    {
                        ((Table) parent).Columns.Add(new TableColumn());
                    }

                    setStyles(qname, data);
                    return data;
                }
            }
            else if (qname.LocalName == "li"
                || qname.LocalName == "dt"
                || qname.LocalName == "dd")
            {
                ListItem data = new ListItem();
                setStyles(qname, data);

                if (parent is List)
                {
                    ((List)parent).ListItems.Add(data);
                }
                else
                {
                    throw new Exception("The given parent TextElement is not valid in this context.");
                }
                return data;
            }
            else
            {
                throw new Exception("The TreeNode's element name is not valid in this context.");
            }
            return parent;
        }


        private TextElement generateFlowDocument_NoChild_Xml_Text(TreeNode node, TextElement parent, QualifiedName qname, AbstractTextMedia textMedia)
        {
            if (!String.IsNullOrEmpty(textMedia.Text))
            {
                // This is likely a <strong>1234</strong> or similar

                if (qname.LocalName == "li"
                    || qname.LocalName == "dt"
                    || qname.LocalName == "dd"
                    || qname.LocalName == "p"
                    || qname.LocalName == "span"
                    || qname.LocalName == "linenum"
                    || qname.LocalName == "sent"
                    || qname.LocalName == "w"
                    || qname.LocalName == "cite"
                    || qname.LocalName == "author"
                    || qname.LocalName == "byline"
                    || qname.LocalName == "title"
                    || qname.LocalName == "sup"
                    || qname.LocalName == "sub"
                    || qname.LocalName == "bdo"
                    || qname.LocalName == "img"
                    || qname.LocalName == "kbd"
                    || qname.LocalName == "dfn"
                    || qname.LocalName == "abbr"
                    || qname.LocalName == "acronym"
                    || qname.LocalName == "q"
                    || qname.LocalName == "sidebar"
                    || qname.LocalName == "doctitle"
                || qname.LocalName == "docauthor"
                || qname.LocalName == "covertitle"
                || qname.LocalName == "div"
                || qname.LocalName == "h1"
                || qname.LocalName == "h2"
                || qname.LocalName == "h3"
                || qname.LocalName == "h4"
                || qname.LocalName == "h4"
                || qname.LocalName == "h5"
                || qname.LocalName == "h6"
                || qname.LocalName == "hd"
                || qname.LocalName == "pagenum"
                || qname.LocalName == "caption"
                || qname.LocalName == "prodnote"
                || qname.LocalName == "noteref"
                || qname.LocalName == "annoref"
                || qname.LocalName == "note"
                || qname.LocalName == "annotation"
                || qname.LocalName == "blockquote"
                || qname.LocalName == "lic"
                || qname.LocalName == "linegroup"
                || qname.LocalName == "bridgehead"
                || qname.LocalName == "code"
                || qname.LocalName == "dateline"
                || qname.LocalName == "samp"
                || qname.LocalName == "line"
                || qname.LocalName == "col"
                || qname.LocalName == "colgroup"
                || qname.LocalName == "thead"
                || qname.LocalName == "tfoot"
                || qname.LocalName == "tbody"
                || qname.LocalName == "th"
                || qname.LocalName == "td"
                || qname.LocalName == "poem")
                {
                    Span data = new Span();

                    pushIdIfNecessary(data, qname, node);

                    prependInnerLinkIfNecessary(data, qname, node);

                    data.Inlines.Add(new Run(textMedia.Text));

                    setStyles(qname, data);

                    //Span span = new Span(new Run(textMedia.Text));
                    if (parent is List)
                    {
                        ((List)parent).ListItems.Add(new ListItem(new Paragraph(data)));
                    }
                    else if (parent is TableCell)
                    {
                        ((TableCell)parent).Blocks.Add(new Paragraph(data));
                    }
                    else if (parent is Section)
                    {
                        ((Section)parent).Blocks.Add(new Paragraph(data));
                    }
                    else if (parent is Floater)
                    {
                        ((Floater)parent).Blocks.Add(new Paragraph(data));
                    }
                    else if (parent is ListItem)
                    {
                        ((ListItem)parent).Blocks.Add(new Paragraph(data));
                    }
                    else if (parent is Paragraph)
                    {
                        ((Paragraph)parent).Inlines.Add(data);
                    }
                    else if (parent is Span)
                    {
                        ((Span)parent).Inlines.Add(data);
                    }
                    else
                    {
                        throw new Exception("The given parent TextElement is not valid in this context.");
                    }
                }
                else if (qname.LocalName == "a")
                {
                    Hyperlink data = new Hyperlink(new Run(textMedia.Text));
                    setStyles(qname, data);

                    data.NavigateUri = new Uri("http://daisy.org");

                    if (parent is Paragraph)
                    {
                        ((Paragraph)parent).Inlines.Add(data);
                    }
                    else if (parent is Section)
                    {
                        ((Section)parent).Blocks.Add(new Paragraph(data));
                    }
                    else if (parent is TableCell)
                    {
                        ((TableCell)parent).Blocks.Add(new Paragraph(data));
                    }
                    else if (parent is Floater)
                    {
                        ((Floater)parent).Blocks.Add(new Paragraph(data));
                    }
                    else if (parent is ListItem)
                    {
                        ((ListItem)parent).Blocks.Add(new Paragraph(data));
                    }
                    else if (parent is Span)
                    {
                        ((Span)parent).Inlines.Add(data);
                    }
                    else
                    {
                        throw new Exception("The given parent TextElement is not valid in this context.");
                    }
                }
                else if (qname.LocalName == "strong"
                    || qname.LocalName == "b")
                {
                    Bold data = new Bold(new Run(textMedia.Text));
                    setStyles(qname, data);

                    if (parent is Paragraph)
                    {
                        ((Paragraph)parent).Inlines.Add(data);
                    }
                    else if (parent is TableCell)
                    {
                        ((TableCell)parent).Blocks.Add(new Paragraph(data));
                    }
                    else if (parent is Section)
                    {
                        ((Section)parent).Blocks.Add(new Paragraph(data));
                    }
                    else if (parent is Floater)
                    {
                        ((Floater)parent).Blocks.Add(new Paragraph(data));
                    }
                    else if (parent is ListItem)
                    {
                        ((ListItem)parent).Blocks.Add(new Paragraph(data));
                    }
                    else if (parent is Span)
                    {
                        ((Span)parent).Inlines.Add(data);
                    }
                    else
                    {
                        throw new Exception("The given parent TextElement is not valid in this context.");
                    }
                }
                else if (qname.LocalName == "underline"
                    || qname.LocalName == "u")
                {
                    Underline data = new Underline(new Run(textMedia.Text));
                    setStyles(qname, data);

                    if (parent is Paragraph)
                    {
                        ((Paragraph)parent).Inlines.Add(data);
                    }
                    else if (parent is TableCell)
                    {
                        ((TableCell)parent).Blocks.Add(new Paragraph(data));
                    }
                    else if (parent is Section)
                    {
                        ((Section)parent).Blocks.Add(new Paragraph(data));
                    }
                    else if (parent is Floater)
                    {
                        ((Floater)parent).Blocks.Add(new Paragraph(data));
                    }
                    else if (parent is ListItem)
                    {
                        ((ListItem)parent).Blocks.Add(new Paragraph(data));
                    }
                    else if (parent is Span)
                    {
                        ((Span)parent).Inlines.Add(data);
                    }
                    else
                    {
                        throw new Exception("The given parent TextElement is not valid in this context.");
                    }
                }
                else if (qname.LocalName == "italic"
                    || qname.LocalName == "emphasis"
                    || qname.LocalName == "em"
                    || qname.LocalName == "i")
                {
                    Italic data = new Italic(new Run(textMedia.Text));
                    setStyles(qname, data);

                    if (parent is Paragraph)
                    {
                        ((Paragraph)parent).Inlines.Add(data);
                    }
                    else if (parent is TableCell)
                    {
                        ((TableCell)parent).Blocks.Add(new Paragraph(data));
                    }
                    else if (parent is Section)
                    {
                        ((Section)parent).Blocks.Add(new Paragraph(data));
                    }
                    else if (parent is Floater)
                    {
                        ((Floater)parent).Blocks.Add(new Paragraph(data));
                    }
                    else if (parent is ListItem)
                    {
                        ((ListItem)parent).Blocks.Add(new Paragraph(data));
                    }
                    else if (parent is Span)
                    {
                        ((Span)parent).Inlines.Add(data);
                    }
                    else
                    {
                        throw new Exception("The given parent TextElement is not valid in this context.");
                    }
                }
                else
                {
                    throw new Exception("The TreeNode's element name is not valid in this context.");
                }
            }
            return parent;
        }

        private void pushIdIfNecessary(TextElement data, QualifiedName qname, TreeNode node)
        {
            if (qname.LocalName == "note"
                        || qname.LocalName == "annotation")
            {
                XmlProperty xmlProp = node.GetProperty<XmlProperty>();
                XmlAttribute idAttr = xmlProp.GetAttribute("id");

                if (idAttr != null)
                {
                    data.Name = idAttr.Value;
                    m_idLinkTargets.Add(data.Name, data);
                }
            }
        }

        private void prependInnerLinkIfNecessary(TextElement data, QualifiedName qname, TreeNode node)
        {
            if (qname.LocalName == "noteref"
            || qname.LocalName == "annoref")
            {
                XmlProperty xmlProp = node.GetProperty<XmlProperty>();
                XmlAttribute linkAttr = xmlProp.GetAttribute("idref");

                if (linkAttr != null)
                {
                    Hyperlink link = new Hyperlink(new Run("_"));
                    link.NavigateUri = new Uri("#" + linkAttr.Value, UriKind.Relative);
                    link.RequestNavigate += new RequestNavigateEventHandler(OnRequestNavigate);

                    if (data is Paragraph)
                    {
                        ((Paragraph)data).Inlines.Add(link);
                    }
                    else if (data is Span)
                    {
                        ((Span)data).Inlines.Add(link);
                    }
                }
            }
        }

        private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            string id = e.Uri.ToString().Substring(1);
            TextElement link = null;
            if (m_idLinkTargets.ContainsKey(id))
            {
                link = m_idLinkTargets[id];
            }
            else
            {
                link = m_FlowDoc.FindName(id) as TextElement;
            }
            if (link != null)
            {
                link.BringIntoView();
                if (m_lastHighlighted != null)
                {
                    m_lastHighlighted.Background = m_FlowDoc.Background;
                }
                m_lastHighlighted = link;
                m_lastHighlighted.Background = Brushes.Yellow;
            }
        }

        private TextElement generateFlowDocument_NoChild_Xml_NoText(TreeNode node, TextElement parent, QualifiedName qname)
        {
            // This is likely a <br/> or empty <p></p>

            if (qname.LocalName == "img")
            {
                XmlProperty xmlProp = node.GetProperty<XmlProperty>();
                XmlAttribute srcAttr = xmlProp.GetAttribute("src");

                if (srcAttr == null) return parent;



                Image image = new Image();


                if (srcAttr.Value.StartsWith("http://"))
                {
                    /*
                    WebClient webClient = new WebClient();
                    fullImagePath = srcAttr.Value;
                    byte[] imageContent = webClient.DownloadData(srcAttr.Value);
                    Stream stream = new MemoryStream(imageContent);
                     */
                    try
                    {
                        image.Source = new BitmapImage(new Uri(srcAttr.Value));
                    }
                    catch (Exception)
                    {
                        return parent;
                    }
                }
                else
                {
                    //http://blogs.msdn.com/yangxind/archive/2006/11/09/don-t-use-net-system-uri-unescapedatastring-in-url-decoding.aspx

                    //stream = new FileStream(fullImagePath, FileMode.Open, FileAccess.Read, FileShare.Read);


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

                /*
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
                image.Stretch = Stretch.None;

                if (parent is Paragraph)
                {
                    ((Paragraph)parent).Inlines.Add(new InlineUIContainer(image));
                }
                else if (parent is TableCell)
                {
                    ((TableCell)parent).Blocks.Add(new BlockUIContainer(image));
                }
                else if (parent is Section)
                {
                    ((Section)parent).Blocks.Add(new BlockUIContainer(image));
                }
                else if (parent is Floater)
                {
                    ((Floater)parent).Blocks.Add(new BlockUIContainer(image));
                }
                else if (parent is ListItem)
                {
                    ((ListItem)parent).Blocks.Add(new BlockUIContainer(image));
                }
                else if (parent is Span)
                {
                    ((Span)parent).Inlines.Add(new InlineUIContainer(image));
                }
            }
            else if (qname.LocalName == "td")
            {
                if (parent is Table)
                {
                    m_currentTD++;
                    TableCell data = new TableCell();
                    TableRowCollection trc = ((Table)parent).RowGroups[0].Rows;
                    trc[trc.Count - 1].Cells.Add(data);

                    if (m_currentTD > ((Table)parent).Columns.Count)
                    {
                        ((Table)parent).Columns.Add(new TableColumn());
                    }

                    setStyles(qname, data);
                    return parent;
                }
            }
            else if (qname.LocalName == "br")
            {
                LineBreak data = new LineBreak();
                setStyles(qname, data);

                if (parent is Paragraph)
                {
                    ((Paragraph)parent).Inlines.Add(data);
                }
                else if (parent is TableCell)
                {
                    ((TableCell)parent).Blocks.Add(new Paragraph(data));
                }
                else if (parent is Section)
                {
                    ((Section)parent).Blocks.Add(new Paragraph(data));
                }
                else if (parent is Floater)
                {
                    ((Floater)parent).Blocks.Add(new Paragraph(data));
                }
                else if (parent is ListItem)
                {
                    ((ListItem)parent).Blocks.Add(new Paragraph(data));
                }
                else if (parent is Span)
                {
                    ((Span)parent).Inlines.Add(data);
                }
                else
                {
                    throw new Exception("The given parent TextElement is not valid in this context.");
                }
            }
            else if (qname.LocalName == "p")
            {
                Paragraph data = new Paragraph();
                setStyles(qname, data);

                data.Inlines.Add(new LineBreak());

                if (parent is Section)
                {
                    ((Section)parent).Blocks.Add(data);
                }
                else if (parent is TableCell)
                {
                    ((TableCell)parent).Blocks.Add(data);
                }
                else if (parent is Floater)
                {
                    ((Floater)parent).Blocks.Add(data);
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
            else
            {
                throw new Exception("The TreeNode's element name is not valid in this context.");
            }
            return parent;
        }

        private TextElement generateFlowDocument_NoChild_NoXml_Text(TreeNode node, TextElement parent, AbstractTextMedia textMedia)
        {
            if (!String.IsNullOrEmpty(textMedia.Text))
            {
                // mixed-content (TreeNode siblings are probably elements with text, such as <b>1234</b>)
                Run data = new Run(textMedia.Text);

                if (parent is Paragraph)
                {
                    ((Paragraph)parent).Inlines.Add(data);
                }
                else if (parent is TableCell)
                {
                    ((TableCell)parent).Blocks.Add(new Paragraph(data));
                }
                else if (parent is Section)
                {
                    ((Section)parent).Blocks.Add(new Paragraph(data));
                }
                else if (parent is Floater)
                {
                    ((Floater)parent).Blocks.Add(new Paragraph(data));
                }
                else if (parent is ListItem)
                {
                    ((ListItem)parent).Blocks.Add(new Paragraph(data));
                }
                else if (parent is Span)
                {
                    ((Span)parent).Inlines.Add(data);
                }
                else
                {
                    throw new Exception("The given parent TextElement is not valid in this context.");
                }
            }
            return parent;
        }

        private void setStyles(QualifiedName qname, TextElement data)
        {
            if (qname.LocalName == "h1")
            {
                data.FontSize = m_FlowDoc.FontSize * 2;
                data.FontWeight = FontWeights.Heavy;
            }
            else if (qname.LocalName == "h2")
            {
                data.FontSize = m_FlowDoc.FontSize * 1.5;
                data.FontWeight = FontWeights.Heavy;
            }
            else if (qname.LocalName == "h3")
            {
                data.FontSize = m_FlowDoc.FontSize * 1.2;
                data.FontWeight = FontWeights.Heavy;
            }
            else if (qname.LocalName == "h4")
            {
                data.FontSize = m_FlowDoc.FontSize;
                data.FontWeight = FontWeights.Heavy;
            }
            else if (qname.LocalName == "table")
            {
                if (data is Table)
                {
                    ((Table)data).CellSpacing = 4.0;
                    ((Table)data).BorderBrush = Brushes.Brown;
                    ((Table)data).BorderThickness = new Thickness(1.0);
                }
            }
            else if (qname.LocalName == "td")
            {
                if (data is TableCell)
                {
                    ((TableCell)data).BorderBrush = Brushes.LightGray;
                    ((TableCell)data).BorderThickness = new Thickness(1.0);
                }
            }
            else if (qname.LocalName == "doctitle"
                || qname.LocalName == "docauthor")
            {
                data.FontSize = m_FlowDoc.FontSize * 1.2;
                data.FontWeight = FontWeights.Heavy;
                data.Foreground = Brushes.Navy;
            }
            else if (qname.LocalName == "blockquote")
            {
                if (data is Block)
                {
                    ((Block)data).BorderBrush = Brushes.Olive;
                    ((Block)data).BorderThickness = new Thickness(2.0);
                    ((Block)data).Padding = new Thickness(2.0);
                    ((Block)data).Margin = new Thickness(4.0);
                }
            }
            else if (qname.LocalName == "annotation")
            {
                if (data is Block)
                {
                    ((Block)data).BorderBrush = Brushes.LightSteelBlue;
                    ((Block)data).BorderThickness = new Thickness(2.0);
                    ((Block)data).Padding = new Thickness(2.0);
                }
                data.FontSize = m_FlowDoc.FontSize / 1.2;
            }
            else if (qname.LocalName == "annoref")
            {
                if (data is Block)
                {
                    ((Block)data).BorderBrush = Brushes.Magenta;
                    ((Block)data).BorderThickness = new Thickness(1.0);
                    ((Block)data).Padding = new Thickness(2.0);
                }
                data.FontSize = m_FlowDoc.FontSize / 1.2;
                data.FontWeight = FontWeights.Bold;

                data.Background = Brushes.LightSteelBlue;
            }
            else if (qname.LocalName == "note")
            {
                if (data is Block)
                {
                    ((Block)data).BorderBrush = Brushes.LightSkyBlue;
                    ((Block)data).BorderThickness = new Thickness(2.0);
                    ((Block)data).Padding = new Thickness(2.0);
                }
                data.FontSize = m_FlowDoc.FontSize/1.2;
            }
            else if (qname.LocalName == "noteref")
            {
                if (data is Block)
                {
                    ((Block)data).BorderBrush = Brushes.Magenta;
                    ((Block)data).BorderThickness = new Thickness(1.0);
                    ((Block)data).Padding = new Thickness(2.0);
                }
                data.FontSize = m_FlowDoc.FontSize / 1.2;
                data.FontWeight = FontWeights.Bold;

                data.Background = Brushes.LightSkyBlue;
            }
            else if (qname.LocalName == "pagenum")
            {
                if (data is Block)
                {
                    ((Block)data).BorderBrush = Brushes.Aqua;
                    ((Block)data).BorderThickness = new Thickness(2.0);
                    ((Block)data).Padding = new Thickness(2.0);
                }
                data.FontWeight = FontWeights.Bold;
                data.Background = Brushes.LightYellow;
                data.Foreground = Brushes.Orange;
            }
            else if (qname.LocalName == "frontmatter"
                || qname.LocalName == "rearmatter")
            {
                if (data is Block)
                {
                    ((Block)data).BorderBrush = Brushes.GreenYellow;
                    ((Block)data).BorderThickness = new Thickness(2.0);
                    ((Block)data).Padding = new Thickness(4.0);
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
    }
}
