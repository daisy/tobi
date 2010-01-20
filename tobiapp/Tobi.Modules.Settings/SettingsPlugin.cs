using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows.Input;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;

namespace Tobi.Plugin.Settings
{
    ///<summary>
    /// The application settings / configuration / options includes a top-level UI to enable user edits
    ///</summary>
    [Export(typeof(ITobiPlugin)), PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class SettingsPlugin : AbstractTobiPlugin, IPartImportsSatisfiedNotification
    {
#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied()
#pragma warning restore 1591
        {
            //#if DEBUG
            //            Debugger.Break();
            //#endif

            // If the toolbar has been resolved, we can push our commands into it.
            tryToolbarCommands();

            // If the menubar has been resolved, we can push our commands into it.
            tryMenubarCommands();
        }

#pragma warning disable 649 // non-initialized fields

        [Import(typeof(IToolBarsView), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = true, AllowDefault = true)]
        private IToolBarsView m_ToolBarsView;

        [Import(typeof(IMenuBarView), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = true, AllowDefault = true)]
        private IMenuBarView m_MenuBarView;

#pragma warning restore 649

        private readonly ILoggerFacade m_Logger;
        private readonly IUnityContainer m_Container;
        private readonly IShellView m_ShellView;

        //private readonly SettingsView m_SettingsView;

        public readonly ISettingsAggregator m_SettingsAggregator;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        ///<param name="shellView">normally obtained from the MEF composition container, it's a Tobi-specific service</param>
        ///<param name="view">normally obtained from the MEF composition container, it's a Tobi-specific service</param>
        [ImportingConstructor]
        public SettingsPlugin(
            ILoggerFacade logger,
            IUnityContainer container,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView,
            //[Import(typeof(SettingsView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            //SettingsView view,
            [Import(typeof(ISettingsAggregator), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = false)]
            ISettingsAggregator settingsAggregator)
        {
            m_Logger = logger;
            m_Container = container;
            m_ShellView = shellView;

            //m_SettingsView = view;

            m_SettingsAggregator = settingsAggregator;

            CommandShowSettings = new RichDelegateCommand(
                "Application preferences",
                "Display an editor for application preferences",
                new KeyGesture(Key.P, ModifierKeys.Shift | ModifierKeys.Control | ModifierKeys.Alt),
                m_ShellView.LoadTangoIcon(@"preferences-system"),
                ShowDialog,
                CanShowDialog);

            m_ShellView.RegisterRichCommand(CommandShowSettings);

            m_Logger.Log(@"SettingsPlugin init", Category.Debug, Priority.Medium);
        }
    
        private readonly RichDelegateCommand CommandShowSettings;

        private int m_ToolBarId_1;
        private bool m_ToolBarCommandsDone;
        private void tryToolbarCommands()
        {
            if (!m_ToolBarCommandsDone && m_ToolBarsView != null)
            {
                m_ToolBarId_1 = m_ToolBarsView.AddToolBarGroup(new[] { CommandShowSettings }, PreferredPosition.Last);

                m_ToolBarCommandsDone = true;

                m_Logger.Log(@"SettingsPlugin commands pushed to toolbar", Category.Debug, Priority.Medium);
            }
        }

        private int m_MenuBarId_1;
        private bool m_MenuBarCommandsDone;
        private void tryMenubarCommands()
        {
            if (!m_MenuBarCommandsDone && m_MenuBarView != null)
            {
                m_MenuBarId_1 = m_MenuBarView.AddMenuBarGroup(RegionNames.MenuBar_Tools, null, new[] { CommandShowSettings }, PreferredPosition.Last, false);
                
                m_MenuBarCommandsDone = true;

                m_Logger.Log(@"SettingsPlugin commands pushed to menubar", Category.Debug, Priority.Medium);
            }
        }

        public override void Dispose()
        {
            if (m_ToolBarCommandsDone)
            {
                m_ToolBarsView.RemoveToolBarGroup(m_ToolBarId_1);

                m_ToolBarCommandsDone = false;

                m_Logger.Log(@"SettingsPlugin commands removed from toolbar", Category.Debug, Priority.Medium);
            }

            if (m_MenuBarCommandsDone)
            {
                m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_Tools, m_MenuBarId_1);

                m_MenuBarCommandsDone = false;

                m_Logger.Log(@"SettingsPlugin commands removed from menubar", Category.Debug, Priority.Medium);
            }
        }

        public override string Name
        {
            get { return @"Application preferences."; }
        }

        public override string Description
        {
            get { return @"A manager and user-interface editor for application preferences."; }
        }

        private bool m_DialogIsShowing;

        private bool CanShowDialog()
        {
            return !m_DialogIsShowing;
        }

        private void ShowDialog()
        {
            m_Logger.Log("SettingsPlugin.ShowDialog", Category.Debug, Priority.Medium);

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   UserInterfaceStrings.EscapeMnemonic(UserInterfaceStrings.Preferences),
                                                   m_Container.Resolve<SettingsView>(),
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Cancel,
                                                   true, 650, 500);

            m_SettingsAggregator.SaveAll(); // Not strictly necessary..but just to make double-sure we've got the current settings in persistent storage.

            windowPopup.ShowFloating(null);

            windowPopup.Closed += (o, args) =>
                                      {
                                          m_DialogIsShowing = false;

                                          // This line is not strictly necessary, but this way we make sure the CanShowDialog method (CanExecute) is called to refresh the command visual enabled/disabled status.
                                          CommandManager.InvalidateRequerySuggested();

                                          if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Ok)
                                          {
                                              m_SettingsAggregator.SaveAll();
                                          }
                                          else
                                          {
                                              m_SettingsAggregator.ReloadAll();
                                          }
                                      };

            m_DialogIsShowing = true;
        }
    }
}
