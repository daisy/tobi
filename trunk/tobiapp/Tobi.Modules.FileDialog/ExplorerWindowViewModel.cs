using System;
using System.Collections.Generic;
using System.Linq;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.UI;

namespace Tobi.Modules.FileDialog
{
    public class ExplorerWindowViewModel : ViewModelBase
    {
        #region // Private Members

        private DirInfo _currentDirectory;
        private IList<DirInfo> _currentItems;

        private FileExplorerViewModel _fileTreeVM;
        private DirectoryViewerViewModel _dirViewerVM;

        #endregion

        public ScalableGreyableImageProvider IconComputer { get; private set; }
        public ScalableGreyableImageProvider IconDrive { get; private set; }
        public ScalableGreyableImageProvider IconFolder { get; private set; }
        public ScalableGreyableImageProvider IconFile { get; private set; }


        #region // .ctor
        public ExplorerWindowViewModel(Action activateFileAction,
                                       ScalableGreyableImageProvider iconComputer, ScalableGreyableImageProvider iconDrive, ScalableGreyableImageProvider iconFolder, ScalableGreyableImageProvider iconFile)
            : base(null)
        {
            IconComputer = iconComputer;
            IconDrive = iconDrive;
            IconFolder = iconFolder;
            IconFile = iconFile;

            FileTreeVM = new FileExplorerViewModel(this);
            DirViewVM = new DirectoryViewerViewModel(this, activateFileAction);
        }
        #endregion

        #region // Public Properties
        /// <summary>
        /// Name of the current directory user is in
        /// </summary>
        public DirInfo CurrentDirectory
        {
            get { return _currentDirectory; }
            set
            {
                _currentDirectory = value;
                RefreshCurrentItems();
                OnPropertyChanged(() => CurrentDirectory);
            }
        }

        /// <summary>
        /// Tree View model
        /// </summary>
        public FileExplorerViewModel FileTreeVM
        {
            get { return _fileTreeVM; }
            set
            {
                _fileTreeVM = value;
                OnPropertyChanged(() => FileTreeVM);
            }
        }
        /// <summary>
        /// Tree View model
        /// </summary>
        public DirectoryViewerViewModel DirViewVM
        {
            get { return _dirViewerVM; }
            set
            {
                _dirViewerVM = value;
                OnPropertyChanged(() => DirViewVM);
            }
        }

        /// <summary>
        /// Children of the current directory to show in the right pane
        /// </summary>
        public IList<DirInfo> CurrentItems
        {
            get
            {
                if (_currentItems == null)
                {
                    _currentItems = new List<DirInfo>();
                }
                return _currentItems;
            }
            set
            {
                _currentItems = value;
                OnPropertyChanged(() => CurrentItems);
            }
        }
        #endregion

        #region // methods


        /// <summary>
        /// this method gets the children of current directory and stores them in the CurrentItems Observable collection
        /// </summary>
        protected void RefreshCurrentItems()
        {
            IList<DirInfo> childDirList = new List<DirInfo>();
            IList<DirInfo> childFileList = new List<DirInfo>();

            //If current directory is "My computer" then get the all logical drives in the system
            if (CurrentDirectory.Name.Equals(UserInterfaceStrings.FileSystem_MyComputer))
            {
                childDirList = (from rd in FileSystemExplorerService.GetRootDirectories()
                                select new DirInfo(rd)).ToList();
            }
            else
            {
                //Combine all the subdirectories and files of the current directory
                childDirList = (from dir in FileSystemExplorerService.GetChildDirectories(CurrentDirectory.Path)
                                select new DirInfo(dir)).ToList();

                childFileList = (from fobj in FileSystemExplorerService.GetChildFiles(CurrentDirectory.Path)
                                 select new DirInfo(fobj)).ToList();

                childDirList = childDirList.Concat(childFileList).ToList();
            }

            CurrentItems = childDirList;
        }
        #endregion
    }
}