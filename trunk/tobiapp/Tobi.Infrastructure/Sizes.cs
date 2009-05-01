
using System;

namespace Tobi.Infrastructure
{
    ///<summary>
    /// Application-wide predefined sizes
    /// TODO: Move this stuff to a separate module, with a 'preferences' service available in the DI container
    ///</summary>
    public static class Sizes
    {
        ///<summary>
        /// Default window height
        ///</summary>
        public const double DefaultWindowWidth = 800;

        ///<summary>
        /// Default window width
        ///</summary>
        public const double DefaultWindowHeight = 600;

        public const double IconWidth_Small = 16;
        public const double IconHeight_Small = 16;
        
        public const double IconWidth_Medium = 24;
        public const double IconHeight_Medium = 24;

        public const double IconWidth_Large = 48;
        public const double IconHeight_Large = 48;
    }
}
