using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Annotations.Storage;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Practices.Composite;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Regions;
using SharpVectors.Converters;
using SharpVectors.Runtime;
using Tobi.Common;
using Tobi.Common._UnusedCode;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using Tobi.Common.UI.XAML;
using urakawa;
using urakawa.command;
using urakawa.commands;
using urakawa.core;
using urakawa.daisy;
using urakawa.daisy.import;
using urakawa.data;
using urakawa.events.undo;
using urakawa.media;
using urakawa.property.alt;
using urakawa.property.xml;
using urakawa.xuk;
using Colors = System.Windows.Media.Colors;

namespace Tobi.Plugin.DocumentPane
{
    public partial class DocumentPaneView
    {
        protected class FlowDocumentAnchorData<T1, T2, T3>
        {
            public T1 Item1 { get; private set; }
            public T2 Item2 { get; private set; }
            public T3 Item3 { get; private set; }
            public FlowDocumentAnchorData(T1 item1, T2 item2, T3 item3)
            {
                Item1 = item1;
                Item2 = item2;
                Item3 = item3;
            }
        }

        private class ObjectTagger : ObjectTag
        {
            private object m_Tag = null;

            public object Tag
            {
                set { m_Tag = value; }
                get { return m_Tag; }
            }
        }

        public static void detachFlowDocumentFragment(bool doDetach, TextElement txtElem, ObjectTag objectTagger)
        {
            DependencyObject parent = txtElem.Parent;

            if (doDetach)
            {
                DebugFix.Assert(parent != null);
            }
            else
            {
                DebugFix.Assert(parent == null);

                DebugFix.Assert(objectTagger.Tag != null);
                //DebugFix.Assert(cmd.Tag is FlowDocumentAnchorData);

                if (objectTagger.Tag != null)
                {
                    if (objectTagger.Tag is FlowDocumentAnchorData<DependencyObject, int, Object>)
                    {
                        FlowDocumentAnchorData<DependencyObject, int, Object> data =
                            (FlowDocumentAnchorData<DependencyObject, int, Object>)objectTagger.Tag;
                        parent = data.Item1;

                        bool condition = parent is Table && txtElem is TableRowGroup
                            || parent is TableRowGroup && txtElem is TableRow
                            || parent is TableRow && txtElem is TableCell;
                        DebugFix.Assert(condition);
                    }
                    else if (objectTagger.Tag is FlowDocumentAnchorData<DependencyObject, ListItem, ListItem>)
                    {
                        FlowDocumentAnchorData<DependencyObject, ListItem, ListItem> data =
                            (FlowDocumentAnchorData<DependencyObject, ListItem, ListItem>)objectTagger.Tag;
                        parent = data.Item1;

                        bool condition = parent is List && txtElem is ListItem;
                        DebugFix.Assert(condition);
                    }
                    else if (objectTagger.Tag is FlowDocumentAnchorData<DependencyObject, Inline, Inline>)
                    {
                        FlowDocumentAnchorData<DependencyObject, Inline, Inline> data =
                            (FlowDocumentAnchorData<DependencyObject, Inline, Inline>)objectTagger.Tag;
                        parent = data.Item1;

                        bool condition = (parent is Paragraph || parent is Span || parent is TextBlock) && txtElem is Inline;
                        DebugFix.Assert(condition);
                    }
                    else if (objectTagger.Tag is FlowDocumentAnchorData<DependencyObject, Block, Block>)
                    {
                        FlowDocumentAnchorData<DependencyObject, Block, Block> data =
                            (FlowDocumentAnchorData<DependencyObject, Block, Block>)objectTagger.Tag;
                        parent = data.Item1;

                        bool condition = (parent is FlowDocument || parent is Section || parent is ListItem || parent is TableCell || parent is Floater || parent is Figure) && txtElem is Block;
                        DebugFix.Assert(condition);
                    }
                    else
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }
                }
            }

            DebugFix.Assert(parent != null);

            if (txtElem is TableRow)
            {
                if (parent is TableRowGroup)
                {
                    if (doDetach && ((TableRowGroup)parent).Rows.Contains((TableRow)txtElem))
                    {
                        objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, int, Object>(parent, ((TableRowGroup)parent).Rows.IndexOf((TableRow)txtElem), null);

                        ((TableRowGroup)parent).Rows.Remove((TableRow)txtElem);
                    }
                    else if (!doDetach)
                    {
                        int index = ((FlowDocumentAnchorData<DependencyObject, int, Object>)objectTagger.Tag).Item2;
                        if (index < 0 || index >= ((TableRowGroup)parent).Rows.Count)
                        {
                            ((TableRowGroup)parent).Rows.Add((TableRow)txtElem);
                        }
                        else
                        {
                            ((TableRowGroup)parent).Rows.Insert(index, (TableRow)txtElem);
                        }
                    }
                    else
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }
                }
                else
                {
#if DEBUG
                    Debugger.Break();
#endif
                }
            }
            else if (txtElem is TableCell)
            {
                if (parent is TableRow)
                {
                    if (doDetach && ((TableRow)parent).Cells.Contains((TableCell)txtElem))
                    {
                        objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, int, Object>(parent, ((TableRow)parent).Cells.IndexOf((TableCell)txtElem), null);

                        ((TableRow)parent).Cells.Remove((TableCell)txtElem);
                    }
                    else if (!doDetach)
                    {
                        int index = ((FlowDocumentAnchorData<DependencyObject, int, Object>)objectTagger.Tag).Item2;
                        if (index < 0 || index >= ((TableRow)parent).Cells.Count)
                        {
                            ((TableRow)parent).Cells.Add((TableCell)txtElem);
                        }
                        else
                        {
                            ((TableRow)parent).Cells.Insert(index, (TableCell)txtElem);
                        }
                    }
                    else
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }
                }
                else
                {
#if DEBUG
                    Debugger.Break();
#endif
                }
            }
            else if (txtElem is TableRowGroup)
            {
                if (parent is Table)
                {
                    if (doDetach && ((Table)parent).RowGroups.Contains((TableRowGroup)txtElem))
                    {
                        objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, int, Object>(parent, ((Table)parent).RowGroups.IndexOf((TableRowGroup)txtElem), null);

                        ((Table)parent).RowGroups.Remove((TableRowGroup)txtElem);
                    }
                    else if (!doDetach)
                    {
                        int index = ((FlowDocumentAnchorData<DependencyObject, int, Object>)objectTagger.Tag).Item2;
                        if (index < 0 || index >= ((Table)parent).RowGroups.Count)
                        {
                            ((Table)parent).RowGroups.Add((TableRowGroup)txtElem);
                        }
                        else
                        {
                            ((Table)parent).RowGroups.Insert(index, (TableRowGroup)txtElem);
                        }
                    }
                    else
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }
                }
                else
                {
#if DEBUG
                    Debugger.Break();
#endif
                }
            }
            else if (txtElem is ListItem)
            {
                if (parent is List)
                {
                    if (doDetach && ((List)parent).ListItems.Contains((ListItem)txtElem))
                    {
                        if (((ListItem)txtElem).PreviousListItem != null)
                        {
                            //listItemAfter

                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, ListItem, ListItem>(parent, ((ListItem)txtElem).PreviousListItem, null);
                        }
                        else if (((ListItem)txtElem).NextListItem != null)
                        {
                            //listItemBefore

                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, ListItem, ListItem>(parent, null, ((ListItem)txtElem).NextListItem);
                        }
                        else
                        {
                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, ListItem, ListItem>(parent, null, null);
                        }

                        ((List)parent).ListItems.Remove((ListItem)txtElem);
                    }
                    else if (!doDetach)
                    {
                        var listItemAfter =
                            ((FlowDocumentAnchorData<DependencyObject, ListItem, ListItem>)objectTagger.Tag).Item2;

                        var listItemBefore =
                            ((FlowDocumentAnchorData<DependencyObject, ListItem, ListItem>)objectTagger.Tag).Item3;

                        if (listItemAfter != null)
                        {
                            ((List)parent).ListItems.InsertAfter(listItemAfter, (ListItem)txtElem);
                        }
                        else if (listItemBefore != null)
                        {
                            ((List)parent).ListItems.InsertBefore(listItemBefore, (ListItem)txtElem);
                        }
                        else
                        {
                            ((List)parent).ListItems.Add((ListItem)txtElem);
                        }
                    }
                    else
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }
                }
                else
                {
#if DEBUG
                    Debugger.Break();
#endif
                }
            }
            else if (txtElem is Inline)
            {
                if (parent is Paragraph)
                {
                    if (doDetach && ((Paragraph)parent).Inlines.Contains((Inline)txtElem))
                    {
                        if (((Inline)txtElem).PreviousInline != null)
                        {
                            //inlineAfter

                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Inline, Inline>(parent, ((Inline)txtElem).PreviousInline, null);
                        }
                        else if (((Inline)txtElem).NextInline != null)
                        {
                            //inlineBefore

                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Inline, Inline>(parent, null, ((Inline)txtElem).NextInline);
                        }
                        else
                        {
                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Inline, Inline>(parent, null, null);
                        }

                        ((Paragraph)parent).Inlines.Remove((Inline)txtElem);
                    }
                    else if (!doDetach)
                    {
                        var inlineAfter =
                            ((FlowDocumentAnchorData<DependencyObject, Inline, Inline>)objectTagger.Tag).Item2;

                        var inlineBefore =
                            ((FlowDocumentAnchorData<DependencyObject, Inline, Inline>)objectTagger.Tag).Item3;

                        if (inlineAfter != null)
                        {
                            ((Paragraph)parent).Inlines.InsertAfter(inlineAfter, (Inline)txtElem);
                        }
                        else if (inlineBefore != null)
                        {
                            ((Paragraph)parent).Inlines.InsertBefore(inlineBefore, (Inline)txtElem);
                        }
                        else
                        {
                            ((Paragraph)parent).Inlines.Add((Inline)txtElem);
                        }
                    }
                    else
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }
                }
                else if (parent is Span)
                {
                    if (doDetach && ((Span)parent).Inlines.Contains((Inline)txtElem))
                    {
                        if (((Inline)txtElem).PreviousInline != null)
                        {
                            //inlineAfter

                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Inline, Inline>(parent, ((Inline)txtElem).PreviousInline, null);
                        }
                        else if (((Inline)txtElem).NextInline != null)
                        {
                            //inlineBefore

                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Inline, Inline>(parent, null, ((Inline)txtElem).NextInline);
                        }
                        else
                        {
                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Inline, Inline>(parent, null, null);
                        }

                        ((Span)parent).Inlines.Remove((Inline)txtElem);
                    }
                    else if (!doDetach)
                    {
                        var inlineAfter =
                            ((FlowDocumentAnchorData<DependencyObject, Inline, Inline>)objectTagger.Tag).Item2;

                        var inlineBefore =
                            ((FlowDocumentAnchorData<DependencyObject, Inline, Inline>)objectTagger.Tag).Item3;

                        if (inlineAfter != null)
                        {
                            ((Span)parent).Inlines.InsertAfter(inlineAfter, (Inline)txtElem);
                        }
                        else if (inlineBefore != null)
                        {
                            ((Span)parent).Inlines.InsertBefore(inlineBefore, (Inline)txtElem);
                        }
                        else
                        {
                            ((Span)parent).Inlines.Add((Inline)txtElem);
                        }
                    }
                    else
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }
                }
                else if (parent is TextBlock)
                {
                    if (doDetach && ((TextBlock)parent).Inlines.Contains((Inline)txtElem))
                    {
                        if (((Inline)txtElem).PreviousInline != null)
                        {
                            //inlineAfter

                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Inline, Inline>(parent, ((Inline)txtElem).PreviousInline, null);
                        }
                        else if (((Inline)txtElem).NextInline != null)
                        {
                            //inlineBefore

                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Inline, Inline>(parent, null, ((Inline)txtElem).NextInline);
                        }
                        else
                        {
                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Inline, Inline>(parent, null, null);
                        }

                        ((TextBlock)parent).Inlines.Remove((Inline)txtElem);
                    }
                    else if (!doDetach)
                    {
                        var inlineAfter =
                            ((FlowDocumentAnchorData<DependencyObject, Inline, Inline>)objectTagger.Tag).Item2;

                        var inlineBefore =
                            ((FlowDocumentAnchorData<DependencyObject, Inline, Inline>)objectTagger.Tag).Item3;

                        if (inlineAfter != null)
                        {
                            ((TextBlock)parent).Inlines.InsertAfter(inlineAfter, (Inline)txtElem);
                        }
                        else if (inlineBefore != null)
                        {
                            ((TextBlock)parent).Inlines.InsertBefore(inlineBefore, (Inline)txtElem);
                        }
                        else
                        {
                            ((TextBlock)parent).Inlines.Add((Inline)txtElem);
                        }
                    }
                    else
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }
                }
                else
                {
#if DEBUG
                    Debugger.Break();
#endif
                }
            }
            else if (txtElem is Block)
            {
                if (parent is FlowDocument)
                {
                    if (doDetach && ((FlowDocument)parent).Blocks.Contains((Block)txtElem))
                    {
                        if (((Block)txtElem).PreviousBlock != null)
                        {
                            //blockAfter

                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Block, Block>(parent, ((Block)txtElem).PreviousBlock, null);
                        }
                        else if (((Block)txtElem).NextBlock != null)
                        {
                            //blockBefore

                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Block, Block>(parent, null, ((Block)txtElem).NextBlock);
                        }
                        else
                        {
                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Block, Block>(parent, null, null);
                        }

                        ((FlowDocument)parent).Blocks.Remove((Block)txtElem);
                    }
                    else if (!doDetach)
                    {
                        var blockAfter =
                            ((FlowDocumentAnchorData<DependencyObject, Block, Block>)objectTagger.Tag).Item2;

                        var blockBefore =
                            ((FlowDocumentAnchorData<DependencyObject, Block, Block>)objectTagger.Tag).Item3;

                        if (blockAfter != null)
                        {
                            ((FlowDocument)parent).Blocks.InsertAfter(blockAfter, (Block)txtElem);
                        }
                        else if (blockBefore != null)
                        {
                            ((FlowDocument)parent).Blocks.InsertBefore(blockBefore, (Block)txtElem);
                        }
                        else
                        {
                            ((FlowDocument)parent).Blocks.Add((Block)txtElem);
                        }
                    }
                    else
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }
                }
                else if (parent is Section)
                {
                    if (doDetach && ((Section)parent).Blocks.Contains((Block)txtElem))
                    {
                        if (((Block)txtElem).PreviousBlock != null)
                        {
                            //blockAfter

                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Block, Block>(parent, ((Block)txtElem).PreviousBlock, null);
                        }
                        else if (((Block)txtElem).NextBlock != null)
                        {
                            //blockBefore

                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Block, Block>(parent, null, ((Block)txtElem).NextBlock);
                        }
                        else
                        {
                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Block, Block>(parent, null, null);
                        }

                        ((Section)parent).Blocks.Remove((Block)txtElem);
                    }
                    else if (!doDetach)
                    {
                        var blockAfter =
                            ((FlowDocumentAnchorData<DependencyObject, Block, Block>)objectTagger.Tag).Item2;

                        var blockBefore =
                            ((FlowDocumentAnchorData<DependencyObject, Block, Block>)objectTagger.Tag).Item3;

                        if (blockAfter != null)
                        {
                            ((Section)parent).Blocks.InsertAfter(blockAfter, (Block)txtElem);
                        }
                        else if (blockBefore != null)
                        {
                            ((Section)parent).Blocks.InsertBefore(blockBefore, (Block)txtElem);
                        }
                        else
                        {
                            ((Section)parent).Blocks.Add((Block)txtElem);
                        }
                    }
                    else
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }
                }
                else if (parent is ListItem)
                {
                    if (doDetach && ((ListItem)parent).Blocks.Contains((Block)txtElem))
                    {
                        if (((Block)txtElem).PreviousBlock != null)
                        {
                            //blockAfter

                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Block, Block>(parent, ((Block)txtElem).PreviousBlock, null);
                        }
                        else if (((Block)txtElem).NextBlock != null)
                        {
                            //blockBefore

                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Block, Block>(parent, null, ((Block)txtElem).NextBlock);
                        }
                        else
                        {
                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Block, Block>(parent, null, null);
                        }

                        ((ListItem)parent).Blocks.Remove((Block)txtElem);
                    }
                    else if (!doDetach)
                    {
                        var blockAfter =
                            ((FlowDocumentAnchorData<DependencyObject, Block, Block>)objectTagger.Tag).Item2;

                        var blockBefore =
                            ((FlowDocumentAnchorData<DependencyObject, Block, Block>)objectTagger.Tag).Item3;

                        if (blockAfter != null)
                        {
                            ((ListItem)parent).Blocks.InsertAfter(blockAfter, (Block)txtElem);
                        }
                        else if (blockBefore != null)
                        {
                            ((ListItem)parent).Blocks.InsertBefore(blockBefore, (Block)txtElem);
                        }
                        else
                        {
                            ((ListItem)parent).Blocks.Add((Block)txtElem);
                        }
                    }
                    else
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }
                }
                else if (parent is TableCell)
                {
                    if (doDetach && ((TableCell)parent).Blocks.Contains((Block)txtElem))
                    {
                        if (((Block)txtElem).PreviousBlock != null)
                        {
                            //blockAfter

                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Block, Block>(parent, ((Block)txtElem).PreviousBlock, null);
                        }
                        else if (((Block)txtElem).NextBlock != null)
                        {
                            //blockBefore

                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Block, Block>(parent, null, ((Block)txtElem).NextBlock);
                        }
                        else
                        {
                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Block, Block>(parent, null, null);
                        }

                        ((TableCell)parent).Blocks.Remove((Block)txtElem);
                    }
                    else if (!doDetach)
                    {
                        var blockAfter =
                            ((FlowDocumentAnchorData<DependencyObject, Block, Block>)objectTagger.Tag).Item2;

                        var blockBefore =
                            ((FlowDocumentAnchorData<DependencyObject, Block, Block>)objectTagger.Tag).Item3;

                        if (blockAfter != null)
                        {
                            ((TableCell)parent).Blocks.InsertAfter(blockAfter, (Block)txtElem);
                        }
                        else if (blockBefore != null)
                        {
                            ((TableCell)parent).Blocks.InsertBefore(blockBefore, (Block)txtElem);
                        }
                        else
                        {
                            ((TableCell)parent).Blocks.Add((Block)txtElem);
                        }
                    }
                    else
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }
                }
                else if (parent is Floater)
                {
                    if (doDetach && ((Floater)parent).Blocks.Contains((Block)txtElem))
                    {
                        if (((Block)txtElem).PreviousBlock != null)
                        {
                            //blockAfter

                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Block, Block>(parent, ((Block)txtElem).PreviousBlock, null);
                        }
                        else if (((Block)txtElem).NextBlock != null)
                        {
                            //blockBefore

                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Block, Block>(parent, null, ((Block)txtElem).NextBlock);
                        }
                        else
                        {
                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Block, Block>(parent, null, null);
                        }

                        ((Floater)parent).Blocks.Remove((Block)txtElem);
                    }
                    else if (!doDetach)
                    {
                        var blockAfter =
                            ((FlowDocumentAnchorData<DependencyObject, Block, Block>)objectTagger.Tag).Item2;

                        var blockBefore =
                            ((FlowDocumentAnchorData<DependencyObject, Block, Block>)objectTagger.Tag).Item3;

                        if (blockAfter != null)
                        {
                            ((Floater)parent).Blocks.InsertAfter(blockAfter, (Block)txtElem);
                        }
                        else if (blockBefore != null)
                        {
                            ((Floater)parent).Blocks.InsertBefore(blockBefore, (Block)txtElem);
                        }
                        else
                        {
                            ((Floater)parent).Blocks.Add((Block)txtElem);
                        }
                    }
                    else
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }
                }
                else if (parent is Figure)
                {
                    if (doDetach && ((Figure)parent).Blocks.Contains((Block)txtElem))
                    {
                        if (((Block)txtElem).PreviousBlock != null)
                        {
                            //blockAfter

                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Block, Block>(parent, ((Block)txtElem).PreviousBlock, null);
                        }
                        else if (((Block)txtElem).NextBlock != null)
                        {
                            //blockBefore

                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Block, Block>(parent, null, ((Block)txtElem).NextBlock);
                        }
                        else
                        {
                            objectTagger.Tag = new FlowDocumentAnchorData<DependencyObject, Block, Block>(parent, null, null);
                        }

                        ((Figure)parent).Blocks.Remove((Block)txtElem);
                    }
                    else if (!doDetach)
                    {
                        var blockAfter =
                            ((FlowDocumentAnchorData<DependencyObject, Block, Block>)objectTagger.Tag).Item2;

                        var blockBefore =
                            ((FlowDocumentAnchorData<DependencyObject, Block, Block>)objectTagger.Tag).Item3;

                        if (blockAfter != null)
                        {
                            ((Figure)parent).Blocks.InsertAfter(blockAfter, (Block)txtElem);
                        }
                        else if (blockBefore != null)
                        {
                            ((Figure)parent).Blocks.InsertBefore(blockBefore, (Block)txtElem);
                        }
                        else
                        {
                            ((Figure)parent).Blocks.Add((Block)txtElem);
                        }
                    }
                    else
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }
                }
                else
                {
#if DEBUG
                    Debugger.Break();
#endif
                }
            }
            else
            {
#if DEBUG
                Debugger.Break();
#endif
            }
        }

        private void updateTextStruct(Command cmd, bool done)
        {
            if (cmd is TextNodeStructureEditCommand)
            {
                TreeNode cmdTreeNode = ((TextNodeStructureEditCommand)cmd).TreeNode;

                bool remove = cmd is TreeNodeRemoveCommand;
                bool add = cmd is TreeNodeInsertCommand;

                // First time insert (no FlowDocument cross-referencing yet)
                if (add && cmdTreeNode.Tag == null)
                {
                    // at undo (remove) time, there should already be FlowDocument tag!!
                    DebugFix.Assert(done);
                    DebugFix.Assert(cmdTreeNode.Parent != null);

                    TreeNode parent = ((TreeNodeInsertCommand)cmd).TreeNodeParent;
                    DebugFix.Assert(cmdTreeNode.Parent == parent);

                    int pos = ((TreeNodeInsertCommand)cmd).TreeNodePos;
                    DebugFix.Assert(pos >= 0 && pos < parent.Children.Count);

                    var parentTextElem = parent.Tag as TextElement;
                    DebugFix.Assert(parentTextElem != null);
                    if (parentTextElem == null) return;

                    int toRemove = parent.Children.Count - 1 - pos;
                    var fakeCmds = new List<ObjectTagger>(toRemove);

                    // Temporarily delete next siblings
                    for (int i = parent.Children.Count - 1; i >= 0; i--)
                    //for (int i = 0; i < parent.Children.Count; i++)
                    {
                        TreeNode childTreeNode = parent.Children.Get(i);

                        if (i > pos && childTreeNode.Tag is TextElement)
                        {
                            var fakeCmd = new ObjectTagger();
                            fakeCmds.Add(fakeCmd);
                            DocumentPaneView.detachFlowDocumentFragment(true, (TextElement)childTreeNode.Tag, fakeCmd);

                            DebugFix.Assert(fakeCmd.Tag != null);
                        }
                    }

                    DebugFix.Assert(fakeCmds.Count == toRemove);

                    var converter = new XukToFlowDocument(this,
                        cmdTreeNode,
                        TheFlowDocument,
                        m_Logger,
                        m_EventAggregator,
                        m_ShellView,
                        m_UrakawaSession
                        );
                    TextElement newTextElem = converter.walkBookTreeAndGenerateFlowDocument(cmdTreeNode, parentTextElem);

                    DebugFix.Assert(newTextElem != null);
                    if (newTextElem != null)
                    {
                        DebugFix.Assert(newTextElem.Parent != null); // already attached inside FlowDocument

                        if (cmdTreeNode.Tag == newTextElem)
                        {
                            DebugFix.Assert(newTextElem.Tag == cmdTreeNode);
                        }
                        if (newTextElem.Tag == cmdTreeNode)
                        {
                            DebugFix.Assert(cmdTreeNode.Tag == newTextElem);
                        }
                    }

                    int j = fakeCmds.Count - 1;

                    // Restore temporarily-deleted next siblings
                    for (int i = 0; i < parent.Children.Count; i++)
                    {
                        TreeNode childTreeNode = parent.Children.Get(i);

                        if (i > pos && childTreeNode.Tag is TextElement)
                        {
                            ObjectTagger fakeCmd = fakeCmds[j--];
                            // METHOD_1: shift all anchors to account for the newly-inserted TextElement (PROBLEM: newTextElem is not necessarily the equivalent of cmdTreeNode! (intermediary-inserted TextElements))
                            // METHOD_2: reset anchors in FlowDocumentAnchorDatas => indicates "append" instruction

                            var txtElem = (TextElement)childTreeNode.Tag;
                            DependencyObject parentTxtElem = null;

                            if (fakeCmd.Tag is FlowDocumentAnchorData<DependencyObject, int, Object>)
                            {
                                FlowDocumentAnchorData<DependencyObject, int, Object> data =
                                    (FlowDocumentAnchorData<DependencyObject, int, Object>)fakeCmd.Tag;
                                parentTxtElem = data.Item1;

                                bool condition = parentTxtElem is Table && txtElem is TableRowGroup
                                    || parentTxtElem is TableRowGroup && txtElem is TableRow
                                    || parentTxtElem is TableRow && txtElem is TableCell;
                                DebugFix.Assert(condition);

                                fakeCmd.Tag = new FlowDocumentAnchorData<DependencyObject, int, Object>(parentTxtElem, -1, null); // reset
                            }
                            else if (fakeCmd.Tag is FlowDocumentAnchorData<DependencyObject, ListItem, ListItem>)
                            {
                                FlowDocumentAnchorData<DependencyObject, ListItem, ListItem> data =
                                    (FlowDocumentAnchorData<DependencyObject, ListItem, ListItem>)fakeCmd.Tag;
                                parentTxtElem = data.Item1;

                                bool condition = parentTxtElem is List && txtElem is ListItem;
                                DebugFix.Assert(condition);

                                fakeCmd.Tag = new FlowDocumentAnchorData<DependencyObject, ListItem, ListItem>(parentTxtElem, null, null); // reset
                            }
                            else if (fakeCmd.Tag is FlowDocumentAnchorData<DependencyObject, Inline, Inline>)
                            {
                                FlowDocumentAnchorData<DependencyObject, Inline, Inline> data =
                                    (FlowDocumentAnchorData<DependencyObject, Inline, Inline>)fakeCmd.Tag;
                                parentTxtElem = data.Item1;

                                bool condition = (parentTxtElem is Paragraph || parentTxtElem is Span || parentTxtElem is TextBlock) && txtElem is Inline;
                                DebugFix.Assert(condition);

                                fakeCmd.Tag = new FlowDocumentAnchorData<DependencyObject, Inline, Inline>(parentTxtElem, null, null); // reset
                            }
                            else if (fakeCmd.Tag is FlowDocumentAnchorData<DependencyObject, Block, Block>)
                            {
                                FlowDocumentAnchorData<DependencyObject, Block, Block> data =
                                    (FlowDocumentAnchorData<DependencyObject, Block, Block>)fakeCmd.Tag;
                                parentTxtElem = data.Item1;

                                bool condition = (parentTxtElem is FlowDocument || parentTxtElem is Section || parentTxtElem is ListItem || parentTxtElem is TableCell || parentTxtElem is Floater || parentTxtElem is Figure) && txtElem is Block;
                                DebugFix.Assert(condition);

                                fakeCmd.Tag = new FlowDocumentAnchorData<DependencyObject, Block, Block>(parentTxtElem, null, null); // reset
                            }
                            else
                            {
#if DEBUG
                                Debugger.Break();
#endif
                            }

                            DocumentPaneView.detachFlowDocumentFragment(false, txtElem, fakeCmd);
                        }
                    }
                }
                else if (cmdTreeNode.Tag is TextElement)
                {
                    DocumentPaneView.detachFlowDocumentFragment(add ? !done : done, (TextElement)cmdTreeNode.Tag, cmd);
                }
            }
        }

        private void OnUndoRedoManagerChanged(object sender, UndoRedoManagerEventArgs eventt)
        {
            if (!Dispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action<object, UndoRedoManagerEventArgs>)OnUndoRedoManagerChanged, sender, eventt);
                return;
            }

            //m_Logger.Log("DocumentPaneViewModel.OnUndoRedoManagerChanged", Category.Debug, Priority.Medium);

            if (!(eventt is DoneEventArgs
                           || eventt is UnDoneEventArgs
                           || eventt is ReDoneEventArgs
                           || eventt is TransactionEndedEventArgs
                           || eventt is TransactionCancelledEventArgs
                           ))
            {
                Debug.Fail("This should never happen !!");
                return;
            }

            if (m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.IsTransactionActive)
            {
                DebugFix.Assert(eventt is DoneEventArgs || eventt is TransactionEndedEventArgs);
                //m_Logger.Log("DocumentPaneViewModel.OnUndoRedoManagerChanged (exit: ongoing TRANSACTION...)", Category.Debug, Priority.Medium);
                //return;
            }

            bool done = eventt is DoneEventArgs || eventt is ReDoneEventArgs || eventt is TransactionEndedEventArgs;
            DebugFix.Assert(done == !(eventt is UnDoneEventArgs || eventt is TransactionCancelledEventArgs));

            Command cmd = eventt.Command;

            if (cmd is CompositeCommand)
            {
                var compo = (CompositeCommand)cmd;
                bool allStructEdits = true;
                foreach (Command command in compo.ChildCommands.ContentsAs_Enumerable)
                {
                    if (!(command is TextNodeStructureEditCommand))
                    {
                        allStructEdits = false;
                        break;
                    }
                }
                //if (allStructEdits && compo.ChildCommands.Count > 0)
                //{
                //    cmd = compo.ChildCommands.Get(compo.ChildCommands.Count - 1); //last
                //}
                if (allStructEdits)
                {
                    if (!done)
                    {
                        for (var i = compo.ChildCommands.Count - 1; i >= 0; i--)
                        {
                            Command command = compo.ChildCommands.Get(i);

                            updateTextStruct(command, done);

                            var remove = command as TreeNodeRemoveCommand;
                            var add = command as TreeNodeInsertCommand;

                            if (remove != null)
                            {
                                m_UrakawaSession.PerformTreeNodeSelection(remove.TreeNode);
                            }
                            else if (add != null)
                            {
                                //m_UrakawaSession.PerformTreeNodeSelection(add.TreeNodeParent);
                            }

                            findAndUpdateTreeNodeAudioTextStatus(command, done);
                        }
                    }
                    else if (eventt is ReDoneEventArgs)
                    {
                        //foreach (Command command in compo.ChildCommands.ContentsAs_Enumerable)
                        for (var i = 0; i < compo.ChildCommands.Count; i++)
                        {
                            Command command = compo.ChildCommands.Get(i);

                            updateTextStruct(command, done);

                            var remove = command as TreeNodeRemoveCommand;
                            var add = command as TreeNodeInsertCommand;

                            if (remove != null)
                            {
                                //m_UrakawaSession.PerformTreeNodeSelection(remove.TreeNodeParent);
                            }
                            else if (add != null && i == (compo.ChildCommands.Count - 1))
                            {
                                m_UrakawaSession.PerformTreeNodeSelection(add.TreeNode);
                            }

                            findAndUpdateTreeNodeAudioTextStatus(command, done);
                        }
                    }

                    return;
                }
            }

            updateTextStruct(cmd, done);

            if (eventt is ReDoneEventArgs || eventt is UnDoneEventArgs)
            {
                var removeCmd = cmd as TreeNodeRemoveCommand;
                var addCmd = cmd as TreeNodeInsertCommand;

                if (removeCmd != null)
                {
                    if (!done)
                    {
                        m_UrakawaSession.PerformTreeNodeSelection(removeCmd.TreeNode);
                    }
                    else
                    {
                        // TODO previous next text
                        // var previous = node.GetPreviousSiblingWithText();
                        // var next = node.GetNextSiblingWithText();
                        
                        m_UrakawaSession.PerformTreeNodeSelection(removeCmd.TreeNodeParent);
                    }
                }
                else if (addCmd != null)
                {
                    if (done)
                    {
                        m_UrakawaSession.PerformTreeNodeSelection(addCmd.TreeNode);
                    }
                    else
                    {
                        // TODO previous next text
                        // var previous = node.GetPreviousSiblingWithText();
                        // var next = node.GetNextSiblingWithText();

                        m_UrakawaSession.PerformTreeNodeSelection(addCmd.TreeNodeParent);
                    }
                }
            }

            findAndUpdateTreeNodeAudioTextStatus(cmd, done);
        }

        private void findAndUpdateTreeNodeAudioTextStatus(Command cmd, bool done)
        {
            if (cmd is TreeNodeChangeTextCommand)
            {
                var command = (TreeNodeChangeTextCommand)cmd;
                findAndUpdateTreeNodeText(command, done);
            }
            else if (cmd is ManagedAudioMediaInsertDataCommand)
            {
                var command = (ManagedAudioMediaInsertDataCommand)cmd;
                findAndUpdateTreeNodeAudioStatus(command.TreeNode);
            }
            else if (cmd is TreeNodeSetManagedAudioMediaCommand)
            {
                var command = (TreeNodeSetManagedAudioMediaCommand)cmd;
                findAndUpdateTreeNodeAudioStatus(command.TreeNode);
            }
            else if (cmd is TreeNodeAudioStreamDeleteCommand)
            {
                var command = (TreeNodeAudioStreamDeleteCommand)cmd;
                findAndUpdateTreeNodeAudioStatus(command.SelectionData.m_TreeNode);
            }
            else if (cmd is TreeNodeRemoveCommand)
            {
            }
            else if (cmd is CompositeCommand)
            {
                foreach (var childCommand in ((CompositeCommand)cmd).ChildCommands.ContentsAs_Enumerable)
                {
                    findAndUpdateTreeNodeAudioTextStatus(childCommand, done);
                }
            }
        }

        private void findAndUpdateTreeNodeText(TreeNodeChangeTextCommand cmd, bool done)
        {
            TreeNode node = cmd.TreeNode;

            TextElement text = null;
            if (m_lastHighlighted != null && m_lastHighlighted.Tag == node)
            {
                text = m_lastHighlighted;
            }
            if (m_lastHighlightedSub != null && m_lastHighlightedSub.Tag == node)
            {
                text = m_lastHighlightedSub;
            }
            if (text == null)
            {
                text = FindTextElement(node);
            }
            if (text != null)
            {
                DebugFix.Assert(node == text.Tag);
                if (node == text.Tag)
                {
                    //var media = node.GetTextMedia();
                    //DebugFix.Assert(media != null);
                    //DebugFix.Assert(!string.IsNullOrEmpty(media.Text));

                    //Run run = VisualLogicalTreeWalkHelper.FindObjectInLogicalTreeWithMatchingType<Run>(text, null);
                    Run run = null;
                    foreach (var run_ in VisualLogicalTreeWalkHelper.FindObjectsInLogicalTreeWithMatchingType<Run>(text, null))
                    {
                        //if (run != null)
                        //{
                        //    run = null;
                        //    Debug.Fail("WTF ?");
                        //    break;
                        //}
                        run = run_;
                        break;
                    }
                    if (run != null)
                    {
                        //ThreadPool.QueueUserWorkItem(obj =>
                        Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(() =>
                        {
                            run.Text = done ? cmd.NewText : cmd.OldText;
                        }));
                    }
                    else
                    {
#if DEBUG // Normally, the TextBlock's Run is picked-up with the code above, no need for the code below
                        Debugger.Break();
#endif
                        TextBlock tb = null;
                        foreach (var tb_ in VisualLogicalTreeWalkHelper.FindObjectsInLogicalTreeWithMatchingType<TextBlock>(text, null))
                        {
                            if (tb != null)
                            {
                                tb = null;
                                Debug.Fail("WTF ?");
                                break;
                            }
                            tb = tb_;
                        }
                        if (tb != null)
                        {
                            //ThreadPool.QueueUserWorkItem(obj =>
                            Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(() =>
                            {
                                tb.Text = done ? cmd.NewText : cmd.OldText;
                            }));
                        }
                    }


                    Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                    TreeNode selected = selection.Item2 ?? selection.Item1;
                    if (selected != node)
                    {
                        m_UrakawaSession.PerformTreeNodeSelection(node);
                    }
                    else
                    {
                        updateSimpleTextView(selected);
                    }
                }
            }
        }

        private void findAndUpdateTreeNodeAudioStatus(TreeNode node)
        {
            foreach (var childTreeNode in node.Children.ContentsAs_Enumerable)
            {
                findAndUpdateTreeNodeAudioStatus(childTreeNode);
            }

            if (!node.NeedsAudio())
            {
                return;
            }

            TextElement text = null;
            if (m_lastHighlighted != null && m_lastHighlighted.Tag == node)
            {
                text = m_lastHighlighted;
            }
            if (m_lastHighlightedSub != null && m_lastHighlightedSub.Tag == node)
            {
                text = m_lastHighlightedSub;
            }
            if (text == null)
            {
                text = FindTextElement(node);
            }
            if (text != null)
            {
                DebugFix.Assert(node == text.Tag);
                if (node == text.Tag)
                {
                    XukToFlowDocument.SetForegroundColorAndCursorBasedOnTreeNodeTag(this, text, false);

                    //DebugFix.Assert(noAudio == !node.HasOrInheritsAudio());

                    //if (m_lastHighlighted == text)
                    //{
                    //    m_lastHighlighted_Foreground = text.Foreground;
                    //}
                    //if (m_lastHighlightedSub == text)
                    //{
                    //    m_lastHighlightedSub_Foreground = text.Foreground;
                    //}
                }
            }
            ////ThreadPool.QueueUserWorkItem(obj =>
            //Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(() =>
            //{

            //}));
        }
    }
}
