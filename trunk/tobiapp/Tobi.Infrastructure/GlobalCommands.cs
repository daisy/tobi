using Microsoft.Practices.Composite.Presentation.Commands;

namespace Tobi.Infrastructure
{
    ///<summary>
    /// Application-wide commands (usually composite, ready for register/unregister)
    ///</summary>
    public static class GlobalCommands
    {
        //public static readonly CompositeCommand ExitCommand = new CompositeCommand();

        ///<summary>
        /// TODO: Make this fake Command meaningful
        ///</summary>
        public readonly static CompositeCommand TestActiveAwareCommand = new CompositeCommand(true);

        ///<summary>
        /// TODO: Make this fake Command meaningful
        ///</summary>
        public readonly static CompositeCommand TestNonActiveAwareCommand = new CompositeCommand();
    }

    ///<summary>
    /// Proxy class for the above static class (to use in dependency injection)
    ///</summary>
    public class GlobalCommandsProxy
    {
        ///<summary>
        /// See <see cref="GlobalCommands.TestActiveAwareCommand"/>.
        ///</summary>
        virtual public CompositeCommand TestActiveAwareCommand
        {
            get { return GlobalCommands.TestActiveAwareCommand; }
        }
        ///<summary>
        /// See <see cref="GlobalCommands.TestNonActiveAwareCommand"/>.
        ///</summary>
        virtual public CompositeCommand TestNonActiveAwareCommand
        {
            get { return GlobalCommands.TestNonActiveAwareCommand; }
        }
        /*
        ///<summary>
        /// See <see cref="GlobalCommands.ExitCommand"/>.
        ///</summary>
        virtual public CompositeCommand ExitCommand
        {
            get { return GlobalCommands.ExitCommand; }
        }
        */
    }
}
