//===================================================================================
// Microsoft patterns & practices
// Composite Application Guidance for Windows Presentation Foundation and Silverlight
//===================================================================================
// Copyright (c) Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===================================================================================
// The example companies, organizations, products, domain names,
// e-mail addresses, logos, people, places, and events depicted
// herein are fictitious.  No association with any real company,
// organization, product, domain name, email address, logo, person,
// places, or events is intended or should be inferred.
//===================================================================================
using System;
using System.Windows;
using System.Windows.Threading;

namespace Microsoft.Practices.Composite.Presentation.Commands
{
    public partial class DelegateCommand<T>
    {
        /// <summary>
        /// Raises <see cref="CanExecuteChanged"/> on the UI thread so every command invoker can requery to check if the command can execute.
        /// </summary>
        protected virtual void OnCanExecuteChanged()
        {
            Dispatcher dispatcher = null;
            if (Application.Current != null)
            {
                dispatcher = Application.Current.Dispatcher;
            }

            EventHandler canExecuteChangedHandler = CanExecuteChanged;
            if (canExecuteChangedHandler != null)
            {
                if (dispatcher != null && !dispatcher.CheckAccess())
                {
                    dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                           (Action)OnCanExecuteChanged);
                }
                else
                {
                    canExecuteChangedHandler(this, EventArgs.Empty);
                }
            }
        }
    }
}
