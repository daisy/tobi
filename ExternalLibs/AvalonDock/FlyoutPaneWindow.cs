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
using System.ComponentModel;
using System.Windows.Markup;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.Windows.Forms.Integration;

namespace AvalonDock
{
    [ContentPropertyAttribute("ReferencedPane")]
    internal class FlyoutPaneWindow : System.Windows.Window
    {
        static FlyoutPaneWindow()
        {
            //This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
            //This style is defined in themes\generic.xaml
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FlyoutPaneWindow), new FrameworkPropertyMetadata(typeof(FlyoutPaneWindow)));

            //AllowsTransparency slow down perfomance under XP/VISTA because rendering is enterely perfomed using CPU
            //Window.AllowsTransparencyProperty.OverrideMetadata(typeof(FlyoutPaneWindow), new FrameworkPropertyMetadata(true));
            
            Window.WindowStyleProperty.OverrideMetadata(typeof(FlyoutPaneWindow), new FrameworkPropertyMetadata(WindowStyle.None));
            Window.ShowInTaskbarProperty.OverrideMetadata(typeof(FlyoutPaneWindow), new FrameworkPropertyMetadata(false));
            Window.ResizeModeProperty.OverrideMetadata(typeof(FlyoutPaneWindow), new FrameworkPropertyMetadata(ResizeMode.NoResize));
            Control.BackgroundProperty.OverrideMetadata(typeof(FlyoutPaneWindow), new FrameworkPropertyMetadata(Brushes.Transparent));
        }

        public FlyoutPaneWindow()
        {
            Title = "AvalonDock_FlyoutPaneWindow";
        }


        WindowsFormsHost _winFormsHost = null;
        double _targetWidth;
        double _targetHeight;

        internal double TargetWidth
        {
            get { return _targetWidth; }
            set { _targetWidth = value; }
        }

        internal double TargetHeight
        {
            get { return _targetHeight; }
            set { _targetHeight = value; }
        }

        public FlyoutPaneWindow(DockableContent content)
            : this()
        {
            //create a new temporary pane
            _refPane = new FlyoutDockablePane(content);

            _winFormsHost = ReferencedPane.GetLogicalChildContained<WindowsFormsHost>();

            if (_winFormsHost != null)
            {
                AllowsTransparency = false;
            }

            this.Loaded += new RoutedEventHandler(FlyoutPaneWindow_Loaded);
        }


        void FlyoutPaneWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Storyboard storyBoard = new Storyboard();
            double originalLeft = this.Left;
            double originalTop = this.Top;

            AnchorStyle CorrectedAnchor = Anchor;

            if (CorrectedAnchor == AnchorStyle.Left && FlowDirection == FlowDirection.RightToLeft)
                CorrectedAnchor = AnchorStyle.Right;
            else if (CorrectedAnchor == AnchorStyle.Right && FlowDirection == FlowDirection.RightToLeft)
                CorrectedAnchor = AnchorStyle.Left;


            if (CorrectedAnchor == AnchorStyle.Left || CorrectedAnchor == AnchorStyle.Right)
            {
                DoubleAnimation anim = new DoubleAnimation(0.0, _targetWidth, new Duration(TimeSpan.FromMilliseconds(200)));
                Storyboard.SetTargetProperty(anim, new PropertyPath("Width"));
                storyBoard.Children.Add(anim);
            }
            if (CorrectedAnchor == AnchorStyle.Right)
            {
                //DoubleAnimation anim = new DoubleAnimation(this.Left, this.Left + this.ActualWidth, new Duration(TimeSpan.FromMilliseconds(500)));

                DoubleAnimation anim = new DoubleAnimation(this.Left, Left - _targetWidth, new Duration(TimeSpan.FromMilliseconds(200)));
                Storyboard.SetTargetProperty(anim, new PropertyPath("Left"));
                storyBoard.Children.Add(anim);
            }

            if (CorrectedAnchor == AnchorStyle.Top || CorrectedAnchor == AnchorStyle.Bottom)
            {
                DoubleAnimation anim = new DoubleAnimation(0.0, _targetHeight, new Duration(TimeSpan.FromMilliseconds(200)));
                Storyboard.SetTargetProperty(anim, new PropertyPath("Height"));
                storyBoard.Children.Add(anim);
            }
            if (CorrectedAnchor == AnchorStyle.Bottom)
            {
                DoubleAnimation anim = new DoubleAnimation(originalTop, originalTop - _targetHeight, new Duration(TimeSpan.FromMilliseconds(200)));
                Storyboard.SetTargetProperty(anim, new PropertyPath("Top"));
                storyBoard.Children.Add(anim);
            }

            {
                DoubleAnimation anim = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromMilliseconds(100)));
                Storyboard.SetTargetProperty(anim, new PropertyPath("Opacity"));
                //AllowsTransparency slow down perfomance under XP/VISTA because rendering is enterely perfomed using CPU
                //storyBoard.Children.Add(anim);
            }

            storyBoard.Completed += (anim, eventargs) =>
                {
                    if (CorrectedAnchor == AnchorStyle.Left)
                    {
                        this.Left = originalLeft;
                        this.Width = _targetWidth;
                    }
                    if (CorrectedAnchor == AnchorStyle.Right)
                    {
                        this.Left = originalLeft - _targetWidth;
                        this.Width = _targetWidth;
                    }
                    if (CorrectedAnchor == AnchorStyle.Top)
                    {
                        this.Top = originalTop;
                        this.Height = _targetHeight;
                    }
                    if (CorrectedAnchor == AnchorStyle.Bottom)
                    {
                        this.Top = originalTop - _targetHeight;
                        this.Height = _targetHeight;
                    }
                };

            foreach (AnimationTimeline animTimeLine in storyBoard.Children)
            {
                animTimeLine.FillBehavior = FillBehavior.Stop;
            }

            storyBoard.Begin(this);
        }

        protected override void OnClosed(EventArgs e)
        {
            ReferencedPane.RestoreOriginalPane();

            base.OnClosed(e);

            _closed = true;
        }

        bool _closed = false;

        internal bool IsClosed
        {
            get { return _closed; }
        }

        //public AnchorStyle Anchor
        //{
        //    get { return (AnchorStyle)GetValue(AnchorPropertyKey.DependencyProperty); }
        //    protected set { SetValue(AnchorPropertyKey, value); }
        //}

        //// Using a DependencyProperty as the backing store for Anchor.  This enables animation, styling, binding, etc...
        //public static readonly DependencyPropertyKey AnchorPropertyKey =
        //    DependencyProperty.RegisterReadOnly("Anchor", typeof(AnchorStyle), typeof(FlyoutPaneWindow), new UIPropertyMetadata(AnchorStyle.Right));

        //public FlyoutDockablePane ReferencedPane
        //{
        //    get { return (FlyoutDockablePane)GetValue(ReferencedPaneProperty); }
        //    set { SetValue(ReferencedPaneProperty, value); }
        //}

        //// Using a DependencyProperty as the backing store for EmbeddedPane.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty ReferencedPaneProperty =
        //    DependencyProperty.Register("ReferencedPane", typeof(FlyoutDockablePane), typeof(FlyoutPaneWindow));

        //AnchorStyle _anchor = AnchorStyle.Top;
        //public AnchorStyle Anchor
        //{
        //    get { return _anchor; }
        //    set { _anchor = value; }
        //}

        public AnchorStyle Anchor
        {
            get { return ReferencedPane.Anchor; }
        }

        FlyoutDockablePane _refPane;

        public FlyoutDockablePane ReferencedPane
        {
            get { return _refPane; }
            //set 
            //{
            //    _refPane = value; 
            //}
        }
     

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            if (ReferencedPane == null)
                _refPane = this.Content as FlyoutDockablePane;

            if (ReferencedPane != null)
            {
                //SetValue(ResizingPanel.ResizeWidthProperty, ReferencedPane.GetValue(ResizingPanel.ResizeWidthProperty));
                //SetValue(ResizingPanel.ResizeHeightProperty, ReferencedPane.GetValue(ResizingPanel.ResizeHeightProperty));
                //SetValue(Window.WidthProperty, ReferencedPane.GetValue(ResizingPanel.ResizeWidthProperty));
                //SetValue(Window.HeightProperty, ReferencedPane.GetValue(ResizingPanel.ResizeHeightProperty));

                Content = ReferencedPane;
                //Anchor = ReferencedPane.Anchor;

                _closingTimer = new DispatcherTimer(
                            new TimeSpan(0, 0, 2),
                            DispatcherPriority.Normal,
                            new EventHandler(OnCloseWindow),
                            Dispatcher.CurrentDispatcher);
            }


        }

        UIElement _resizer = null;
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _resizer = GetTemplateChild("INT_Resizer") as UIElement;

            if (_resizer != null)
            {
                _resizer.MouseDown += new MouseButtonEventHandler(_resizer_MouseDown);
                _resizer.MouseMove += new MouseEventHandler(_resizer_MouseMove);
                _resizer.MouseUp += new MouseButtonEventHandler(_resizer_MouseUp);
            }
        }


        #region Resize management
        double originalWidth = 0.0;
        double originalHeight = 0.0;
        double originalLeft = 0.0;
        double originalTop = 0.0;
        Point ptStartDrag;

        private void _resizer_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            UIElement dragElement = sender as UIElement;

            originalLeft = Left;
            originalTop = Top;
            originalWidth = Width;
            originalHeight = Height;

            ptStartDrag = e.GetPosition(dragElement);
            dragElement.CaptureMouse();
        }

        private void _resizer_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            UIElement dragElement = sender as UIElement;

            if (dragElement.IsMouseCaptured)
            {
                Point ptMoveDrag = e.GetPosition(dragElement);
                AnchorStyle CorrectedAnchor = Anchor;

                if (CorrectedAnchor == AnchorStyle.Left && FlowDirection == FlowDirection.RightToLeft)
                    CorrectedAnchor = AnchorStyle.Right;
                else if (CorrectedAnchor == AnchorStyle.Right && FlowDirection == FlowDirection.RightToLeft)
                    CorrectedAnchor = AnchorStyle.Left;

                double deltaX = FlowDirection == FlowDirection.LeftToRight ? ptMoveDrag.X - ptStartDrag.X : ptStartDrag.X - ptMoveDrag.X;

                if (CorrectedAnchor == AnchorStyle.Left)
                {
                    if (Width + deltaX < 4.0)
                        Width = 4.0;
                    else
                        Width += deltaX;

                }
                else if (CorrectedAnchor == AnchorStyle.Top)
                {
                    if (Height + (ptMoveDrag.Y - ptStartDrag.Y) < 4.0)
                        Height = 4.0;
                    else
                        Height += ptMoveDrag.Y - ptStartDrag.Y;

                }
                else if (CorrectedAnchor == AnchorStyle.Right)
                {
                    if (Width - (deltaX) < 4)
                    {
                        Left = originalLeft + originalWidth - 4;
                        Width = 4;
                    }
                    else
                    {
                        Left += deltaX;
                        Width -= deltaX;
                    }

                }
                else if (CorrectedAnchor == AnchorStyle.Bottom)
                {
                    if (Height - (ptMoveDrag.Y - ptStartDrag.Y) < 4)
                    {
                        Top = originalTop + originalHeight - 4;
                        Height = 4;
                    }
                    else
                    {
                        Top += ptMoveDrag.Y - ptStartDrag.Y;
                        Height -= ptMoveDrag.Y - ptStartDrag.Y;
                    }
                }

                ResizingPanel.SetResizeHeight(ReferencedPane, ReferencedPane.ActualHeight);
                ResizingPanel.SetResizeWidth(ReferencedPane, ReferencedPane.ActualWidth);

            }
        }

        private void _resizer_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            UIElement dragElement = sender as UIElement;
            dragElement.ReleaseMouseCapture();

        }
        
        #endregion

        #region Closing window strategies


        DispatcherTimer _closingTimer = null;

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            if (!IsFocused && !IsKeyboardFocusWithin && !ReferencedPane.IsOptionsMenuOpened)
                _closingTimer.Start();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            _closingTimer.Stop();
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            if (!IsMouseOver && !ReferencedPane.IsOptionsMenuOpened)
                _closingTimer.Start();
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnLostKeyboardFocus(e);

            if (!IsMouseOver)
                _closingTimer.Start();
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            _closingTimer.Stop();
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);

            _closingTimer.Stop();
        }

        void OnCloseWindow(object sender, EventArgs e)
        {
            //options menu is open don't close the flyout window
            if (ReferencedPane.IsOptionsMenuOpened ||
                IsMouseDirectlyOver || 
                (_winFormsHost != null && _winFormsHost.IsFocused))
            {
                _closingTimer.Start();
                return;
            }

            _closingTimer.Stop();

            if (IsClosed)
                return;

            StartCloseAnimation();
        }


        internal void StartCloseAnimation()
        {
            AnchorStyle CorrectedAnchor = Anchor;

            if (CorrectedAnchor == AnchorStyle.Left && FlowDirection == FlowDirection.RightToLeft)
                CorrectedAnchor = AnchorStyle.Right;
            else if (CorrectedAnchor == AnchorStyle.Right && FlowDirection == FlowDirection.RightToLeft)
                CorrectedAnchor = AnchorStyle.Left;


            //Let closing animation to occur
            //Here we get a reference to a storyboard resource with a name ClosingStoryboard and 
            //wait that it completes before closing the window
            FrameworkElement targetElement = GetTemplateChild("INT_pane") as FrameworkElement;
            if (targetElement != null)
            {
                Storyboard storyBoard = new Storyboard();

                if (CorrectedAnchor == AnchorStyle.Left || CorrectedAnchor == AnchorStyle.Right)
                {
                    DoubleAnimation anim = new DoubleAnimation(this.ActualWidth, 0.0, new Duration(TimeSpan.FromMilliseconds(500)));
                    Storyboard.SetTargetProperty(anim, new PropertyPath("Width"));
                    storyBoard.Children.Add(anim);
                }
                if (CorrectedAnchor == AnchorStyle.Right)
                {
                    DoubleAnimation anim = new DoubleAnimation(this.Left, this.Left + this.ActualWidth, new Duration(TimeSpan.FromMilliseconds(500)));
                    Storyboard.SetTargetProperty(anim, new PropertyPath("Left"));
                    storyBoard.Children.Add(anim);
                }
                if (CorrectedAnchor == AnchorStyle.Top || CorrectedAnchor == AnchorStyle.Bottom)
                {
                    DoubleAnimation anim = new DoubleAnimation(this.Height, 0.0, new Duration(TimeSpan.FromMilliseconds(500)));
                    Storyboard.SetTargetProperty(anim, new PropertyPath("Height"));
                    storyBoard.Children.Add(anim);
                }
                if (CorrectedAnchor == AnchorStyle.Bottom)
                {
                    DoubleAnimation anim = new DoubleAnimation(this.Top, this.Top + this.Height, new Duration(TimeSpan.FromMilliseconds(500)));
                    Storyboard.SetTargetProperty(anim, new PropertyPath("Top"));
                    storyBoard.Children.Add(anim);
                }

                {
                    //DoubleAnimation anim = new DoubleAnimation(1.0, 0.0, new Duration(TimeSpan.FromMilliseconds(500)));
                    //Storyboard.SetTargetProperty(anim, new PropertyPath("Opacity"));
                    //AllowsTransparency slow down perfomance under XP/VISTA because rendering is enterely perfomed using CPU
                    //storyBoard.Children.Add(anim);
                }

                storyBoard.Completed += (animation, eventArgs) =>
                {
                    if (!IsClosed)
                        Close();
                };

                foreach (AnimationTimeline animTimeLine in storyBoard.Children)
                {
                    animTimeLine.FillBehavior = FillBehavior.Stop;
                }

                storyBoard.Begin(this);
            }
        
        }
        
        #endregion


    }
}
