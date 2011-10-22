using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.UI;
using urakawa.core;

namespace Tobi.Plugin.Descriptions
{
    /// <summary>
    /// Interaction logic for DescriptionsNavigationView.xaml
    /// </summary>
    [Export(typeof(DescriptionsNavigationView)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class DescriptionsNavigationView : ITobiViewFocusable // : IActiveAware
    {
        private bool _ignoreTreeNodeSelectedEvent = false;

        public DescriptionsNavigationViewModel ViewModel
        {
            get; private set;
        }
        private readonly IEventAggregator m_EventAggregator;
        private readonly ILoggerFacade m_Logger;
        private readonly IUrakawaSession m_UrakawaSession;

        private readonly DescriptionsView m_DescriptionsView;

        [ImportingConstructor]
        public DescriptionsNavigationView(
            IEventAggregator eventAggregator,
            ILoggerFacade logger,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession urakawaSession,
            [Import(typeof(DescriptionsNavigationViewModel), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            DescriptionsNavigationViewModel viewModel,
            [Import(typeof(IDescriptionsView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            DescriptionsView view
            )
        {
            m_UrakawaSession = urakawaSession;
            m_EventAggregator = eventAggregator;
            m_Logger = logger;

            ViewModel = viewModel;
            DataContext = ViewModel;

            m_DescriptionsView = view;

            InitializeComponent();
            ViewModel.SetView(this);
        }

        public string ViewName
        {
            get { return "Descriptions"; }
        }

        public void LoadProject()
        {
            //m_LastListItemSelected = null;
        }
        public void UnloadProject()
        {
            //m_LastListItemSelected = null;
            SearchBox.Text = "";
        }
        private void OnSearchLostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchBox.Text))
            {
                ViewModel.IsSearchVisible = false;
            }
        }
        public UIElement FocusableItem
        {
            get
            {
                if (ListView.Focusable) return ListView;

                if (ListView.SelectedIndex != -1)
                {
                    return ListView.ItemContainerGenerator.ContainerFromIndex(ListView.SelectedIndex) as ListViewItem;
                }

                if (ListView.Items.Count > 0)
                {
                    return ListView.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
                }

                return null;
            }
        }
        //public UIElement ViewControl
        //{
        //    get { return this; }
        //}
        //public UIElement ViewFocusStart
        //{
        //    get { return ListView; }
        //}



        private void OnClick_Button(object sender, RoutedEventArgs e)
        {
            handleListCurrentSelection();

            m_DescriptionsView.Popup();
        }

        private void handleListCurrentSelection()
        {
            if (ListView.SelectedIndex >= 0)
            {
                DescribableTreeNode mnode = ListView.SelectedItem as DescribableTreeNode;
                if (mnode == null) return;
                TreeNode treeNode = mnode.TreeNode;

                if (treeNode == null) return;

                _ignoreTreeNodeSelectedEvent = true;

                m_UrakawaSession.PerformTreeNodeSelection(treeNode);
                //m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(treeNode);
            }

            //m_DescriptionsView.Popup();
        }

        private void OnKeyUp_ListItem(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                handleListCurrentSelection();
            }
        }

        private void OnMouseDoubleClick_ListItem(object sender, MouseButtonEventArgs e)
        {
            handleListCurrentSelection();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ViewModel.DescriptionsNavigator == null) { return; }
            ViewModel.DescriptionsNavigator.SearchTerm = SearchBox.Text;
        }

        //internal ListViewItem m_LastListItemSelected;

        //private void OnSelected_ListItem(object sender, RoutedEventArgs e)
        //{
        //    Debug.Assert(sender == e.Source);
        //    m_LastListItemSelected = (ListViewItem)sender;
        //}

        private void OnSearchBoxKeyUp(object sender, KeyEventArgs e)
        {
            var key = (e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));
            
            if (key == Key.Return && ViewModel.CommandFindNextDescription.CanExecute())
            {
                ViewModel.CommandFindNextDescription.Execute();
            }

            if (key == Key.Escape)
            {
                SearchBox.Text = "";
                FocusHelper.FocusBeginInvoke(FocusableItem);
            }
        }
    }
}
