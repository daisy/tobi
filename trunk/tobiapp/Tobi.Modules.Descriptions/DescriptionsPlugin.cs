using System;
using System.ComponentModel.Composition;
using System.Windows.Input;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa.core;

namespace Tobi.Plugin.Descriptions
{
    ///<summary>
    /// The application settings / configuration / options includes a top-level UI to enable user edits
    ///</summary>
    public sealed class DescriptionsPlugin : AbstractTobiPlugin
    {
        private readonly IUnityContainer m_Container;
        private readonly IShellView m_ShellView;

        //private readonly DescriptionsView m_DescriptionsView;

        private readonly ILoggerFacade m_Logger;
        
        private readonly IUrakawaSession m_UrakawaSession;
        private readonly IEventAggregator m_EventAggregator;
        private readonly DescriptionsView m_DescriptionsView;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        ///<param name="container">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        ///<param name="shellView">normally obtained from the MEF composition container, it's a Tobi-specific service</param>
        [ImportingConstructor]
        public DescriptionsPlugin(
            ILoggerFacade logger,
            IUnityContainer container,
            IEventAggregator eventAggregator,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession session,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView,
            [Import(typeof(IDescriptionsView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            DescriptionsView view
            )
        {
            m_Logger = logger;
            m_Container = container;
            m_ShellView = shellView;
            m_DescriptionsView = view;

            m_UrakawaSession = session;
            m_EventAggregator = eventAggregator;

            CommandShowDescriptions = new RichDelegateCommand(
                Tobi_Plugin_Descriptions_Lang.CmdEditDescriptions_ShortDesc, 
                Tobi_Plugin_Descriptions_Lang.CmdEditDescriptions_LongDesc, 
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon(@"edit-find-replace"),
                ShowDialog,
                CanShowDialog,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_EditDescriptions));

            m_ShellView.RegisterRichCommand(CommandShowDescriptions);

            //m_Logger.Log(@"DescriptionsPlugin init", Category.Debug, Priority.Medium);
        }
    
        private readonly RichDelegateCommand CommandShowDescriptions;

        //private int m_ToolBarId_1;
        protected override void OnToolBarReady()
        {
            //m_ToolBarId_1 = m_ToolBarsView.AddToolBarGroup(new[] { CommandShowDescriptions }, PreferredPosition.Last);
            //m_Logger.Log(@"DescriptionsPlugin commands pushed to toolbar", Category.Debug, Priority.Medium);
        }

        private int m_MenuBarId_1;
        protected override void OnMenuBarReady()
        {
            m_MenuBarId_1 = m_MenuBarView.AddMenuBarGroup(
                Tobi_Common_Lang.Menu_Edit, PreferredPosition.Last, true,
                null, PreferredPosition.Last, true,
                new[] { CommandShowDescriptions });

            m_Logger.Log(@"DescriptionsPlugin commands pushed to menubar", Category.Debug, Priority.Medium);
        }

        public override void Dispose()
        {
            if (m_ToolBarsView != null)
            {
                //m_ToolBarsView.RemoveToolBarGroup(m_ToolBarId_1);
                //m_Logger.Log(@"DescriptionsPlugin commands removed from toolbar", Category.Debug, Priority.Medium);
            }

            if (m_MenuBarView != null)
            {
                m_MenuBarView.RemoveMenuBarGroup(Tobi_Common_Lang.Menu_Edit, m_MenuBarId_1);

                m_Logger.Log(@"DescriptionsPlugin commands removed from menubar", Category.Debug, Priority.Medium);
            }
        }

        public override string Name
        {
            get { return Tobi_Plugin_Descriptions_Lang.DescriptionsPlugin_Name; }
        }

        public override string Description
        {
            get { return Tobi_Plugin_Descriptions_Lang.DescriptionsPlugin_Description; }
        }

        private bool CanShowDialog()
        {
            if (m_UrakawaSession.DocumentProject == null) return false;

            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            return
                //m_TextElementForEdit != null ||
                node != null;
            //&& node.GetXmlElementQName().LocalName == "img";
        }

        private void ShowDialog()
        {
            m_Logger.Log("DescriptionsPlugin.ShowDialog", Category.Debug, Priority.Medium);

            //var view = m_Container.Resolve<DescriptionsView>();
            m_DescriptionsView.Popup();
        }
    }
}
