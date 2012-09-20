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
    public struct XukSpineItemData
    {
        public XukSpineItemData(Uri uri, string title)
        {
            Uri = uri;
            Title = title;
        }

        public Uri Uri;
        public string Title;
    }

    ///<summary>
    /// The public facade for an authoring session based on the Urakawa SDK data model.
    ///</summary>
    public interface IUrakawaSession : IPropertyChangedNotifyBase
    {
        bool isTreeNodeSkippable(TreeNode node);
        TreeNode AdjustTextSyncGranularity(TreeNode node, TreeNode upperLimit);

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

        ObservableCollection<XukSpineItemData> XukSpineItems { get; }

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

        bool IsXukSpine { get; }

        /// <summary>
        /// Requests to close the current document.
        /// </summary>
        /// <returns>OK if the current document was successfully closed.</returns>
        PopupModalWindow.DialogButton CheckSaveDirtyAndClose(PopupModalWindow.DialogButtonsSet buttonset, string role);
    }
}
