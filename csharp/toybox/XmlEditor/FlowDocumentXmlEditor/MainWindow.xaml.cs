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



        public MainWindow()
        {
            InitializeComponent();

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

        void XmlRichTextBox_TextInput(object sender, TextCompositionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(String.Format(
                "{0}: {1}",
                e.OriginalSource.GetType().Name,
                e.ControlText));
        }

        private T GetContaining<T>(DependencyObject elem) where T : DependencyObject
        {
            if (elem == null) return null;
            if (elem is T) return elem as T;
            return GetContaining<T>(LogicalTreeHelper.GetParent(elem));
        }

        private void InsertTable(RichTextBox rtb)
        {
            TableCell cell = InsertTable(rtb.Selection.End);
            rtb.Selection.Select(cell.ContentStart, cell.ContentEnd);
        }

        private TableCell CreateNewTableCell(string text)
        {
            TableCell newCell = new TableCell(new Paragraph(new Run(text)));
            newCell.BorderThickness = new Thickness(1);
            newCell.BorderBrush = Brushes.Black;
            return newCell;
        }

        private TableCell InsertTable(TextPointer pointer)
        {
            Table newTable = new Table();
            newTable.CellSpacing = 0;
            newTable.RowGroups.Add(new TableRowGroup());
            TableRow newRow = new TableRow();
            TableCell newCell = CreateNewTableCell("...");
            newRow.Cells.Add(newCell);
            newTable.RowGroups[0].Rows.Add(newRow);
            if (pointer.Parent is FlowDocument)
            {
                ((FlowDocument)pointer.Parent).Blocks.Add(newTable);
            }
            else
            {
                Block curBlock = GetContaining<Block>(pointer.Parent);
                curBlock.SiblingBlocks.InsertAfter(curBlock, newTable);
            }
            return newCell;
        }


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
