using System.Windows;
using System.Windows.Media;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM.Command;

namespace Tobi.Modules.NavigationPane
{
    public partial class HeadingPaneViewModel
    {
        public static RichDelegateCommand<object> CommandExpandAll { get; private set; }
        public static RichDelegateCommand<object> CommandCollapseAll { get; private set; }

        private void intializeCommands()
        {
            Logger.Log("HeadingPaneViewModel.initializeCommands", Category.Debug, Priority.Medium);
            var shellPresenter = Container.Resolve<IShellPresenter>();
            //
            CommandExpandAll = new RichDelegateCommand<object>(
                UserInterfaceStrings.TreeExpandAll,
                UserInterfaceStrings.TreeExpandAll_,
                null,
                shellPresenter.LoadTangoIcon("list-add"),
                obj => _headingsNavigator.ExpandAll(),
                obj => _headingsNavigator != null);
            //
            CommandCollapseAll = new RichDelegateCommand<object>(
                UserInterfaceStrings.TreeCollapseAll,
                UserInterfaceStrings.TreeCollapseAll_,
                null,
                shellPresenter.LoadTangoIcon("list-remove"),
                obj => _headingsNavigator.CollapseAll(),
                obj => _headingsNavigator != null);

            shellPresenter.RegisterRichCommand(CommandCollapseAll);
        }
    }
}
