using System.Windows;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Common.UI;

namespace Tobi.Modules.FileDialog
{
    public class FileDialogService : IFileDialogService
    {
        protected ILoggerFacade Logger { get; private set; }
        protected IUnityContainer Container { get; private set; }

        public FileDialogService(IUnityContainer container,
                            ILoggerFacade logger)
        {
            Container = container;
            Logger = logger;
        }

        public string SaveAs()
        {
            var shellPresenter = Container.Resolve<IShellPresenter>();
            var window = shellPresenter.View as Window;

            var panel = new FileBrowserPanel();

            var windowPopup = new PopupModalWindow(shellPresenter,
                                                   UserInterfaceStrings.EscapeMnemonic(
                                                       UserInterfaceStrings.SaveAs),
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 500, 300);

            var iconComputer = new ScalableGreyableImageProvider(shellPresenter.LoadTangoIcon("computer"))
            {
                IconDrawScale = shellPresenter.View.MagnificationLevel
            };
            var iconDrive = new ScalableGreyableImageProvider(shellPresenter.LoadTangoIcon("drive-harddisk"))
            {
                IconDrawScale = shellPresenter.View.MagnificationLevel
            };
            var iconFolder = new ScalableGreyableImageProvider(shellPresenter.LoadTangoIcon("folder"))
            {
                IconDrawScale = shellPresenter.View.MagnificationLevel
            };
            var iconFile = new ScalableGreyableImageProvider(shellPresenter.LoadTangoIcon("text-x-generic-template"))
            {
                IconDrawScale = shellPresenter.View.MagnificationLevel
            };

            var viewModel = new ExplorerWindowViewModel(() => windowPopup.ForceClose(PopupModalWindow.DialogButton.Ok),
                iconComputer, iconDrive, iconFolder, iconFile);
            panel.DataContext = viewModel;

            windowPopup.ShowModal();

            if (windowPopup.ClickedDialogButton != PopupModalWindow.DialogButton.Ok)
            {
                return null;
            }

            if (viewModel.DirViewVM.CurrentItem != null
                && (ObjectType)viewModel.DirViewVM.CurrentItem.DirType == ObjectType.File)
            {
                return viewModel.DirViewVM.CurrentItem.Path;
            }

            return null;
        }
    }
}
