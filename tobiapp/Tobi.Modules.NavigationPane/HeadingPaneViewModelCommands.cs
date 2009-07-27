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
            //CommandExpandAll = new RichDelegateCommand<object>(UserInterfaceStrings.TreeExpandAll,
            //    UserInterfaceStrings.TreeExpandAll_,
            //    null,
            //    (VisualBrush)Application.Current.FindResource("list-add"),
            //    obj => OnExpandAll(null, null), obj => true);
            CommandExpandAll = new RichDelegateCommand<object>(UserInterfaceStrings.TreeExpandAll,
                UserInterfaceStrings.TreeExpandAll_,
                null,
                shellPresenter.LoadTangoIcon("list-add"),
                obj => OnExpandAll(null, null), obj => true);

            shellPresenter.RegisterRichCommand(CommandExpandAll);
            //
            //CommandCollapseAll = new RichDelegateCommand<object>(UserInterfaceStrings.TreeCollapseAll,
            //    UserInterfaceStrings.TreeCollapseAll_,
            //    null,
            //    (VisualBrush)Application.Current.FindResource("list-remove"),
            //    obj => OnCollapseAll(null, null), obj => true);
            CommandCollapseAll = new RichDelegateCommand<object>(UserInterfaceStrings.TreeCollapseAll,
                UserInterfaceStrings.TreeCollapseAll_,
                null,
                shellPresenter.LoadTangoIcon("list-remove"),
                obj => OnCollapseAll(null, null), obj => true);

            shellPresenter.RegisterRichCommand(CommandCollapseAll);
        }
        private void OnExpandAll(object sender, RoutedEventArgs e)
        {
            if (_headingsNavigator == null) return;
            _headingsNavigator.ExpandAll();
        }

        private void OnCollapseAll(object sender, RoutedEventArgs e)
        {
            if (_headingsNavigator == null) return;
            _headingsNavigator.CollapseAll();
        }
    }
}
