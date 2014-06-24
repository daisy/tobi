using System.Collections.Generic;
using Tobi.Common;
using Tobi.Common.MVVM;

namespace Tobi.Modules.FileDialog
{
    public class FileExplorerViewModel : ViewModelBase
    {
        #region // Private fields
        private ExplorerWindowViewModel _evm;
        private DirInfo _currentTreeItem;
        private IList<DirInfo> _sysDirSource;
        #endregion

        #region // Public properties
        /// <summary>
        /// list of the directories 
        /// </summary>
        public IList<DirInfo> SystemDirectorySource
        {
            get { return _sysDirSource; }
            set
            {
                _sysDirSource = value;
                RaisePropertyChanged(() => SystemDirectorySource);
            }
        }

        /// <summary>
        /// Current selected item in the tree
        /// </summary>
        public DirInfo CurrentTreeItem
        {
            get { return _currentTreeItem; }
            set
            {
                _currentTreeItem = value;
                _evm.CurrentDirectory = _currentTreeItem;
            }
        }
        #endregion

        #region // .ctor
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="evm"></param>
        public FileExplorerViewModel(ExplorerWindowViewModel evm) : base(null)
        {
            _evm = evm;

            //create a node for "my computer"
            // this will be the root for the file system tree
            DirInfo rootNode = new DirInfo(UserInterfaceStrings.FileSystem_MyComputer);
            rootNode.Path = UserInterfaceStrings.FileSystem_MyComputer;
            _evm.CurrentDirectory = rootNode; //make root node as the current directory

            SystemDirectorySource = new List<DirInfo> { rootNode };
        }
        #endregion

        #region // public methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="curDir"></param>
        public void ExpandToCurrentNode(DirInfo curDir)
        {
            //expand the current selected node in tree 
            //if this is an ancestor of the directory we want to navigate or "My Computer" current node 
            if (CurrentTreeItem != null && (curDir.Path.Contains(CurrentTreeItem.Path)
                                            || CurrentTreeItem.Path == UserInterfaceStrings.FileSystem_MyComputer))
            {
                // expand the current node
                // If the current node is already expanded then first collapse it n then expand it
                CurrentTreeItem.IsExpanded = false;
                CurrentTreeItem.IsExpanded = true;
            }
        }
        #endregion
    }
}