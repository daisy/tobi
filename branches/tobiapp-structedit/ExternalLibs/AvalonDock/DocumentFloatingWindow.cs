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
    public class DocumentFloatingWindow : FloatingWindow  
    {
        static DocumentFloatingWindow()
        {
            //This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
            //This style is defined in themes\generic.xaml
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DocumentFloatingWindow), new FrameworkPropertyMetadata(typeof(DocumentFloatingWindow)));

            Window.AllowsTransparencyProperty.OverrideMetadata(typeof(DocumentFloatingWindow), new FrameworkPropertyMetadata(true));
            Window.WindowStyleProperty.OverrideMetadata(typeof(DocumentFloatingWindow), new FrameworkPropertyMetadata(WindowStyle.None));
            Window.ShowInTaskbarProperty.OverrideMetadata(typeof(DocumentFloatingWindow), new FrameworkPropertyMetadata(false));
        }


        public DocumentFloatingWindow(DockingManager manager)
            :base(manager)
        {
        }        

        Pane _previousPane = null;

        int _arrayIndexPreviousPane = -1;

        public DocumentFloatingWindow(DockingManager manager, DocumentContent content)
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
        }


        internal override void OnEndDrag()
        {
            if (HostedPane.Items.Count > 0)
            {
                DocumentContent content = HostedPane.Items[0] as DocumentContent;
                HostedPane.Items.RemoveAt(0);
                _previousPane.Items.Insert(_arrayIndexPreviousPane, content);
                _previousPane.SelectedItem = content;
            }
            else
            {
                DocumentPane originalDocumentPane = _previousPane as DocumentPane;
                originalDocumentPane.CheckContentsEmpty();
              
            }

            Close();

            base.OnEndDrag();
        }


        public override Pane ClonePane()
        {
            DocumentPane paneToAnchor = new DocumentPane();

            ////transfer the resizing panel sizes
            //paneToAnchor.SetValue(ResizingPanel.ResizeWidthProperty,
            //    HostedPane.GetValue(ResizingPanel.ResizeWidthProperty));
            //paneToAnchor.SetValue(ResizingPanel.ResizeHeightProperty,
            //    HostedPane.GetValue(ResizingPanel.ResizeHeightProperty));

            //transfer contents from hosted pane in the floating window and
            //the new created dockable pane
            while (HostedPane.Items.Count > 0)
            {
                paneToAnchor.Items.Add(
                    HostedPane.RemoveContent(0));
            }

            return paneToAnchor;
        }


        internal override void OnShowSelectionBox()
        {
            this.Visibility = Visibility.Hidden;
            base.OnShowSelectionBox();
        }

        internal override void OnHideSelectionBox()
        {
            this.Visibility = Visibility.Visible;
            base.OnHideSelectionBox();
        }
    }
}
