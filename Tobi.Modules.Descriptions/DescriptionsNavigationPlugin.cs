using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Regions;
using Tobi.Common;
using Tobi.Common.UI;

namespace Tobi.Plugin.Descriptions
{
    public sealed class DescriptionsNavigationPlugin : AbstractTobiPlugin
    {
        private readonly IRegionManager m_RegionManager;

        private readonly IUrakawaSession m_UrakawaSession;
        private readonly IShellView m_ShellView;

        private readonly DescriptionsNavigationView m_DescriptionsNavView;
        private readonly DescriptionsNavigationViewModel m_DescriptionsNavViewModel;


        private readonly ILoggerFacade m_Logger;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        [ImportingConstructor]
        public DescriptionsNavigationPlugin(
            ILoggerFacade logger,
            IRegionManager regionManager,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession session,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView,
            [Import(typeof(DescriptionsNavigationView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            DescriptionsNavigationView pane,
            [Import(typeof(DescriptionsNavigationViewModel), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            DescriptionsNavigationViewModel viewModel
            )
        {
            m_Logger = logger;
            m_RegionManager = regionManager;

            m_UrakawaSession = session;
            m_ShellView = shellView;
            m_DescriptionsNavView = pane;
            m_DescriptionsNavViewModel = viewModel;

            // Remark: using direct access instead of delayed lookup (via the region registry)
            // generates an exception, because the region does not exist yet (see "parent" plugin constructor, RegionManager.SetRegionManager(), etc.)            

            m_RegionManager.RegisterNamedViewWithRegion(RegionNames.NavigationPaneTabs,
                new PreferredPositionNamedView
                {
                    m_viewInstance = m_DescriptionsNavView,
                    m_viewName = @"ViewOf_" + RegionNames.NavigationPaneTabs + @"_Descriptions",
                    m_viewPreferredPosition = PreferredPosition.Last
                });

            //m_RegionManager.RegisterViewWithRegion(RegionNames.NavigationPaneTabs, typeof(IDescriptionsNavigationView));

            //IRegion targetRegion = m_RegionManager.Regions[RegionNames.NavigationPaneTabs];
            //targetRegion.Add(m_DescriptionsNavView);
            //targetRegion.Activate(m_DescriptionsNavView);

            //m_Logger.Log(@"Navigation pane plugin initializing...", Category.Debug, Priority.Medium);
        }

        //private int m_ToolBarId_1;
        protected override void OnToolBarReady()
        {
            //m_ToolBarId_1 = m_ToolBarsView.AddToolBarGroup(new[] { m_DescriptionsNavView.CommandSwitchPhrasePrevious, m_DocView.CommandSwitchPhraseNext });

            m_Logger.Log(@"Navigation commands pushed to toolbar", Category.Debug, Priority.Medium);
        }

        protected override void OnMenuBarReady()
        {
            m_Logger.Log(@"Navigation commands pushed to menubar", Category.Debug, Priority.Medium);
        }

        public override void Dispose()
        {
            if (m_ToolBarsView != null)
            {
                //m_ToolBarsView.RemoveToolBarGroup(m_ToolBarId_1);

                m_Logger.Log(@"Navigation commands removed from toolbar", Category.Debug, Priority.Medium);
            }

            if (m_MenuBarView != null)
            {
                m_Logger.Log(@"Navigation commands removed from menubar", Category.Debug, Priority.Medium);
            }

            m_RegionManager.Regions[RegionNames.NavigationPaneTabs].Deactivate(m_DescriptionsNavView);
            m_RegionManager.Regions[RegionNames.NavigationPaneTabs].Remove(m_DescriptionsNavView);
        }

        public override string Name
        {
            get { return "Descriptions Navigator"; }    // TODO LOCALIZE
        }

        public override string Description
        {
            get { return "Navigation plugin for descriptions"; }     // TODO LOCALIZE
        }
    }
}