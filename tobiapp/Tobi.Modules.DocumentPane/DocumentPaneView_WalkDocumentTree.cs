using System;
using System.Diagnostics;
using System.Windows.Documents;
using urakawa.core;

namespace Tobi.Plugin.DocumentPane
{
    public partial class DocumentPaneView
    {
        private void WalkDocumentTree(TextElement textElement, Action<TextElement> action)
        {
            action.Invoke(textElement);

            if (textElement is ListItem) // TEXT_ELEMENT
            {
                var blocks = ((ListItem)textElement).Blocks;
                foreach (var block in blocks)
                {
                    WalkDocumentTree(block, action);
                }
            }
            else if (textElement is TableRowGroup) // TEXT_ELEMENT
            {
                var rows = ((TableRowGroup)textElement).Rows;
                foreach (var row in rows)
                {
                    WalkDocumentTree(row, action);
                }
            }
            else if (textElement is TableRow) // TEXT_ELEMENT
            {
                var cells = ((TableRow)textElement).Cells;
                foreach (var cell in cells)
                {
                    WalkDocumentTree(cell, action);
                }
            }
            else if (textElement is TableCell) // TEXT_ELEMENT
            {
                var blocks = ((TableCell)textElement).Blocks;
                foreach (var block in blocks)
                {
                    WalkDocumentTree(block, action);
                }
            }
            else if (textElement is Table) // BLOCK
            {
                var rowGs = ((Table)textElement).RowGroups;
                foreach (var rowG in rowGs)
                {
                    WalkDocumentTree(rowG, action);
                }
            }
            else if (textElement is Paragraph) // BLOCK
            {
                var inlines = ((Paragraph)textElement).Inlines;
                foreach (var inline in inlines)
                {
                    WalkDocumentTree(inline, action);
                }
            }
            else if (textElement is Section) // BLOCK
            {
                var blocks = ((Section)textElement).Blocks;
                foreach (var block in blocks)
                {
                    WalkDocumentTree(block, action);
                }
            }
            else if (textElement is List) // BLOCK
            {
                var lis = ((List)textElement).ListItems;
                foreach (var li in lis)
                {
                    WalkDocumentTree(li, action);
                }
            }
            else if (textElement is BlockUIContainer) // BLOCK
            {
                //((BlockUIContainer)textElement).Child => UIElement
            }
            else if (textElement is Span) // INLINE
            {
                var inlines = ((Span)textElement).Inlines;
                foreach (var inline in inlines)
                {
                    WalkDocumentTree(inline, action);
                }
            }
            else if (textElement is Floater) // INLINE
            {
                var blocks = ((Floater)textElement).Blocks;
                foreach (var block in blocks)
                {
                    WalkDocumentTree(block, action);
                }
            }
            else if (textElement is Figure) // INLINE
            {
                var blocks = ((Figure)textElement).Blocks;
                foreach (var block in blocks)
                {
                    WalkDocumentTree(block, action);
                }
            }
            else if (textElement is Inline) // includes InlineUIContainer, LineBreak and Run
            {
                //
            }
            else
            {
#if DEBUG
                Debugger.Break();
#endif
            }
        }


        private void WalkDocumentTree(Action<TextElement> action)
        {
            WalkDocumentTree(action, TheFlowDocument.Blocks);
        }
        private void WalkDocumentTree(Action<TextElement> action, InlineCollection ic)
        {
            foreach (Inline inline in ic)
            {
                if (inline is Figure)
                {
                    WalkDocumentTree(action, (Figure)inline);
                }
                else if (inline is Floater)
                {
                    WalkDocumentTree(action, (Floater)inline);

                }
                else if (inline is Run)
                {
                    WalkDocumentTree(action, (Run)inline);

                }
                else if (inline is LineBreak)
                {
                    WalkDocumentTree(action, (LineBreak)inline);

                }
                else if (inline is InlineUIContainer)
                {
                    WalkDocumentTree(action, (InlineUIContainer)inline);

                }
                else if (inline is Span)
                {
                    WalkDocumentTree(action, (Span)inline);

                }
                else
                {
                    System.Diagnostics.Debug.Fail("TextElement type not matched ??");
                }
            }
        }

        private void WalkDocumentTree(Action<TextElement> action, TableCellCollection tcc)
        {
            foreach (TableCell tc in tcc)
            {
                WalkDocumentTree(action, tc);

            }
            
        }
        private void WalkDocumentTree(Action<TextElement> action, TableRowCollection trc)
        {
            foreach (TableRow tr in trc)
            {
                WalkDocumentTree(action, tr);

            }
            
        }
        private void WalkDocumentTree(Action<TextElement> action, TableRowGroupCollection trgc)
        {
            foreach (TableRowGroup trg in trgc)
            {
                WalkDocumentTree(action, trg);

            }
            
        }
        private void WalkDocumentTree(Action<TextElement> action, ListItemCollection lic)
        {
            foreach (ListItem li in lic)
            {
                WalkDocumentTree(action, li);

            }
            
        }

        private void WalkDocumentTree(Action<TextElement> action, BlockCollection bc)
        {
            foreach (Block block in bc)
            {
                if (block is Section)
                {
                    WalkDocumentTree(action, (Section)block);

                }
                else if (block is Paragraph)
                {
                    WalkDocumentTree(action, (Paragraph)block);

                }
                else if (block is List)
                {
                    WalkDocumentTree(action, (List)block);

                }
                else if (block is Table)
                {
                    WalkDocumentTree(action, (Table)block);

                }
                else if (block is BlockUIContainer)
                {
                    WalkDocumentTree(action, (BlockUIContainer)block);

                }
                else
                {
                    System.Diagnostics.Debug.Fail("TextElement type not matched ??");
                }
            }

            
        }

        private void WalkDocumentTree(Action<TextElement> action, Span span)
        {
            action.Invoke(span);
            WalkDocumentTree(action, span.Inlines);
        }

        private void WalkDocumentTree(Action<TextElement> action, TableCell tc)
        {
            action.Invoke(tc);
            WalkDocumentTree(action, tc.Blocks);
        }

        private void WalkDocumentTree(Action<TextElement> action, Run r)
        {
            action.Invoke(r);
            
        }
        private void WalkDocumentTree(Action<TextElement> action, LineBreak lb)
        {
            action.Invoke(lb);
            
        }
        private void WalkDocumentTree(Action<TextElement> action, InlineUIContainer iuc)
        {
            action.Invoke(iuc);
            
        }
        private void WalkDocumentTree(Action<TextElement> action, BlockUIContainer b)
        {
            action.Invoke(b);
            
        }
        private void WalkDocumentTree(Action<TextElement> action, Floater f)
        {
            action.Invoke(f);
            WalkDocumentTree(action, f.Blocks);
        }
        private void WalkDocumentTree(Action<TextElement> action, Figure f)
        {
            action.Invoke(f);
            WalkDocumentTree(action, f.Blocks);
        }
        private void WalkDocumentTree(Action<TextElement> action, TableRow tr)
        {
            action.Invoke(tr);
            WalkDocumentTree(action, tr.Cells);
        }
        private void WalkDocumentTree(Action<TextElement> action, TableRowGroup trg)
        {
            action.Invoke(trg);
            WalkDocumentTree(action, trg.Rows);
        }
        private void WalkDocumentTree(Action<TextElement> action, ListItem li)
        {
            action.Invoke(li);
            WalkDocumentTree(action, li.Blocks);
        }
        private void WalkDocumentTree(Action<TextElement> action, Section section)
        {
            action.Invoke(section);
            WalkDocumentTree(action, section.Blocks);
        }
        private void WalkDocumentTree(Action<TextElement> action, Paragraph para)
        {
            action.Invoke(para);
            WalkDocumentTree(action, para.Inlines);
        }
        private void WalkDocumentTree(Action<TextElement> action, List list)
        {
            action.Invoke(list);
            WalkDocumentTree(action, list.ListItems);
        }
        private void WalkDocumentTree(Action<TextElement> action, Table table)
        {
            action.Invoke(table);
            WalkDocumentTree(action, table.RowGroups);
        }
    }
}
