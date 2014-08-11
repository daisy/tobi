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
                return;
            }

            bool done = eventt is DoneEventArgs || eventt is ReDoneEventArgs || eventt is TransactionEndedEventArgs;
            DebugFix.Assert(done == !(eventt is UnDoneEventArgs || eventt is TransactionCancelledEventArgs));

            Command cmd = eventt.Command;

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

                    TreeNode parent = ((TreeNodeInsertCommand)cmd).TreeNodeParent;
                    int pos = ((TreeNodeInsertCommand)cmd).TreeNodePos;

                    var converter = new XukToFlowDocument(this,
                        cmdTreeNode,
                        TheFlowDocument,
                        m_Logger,
                        m_EventAggregator,
                        m_ShellView,
                        m_UrakawaSession
                        );
                    converter.walkBookTreeAndGenerateFlowDocument(cmdTreeNode, parent.Tag as TextElement);
                }
                else if (cmdTreeNode.Tag is TextElement)
                {
                    if (add) done = !done;

                    var txtElem = (TextElement)cmdTreeNode.Tag;

                    var parent = txtElem.Parent;

                    if (done)
                    {
                        DebugFix.Assert(parent != null);
                    }
                    else
                    {
                        DebugFix.Assert(parent == null);

                        DebugFix.Assert(cmd.Tag != null);
                        //DebugFix.Assert(cmd.Tag is Tuple);

                        if (cmd.Tag != null)
                        {
                            if (cmd.Tag is Tuple<DependencyObject, int>)
                            {
                                Tuple<DependencyObject, int> data =
                                    (Tuple<DependencyObject, int>)cmd.Tag;
                                parent = data.Item1;
                            }
                            else if (cmd.Tag is Tuple<DependencyObject, ListItem, ListItem>)
                            {
                                Tuple<DependencyObject, ListItem, ListItem> data =
                                    (Tuple<DependencyObject, ListItem, ListItem>)cmd.Tag;
                                parent = data.Item1;
                            }
                            else if (cmd.Tag is Tuple<DependencyObject, Inline, Inline>)
                            {
                                Tuple<DependencyObject, Inline, Inline> data =
                                    (Tuple<DependencyObject, Inline, Inline>)cmd.Tag;
                                parent = data.Item1;
                            }
                            else if (cmd.Tag is Tuple<DependencyObject, Block, Block>)
                            {
                                Tuple<DependencyObject, Block, Block> data =
                                    (Tuple<DependencyObject, Block, Block>)cmd.Tag;
                                parent = data.Item1;
                            }
                            else
                            {
#if DEBUG
                                Debugger.Break();
#endif
                            }
                        }
                    }

                    if (txtElem is TableRow)
                    {
                        if (parent is TableRowGroup)
                        {
                            if (done && ((TableRowGroup)parent).Rows.Contains((TableRow)txtElem))
                            {
                                cmd.Tag = new Tuple<DependencyObject, int>(parent, ((TableRowGroup)parent).Rows.IndexOf((TableRow)txtElem));

                                ((TableRowGroup)parent).Rows.Remove((TableRow)txtElem);
                            }
                            else if (!done)
                            {
                                int index = ((Tuple<DependencyObject, int>)cmd.Tag).Item2;
                                if (index >= ((TableRowGroup)parent).Rows.Count)
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
                            if (done && ((TableRow)parent).Cells.Contains((TableCell)txtElem))
                            {
                                cmd.Tag = new Tuple<DependencyObject, int>(parent, ((TableRow)parent).Cells.IndexOf((TableCell)txtElem));

                                ((TableRow)parent).Cells.Remove((TableCell)txtElem);
                            }
                            else if (!done)
                            {
                                int index = ((Tuple<DependencyObject, int>)cmd.Tag).Item2;
                                if (index >= ((TableRow)parent).Cells.Count)
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
                            if (done && ((Table)parent).RowGroups.Contains((TableRowGroup)txtElem))
                            {
                                cmd.Tag = new Tuple<DependencyObject, int>(parent, ((Table)parent).RowGroups.IndexOf((TableRowGroup)txtElem));

                                ((Table)parent).RowGroups.Remove((TableRowGroup)txtElem);
                            }
                            else if (!done)
                            {
                                int index = ((Tuple<DependencyObject, int>)cmd.Tag).Item2;
                                if (index >= ((Table)parent).RowGroups.Count)
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
                            if (done && ((List)parent).ListItems.Contains((ListItem)txtElem))
                            {
                                if (((ListItem)txtElem).PreviousListItem != null)
                                {
                                    //listItemAfter

                                    cmd.Tag = new Tuple<DependencyObject, ListItem, ListItem>(parent, ((ListItem)txtElem).PreviousListItem, null);
                                }
                                else if (((ListItem)txtElem).NextListItem != null)
                                {
                                    //listItemBefore

                                    cmd.Tag = new Tuple<DependencyObject, ListItem, ListItem>(parent, null, ((ListItem)txtElem).NextListItem);
                                }

                                ((List)parent).ListItems.Remove((ListItem)txtElem);
                            }
                            else if (!done)
                            {
                                var listItemAfter =
                                    ((Tuple<DependencyObject, ListItem, ListItem>)cmd.Tag).Item2;

                                var listItemBefore =
                                    ((Tuple<DependencyObject, ListItem, ListItem>)cmd.Tag).Item3;

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
                            if (done && ((Paragraph)parent).Inlines.Contains((Inline)txtElem))
                            {
                                if (((Inline)txtElem).PreviousInline != null)
                                {
                                    //inlineAfter

                                    cmd.Tag = new Tuple<DependencyObject, Inline, Inline>(parent, ((Inline)txtElem).PreviousInline, null);
                                }
                                else if (((Inline)txtElem).NextInline != null)
                                {
                                    //inlineBefore

                                    cmd.Tag = new Tuple<DependencyObject, Inline, Inline>(parent, null, ((Inline)txtElem).NextInline);
                                }

                                ((Paragraph)parent).Inlines.Remove((Inline)txtElem);
                            }
                            else if (!done)
                            {
                                var inlineAfter =
                                    ((Tuple<DependencyObject, Inline, Inline>)cmd.Tag).Item2;

                                var inlineBefore =
                                    ((Tuple<DependencyObject, Inline, Inline>)cmd.Tag).Item3;

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
                            if (done && ((Span)parent).Inlines.Contains((Inline)txtElem))
                            {
                                if (((Inline)txtElem).PreviousInline != null)
                                {
                                    //inlineAfter

                                    cmd.Tag = new Tuple<DependencyObject, Inline, Inline>(parent, ((Inline)txtElem).PreviousInline, null);
                                }
                                else if (((Inline)txtElem).NextInline != null)
                                {
                                    //inlineBefore

                                    cmd.Tag = new Tuple<DependencyObject, Inline, Inline>(parent, null, ((Inline)txtElem).NextInline);
                                }

                                ((Span)parent).Inlines.Remove((Inline)txtElem);
                            }
                            else if (!done)
                            {
                                var inlineAfter =
                                    ((Tuple<DependencyObject, Inline, Inline>)cmd.Tag).Item2;

                                var inlineBefore =
                                    ((Tuple<DependencyObject, Inline, Inline>)cmd.Tag).Item3;

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
                            if (done && ((TextBlock)parent).Inlines.Contains((Inline)txtElem))
                            {
                                if (((Inline)txtElem).PreviousInline != null)
                                {
                                    //inlineAfter

                                    cmd.Tag = new Tuple<DependencyObject, Inline, Inline>(parent, ((Inline)txtElem).PreviousInline, null);
                                }
                                else if (((Inline)txtElem).NextInline != null)
                                {
                                    //inlineBefore

                                    cmd.Tag = new Tuple<DependencyObject, Inline, Inline>(parent, null, ((Inline)txtElem).NextInline);
                                }

                                ((TextBlock)parent).Inlines.Remove((Inline)txtElem);
                            }
                            else if (!done)
                            {
                                var inlineAfter =
                                    ((Tuple<DependencyObject, Inline, Inline>)cmd.Tag).Item2;

                                var inlineBefore =
                                    ((Tuple<DependencyObject, Inline, Inline>)cmd.Tag).Item3;

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
                            if (done && ((FlowDocument)parent).Blocks.Contains((Block)txtElem))
                            {
                                if (((Block)txtElem).PreviousBlock != null)
                                {
                                    //blockAfter

                                    cmd.Tag = new Tuple<DependencyObject, Block, Block>(parent, ((Block)txtElem).PreviousBlock, null);
                                }
                                else if (((Block)txtElem).NextBlock != null)
                                {
                                    //blockBefore

                                    cmd.Tag = new Tuple<DependencyObject, Block, Block>(parent, null, ((Block)txtElem).NextBlock);
                                }

                                ((FlowDocument)parent).Blocks.Remove((Block)txtElem);
                            }
                            else if (!done)
                            {
                                var blockAfter =
                                    ((Tuple<DependencyObject, Block, Block>)cmd.Tag).Item2;

                                var blockBefore =
                                    ((Tuple<DependencyObject, Block, Block>)cmd.Tag).Item3;

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
                            if (done && ((Section)parent).Blocks.Contains((Block)txtElem))
                            {
                                if (((Block)txtElem).PreviousBlock != null)
                                {
                                    //blockAfter

                                    cmd.Tag = new Tuple<DependencyObject, Block, Block>(parent, ((Block)txtElem).PreviousBlock, null);
                                }
                                else if (((Block)txtElem).NextBlock != null)
                                {
                                    //blockBefore

                                    cmd.Tag = new Tuple<DependencyObject, Block, Block>(parent, null, ((Block)txtElem).NextBlock);
                                }

                                ((Section)parent).Blocks.Remove((Block)txtElem);
                            }
                            else if (!done)
                            {
                                var blockAfter =
                                    ((Tuple<DependencyObject, Block, Block>)cmd.Tag).Item2;

                                var blockBefore =
                                    ((Tuple<DependencyObject, Block, Block>)cmd.Tag).Item3;

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
                            if (done && ((ListItem)parent).Blocks.Contains((Block)txtElem))
                            {
                                if (((Block)txtElem).PreviousBlock != null)
                                {
                                    //blockAfter

                                    cmd.Tag = new Tuple<DependencyObject, Block, Block>(parent, ((Block)txtElem).PreviousBlock, null);
                                }
                                else if (((Block)txtElem).NextBlock != null)
                                {
                                    //blockBefore

                                    cmd.Tag = new Tuple<DependencyObject, Block, Block>(parent, null, ((Block)txtElem).NextBlock);
                                }

                                ((ListItem)parent).Blocks.Remove((Block)txtElem);
                            }
                            else if (!done)
                            {
                                var blockAfter =
                                    ((Tuple<DependencyObject, Block, Block>)cmd.Tag).Item2;

                                var blockBefore =
                                    ((Tuple<DependencyObject, Block, Block>)cmd.Tag).Item3;

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
                            if (done && ((TableCell)parent).Blocks.Contains((Block)txtElem))
                            {
                                if (((Block)txtElem).PreviousBlock != null)
                                {
                                    //blockAfter

                                    cmd.Tag = new Tuple<DependencyObject, Block, Block>(parent, ((Block)txtElem).PreviousBlock, null);
                                }
                                else if (((Block)txtElem).NextBlock != null)
                                {
                                    //blockBefore

                                    cmd.Tag = new Tuple<DependencyObject, Block, Block>(parent, null, ((Block)txtElem).NextBlock);
                                }

                                ((TableCell)parent).Blocks.Remove((Block)txtElem);
                            }
                            else if (!done)
                            {
                                var blockAfter =
                                    ((Tuple<DependencyObject, Block, Block>)cmd.Tag).Item2;

                                var blockBefore =
                                    ((Tuple<DependencyObject, Block, Block>)cmd.Tag).Item3;

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
                            if (done && ((Floater)parent).Blocks.Contains((Block)txtElem))
                            {
                                if (((Block)txtElem).PreviousBlock != null)
                                {
                                    //blockAfter

                                    cmd.Tag = new Tuple<DependencyObject, Block, Block>(parent, ((Block)txtElem).PreviousBlock, null);
                                }
                                else if (((Block)txtElem).NextBlock != null)
                                {
                                    //blockBefore

                                    cmd.Tag = new Tuple<DependencyObject, Block, Block>(parent, null, ((Block)txtElem).NextBlock);
                                }

                                ((Floater)parent).Blocks.Remove((Block)txtElem);
                            }
                            else if (!done)
                            {
                                var blockAfter =
                                    ((Tuple<DependencyObject, Block, Block>)cmd.Tag).Item2;

                                var blockBefore =
                                    ((Tuple<DependencyObject, Block, Block>)cmd.Tag).Item3;

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
                            if (done && ((Figure)parent).Blocks.Contains((Block)txtElem))
                            {
                                if (((Block)txtElem).PreviousBlock != null)
                                {
                                    //blockAfter

                                    cmd.Tag = new Tuple<DependencyObject, Block, Block>(parent, ((Block)txtElem).PreviousBlock, null);
                                }
                                else if (((Block)txtElem).NextBlock != null)
                                {
                                    //blockBefore

                                    cmd.Tag = new Tuple<DependencyObject, Block, Block>(parent, null, ((Block)txtElem).NextBlock);
                                }

                                ((Figure)parent).Blocks.Remove((Block)txtElem);
                            }
                            else if (!done)
                            {
                                var blockAfter =
                                    ((Tuple<DependencyObject, Block, Block>)cmd.Tag).Item2;

                                var blockBefore =
                                    ((Tuple<DependencyObject, Block, Block>)cmd.Tag).Item3;

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
                foreach (var childCommand in ((CompositeCommand) cmd).ChildCommands.ContentsAs_Enumerable)
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
