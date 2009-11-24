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

    ///<summary>
    /// Default implementation for Version and Home.
    ///</summary>
    public abstract class AbstractTobiPlugin : ITobiPlugin
    {
        public string Version
        {
            get { return UserInterfaceStrings.APP_VERSION; }
        }

        public Uri Home
        {
            get { return UserInterfaceStrings.TobiHomeUri; }
        }

        public abstract string Name { get; }
        public abstract string Description { get; }

        public abstract void Dispose();
    }
}
