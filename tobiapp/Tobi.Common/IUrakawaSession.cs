using System;
using Tobi.Common.MVVM;
using Tobi.Common.UI;
using urakawa;
using urakawa.core;

namespace Tobi.Common
{
    ///<summary>
    /// The public facade for an authoring session based on the Urakawa SDK data model.
    ///</summary>
    public interface IUrakawaSession : IPropertyChangedNotifyBase
    {
        void TryOpenFile(string filename);

        Tuple<TreeNode, TreeNode> PerformTreeNodeSelection(TreeNode clickedNode);
        Tuple<TreeNode, TreeNode> GetTreeNodeSelection();

        /// <summary>
        /// The Project instance for the currently active document.
        /// </summary>
        Project DocumentProject { get; }

        /// <summary>
        /// The file path where the current document was last saved.
        /// </summary>
        string DocumentFilePath { get; }

        /// <summary>
        /// Tells whether the current document contains unsaved modifications.
        /// </summary>
        bool IsDirty { get; }

        bool IsAcmCodecsDisabled { get; }

        /// <summary>
        /// Requests to close the current document.
        /// </summary>
        /// <returns>OK if the current document was successfully closed.</returns>
        PopupModalWindow.DialogButton CheckSaveDirtyAndClose(PopupModalWindow.DialogButtonsSet buttonset, string role);
    }
}
