using System;
using System.Collections.Generic;
using Microsoft.Practices.Composite.Modularity;
using Tobi.Common.MVVM.Command;

namespace Tobi.Common
{
    /// <summary>
    /// We re-use the concept of CAG IModule (normally instanciated in the context of Unity),
    /// but we extend it to make it more verbose about its role in the context of a MEF container instead
    /// (so it behaves more like a dynamically discoverable plugin).
    /// </summary>
    public interface ITobiModule : IDisposable
    {
        ///<summary>
        /// A short name for the plugin
        ///</summary>
        string Name { get; }

        ///<summary>
        /// A single-line description of the plugin's role
        ///</summary>
        string Description { get; }

        ///<summary>
        /// This points to the "home page" of the plugin (i.e. where updates can be found).
        /// TODO: should this Uri designate a ClickOnce *.application manifest ?
        ///</summary>
        Uri Home { get; }
    }
}
