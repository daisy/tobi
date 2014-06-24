// Copyright (c) 2008 Joel Bennett http://HuddledMasses.org/

// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
// SOFTWARE.
// *****************************************************************************
// NOTE: YOU MAY *ALSO* DISTRIBUTE THIS FILE UNDER ANY OF THE FOLLOWING...
// PERMISSIVE LICENSES:
// BSD:	 http://www.opensource.org/licenses/bsd-license.php
// MIT:   http://www.opensource.org/licenses/mit-license.html
// Ms-PL: http://www.opensource.org/licenses/ms-pl.html
// RECIPROCAL LICENSES:
// Ms-RL: http://www.opensource.org/licenses/ms-rl.html
// GPL 2: http://www.gnu.org/copyleft/gpl.html
// *****************************************************************************
// LASTLY: THIS IS NOT LICENSED UNDER GPL v3 (although the above are compatible)

using System.Collections.Generic;
using System.Windows;
using MessageMapping = System.Collections.Generic.KeyValuePair<Tobi.Common._UnusedCode.NativeMethods.WindowMessage, Tobi.Common._UnusedCode.NativeMethods.MessageHandler>;

namespace Tobi.Common._UnusedCode //Huddled.Wpf
{
    /// <summary>A behavior based on hooking a window message</summary>
    public abstract class NativeBehavior : DependencyObject
    {
        /// <summary>
        /// Called when this behavior is initially hooked up to an initialized <see cref="System.Windows.Window"/>
        /// <see cref="NativeBehavior"/> implementations may override this to perfom actions
        /// on the actual window (the Chrome behavior uses this to change the template)
        /// </summary>
        /// <remarks>Implementations should NOT depend on this being exectued before 
        /// the Window is SourceInitialized, and should use a WeakReference if they need 
        /// to keep track of the window object...
        /// </remarks>
        /// <param name="window"></param>
        virtual public void AddTo(Window window) { }

        /// <summary>
        /// Called when this behavior is unhooked from a <see cref="System.Windows.Window"/>
        /// <see cref="NativeBehavior"/> implementations may override this to perfom actions
        /// on the actual window.
        /// </summary>
        /// <param name="window"></param>
        virtual public void RemoveFrom(Window window) { }

        /// <summary>
        /// Gets the <see cref="MessageMapping"/>s for this behavior 
        /// (one for each Window Message you need to handle)
        /// </summary>
        /// <value>A collection of <see cref="MessageMapping"/> objects.</value>
        public abstract IEnumerable<MessageMapping> GetHandlers();
    }
}