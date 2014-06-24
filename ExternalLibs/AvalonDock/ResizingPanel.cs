//Copyright (c) 2007, Adolfo Marinucci
//All rights reserved.

//Redistribution and use in source and binary forms, with or without modification, 
//are permitted provided that the following conditions are met:
//
//* Redistributions of source code must retain the above copyright notice, 
//  this list of conditions and the following disclaimer.
//* Redistributions in binary form must reproduce the above copyright notice, 
//  this list of conditions and the following disclaimer in the documentation 
//  and/or other materials provided with the distribution.
//* Neither the name of Adolfo Marinucci nor the names of its contributors may 
//  be used to endorse or promote products derived from this software without 
//  specific prior written permission.
//
//THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" 
//AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED 
//WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. 
//IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, 
//INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, 
//PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) 
//HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, 
//OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, 
//EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. 

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Markup;
using System.Diagnostics;

namespace AvalonDock
{
    public class ResizingPanel : System.Windows.Controls.Panel, IDockableControl
    {
        /// <summary>
        /// Gets or sets the orientation of the panel
        /// </summary>
        /// <remarks>If horizontal oriented children are positioned from left to right and width of each child is computed according to <see cref="ResizingWidth"/> attached property value. When vertical oriented children are arranged from top to bottom, according to <see cref="ResizingHeight"/> of each child.</remarks>
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// Give access to Orientation attached property
        /// </summary>
        /// <remarks>If horizontal oriented children are positioned from left to right and width of each child is computed according to <see cref="ResizingWidth"/> attached property value. When vertical oriented children are arranged from top to bottom, according to <see cref="ResizingHeight"/> of each child.</remarks>
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(Orientation), typeof(ResizingPanel), new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsMeasure,  OnOrientationChanged));

        static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ResizingPanel)d).splitterListIsDirty = true;
        }

        public static double GetResizeWidth(DependencyObject obj)
        {
            return (double)obj.GetValue(ResizeWidthProperty);
        }

        public static void SetResizeWidth(DependencyObject obj, double value)
        {
            obj.SetValue(ResizeWidthProperty, value);
        }

        public static readonly DependencyProperty ResizeWidthProperty =
            DependencyProperty.RegisterAttached("ResizeWidth", 
            typeof(double), 
            typeof(ResizingPanel),
            new FrameworkPropertyMetadata(double.PositiveInfinity,
            FrameworkPropertyMetadataOptions.AffectsMeasure |
            FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static double GetResizeHeight(DependencyObject obj)
        {
            return (double)obj.GetValue(ResizeHeightProperty);
        }

        public static void SetResizeHeight(DependencyObject obj, double value)
        {
            obj.SetValue(ResizeHeightProperty, value);
        }

        public static readonly DependencyProperty ResizeHeightProperty =
            DependencyProperty.RegisterAttached("ResizeHeight",
            typeof(double),
            typeof(ResizingPanel),
            new FrameworkPropertyMetadata(double.PositiveInfinity,
            FrameworkPropertyMetadataOptions.AffectsMeasure |
            FrameworkPropertyMetadataOptions.AffectsParentMeasure));


        public static readonly DependencyProperty HiddenProperty =
            DependencyProperty.RegisterAttached("Hidden",
            typeof(bool),
            typeof(ResizingPanel),
            new FrameworkPropertyMetadata(false,
            FrameworkPropertyMetadataOptions.AffectsMeasure |
            FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static double GetHidden(DependencyObject obj)
        {
            return (double)obj.GetValue(HiddenProperty);
        }

        public static void SetHidden(DependencyObject obj, double value)
        {
            obj.SetValue(HiddenProperty, value);
        }

        /// <summary>
        /// Checks if next child starting from a given index is visible.
        /// </summary>
        /// <param name="iChild">Starting child index.</param>
        /// <returns>Returns true if next child is visible.</returns>
        /// <remarks>This method takes in count the flow <see cref="ResizingDirection"/> of the children.</remarks>
        protected bool NextChildIsVisible(int iChild)
        {
            if (iChild == Children.Count - 1)
                return false;

            //if (ResizingDirection == ResizingDirection.Direct)
            //if (true)//FlowDirection == FlowDirection.LeftToRight)
            //{
                IDockableControl dockableControl = Children[iChild + 1] as IDockableControl;

                if (dockableControl == null)
                    return true;

                return dockableControl.IsDocked;
            //}
            //else
            //{
            //    IDockableControl dockableControl = Children[Children.Count - 2 - iChild] as IDockableControl;

            //    if (dockableControl == null)
            //        return true;

            //    return dockableControl.IsDocked;            
            //}
        }

        /// <summary>
        /// Checks if previous child starting from a given index is visible.
        /// </summary>
        /// <param name="iChild">Starting child index.</param>
        /// <returns>Returns true if previous child is visible.</returns>
        /// <remarks>This method takes in count the flow <see cref="ResizingDirection"/> of the children.</remarks>
        protected bool PrevChildIsVisible(int iChild)
        {
            if (iChild == 0)
                return false;

            //if (ResizingDirection == ResizingDirection.Direct)
            //if (true)//(FlowDirection == FlowDirection.LeftToRight)
            //{
                IDockableControl dockableControl = Children[iChild - 1] as IDockableControl;

                if (dockableControl == null)
                    return true;

                return dockableControl.IsDocked;
            //}
            //else
            //{
            //    IDockableControl dockableControl = Children[Children.Count - (iChild + 1)] as IDockableControl;

            //    if (dockableControl == null)
            //        return true;

            //    return dockableControl.IsDocked;
            //}
        }


        List<ResizingPanelSplitter> _splitterList = new List<ResizingPanelSplitter>();
  
        protected override Size MeasureOverride(Size availableSize)
        {
            SetupSplitters();
            ChildDefinitiveLenght.Clear();

            if (Children.Count == 0)
                return base.MeasureOverride(availableSize);

            //if (!IsDocked)
            //    return new Size();
            
            System.Diagnostics.Debug.Assert(_splitterList.Count == Children.Count/2, "One and only one splitter must be present between two consecutive childs!");
            bool foundChildWithInfResizeProperty = false;

            foreach (UIElement child in Children)
            {
                if (child is ResizingPanelSplitter)
                    continue;

                if (Orientation == Orientation.Horizontal &&
                    double.IsInfinity(GetResizeWidth(child)))
                {
                    if (child is DocumentPane ||
                        (child is ResizingPanel && DockingManager.IsPanelContainingDocumentPane(child as ResizingPanel)))
                    {
                        if (foundChildWithInfResizeProperty)
                        {
                            Debug.WriteLine("A child with resize width set to infinty has been corrected to 200");
                            SetResizeWidth(child, 200);
                        }

                        foundChildWithInfResizeProperty = true;
                    }
                    else
                    {
                        SetResizeWidth(child, 200);
                    }


                }
                else if (Orientation == Orientation.Vertical &&
                    double.IsInfinity(GetResizeHeight(child)))
                {
                    if (child is DocumentPane ||
                        (child is ResizingPanel && DockingManager.IsPanelContainingDocumentPane(child as ResizingPanel)))
                    {
                        if (foundChildWithInfResizeProperty)
                        {
                            Debug.WriteLine("A child with resize height set to infinty has been corrected to 200");
                            SetResizeHeight(child, 200);
                        }

                        foundChildWithInfResizeProperty = true;
                    }
                    else
                    {
                        SetResizeHeight(child, 200);
                    }
                }
                
            }

            if (!foundChildWithInfResizeProperty)
            {
                foreach (UIElement child in Children)
                {
                    if (child is ResizingPanelSplitter)
                        continue;

                    if (Orientation == Orientation.Horizontal)
                    {
                        if (child is DocumentPane ||
                            (child is ResizingPanel && DockingManager.IsPanelContainingDocumentPane(child as ResizingPanel)))
                        {
                            SetResizeWidth(child, double.PositiveInfinity);
                            foundChildWithInfResizeProperty = true;
                            break;
                        }
                    }
                    else if (Orientation == Orientation.Vertical &&
                    double.IsInfinity(GetResizeHeight(child)))
                    {
                        if (child is DocumentPane ||
                            (child is ResizingPanel && DockingManager.IsPanelContainingDocumentPane(child as ResizingPanel)))
                        {
                            SetResizeHeight(child, double.PositiveInfinity);
                            foundChildWithInfResizeProperty = true;
                            break;
                        }
                    }
                }
            }

            if (!foundChildWithInfResizeProperty)
            {
                if (Children.Count == 1)
                {
                    if (Orientation == Orientation.Horizontal)
                        SetResizeWidth(Children[0], double.PositiveInfinity);
                    else
                        SetResizeHeight(Children[0], double.PositiveInfinity);
                }
                else if (Children.Count > 2)
                {
                    if (Orientation == Orientation.Horizontal)
                        SetResizeWidth(Children[2], double.PositiveInfinity);
                    else
                        SetResizeHeight(Children[2], double.PositiveInfinity);
                }
            }



            #region Horizontal orientation
            if (Orientation == Orientation.Horizontal)
            {
                double totSplittersWidth = 0;
                double totWidth = 0;
                int childWithPosInfWidth = 0;

                List<UIElement> childsOrderedByWidth = new List<UIElement>();
                ResizingPanelSplitter currentSplitter = null;
                bool prevChildIsVisible = false;

                for (int i = 0; i < Children.Count; i++)
                {
                    UIElement child = Children[i];
                    IDockableControl dockableChild = child as IDockableControl;
                    if (dockableChild != null && !dockableChild.IsDocked)
                    {
                        prevChildIsVisible = false;
                        child.Measure(new Size());
                    } 
                    else if (child is ResizingPanelSplitter)
                    {
                        if (currentSplitter != null)
                        {
                            child.Measure(new Size());
                        }
                        else if (prevChildIsVisible)
                            currentSplitter = child as ResizingPanelSplitter;

                        //if (//(i < Children.Count - 2 && PrevChildIsVisible(i)) ||
                        //    NextChildIsVisible(i) && PrevChildIsVisible(i)
                        //    )
                        //{
                        //    child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
                        //    totSplittersWidth += child.DesiredSize.Width;
                        //}
                        //else
                        //{
                        //    child.Measure(new Size());
                        //}
                    }
                    else
                    {
                        if (currentSplitter != null)
                        {
                            currentSplitter.Measure(new Size(double.PositiveInfinity, availableSize.Height));
                            totSplittersWidth += currentSplitter.DesiredSize.Width;
                            currentSplitter = null;
                        }

                        prevChildIsVisible = true;
                        double resWidth = (double)child.GetValue(ResizeWidthProperty);

                        if (double.IsPositiveInfinity(resWidth))
                            childWithPosInfWidth++;
                        else
                        {
                            totWidth += resWidth;
                            childsOrderedByWidth.Add(child);
                        }
                    }
                }

                if (currentSplitter != null)
                {
                    currentSplitter.Measure(new Size());
                    currentSplitter = null;
                }


                double widthForPosInfChilds = 0;
                double availWidth = availableSize.Width-totSplittersWidth;
                double shWidth = double.PositiveInfinity;

                if (totWidth > availWidth)
                {
                    childsOrderedByWidth.Sort(delegate(UIElement e1, UIElement e2)
                    {
                        double resWidth1 = (double)e1.GetValue(ResizeWidthProperty);
                        double resWidth2 = (double)e2.GetValue(ResizeWidthProperty);

                        return resWidth1.CompareTo(resWidth2);
                    });
                    

                    int count = childsOrderedByWidth.Count;
                    int i = 0;
                    double resWidth = 0;

                    while ((double)childsOrderedByWidth[i].GetValue(ResizeWidthProperty) * (count - i) + resWidth < availWidth)
                    {
                        i++;

                        if (i >= count)
                            break;

                        resWidth += (double)childsOrderedByWidth[i - 1].GetValue(ResizeWidthProperty);
                    }

                    shWidth = (availWidth - resWidth) / (count-i);
                    if (shWidth < 0)
                        shWidth = 0;
                }
                else
                {
                    if (childWithPosInfWidth > 0)
                    {
                        widthForPosInfChilds = (availWidth - totWidth) / childWithPosInfWidth;
                    }
                    else
                    {
                        widthForPosInfChilds = (availWidth - totWidth) / childsOrderedByWidth.Count;
                        
                        foreach (UIElement child in childsOrderedByWidth)
                        {
                            double resWidth = (double)child.GetValue(ResizeWidthProperty);
                            //double resHeight = (double)child.GetValue(ResizeHeightProperty);

                            child.SetValue(ResizeWidthProperty, resWidth + widthForPosInfChilds);
                        }
                    }
                }



                for (int i = 0; i < Children.Count; i++)
                {
                    UIElement child = Children[i];
                    IDockableControl dockableChild = child as IDockableControl;
                    if (dockableChild != null && !dockableChild.IsDocked)
                    {
                        child.Measure(new Size());
                    } 
                    else if (!(child is ResizingPanelSplitter))
                    {
                        double resWidth = (double)child.GetValue(ResizeWidthProperty);
                        
                        if (double.IsPositiveInfinity(resWidth))
                            ChildDefinitiveLenght[child] = widthForPosInfChilds;
                        else if (shWidth < resWidth)
                            ChildDefinitiveLenght[child] = shWidth;
                        else
                            ChildDefinitiveLenght[child] = resWidth;

                        child.Measure(new Size(ChildDefinitiveLenght[child], availableSize.Height));
                    }
                }

            }
            #endregion
            #region Vertical orientation
            else //if (Orientation == Orientation.Horizontal)
            {
                double totSplittersHeight = 0;
                double totHeight = 0;
                int childWithPosInfHeight = 0;

                List<UIElement> childsOrderedByHeight = new List<UIElement>();
                ResizingPanelSplitter currentSplitter = null;
                bool prevChildIsVisible = false;

                for (int i = 0; i < Children.Count; i++)
                {
                    UIElement child = Children[i];
                    IDockableControl dockableChild = child as IDockableControl;
                    
                    if (dockableChild != null && !dockableChild.IsDocked)
                    {
                        prevChildIsVisible = true;
                        child.Measure(new Size());
                    }  
                    else if (child is ResizingPanelSplitter)
                    {
                        if (currentSplitter != null)
                        {
                            child.Measure(new Size());
                        }
                        else if (prevChildIsVisible)
                            currentSplitter = child as ResizingPanelSplitter;

                        //if ((i < Children.Count - 2 &&
                        //    PrevChildIsVisible(i)) ||
                        //    NextChildIsVisible(i) && PrevChildIsVisible(i)
                        //    )
                        //{
                        //    child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                        //    totSplittersHeight += child.DesiredSize.Height;
                        //}
                        //else
                        //{
                        //    child.Measure(new Size());
                        //}
                    }
                    else
                    {
                        if (currentSplitter != null)
                        {
                            currentSplitter.Measure(new Size(double.PositiveInfinity, availableSize.Height));
                            totSplittersHeight += currentSplitter.DesiredSize.Height;
                            currentSplitter = null;
                        }

                        prevChildIsVisible = true;

                        double resHeight = (double)child.GetValue(ResizeHeightProperty);

                        if (double.IsPositiveInfinity(resHeight))
                            childWithPosInfHeight++;
                        else
                        {
                            totHeight += resHeight;
                            childsOrderedByHeight.Add(child);
                        }
                    }

                }

                if (currentSplitter != null)
                {
                    currentSplitter.Measure(new Size());
                    currentSplitter = null;
                }

                Debug.Assert(childWithPosInfHeight <= 1);

                double heightForPosInfChilds = 0;
                double availHeight = availableSize.Height - totSplittersHeight;
                double shHeight = double.PositiveInfinity;

                if (totHeight > availHeight)
                {
                    childsOrderedByHeight.Sort(delegate(UIElement e1, UIElement e2)
                    {
                        double resHeight1 = (double)e1.GetValue(ResizeHeightProperty);
                        double resHeight2 = (double)e2.GetValue(ResizeHeightProperty);

                        return resHeight1.CompareTo(resHeight2);
                    });

                    int count = childsOrderedByHeight.Count;
                    int i = 0;
                    double resHeight = 0;

                    while ((double)childsOrderedByHeight[i].GetValue(ResizeHeightProperty) * (count - i) + resHeight < availHeight)
                    {
                        i++;

                        if (i >= count)
                            break;

                        resHeight += (double)childsOrderedByHeight[i - 1].GetValue(ResizeHeightProperty);
                    }

                    shHeight = (availHeight - resHeight) / (count - i);
                    if (shHeight < 0)
                        shHeight = 0;
                }
                else
                {

                    if (childWithPosInfHeight > 0)
                        heightForPosInfChilds = (availHeight - totHeight) / childWithPosInfHeight;
                    else
                    {
                        heightForPosInfChilds = (availHeight - totHeight) / childsOrderedByHeight.Count;

                        foreach (UIElement child in childsOrderedByHeight)
                        {
                            //double resWidth = (double)child.GetValue(ResizeWidthProperty);
                            double resHeight = (double)child.GetValue(ResizeHeightProperty);


                            child.SetValue(ResizeHeightProperty, resHeight + heightForPosInfChilds);
                        }
                    }
                }

                for (int i = 0; i < Children.Count; i++)
                {
                    UIElement child = Children[i];
                    IDockableControl dockableChild = child as IDockableControl;
                    if (dockableChild != null && !dockableChild.IsDocked)
                    {
                        child.Measure(new Size());
                    } 
                    else if (!(child is ResizingPanelSplitter))
                    {
                        double resHeight = (double)child.GetValue(ResizeHeightProperty);

                        if (double.IsPositiveInfinity(resHeight))
                            ChildDefinitiveLenght[child] = heightForPosInfChilds;
                        else if (shHeight < resHeight)
                            ChildDefinitiveLenght[child] = shHeight;
                        else
                            ChildDefinitiveLenght[child] = resHeight;

                        child.Measure(new Size(availableSize.Width, ChildDefinitiveLenght[child]));
                    }
                }

            }
           #endregion

            //Debug.Assert(ChildDefinitiveLenght.Count > 0);

            return base.MeasureOverride(availableSize);
        }

        Dictionary<UIElement, double> ChildDefinitiveLenght = new Dictionary<UIElement, double>();

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (ChildDefinitiveLenght.Count == 0)
            {
                foreach (UIElement child in Children)
                    child.Arrange(new Rect());
                return new Size();
            }

            if (Children.Count == 0)
            {
            
            }
            else if (Children.Count == 1)
            {
                if (Children[0]!=null)
                    Children[0].Arrange(new Rect(new Point(0, 0), finalSize));
            }
            else
            {
                double totSum = 0.0;
                int childWithInfDefLen = 0;
                for (int i = 0; i < Children.Count; i++)
                { 
                    //UIElement child = ResizingDirection == ResizingDirection.Direct ? Children[i] : Children[Children.Count - 1 - i];
                    //UIElement child = FlowDirection == FlowDirection.LeftToRight ? Children[i] : Children[Children.Count - 1 - i];
                    UIElement child = Children[i];
                    IDockableControl dockableChild = child as IDockableControl;
                    if (dockableChild != null && !dockableChild.IsDocked)
                        continue;
                    if (!ChildDefinitiveLenght.ContainsKey(child))
                    {
                        totSum += Orientation == Orientation.Horizontal ?
                            child.DesiredSize.Width :
                            child.DesiredSize.Height;

                        continue;
                    }

                    double defLength = ChildDefinitiveLenght[child];
                    if (!double.IsInfinity(defLength))
                        totSum += defLength;
                    else
                        childWithInfDefLen++;
                }

                double sizeForInfDefLenght = 0.0;
                if ((Orientation == Orientation.Horizontal && HelperFunc.IsLessThen(totSum, finalSize.Width)) ||
                    (Orientation == Orientation.Vertical && HelperFunc.IsLessThen(totSum, finalSize.Height)))
                {
                    Debug.Assert(childWithInfDefLen > 0);
                    if (childWithInfDefLen > 0)
                    {
                        sizeForInfDefLenght = Orientation == Orientation.Horizontal ?
                            (finalSize.Width - totSum) / childWithInfDefLen :
                            (finalSize.Height - totSum) / childWithInfDefLen;
                    }
                }


                double offset = 0;

                for (int i = 0; i < Children.Count; i++)
                {
                    //UIElement child = ResizingDirection == ResizingDirection.Direct ? Children[i] : Children[Children.Count - 1 - i];
                    //UIElement child = FlowDirection == FlowDirection.LeftToRight ? Children[i] : Children[Children.Count - 1 - i];
                    UIElement child = Children[i];
                    IDockableControl dockableChild = child as IDockableControl;
                    if (dockableChild != null && !dockableChild.IsDocked)
                    {
                        child.Arrange(new Rect());
                    }
                    else if (ChildDefinitiveLenght.ContainsKey(child))
                    {
                        double defLength = ChildDefinitiveLenght[child];

                        if (double.IsInfinity(defLength))
                            defLength = sizeForInfDefLenght;

                        if (Orientation == Orientation.Horizontal)
                            child.Arrange(new Rect(offset, 0, defLength, finalSize.Height));
                        else
                            child.Arrange(new Rect(0, offset, finalSize.Width, defLength));
                        
                        offset += defLength;
                    }
                    else
                    {
                        bool splitterIsVisible = ((i < Children.Count - 2 &&
                            PrevChildIsVisible(i)) ||
                            NextChildIsVisible(i) && PrevChildIsVisible(i)
                            );


                        if (!splitterIsVisible)
                        {
                            child.Arrange(new Rect());
                        }
                        else
                        {
                            //Splitters..
                            if (Orientation == Orientation.Horizontal)
                            {
                                child.Arrange(new Rect(offset, 0, child.DesiredSize.Width, finalSize.Height));
                                offset += child.DesiredSize.Width;
                            }
                            else
                            {
                                child.Arrange(new Rect(0, offset, finalSize.Width, child.DesiredSize.Height));
                                offset += child.DesiredSize.Height;
                            }
                        }
                    }
                }

            }

            return base.ArrangeOverride(finalSize);
        }

        bool setupSplitters = false;
        bool splitterListIsDirty = false;

        void SetupSplitters()
        {
            if (!splitterListIsDirty)
                return;

            if (setupSplitters)
                return;

            setupSplitters = true;

            while (_splitterList.Count > 0)
            {
                ResizingPanelSplitter splitter = _splitterList[0];
                splitter.DragStarted -= new DragStartedEventHandler(splitter_DragStarted);
                splitter.DragDelta -= new DragDeltaEventHandler(splitter_DragDelta);
                splitter.DragCompleted -= new DragCompletedEventHandler(splitter_DragCompleted);
                _splitterList.Remove(splitter);
                Children.Remove(splitter);
            }

            int i = 0;//child index
            int j = 0;//splitter index

            while (i < Children.Count - 1)
            {
                if (j == _splitterList.Count)
                {
                    ResizingPanelSplitter splitter = new ResizingPanelSplitter();
                    _splitterList.Add(splitter);
                    splitter.DragStarted += new DragStartedEventHandler(splitter_DragStarted);
                    splitter.DragDelta += new DragDeltaEventHandler(splitter_DragDelta);
                    splitter.DragCompleted += new DragCompletedEventHandler(splitter_DragCompleted);
                    Children.Insert(i + 1, splitter);
                }

                i += 2;
                j++;
            }

            for (j = 0; j < _splitterList.Count; j++)
            {
                _splitterList[j].Width = (Orientation == Orientation.Horizontal) ? 4 : double.NaN;
                _splitterList[j].Height = (Orientation == Orientation.Vertical) ? 4 : double.NaN;
            }

#if DEBUG
            Debug.Assert(_splitterList.Count == Children.Count / 2);
            i = 0;
            while (true)
            {
                Debug.Assert(Children[i] != null);
                Debug.Assert(!(Children[i] is ResizingPanelSplitter));
                i++;
                if (i >= Children.Count)
                    break;

                Debug.Assert((Children[i] is ResizingPanelSplitter));
                i++;
            }
#endif
            splitterListIsDirty = false;
            setupSplitters = false;
        }

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);

            splitterListIsDirty = true;
        }

        void splitter_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            Cursor = Cursors.Arrow;
        }

        void splitter_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ResizingPanelSplitter splitter = e.Source as ResizingPanelSplitter;
            int iSplitter = Children.IndexOf(splitter);

            UIElement childPrev = null;
            UIElement childNext = null;

            //int posInc = ResizingDirection == ResizingDirection.Direct ? 2 : -2;
            int posInc = 2;// FlowDirection == FlowDirection.LeftToRight ? 2 : -2;
            int negInc = -posInc;
            int i = iSplitter;

            while (i >= 0 ||
                i < Children.Count - 1)
            {
                if (NextChildIsVisible(i))
                {
                    //childNext = Children[ResizingDirection == ResizingDirection.Direct ? i + 1 : i - 1];
                    childNext = Children[i+1];//FlowDirection == FlowDirection.LeftToRight ? i + 1 : i - 1];
                    break;
                }

                i += posInc;
            }

            i = iSplitter;

            while (i >= 0 ||
                    i < Children.Count - 1)
            {
                if (PrevChildIsVisible(i))
                {
                    //childPrev = Children[ResizingDirection == ResizingDirection.Direct ? i - 1 : i + 1];
                    childPrev = Children[i - 1];//FlowDirection == FlowDirection.LeftToRight ? i - 1 : i + 1]; 
                    break;
                }

                i -= posInc;
            }

            Size resExtPrev = new Size((double)childPrev.GetValue(ResizeWidthProperty), (double)childPrev.GetValue(ResizeHeightProperty));
            Size resExtNext = new Size((double)childNext.GetValue(ResizeWidthProperty), (double)childNext.GetValue(ResizeHeightProperty));


            #region Orientation == Horizontal
            if (Orientation == Orientation.Horizontal)
            {
                double delta = e.HorizontalChange;

                if (!double.IsPositiveInfinity(resExtPrev.Width) &&
                    (resExtPrev.Width + delta < 0))
                    delta = -resExtPrev.Width;

                if (!double.IsPositiveInfinity(resExtNext.Width) &&
                    resExtNext.Width - delta < 0)
                    delta = resExtNext.Width;


                if (!double.IsPositiveInfinity(resExtPrev.Width))
                    childPrev.SetValue(ResizeWidthProperty, resExtPrev.Width + delta);
                if (!double.IsPositiveInfinity(resExtNext.Width))
                    childNext.SetValue(ResizeWidthProperty, resExtNext.Width - delta);
            }
            #endregion        
            #region Orientation == Vertical
            else //if (Orientation == Orientation.Vertical)
            {
                double delta = e.VerticalChange;
 
                if (!double.IsPositiveInfinity(resExtPrev.Height) &&
                    (resExtPrev.Height + delta < 0))
                    delta = -resExtPrev.Height;

                if (!double.IsPositiveInfinity(resExtNext.Height) &&
                    resExtNext.Height - delta < 0)
                    delta = resExtNext.Height;


                if (!double.IsPositiveInfinity(resExtPrev.Height))
                    childPrev.SetValue(ResizeHeightProperty, resExtPrev.Height + delta);

                if (!double.IsPositiveInfinity(resExtNext.Height))
                    childNext.SetValue(ResizeHeightProperty, resExtNext.Height - delta);
            }
            #endregion        
        
        }

        void splitter_DragStarted(object sender, DragStartedEventArgs e)
        {
            Cursor = Orientation == Orientation.Horizontal ? Cursors.SizeWE : Cursors.SizeNS;
        }

        #region IDockableControl Membri di

        public bool IsDocked
        {
            get 
            {
                foreach (UIElement child in this.Children)
                {
                    if (child is IDockableControl)
                        if (((IDockableControl)child).IsDocked)
                            return true;
                }

                return false;
            }
        }

        #endregion


        /// <summary>
        /// Remove a child from children collection
        /// </summary>
        /// <param name="childToRemove"></param>
        internal void RemoveChild(FrameworkElement childToRemove)
        {
            int indexOfChildToRemove = Children.IndexOf(childToRemove);

            Debug.Assert(indexOfChildToRemove != -1);

            Children.RemoveAt(indexOfChildToRemove);

            if (Children.Count > 0)
            {
                SetupSplitters();

                if (Children.Count == 1)
                {
                    UIElement singleChild = this.Children[0];

                    if (Parent is ResizingPanel)
                    {
                        ResizingPanel parentPanel = Parent as ResizingPanel;
                        if (parentPanel != null)
                        {
                            int indexOfThisPanel = parentPanel.Children.IndexOf(this);
                            parentPanel.Children.RemoveAt(indexOfThisPanel);
                            this.Children.Remove(singleChild);
                            parentPanel.Children.Insert(indexOfThisPanel, singleChild);
                        }
                    }
                    else if (Parent is DockingManager)
                    {
                        DockingManager manager = Parent as DockingManager;
                        if (manager != null)
                        {
                            this.Children.Remove(singleChild);
                            manager.Content = singleChild;
                        }
                    }

                }
            }
            else
            {
                ResizingPanel parentPanel = Parent as ResizingPanel;
                if (parentPanel != null)
                {
                    parentPanel.RemoveChild(this);
                }
            }
         }

        /// <summary>
        /// Insert a new child element into the children collection.
        /// </summary>
        /// <param name="childToInsert">New child element to insert.</param>
        /// <param name="relativeChild">Child after or before which <see cref="childToInsert"/> element must be insert.</param>
        /// <param name="next">True if new child must be insert after the <see cref="relativeChild"/> element. False otherwise.</param>
        internal void InsertChildRelativeTo(FrameworkElement childToInsert, FrameworkElement relativeChild, bool next)
        {
            int childRelativeIndex = Children.IndexOf(relativeChild);

            Debug.Assert(childRelativeIndex != -1);

            Children.Insert(
                next ? childRelativeIndex + 1 : childRelativeIndex, childToInsert);

            SetupSplitters();
        }


    }
}
