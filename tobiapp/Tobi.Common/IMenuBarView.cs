using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;

namespace Tobi.Common
{
    public interface IMenuBarView
    {
        ///<summary>
        /// Appends a command group of menu items
        ///</summary>
        ///<param name="topLevelMenuItemID">the menu parent where to insert the given commands</param>
        ///<param name="commands">an ordered list of commands (either RichDelegateCommand or TwoStateMenuItemRichCommand_DataContextWrapper)</param>
        ///<param name="subMenuItemID">the name of the root menu to create, if null or empty, no root will be created</param>
        ///<param name="addSeparator">whether or not to add a visual separator before the submitted command group</param>
        ///<param name="position">a hint for the preferred position within the popup menu</param>
        ///<returns>a unique identifier for the submitted group</returns>
        int AddMenuBarGroup(string topLevelMenuItemID, string subMenuItemID, object[] commands, PreferredPosition position, bool addSeparator);

        /// <summary>
        /// Removes a given command group of menu items
        /// </summary>
        ///<param name="region">the menu parent from which to remove the given group of commands</param>
        /// <param name="uid">the unique identifier previously obtained from a call to AddMenuBarGroup()</param>
        void RemoveMenuBarGroup(string region, int uid);
    }
}
