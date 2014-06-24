using System;
using System.Windows.Media;

namespace Tobi.Infrastructure.UI.FileDialog
{
    /// <summary>
    /// View model for the right side pane
    /// </summary>
    public class DirectoryViewerViewModel : ViewModelBase
    {
        #region // Private variables
        private ExplorerWindowViewModel _evm;
        private DirInfo _currentItem;
        private Action m_action;

        #endregion

        #region // .ctor
        public DirectoryViewerViewModel(ExplorerWindowViewModel evm, Action activateFileAction)
        {
            _evm = evm;
            m_action = activateFileAction;
        }
        #endregion

        #region // Public members
        /// <summary>
        /// Indicates the current directory in the Directory view pane
        /// </summary>
        public DirInfo CurrentItem
        {
            get { return _currentItem; }
            set { _currentItem = value; }
        }
        #endregion

        #region // Public Methods
        /// <summary>
        /// processes the current object. If this is a file then open it or if it is a directory then return its subdirectories
        /// </summary>
        public void OpenCurrentObject()
        {
            int objType = CurrentItem.DirType; //Dir/File type

            if ((ObjectType)CurrentItem.DirType == ObjectType.File)
            {
                m_action.Invoke();
            }
            else
            {
                _evm.CurrentDirectory = CurrentItem;
                _evm.FileTreeVM.ExpandToCurrentNode(_evm.CurrentDirectory);
            }
        }
        #endregion
    }
}
