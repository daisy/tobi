using System.Windows;
using System.Windows.Media;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using urakawa.core;

namespace Tobi.Modules.NavigationPane
{
    public partial class HeadingPaneViewModel
    {
        public static RichDelegateCommand<object> CommandExpandAll { get; private set; }
//        public static RichDelegateCommand<object> CommandExpand { get; private set; }
        public static RichDelegateCommand<object> CommandCollapseAll { get; private set; }
//        public static RichDelegateCommand<object> CommandCollapse { get; private set; }
        public static RichDelegateCommand<object> CommandEditText { get; private set; }

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
/*
            CommandExpand = new RichDelegateCommand<object>(
                UserInterfaceStrings.TreeExpand,
                UserInterfaceStrings.TreeExpand_,
                null,
                shellPresenter.LoadGnomeNeuIcon("Neu_list-add"),
                obj => _headingsNavigator.Expand((HeadingTreeNodeWrapper)obj),
                obj => (_headingsNavigator != null && (obj as HeadingTreeNodeWrapper).HasChildren && !(obj as HeadingTreeNodeWrapper).IsExpanded));
 */
            //
            CommandCollapseAll = new RichDelegateCommand<object>(
                UserInterfaceStrings.TreeCollapseAll,
                UserInterfaceStrings.TreeCollapseAll_,
                null,
                shellPresenter.LoadTangoIcon("list-remove"),
                obj => _headingsNavigator.CollapseAll(),
                obj => _headingsNavigator != null);
/*
            CommandCollapse = new RichDelegateCommand<object>(
                UserInterfaceStrings.TreeCollapse,
                UserInterfaceStrings.TreeCollapse_,
                null,
                shellPresenter.LoadGnomeNeuIcon("Neu_list-remove"),
                obj => _headingsNavigator.Collapse((HeadingTreeNodeWrapper)obj),
                obj => (_headingsNavigator != null && (obj as HeadingTreeNodeWrapper).HasChildren && (obj as HeadingTreeNodeWrapper).IsExpanded));
*/
            CommandEditText = new RichDelegateCommand<object>(
                UserInterfaceStrings.TreeEdit,
                UserInterfaceStrings.TreeEdit_,
                null,
                shellPresenter.LoadTangoIcon("accessories-text-editor"),
                obj => _headingsNavigator.EditText((HeadingTreeNodeWrapper)obj),
                obj => _headingsNavigator!=null);

            shellPresenter.RegisterRichCommand(CommandExpandAll);
//            shellPresenter.RegisterRichCommand(CommandExpand);
            shellPresenter.RegisterRichCommand(CommandCollapseAll);
//            shellPresenter.RegisterRichCommand(CommandCollapse);
            shellPresenter.RegisterRichCommand(CommandEditText);
        }
    }
}
