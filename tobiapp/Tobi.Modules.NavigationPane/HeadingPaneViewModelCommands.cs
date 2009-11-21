using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using urakawa.core;

namespace Tobi.Plugin.NavigationPane
{
    public partial class HeadingPaneViewModel
    {
        public static RichDelegateCommand CommandExpandAll { get; private set; }
//        public static RichDelegateCommand CommandExpand { get; private set; }
        public static RichDelegateCommand CommandCollapseAll { get; private set; }
//        public static RichDelegateCommand CommandCollapse { get; private set; }
        //public static RichDelegateCommand CommandEditText { get; private set; }

        public static RichDelegateCommand CommandFindNext { get; private set; }
        public static RichDelegateCommand CommandFindPrev { get; private set; }
        private void intializeCommands()
        {
            Logger.Log("HeadingPaneViewModel.initializeCommands", Category.Debug, Priority.Medium);
            var shellView = Container.Resolve<IShellView>();
            //
            CommandExpandAll = new RichDelegateCommand(
                UserInterfaceStrings.TreeExpandAll,
                UserInterfaceStrings.TreeExpandAll_,
                null, 
                shellView.LoadTangoIcon("list-add"),
                ()=> _headingsNavigator.ExpandAll(),
                ()=> _headingsNavigator != null);
/*
            CommandExpand = new RichDelegateCommand(
                UserInterfaceStrings.TreeExpand,
                UserInterfaceStrings.TreeExpand_,
                null,
                shellView.LoadGnomeNeuIcon("Neu_list-add"),
                ()=> _headingsNavigator.Expand((HeadingTreeNodeWrapper)obj),
                ()=> (_headingsNavigator != null && (obj as HeadingTreeNodeWrapper).HasChildren && !(obj as HeadingTreeNodeWrapper).IsExpanded));
 */
            //
            CommandCollapseAll = new RichDelegateCommand(
                UserInterfaceStrings.TreeCollapseAll,
                UserInterfaceStrings.TreeCollapseAll_,
                null,
                shellView.LoadTangoIcon("list-remove"),
                ()=> _headingsNavigator.CollapseAll(),
                ()=> _headingsNavigator != null);

            CommandFindNext = new RichDelegateCommand(
                UserInterfaceStrings.TreeFindNext,
                UserInterfaceStrings.TreeFindNext_,
                UserInterfaceStrings.TreeFindNext_KEYS,
                null,
                () => _headingsNavigator.FindNext(),
                () => _headingsNavigator != null);

            CommandFindPrev = new RichDelegateCommand(
                UserInterfaceStrings.TreeFindPrev,
                UserInterfaceStrings.TreeFindPrev_,
                UserInterfaceStrings.TreeFindPrev_KEYS, 
                null,
                () => _headingsNavigator.FindPrevious(),
                () => _headingsNavigator != null);
            /*
                        CommandCollapse = new RichDelegateCommand(
                            UserInterfaceStrings.TreeCollapse,
                            UserInterfaceStrings.TreeCollapse_,
                            null,
                            shellView.LoadGnomeNeuIcon("Neu_list-remove"),
                            ()=> _headingsNavigator.Collapse((HeadingTreeNodeWrapper)obj),
                            ()=> (_headingsNavigator != null && (obj as HeadingTreeNodeWrapper).HasChildren && (obj as HeadingTreeNodeWrapper).IsExpanded));
            */
            /*CommandEditText = new RichDelegateCommand(
                UserInterfaceStrings.TreeEdit,
                UserInterfaceStrings.TreeEdit_,
                null,
                shellView.LoadTangoIcon("accessories-text-editor"),
                ()=> _headingsNavigator.EditText((HeadingTreeNodeWrapper)obj),
                ()=> _headingsNavigator!=null);*/

            shellView.RegisterRichCommand(CommandExpandAll);
//            shellView.RegisterRichCommand(CommandExpand);
            shellView.RegisterRichCommand(CommandCollapseAll);
//            shellView.RegisterRichCommand(CommandCollapse);
            //shellView.RegisterRichCommand(CommandEditText);
            shellView.RegisterRichCommand(CommandFindNext);
            shellView.RegisterRichCommand(CommandFindPrev);
        }
    }
}
