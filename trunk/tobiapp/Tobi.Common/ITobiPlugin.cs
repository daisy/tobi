using System;

namespace Tobi.Common
{
    /// <summary>
    /// Acts as an intermediate between a plugin and the host application,
    /// by wiring things together, such as establishing region mapping in the user-interface.
    /// </summary>
    public interface ITobiPlugin : IDisposable
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
        ///</summary>
        Uri Home { get; }

        ///<summary>
        /// The current plugin version
        ///</summary>
        string Version { get; }
    }
}
