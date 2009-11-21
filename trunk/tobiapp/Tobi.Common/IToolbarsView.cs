using Tobi.Common.MVVM.Command;

namespace Tobi.Common
{
    public interface IToolBarsView
    {
        ///<summary>
        /// Appends a command group of icons
        ///</summary>
        ///<param name="commands">an ordered list of commands</param>
        ///<returns>a unique identifier for the submitted group</returns>
        int AddToolBarGroup(RichDelegateCommand[] commands);

        /// <summary>
        /// Removes a given command group of icons
        /// </summary>
        /// <param name="uid">the unique identifier previously obtained from a call to AddToolBarGroup()</param>
        void RemoveToolBarGroup(int uid);
    }
}
