using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;
using FlowDocumentXmlEditor.FlowDocumentExtraction;
using urakawa.xuk;
using urakawa;

namespace FlowDocumentXmlEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public RoutedUICommand InsertTableCommand = new RoutedUICommand();
        public RoutedUICommand InsertColumnCommand = new RoutedUICommand();
        public RoutedUICommand InsertRowCommand = new RoutedUICommand();
        public RoutedUICommand InsertParagraphCommand = new RoutedUICommand();

        public FlowDocument EditedDocument
        {
            get
            {
                return mFlowViewer.Document;
            }
            set
            {
                mFlowViewer.Document = value;
            }
        }

        //public event EventHandler Closed

        private void Window_Closed(object sender, EventArgs e)
        {

            if (ProjectUri != null && ProjectUri.IsFile)
            {
                string strPath = ProjectUri.LocalPath;
                int indexOfLastSlash = strPath.LastIndexOf('\\');
                string strFile = strPath.Substring(indexOfLastSlash);
                Uri projectUri = new Uri(strPath.Remove(indexOfLastSlash) + strFile + ".out.xuk");
                //FileStream fs = new FileStream(projectUri.LocalPath, FileMode.Create, FileAccess.Write);

                SaveXukAction action = new SaveXukAction(projectUri, Project);
                bool wasCancelled;
                ProgressWindow.ExecuteProgressAction(action, out wasCancelled);
                if (wasCancelled)
                {
                    return;
                }
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            foreach (TextMediaBindableRun curRun in UrakawaHtmlFlowDocument.GetChildren<TextMediaBindableRun>(mFlowViewer.Document, true))

                addMouseButtonEventHandler(curRun);

        }
        private TextMediaTextBox mLastInlineBoxed = null;

        public void addMouseButtonEventHandler(TextMediaBindableRun run)
        {
            if (run != null)
                run.MouseDown += new MouseButtonEventHandler(curRun_MouseDown);
        }

        private void DoMouseDownStuff(TextMediaBindableRun run, InlineCollection inlines)
        {

            TextMediaTextBox tb = new TextMediaTextBox(run.TextMedia);
            InlineUIContainer newInlineBoxed = new InlineUIContainer(tb);

            inlines.InsertAfter(run, newInlineBoxed);
            inlines.Remove(run);

            run.MouseDown -= new MouseButtonEventHandler(curRun_MouseDown);
            run.InvalidateBinding();

            if (mLastInlineBoxed != null)
            {
                TextMediaBindableRun newRun = mLastInlineBoxed.CloseSelf(true);

                addMouseButtonEventHandler(newRun);


            }

            mLastInlineBoxed = tb;
        }
        public void curRun_MouseDown(object sender, MouseButtonEventArgs e)
        {

            Point mousePosition = Mouse.GetPosition(mFlowViewer);

            TextMediaBindableRun senderRun = (TextMediaBindableRun)sender;

            if (senderRun.Parent is Span)
            {
                Span o = senderRun.Parent as Span;

                DoMouseDownStuff(senderRun, o.Inlines);

            }
            else
                if (senderRun.Parent is Paragraph)
                {

                    Paragraph o = senderRun.Parent as Paragraph;

                    DoMouseDownStuff(senderRun, o.Inlines);
                } else if (senderRun.Parent is TextBlock)
            {

                TextBlock o = senderRun.Parent as TextBlock;

                DoMouseDownStuff(senderRun, o.Inlines);
            }
            else
            {
                Debug.Print("Should never happen");
            }

            TextPointer ptr = UrakawaHtmlFlowDocument.GetPositionFromPoint(senderRun, mousePosition);

            if (ptr != null)
            {

                string textAfterCursor = ptr.GetTextInRun(LogicalDirection.Forward);

                string textBeforeCursor = ptr.GetTextInRun(LogicalDirection.Backward);



                string[] textsAfterCursor = textAfterCursor.Split('.', ' ');

                string[] textsBeforeCursor = textBeforeCursor.Split('.', ' ');



                string currentWord = textsBeforeCursor[textsBeforeCursor.Length - 1] + textsAfterCursor[0];

            }

        }


        private void FlowDocument_MouseDown(object sender, MouseButtonEventArgs e)
        {
            return;

            Point mousePosition = Mouse.GetPosition(mFlowViewer);

            TextPointer ptr = UrakawaHtmlFlowDocument.GetPositionFromPoint(mFlowViewer, mousePosition);

            if (ptr != null)
            {

                string textAfterCursor = ptr.GetTextInRun(LogicalDirection.Forward);

                string textBeforeCursor = ptr.GetTextInRun(LogicalDirection.Backward);
                string[] textsAfterCursor = textAfterCursor.Split('.', ' ');

                string[] textsBeforeCursor = textBeforeCursor.Split('.', ' ');



                string currentWord = textsBeforeCursor[textsBeforeCursor.Length - 1] + textsAfterCursor[0];


            }

        }
        private Uri mProjectUri = null;

        public Uri ProjectUri
        {
            get { return mProjectUri; }
            set { mProjectUri = value; }
        }

        private Project mProject = null;

        public Project Project
        {
            get { return mProject; }
            set { mProject = value; }
        }

        public MainWindow(Project project, Uri projectUri)
        {
            InitializeComponent();

            Project = project;
            ProjectUri = projectUri;

            //InsertTableCommand.Text = "Insert Table";
            //mInsertTableMenuItem.Command = InsertTableCommand;
            //this.CommandBindings.Add(new CommandBinding(
            //    InsertTableCommand, InsertTableCommand_Executed, InsertTableCommand_CanExecute));

            //InsertColumnCommand.Text = "Insert Table Column";
            //mInsertColumnMenuItem.Command = InsertColumnCommand;
            //this.CommandBindings.Add(new CommandBinding(
            //    InsertColumnCommand, InsertColumnCommand_Executed, InsertColumnCommand_CanExecute));

            //InsertRowCommand.Text = "Insert Table Row";
            //mInsertRowMenuItem.Command = InsertRowCommand;
            //this.CommandBindings.Add(new CommandBinding(
            //    InsertRowCommand, InsertRowCommand_Executed, InsertRowCommand_CanExecute));

            //mXmlRichTextBox.TextInput += new TextCompositionEventHandler(XmlRichTextBox_TextInput);
        }

        //private static T GetContaining<T>(DependencyObject elem) where T : DependencyObject
        //{
        //    if (elem == null) return null;
        //    if (elem is T) return elem as T;
        //    return GetContaining<T>(LogicalTreeHelper.GetParent(elem));
        //}

        //private void InsertTable(RichTextBox rtb)
        //{
        //    TableCell cell = InsertTable(rtb.Selection.End);
        //    rtb.Selection.Select(cell.ContentStart, cell.ContentEnd);
        //}

        //private static TableCell CreateNewTableCell(string text)
        //{
        //    TableCell newCell = new TableCell(new Paragraph(new Run(text)));
        //    newCell.BorderThickness = new Thickness(1);
        //    newCell.BorderBrush = Brushes.Black;
        //    return newCell;
        //}

        //private TableCell InsertTable(TextPointer pointer)
        //{
        //    Table newTable = new Table();
        //    newTable.CellSpacing = 0;
        //    newTable.RowGroups.Add(new TableRowGroup());
        //    TableRow newRow = new TableRow();
        //    TableCell newCell = CreateNewTableCell("...");
        //    newRow.Cells.Add(newCell);
        //    newTable.RowGroups[0].Rows.Add(newRow);
        //    if (pointer.Parent is FlowDocument)
        //    {
        //        ((FlowDocument)pointer.Parent).Blocks.Add(newTable);
        //    }
        //    else
        //    {
        //        Block curBlock = GetContaining<Block>(pointer.Parent);
        //        curBlock.SiblingBlocks.InsertAfter(curBlock, newTable);
        //    }
        //    return newCell;
        //}


        //private void mInsertTableButton_Click(object sender, RoutedEventArgs e)
        //{
        //    TableCell curCell = GetContaining<TableCell>(mXmlRichTextBox.Selection.Start.Parent);
        //    if (curCell != null)
        //    {
        //        InsertColumn(curCell);
        //    }
        //    else
        //    {
        //        InsertTable(mXmlRichTextBox.Selection.End);
        //    }
        //}

        //private void InsertColumn(RichTextBox rtb)
        //{
        //    InsertColumn(GetContaining<TableCell>(rtb.Selection.End.Parent) as TableCell);
        //}

        //private void InsertColumn(TableCell curCell)
        //{
        //    TableRow curRow = curCell.Parent as TableRow;
        //    int colIndex = curRow.Cells.IndexOf(curCell);
        //    Table curTable = GetContaining<Table>(curRow.Parent);
        //    foreach (TableRow tr in GetContaining<TableRowGroup>(curCell.Parent).Rows)
        //    {
        //        tr.Cells.Insert(colIndex, CreateNewTableCell(""));
        //    }
        //}

        //private void InsertTableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        //{
        //    e.CanExecute = false;
        //    RichTextBox rtb = e.Source as RichTextBox;
        //    if (rtb != null)
        //    {
        //        e.CanExecute = (GetContaining<Table>(rtb.Selection.End.Parent)==null);
        //    }
        //}

        //private void InsertTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        //{
        //    InsertTable(e.Source as RichTextBox);
        //}

        //private void InsertColumnCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        //{
        //    e.CanExecute = false;
        //    RichTextBox rtb = e.Source as RichTextBox;
        //    if (rtb != null)
        //    {
        //        e.CanExecute = (GetContaining<TableCell>(rtb.Selection.End.Parent) != null);
        //    }
        //}

        //private void InsertColumnCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        //{
        //    InsertColumn(e.Source as RichTextBox);
        //}

        //private void InsertRow(RichTextBox rtb)
        //{
        //}

        //private void InsertParagraph(RichTextBox rtb)
        //{
        //    TextPointer ins = rtb.Selection.End.InsertParagraphBreak();
        //    rtb.Selection.Select(ins, ins);
        //}

        //private void InsertRowCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        //{
        //    e.CanExecute = false;
        //    RichTextBox rtb = e.Source as RichTextBox;
        //    if (rtb != null)
        //    {
        //        e.CanExecute = (GetContaining<TableCell>(rtb.Selection.End.Parent) != null);
        //    }
        //}

        //private void InsertRowCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        //{
        //    InsertRow(e.Source as RichTextBox);
        //}

        //private void InsertParagraphCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        //{
        //    e.CanExecute = (e.Source is RichTextBox);
        //}

        //private void InsertParagraphCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        //{
        //    InsertParagraph(e.Source as RichTextBox);
        //}
    }
}
