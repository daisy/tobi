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
        public static RichDelegateCommand CommandExpandAll { get; private set; }
//        public static RichDelegateCommand CommandExpand { get; private set; }
        public static RichDelegateCommand CommandCollapseAll { get; private set; }
//        public static RichDelegateCommand CommandCollapse { get; private set; }
        //public static RichDelegateCommand CommandEditText { get; private set; }

        private void intializeCommands()
        {
            Logger.Log("HeadingPaneViewModel.initializeCommands", Category.Debug, Priority.Medium);
            var shellPresenter = Container.Resolve<IShellPresenter>();
            //
            CommandExpandAll = new RichDelegateCommand(
                UserInterfaceStrings.TreeExpandAll,
                UserInterfaceStrings.TreeExpandAll_,
                null,
                shellPresenter.LoadTangoIcon("list-add"),
                ()=> _headingsNavigator.ExpandAll(),
                ()=> _headingsNavigator != null);
/*
            CommandExpand = new RichDelegateCommand(
                UserInterfaceStrings.TreeExpand,
                UserInterfaceStrings.TreeExpand_,
                null,
                shellPresenter.LoadGnomeNeuIcon("Neu_list-add"),
                ()=> _headingsNavigator.Expand((HeadingTreeNodeWrapper)obj),
                ()=> (_headingsNavigator != null && (obj as HeadingTreeNodeWrapper).HasChildren && !(obj as HeadingTreeNodeWrapper).IsExpanded));
 */
            //
            CommandCollapseAll = new RichDelegateCommand(
                UserInterfaceStrings.TreeCollapseAll,
                UserInterfaceStrings.TreeCollapseAll_,
                null,
                shellPresenter.LoadTangoIcon("list-remove"),
                ()=> _headingsNavigator.CollapseAll(),
                ()=> _headingsNavigator != null);
/*
            CommandCollapse = new RichDelegateCommand(
                UserInterfaceStrings.TreeCollapse,
                UserInterfaceStrings.TreeCollapse_,
                null,
                shellPresenter.LoadGnomeNeuIcon("Neu_list-remove"),
                ()=> _headingsNavigator.Collapse((HeadingTreeNodeWrapper)obj),
                ()=> (_headingsNavigator != null && (obj as HeadingTreeNodeWrapper).HasChildren && (obj as HeadingTreeNodeWrapper).IsExpanded));
*/
            /*CommandEditText = new RichDelegateCommand(
                UserInterfaceStrings.TreeEdit,
                UserInterfaceStrings.TreeEdit_,
                null,
                shellPresenter.LoadTangoIcon("accessories-text-editor"),
                ()=> _headingsNavigator.EditText((HeadingTreeNodeWrapper)obj),
                ()=> _headingsNavigator!=null);*/

            shellPresenter.RegisterRichCommand(CommandExpandAll);
//            shellPresenter.RegisterRichCommand(CommandExpand);
            shellPresenter.RegisterRichCommand(CommandCollapseAll);
//            shellPresenter.RegisterRichCommand(CommandCollapse);
            //shellPresenter.RegisterRichCommand(CommandEditText);
        }
    }
}
