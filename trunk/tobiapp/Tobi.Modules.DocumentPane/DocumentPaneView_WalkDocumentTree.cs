using System;
using System.Windows.Documents;
using urakawa.core;

namespace Tobi.Plugin.DocumentPane
{
    public partial class DocumentPaneView
    {
        private TextElement WalkDocumentTree(Action<TextElement> action)
        {
            return WalkDocumentTree(action, TheFlowDocument.Blocks);
        }
        private TextElement WalkDocumentTree(Action<TextElement> action, InlineCollection ic)
        {
            foreach (Inline inline in ic)
            {
                if (inline is Figure)
                {
                    TextElement te = WalkDocumentTree(action, (Figure)inline);
                    if (te != null) return te;
                }
                else if (inline is Floater)
                {
                    TextElement te = WalkDocumentTree(action, (Floater)inline);
                    if (te != null) return te;
                }
                else if (inline is Run)
                {
                    TextElement te = WalkDocumentTree(action, (Run)inline);
                    if (te != null) return te;
                }
                else if (inline is LineBreak)
                {
                    TextElement te = WalkDocumentTree(action, (LineBreak)inline);
                    if (te != null) return te;
                }
                else if (inline is InlineUIContainer)
                {
                    TextElement te = WalkDocumentTree(action, (InlineUIContainer)inline);
                    if (te != null) return te;
                }
                else if (inline is Span)
                {
                    TextElement te = WalkDocumentTree(action, (Span)inline);
                    if (te != null) return te;
                }
                else
                {
                    System.Diagnostics.Debug.Fail("TextElement type not matched ??");
                }
            }

            return null;
        }

        private TextElement WalkDocumentTree(Action<TextElement> action, Span span)
        {
            //if (span.Tag == node) return span;
            action.Invoke(span);
            return WalkDocumentTree(action, span.Inlines);
        }

        private TextElement WalkDocumentTree(Action<TextElement> action, TableCellCollection tcc)
        {
            foreach (TableCell tc in tcc)
            {
                TextElement te = WalkDocumentTree(action, tc);
                if (te != null) return te;
            }
            return null;
        }
        private TextElement WalkDocumentTree(Action<TextElement> action, TableRowCollection trc)
        {
            foreach (TableRow tr in trc)
            {
                TextElement te = WalkDocumentTree(action, tr);
                if (te != null) return te;
            }
            return null;
        }
        private TextElement WalkDocumentTree(Action<TextElement> action, TableRowGroupCollection trgc)
        {
            foreach (TableRowGroup trg in trgc)
            {
                TextElement te = WalkDocumentTree(action, trg);
                if (te != null) return te;
            }
            return null;
        }
        private TextElement WalkDocumentTree(Action<TextElement> action, ListItemCollection lic)
        {
            foreach (ListItem li in lic)
            {
                TextElement te = WalkDocumentTree(action, li);
                if (te != null) return te;
            }
            return null;
        }

        private TextElement WalkDocumentTree(Action<TextElement> action, BlockCollection bc)
        {
            foreach (Block block in bc)
            {
                if (block is Section)
                {
                    TextElement te = WalkDocumentTree(action, (Section)block);
                    if (te != null) return te;
                }
                else if (block is Paragraph)
                {
                    TextElement te = WalkDocumentTree(action, (Paragraph)block);
                    if (te != null) return te;
                }
                else if (block is List)
                {
                    TextElement te = WalkDocumentTree(action, (List)block);
                    if (te != null) return te;
                }
                else if (block is Table)
                {
                    TextElement te = WalkDocumentTree(action, (Table)block);
                    if (te != null) return te;
                }
                else if (block is BlockUIContainer)
                {
                    TextElement te = WalkDocumentTree(action, (BlockUIContainer)block);
                    if (te != null) return te;
                }
                else
                {
                    System.Diagnostics.Debug.Fail("TextElement type not matched ??");
                }
            }

            return null;
        }

        private TextElement WalkDocumentTree(Action<TextElement> action, TableCell tc)
        {
            //if (tc.Tag == node) return tc;
            action.Invoke(tc);
            return WalkDocumentTree(action, tc.Blocks);
        }

        private TextElement WalkDocumentTree(Action<TextElement> action, Run r)
        {
            //if (r.Tag == node) return r;
            action.Invoke(r);
            return null;
        }
        private TextElement WalkDocumentTree(Action<TextElement> action, LineBreak lb)
        {
            //if (lb.Tag == node) return lb;
            action.Invoke(lb);
            return null;
        }
        private TextElement WalkDocumentTree(Action<TextElement> action, InlineUIContainer iuc)
        {
            //if (iuc.Tag == node) return iuc;
            action.Invoke(iuc);
            return null;
        }
        private TextElement WalkDocumentTree(Action<TextElement> action, BlockUIContainer b)
        {
            //if (b.Tag == node) return b;
            action.Invoke(b);
            return null;
        }
        private TextElement WalkDocumentTree(Action<TextElement> action, Floater f)
        {
            //if (f.Tag == node) return f;
            action.Invoke(f);
            return WalkDocumentTree(action, f.Blocks);
        }
        private TextElement WalkDocumentTree(Action<TextElement> action, Figure f)
        {
            //if (f.Tag == node) return f;
            action.Invoke(f);
            return WalkDocumentTree(action, f.Blocks);
        }
        private TextElement WalkDocumentTree(Action<TextElement> action, TableRow tr)
        {
            //if (tr.Tag == node) return tr;
            action.Invoke(tr);
            return WalkDocumentTree(action, tr.Cells);
        }
        private TextElement WalkDocumentTree(Action<TextElement> action, TableRowGroup trg)
        {
            //if (trg.Tag == node) return trg;
            action.Invoke(trg);
            return WalkDocumentTree(action, trg.Rows);
        }
        private TextElement WalkDocumentTree(Action<TextElement> action, ListItem li)
        {
            //if (li.Tag == node) return li;
            action.Invoke(li);
            return WalkDocumentTree(action, li.Blocks);
        }
        private TextElement WalkDocumentTree(Action<TextElement> action, Section section)
        {
            //if (section.Tag == node) return section;
            action.Invoke(section);
            return WalkDocumentTree(action, section.Blocks);
        }
        private TextElement WalkDocumentTree(Action<TextElement> action, Paragraph para)
        {
            //if (para.Tag == node) return para;
            action.Invoke(para);
            return WalkDocumentTree(action, para.Inlines);
        }
        private TextElement WalkDocumentTree(Action<TextElement> action, List list)
        {
            //if (list.Tag == node) return list;
            action.Invoke(list);
            return WalkDocumentTree(action, list.ListItems);
        }
        private TextElement WalkDocumentTree(Action<TextElement> action, Table table)
        {
            //if (table.Tag == node) return table;
            action.Invoke(table);
            return WalkDocumentTree(action, table.RowGroups);
        }
    }
}
