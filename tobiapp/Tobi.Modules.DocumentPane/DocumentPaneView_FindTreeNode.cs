using System.Windows.Documents;
using urakawa.core;

namespace Tobi.Plugin.DocumentPane
{
    public partial class DocumentPaneView
    {
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
                else if (inline is InlineUIContainer)
                {
                    TextElement te = FindTextElement(node, (InlineUIContainer)inline);
                    if (te != null) return te;
                }
                else if (inline is Span)
                {
                    TextElement te = FindTextElement(node, (Span)inline);
                    if (te != null) return te;
                }
                else
                {
                    System.Diagnostics.Debug.Fail("TextElement type not matched ??");
                }
            }

            return null;
        }

        private TextElement FindTextElement(TreeNode node, Span span)
        {
            if (span.Tag == node) return span;
            return FindTextElement(node, span.Inlines);
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
                else if (block is BlockUIContainer)
                {
                    TextElement te = FindTextElement(node, (BlockUIContainer)block);
                    if (te != null) return te;
                }
                else
                {
                    System.Diagnostics.Debug.Fail("TextElement type not matched ??");
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
        private TextElement FindTextElement(TreeNode node, InlineUIContainer iuc)
        {
            if (iuc.Tag == node) return iuc;
            return null;
        }
        private TextElement FindTextElement(TreeNode node, BlockUIContainer b)
        {
            if (b.Tag == node) return b;
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
            return FindTextElement(node, TheFlowDocument.Blocks);
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
    }
}
