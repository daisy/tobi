using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Unity;
using Tobi.Infrastructure;
using Tobi.Infrastructure.Commanding;
using urakawa.core;

namespace Tobi.Modules.NavigationPane
{
    class HeadingNavigation
    {
        private HeadingsNavigator m_HeadingsNavigator;
        private bool m_ignoreTreeNodeSelectedEvent = false;
        private bool m_ignoreHeadingSelected = false;
        protected IEventAggregator EventAggregator { private set; get; }
        protected ILoggerFacade Logger { private set; get; }
        protected IUnityContainer Container { private set; get; }
        public HeadingNavigation(IUnityContainer container, IEventAggregator eventAggregator, ILoggerFacade logger)
        {
            EventAggregator = eventAggregator;
            Logger = logger;
            Container = container;

        }

        public HeadingsNavigator HeadingsNavigator
        {
            get
            {
                return m_HeadingsNavigator;
            }
        }
        public static RichDelegateCommand<object> CommandExpandAll { get; private set; }
        public static RichDelegateCommand<object> CommandCollapseAll { get; private set; }
        public void RegisterCommands()
        {
            var shellPresenter = Container.Resolve<IShellPresenter>();
            //
            CommandExpandAll = new RichDelegateCommand<object>(UserInterfaceStrings.TreeExpandAll,
                UserInterfaceStrings.TreeExpandAll_,
                null,
                (VisualBrush)Application.Current.FindResource("list-add"),
                obj => OnExpandAll(null, null), obj => true);

            shellPresenter.RegisterRichCommand(CommandExpandAll);
            //
            CommandCollapseAll = new RichDelegateCommand<object>(UserInterfaceStrings.TreeCollapseAll,
                UserInterfaceStrings.TreeCollapseAll_,
                null,
                (VisualBrush)Application.Current.FindResource("list-remove"),
                obj => OnCollapseAll(null, null), obj => true);

            shellPresenter.RegisterRichCommand(CommandCollapseAll);

            
        }
        private void OnHeadingSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (m_ignoreHeadingSelected)
            {
                m_ignoreHeadingSelected = false;
                return;
            }

            HeadingTreeNodeWrapper node = TreeView.SelectedItem as HeadingTreeNodeWrapper;
            if (node != null)
            {
                TreeNode treeNode = (node.WrappedTreeNode_LevelHeading ?? node.WrappedTreeNode_Level);

                //UpdatePageListSelection(treeNode);

                m_ignoreTreeNodeSelectedEvent = true;

                Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] NavigationPaneView.OnHeadingSelected", Category.Debug, Priority.Medium);

                EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(treeNode);
            }
        }

        private void OnExpandAll(object sender, RoutedEventArgs e)
        {
            if (m_HeadingsNavigator != null)
            {
                m_HeadingsNavigator.ExpandAll();
            }
        }
        private void OnCollapseAll(object sender, RoutedEventArgs e)
        {
            if (m_HeadingsNavigator != null)
            {
                m_HeadingsNavigator.CollapseAll();
            }
        }

    }
}
