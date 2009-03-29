using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using urakawa;
using urakawa.property.channel;
using urakawa.core;
using FlowDocumentXmlEditor.FlowDocumentExtraction.Html;
using System.Windows;
using System.Collections;
using System.Windows.Controls;

namespace FlowDocumentXmlEditor
{
    public class UrakawaHtmlFlowDocument : FlowDocument
    {
        private TreeNode mRootTreeNode;

        public TreeNode RootTreeNode { get { return mRootTreeNode; } }
        private Channel mTextChannel;
        public Channel TextChannel { get { return mTextChannel; } }

        public UrakawaHtmlFlowDocument(TreeNode root, Channel textCh)
        {
            mRootTreeNode = root;
            mTextChannel = textCh;
            LoadFromRoot();
        }
        public static IEnumerable GetChildren(DependencyObject obj, Boolean AllChildrenInHierachy)
        {

            if (!AllChildrenInHierachy)

                return LogicalTreeHelper.GetChildren(obj);



            else
            {

                List<object> ReturnValues = new List<object>();



                RecursionReturnAllChildren(obj, ReturnValues);



                return ReturnValues;

            }

        }



        private static void RecursionReturnAllChildren(DependencyObject obj, List<object> returnValues)
        {

            foreach (object curChild in LogicalTreeHelper.GetChildren(obj))
            {

                returnValues.Add(curChild);

                if (curChild is DependencyObject)

                    RecursionReturnAllChildren((DependencyObject)curChild, returnValues);

            }

        }



        public static IEnumerable<ReturnType> GetChildren<ReturnType>(DependencyObject obj, Boolean AllChildrenInHierachy)
        {

            foreach (object child in GetChildren(obj, AllChildrenInHierachy))

                if (child is ReturnType)

                    yield return (ReturnType)child;

        }


        public static TextPointer GetPositionFromPoint(Control _this, Point searchForPoint)
        {

            foreach (Run curRun in GetChildren<Run>(_this, true))
            {

                TextPointer ptr = GetPositionFromPoint(curRun, searchForPoint);

                if (ptr != null)

                    return ptr;

            }

            return null;

        }
        public static TextPointer GetPositionFromPoint(Run _this, Point searchForPoint)
        {

            TextPointer ptrCurCharcter = _this.ContentStart.GetNextInsertionPosition(LogicalDirection.Forward);

            TextPointer ptrNextCharcter = ptrCurCharcter.GetNextInsertionPosition(LogicalDirection.Forward);

            while (ptrNextCharcter != null)
            {

                Rect charcterInsertionPointRectangle = ptrCurCharcter.GetCharacterRect(LogicalDirection.Forward);

                Rect nextCharcterInsertionPointRectangle = ptrNextCharcter.GetCharacterRect(LogicalDirection.Backward);



                if (searchForPoint.X >= charcterInsertionPointRectangle.X

                    && searchForPoint.X <= nextCharcterInsertionPointRectangle.X

                    && searchForPoint.Y >= charcterInsertionPointRectangle.Top

                    && searchForPoint.Y <= charcterInsertionPointRectangle.Bottom)
                {

                    return ptrCurCharcter;

                }



                ptrCurCharcter = ptrCurCharcter.GetNextInsertionPosition(LogicalDirection.Forward);

                ptrNextCharcter = ptrNextCharcter.GetNextInsertionPosition(LogicalDirection.Forward);

            }



            return null;

        }
        public void LoadFromRoot()
        {
            Blocks.Clear();
            if (RootTreeNode != null)
            {
                /*
                LoadFromRootAction action = new LoadFromRootAction();
                bool wasCancelled;
                ProgressWindow.ExecuteProgressAction(action, out wasCancelled);
                if (wasCancelled)
                {
                    Shutdown(-1);
                    return;
                }
                */

                HtmlBlockExtractionVisitor blockVisitor = new HtmlBlockExtractionVisitor(TextChannel);
                RootTreeNode.AcceptDepthFirst(blockVisitor);
                Blocks.AddRange(blockVisitor.ExtractedBlocks);
            }
        }
    }
}
