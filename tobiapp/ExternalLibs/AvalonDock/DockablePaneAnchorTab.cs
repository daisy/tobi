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
using System.Diagnostics;

namespace AvalonDock
{
    /// <summary>
    /// ========================================
    /// .NET Framework 3.0 Custom Control
    /// ========================================
    ///
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:AvalonDock"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:AvalonDock;assembly=AvalonDock"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file. Note that Intellisense in the
    /// XML editor does not currently work on custom controls and its child elements.
    ///
    ///     <MyNamespace:DockablePaneAnchorTab/>
    ///
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class DockablePaneAnchorTab : System.Windows.Controls.Control, INotifyPropertyChanged
    {
        static DockablePaneAnchorTab()
        {
            //This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
            //This style is defined in themes\generic.xaml
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DockablePaneAnchorTab), new FrameworkPropertyMetadata(typeof(DockablePaneAnchorTab)));
        }

        public DockableContent ReferencedContent
        {
            get { return (DockableContent)GetValue(ReferencedContentPropertyKey.DependencyProperty); }
            internal set { SetValue(ReferencedContentPropertyKey, value); }
        }

        // Using a DependencyProperty as the backing store for DockableContent.  This enables animation, styling, binding, etc...
        public static readonly DependencyPropertyKey ReferencedContentPropertyKey =
            DependencyProperty.RegisterReadOnly("ReferencedContent", typeof(DockableContent), typeof(DockablePaneAnchorTab), new UIPropertyMetadata(null, new PropertyChangedCallback(OnPaneAttached)));


        static void OnPaneAttached(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == ReferencedContentPropertyKey.DependencyProperty)
            {
                if (((DockablePaneAnchorTab)depObj).PropertyChanged != null)
                {
                    ((DockablePaneAnchorTab)depObj).PropertyChanged(depObj, new PropertyChangedEventArgs("Anchor"));
                    ((DockablePaneAnchorTab)depObj).PropertyChanged(depObj, new PropertyChangedEventArgs("Icon"));
                    ((DockablePaneAnchorTab)depObj).PropertyChanged(depObj, new PropertyChangedEventArgs("ReferencedContent"));
                }
            }

        }

        public AnchorStyle Anchor
        {
            get { return (AnchorStyle)GetValue(AnchorPropertyKey.DependencyProperty); }
            internal set { SetValue(AnchorPropertyKey, value); }
        }

        // Using a DependencyProperty as the backing store for IsSelected.  This enables animation, styling, binding, etc...
        public static readonly DependencyPropertyKey AnchorPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly("Anchor", typeof(AnchorStyle), typeof(DockablePaneAnchorTab), new PropertyMetadata(AnchorStyle.Left));

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (ReferencedContent != null)
            {
                ReferencedContent.Manager.ShowFlyoutWindow(ReferencedContent);
            }
            base.OnMouseMove(e);
        }



        public object Icon
        {
            get { return (object)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Icon.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(object), typeof(DockablePaneAnchorTab));





        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
