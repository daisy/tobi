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
using System.Windows.Interop;

namespace AvalonDock
{
    public class DockableFloatingWindow : FloatingWindow
    {
        static DockableFloatingWindow()
        {
            //This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
            //This style is defined in themes\generic.xaml
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DockableFloatingWindow), new FrameworkPropertyMetadata(typeof(DockableFloatingWindow)));

        }


        public DockableFloatingWindow(DockingManager manager)
            : base(manager)
        {
            this.Loaded += new RoutedEventHandler(OnLoaded);
            this.Unloaded += new RoutedEventHandler(OnUnloaded);

            this.CommandBindings.Add(new CommandBinding(SetAsDockableWindowCommand, OnExecuteCommand, OnCanExecuteCommand));
            this.CommandBindings.Add(new CommandBinding(SetAsFloatingWindowCommand, OnExecuteCommand, OnCanExecuteCommand));
        }

        Pane _previousPane = null;

        int _arrayIndexPreviousPane = -1;

        public DockableFloatingWindow(DockingManager manager, DockableContent content)
            : this(manager)
        {

            //create a new temporary pane
            FloatingDockablePane pane = new FloatingDockablePane(this);

            //setup window size
            Width = content.ContainerPane.ActualWidth;
            Height = content.ContainerPane.ActualHeight;

            //save current content position in container pane
            _previousPane = content.ContainerPane;
            _arrayIndexPreviousPane = _previousPane.Items.IndexOf(content);
            pane.SetValue(ResizingPanel.ResizeWidthProperty, _previousPane.GetValue(ResizingPanel.ResizeWidthProperty));
            pane.SetValue(ResizingPanel.ResizeHeightProperty, _previousPane.GetValue(ResizingPanel.ResizeHeightProperty));

            //remove content from container pane
            content.ContainerPane.RemoveContent(_arrayIndexPreviousPane);

            //add content to my temporary pane
            pane.Items.Add(content);

            //let templates access this pane
            HostedPane = pane;

            //Change state on contents
            IsDockableWindow = true;
        }

        public DockableFloatingWindow(DockingManager manager, DockablePane dockablePane)
            : this(manager)
        {
            //create a new temporary pane
            FloatingDockablePane pane = new FloatingDockablePane(this);

            //setup window size
            Width = dockablePane.ActualWidth;
            Height = dockablePane.ActualHeight;

            //save current content position in container pane
            _previousPane = dockablePane;
            _arrayIndexPreviousPane = -1;
            pane.SetValue(ResizingPanel.ResizeWidthProperty, _previousPane.GetValue(ResizingPanel.ResizeWidthProperty));
            pane.SetValue(ResizingPanel.ResizeHeightProperty, _previousPane.GetValue(ResizingPanel.ResizeHeightProperty));

            int selectedIndex = _previousPane.SelectedIndex;

            //remove contents from container pane and insert in hosted pane
            while (_previousPane.Items.Count > 0)
            {
                ManagedContent content = _previousPane.RemoveContent(0);

                //add content to my temporary pane
                pane.Items.Add(content);
            }

            //let templates access this pane
            HostedPane = pane;
            HostedPane.SelectedIndex = selectedIndex;

            //Change state on contents
            IsDockableWindow = true;
        }


        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            if (HostedPane == null)
                HostedPane = Content as FloatingDockablePane;

            if (HostedPane != null)
            {
                Content = HostedPane;

                //_closingTimer = new DispatcherTimer(
                //            new TimeSpan(0, 0, 1),
                //            DispatcherPriority.Normal,
                //            new EventHandler(OnCloseWindow),
                //            Dispatcher.CurrentDispatcher);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            while (HostedPane.Items.Count > 0)
            {
                Manager.Hide(HostedPane.Items[0] as DockableContent);
            }

            Manager.UnregisterFloatingWindow(this);
        }


        public override Pane ClonePane()
        {
            DockablePane paneToAnchor = new DockablePane();

            //transfer the resizing panel sizes
            paneToAnchor.SetValue(ResizingPanel.ResizeWidthProperty,
                HostedPane.GetValue(ResizingPanel.ResizeWidthProperty));
            paneToAnchor.SetValue(ResizingPanel.ResizeHeightProperty,
                HostedPane.GetValue(ResizingPanel.ResizeHeightProperty));

            int selectedIndex = HostedPane.SelectedIndex;

            //transfer contents from hosted pane in the floating window and
            //the new created dockable pane
            while (HostedPane.Items.Count > 0)
            {
                paneToAnchor.Items.Add(
                    HostedPane.RemoveContent(0));
            }

            paneToAnchor.SelectedIndex = selectedIndex;

            return paneToAnchor;
        }


        #region Floating/dockable window state
        bool _dockableWindow = true;

        public bool IsDockableWindow
        {
            get { return _dockableWindow; }
            set 
            { 
                _dockableWindow = value;

                if (_dockableWindow)
                {
                    foreach (DockableContent content in HostedPane.Items)
                        content.SetStateToDockableWindow();
                }
                else
                {
                    foreach (DockableContent content in HostedPane.Items)
                        content.SetStateToFloatingWindow();
                }
            }
        }

        public bool IsFloatingWindow
        {
            get { return !IsDockableWindow; }
            set { IsDockableWindow = !value; }
        }
        #endregion


        #region Commands
        private static object syncRoot = new object();


        private static RoutedUICommand dockableCommand = null;
        public static RoutedUICommand SetAsDockableWindowCommand
        {
            get
            {
                lock (syncRoot)
                {
                    if (null == dockableCommand)
                    {
                        dockableCommand = new RoutedUICommand("D_ockable", "Dockable", typeof(FloatingWindow));
                    }

                }
                return dockableCommand;
            }
        }

        private static RoutedUICommand floatingCommand = null;
        public static RoutedUICommand SetAsFloatingWindowCommand
        {
            get
            {
                lock (syncRoot)
                {
                    if (null == floatingCommand)
                    {
                        floatingCommand = new RoutedUICommand("F_loating", "Floating", typeof(FloatingWindow));
                    }

                }
                return floatingCommand;
            }
        }


        void OnExecuteCommand(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == SetAsDockableWindowCommand)
            {
                IsDockableWindow = true;
                e.Handled = true;
            }
            else if (e.Command == SetAsFloatingWindowCommand)
            {
                IsFloatingWindow = true;
                e.Handled = true;
            }
            else if (e.Command == DockablePane.CloseCommand)
            {
                Close();
                e.Handled = true;
            }
        }

        void OnCanExecuteCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Command == SetAsDockableWindowCommand)
                e.CanExecute = IsFloatingWindow;
            else if (e.Command == SetAsFloatingWindowCommand)
                e.CanExecute = IsDockableWindow;


        }
        #endregion


        #region Non-Client area management

        private const int WM_MOVE = 0x0003;
        private const int WM_SIZE = 0x0005;
        private const int WM_NCMOUSEMOVE = 0xa0;
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int WM_NCLBUTTONUP = 0xA2;
        private const int WM_NCLBUTTONDBLCLK = 0xA3;
        private const int WM_NCRBUTTONDOWN = 0xA4;
        private const int WM_NCRBUTTONUP = 0xA5;
        private const int HTCAPTION = 2;
        private const int SC_MOVE = 0xF010;
        private const int WM_SYSCOMMAND = 0x0112;



        #region Load/Unload window events
        HwndSource _hwndSource;
        HwndSourceHook _wndProcHandler;

        protected void OnLoaded(object sender, EventArgs e)
        {
            WindowInteropHelper helper = new WindowInteropHelper(this);
            _hwndSource = HwndSource.FromHwnd(helper.Handle);
            _wndProcHandler = new HwndSourceHook(HookHandler);
            _hwndSource.AddHook(_wndProcHandler);
        }
        protected void OnUnloaded(object sender, EventArgs e)
        {
            //HostedPane.ReferencedPane.SaveFloatingWindowSizeAndPosition(this);
            //HostedPane.Close();

            if (_hwndSource != null)
                _hwndSource.RemoveHook(_wndProcHandler);
        } 
        #endregion


        private IntPtr HookHandler(
            IntPtr hwnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled
        )
        {
            handled = false;

            switch (msg)
            {
                case WM_SIZE:
                case WM_MOVE:
                    //HostedPane.ReferencedPane.SaveFloatingWindowSizeAndPosition(this);
                    break;
                case WM_NCLBUTTONDOWN:
                    if (IsDockableWindow && wParam.ToInt32() == HTCAPTION)
                    {
                        short x = (short)((lParam.ToInt32() & 0xFFFF));
                        short y = (short)((lParam.ToInt32() >> 16));

                        Point clickPoint = this.TransformToDeviceDPI(new Point(x, y));
                        Manager.Drag(this, clickPoint, new Point(clickPoint.X - Left, clickPoint.Y - Top));

                        handled = true;
                    }
                    break;
                case WM_NCLBUTTONDBLCLK:
                    if (IsDockableWindow && wParam.ToInt32() == HTCAPTION)
                    {
                        //
                        //HostedPane.ReferencedPane.ChangeState(PaneState.Docked);
                        //HostedPane.ReferencedPane.Show();
                        //this.Close();

                        handled = true;
                    }
                    break;
                case WM_NCRBUTTONDOWN:
                    if (wParam.ToInt32() == HTCAPTION)
                    {
                        short x = (short)((lParam.ToInt32() & 0xFFFF));
                        short y = (short)((lParam.ToInt32() >> 16));

                        ContextMenu cxMenu = FindResource(new ComponentResourceKey(typeof(DockingManager), ContextMenuElement.FloatingWindow)) as ContextMenu;
                        if (cxMenu != null)
                        {
                            foreach (MenuItem menuItem in cxMenu.Items)
                                menuItem.CommandTarget = this;

                            cxMenu.Placement = PlacementMode.AbsolutePoint;
                            cxMenu.PlacementRectangle = new Rect(new Point(x, y), new Size(0, 0));
                            cxMenu.PlacementTarget = this;
                            cxMenu.IsOpen = true;
                        }

                        handled = true;
                    }
                    break;
                case WM_NCRBUTTONUP:
                    if (wParam.ToInt32() == HTCAPTION)
                    {

                        handled = true;
                    }
                    break;

            }


            return IntPtr.Zero;
        }
        #endregion
 


    }
}
