﻿//Copyright (c) 2007, Adolfo Marinucci
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

namespace AvalonDock
{
    public class FloatingDockablePane : DockablePane
    {
        static FloatingDockablePane()
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(FloatingDockablePane), new FrameworkPropertyMetadata(typeof(FloatingDockablePane)));            
            DockablePane.ShowHeaderProperty.OverrideMetadata(typeof(FloatingDockablePane), new FrameworkPropertyMetadata(false));
        }

        public FloatingDockablePane(FloatingWindow floatingWindow)
        {
            //_referencedPane = referencedPane;
            _floatingWindow = floatingWindow;
        }

        FloatingWindow _floatingWindow = null;

        public FloatingWindow FloatingWindow
        {
            get { return _floatingWindow; }
        }

        //Pane _referencedPane = null;

        //public Pane ReferencedPane
        //{
        //    get { return _referencedPane; }
        //}

        public override DockingManager GetManager()
        {
            return _floatingWindow.Manager;//_referencedPane.GetManager();
        }

        protected override void CheckItems(System.Collections.IList newItems)
        {
            foreach (object newItem in newItems)
            {
                if (!(newItem is DockableContent) && !(newItem is DocumentContent))
                    throw new InvalidOperationException("FloatingDockablePane can contain only DockableContents and DocumentContents!");
            }
        }

    }
}
