using System;
using System.Collections.ObjectModel;
using System.Windows;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
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
        string Convert_MathML_to_SVG(string mathML, string svgFileOutput);

        bool askUserConfirmOverwriteFileFolder(string path, bool folder, Window owner);

        RichDelegateCommand UndoCommand { get; }
        RichDelegateCommand RedoCommand { get; }

        void TryOpenFile(string filename);

        Tuple<TreeNode, TreeNode> PerformTreeNodeSelection(TreeNode clickedNode);
        Tuple<TreeNode, TreeNode> PerformTreeNodeSelection(TreeNode clickedNode, bool allowAutoSubNodeAndToggle, TreeNode subClickedNode);

        Tuple<TreeNode, TreeNode> GetTreeNodeSelection();
        
        void SaveRecentFiles();
        ObservableCollection<Uri> RecentFiles { get; }

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
