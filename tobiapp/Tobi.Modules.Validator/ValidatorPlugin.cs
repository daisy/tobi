using System;
using System.ComponentModel.Composition;
using System.Windows.Input;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;

namespace Tobi.Plugin.Validator
{
    ///<summary>
    /// The validation framework includes a top-level UI to display all publication errors as they are detected.
    ///</summary>
    public sealed class ValidatorPlugin : AbstractTobiPlugin
    {
        private readonly IShellView m_ShellView;
        private readonly IUrakawaSession m_UrakawaSession;

        private readonly ValidatorPaneView m_ValidatorPaneView;

        private readonly ILoggerFacade m_Logger;

        private readonly IEventAggregator m_EventAggregator;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        ///<param name="shellView">normally obtained from the MEF composition container, it's a Tobi-specific service</param>
        ///<param name="urakawaSession">normally obtained from the MEF composition container, it's a Tobi-specific service</param>
        ///<param name="view">normally obtained from the MEF composition container, it's a Tobi-specific service</param>
        [ImportingConstructor]
        public ValidatorPlugin(
            ILoggerFacade logger,
            IEventAggregator eventAggregator,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession urakawaSession,
            [Import(typeof(ValidatorPaneView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            ValidatorPaneView view)
        {
            m_Logger = logger;
            m_ShellView = shellView;
            m_UrakawaSession = urakawaSession;

            m_EventAggregator = eventAggregator;

            m_ValidatorPaneView = view;

            m_EventAggregator.GetEvent<ValidationReportRequestEvent>().Subscribe(
                obj => CommandShowValidator.Execute(),
                ValidationReportRequestEvent.THREAD_OPTION);


            CommandShowValidator = new RichDelegateCommand(
                Tobi_Plugin_Validator_Lang.CmdValidationCheck_ShortDesc,                                                // TODO LOCALIZE ValidationCheck
                Tobi_Plugin_Validator_Lang.CmdValidationCheck_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadGnomeGionIcon(@"Gion_application-certificate"),
                ShowDialog,
                CanShowDialog,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_DisplayValidator));

            m_ShellView.RegisterRichCommand(CommandShowValidator);

            //m_Logger.Log(@"ValidatorPlugin init", Category.Debug, Priority.Medium);
        }

        private readonly RichDelegateCommand CommandShowValidator;

        //private int m_ToolBarId_1;
        protected override void OnToolBarReady()
        {
            //m_ToolBarId_1 = m_ToolBarsView.AddToolBarGroup(new[] { CommandShowValidator }, PreferredPosition.Any);

            m_Logger.Log(@"ValidatorPlugin commands pushed to toolbar", Category.Debug, Priority.Medium);
        }

        private int m_MenuBarId_1;
        protected override void OnMenuBarReady()
        {
            m_MenuBarId_1 = m_MenuBarView.AddMenuBarGroup(
                Tobi_Common_Lang.Menu_Tools, PreferredPosition.Any, true,
                null, PreferredPosition.First, true,
                new[] { CommandShowValidator });

            m_Logger.Log(@"ValidatorPlugin commands pushed to menubar", Category.Debug, Priority.Medium);
        }

        public override void Dispose()
        {
            if (m_ToolBarsView != null)
            {
                //m_ToolBarsView.RemoveToolBarGroup(m_ToolBarId_1);

                m_Logger.Log(@"ValidatorPlugin commands removed from toolbar", Category.Debug, Priority.Medium);
            }

            if (m_MenuBarView != null)
            {
                m_MenuBarView.RemoveMenuBarGroup(Tobi_Common_Lang.Menu_Tools, m_MenuBarId_1);

                m_Logger.Log(@"ValidatorPlugin commands removed from menubar", Category.Debug, Priority.Medium);
            }
        }

        public override string Name
        {
            get { return Tobi_Plugin_Validator_Lang.ValidatorPlugin_Name; }    // TODO LOCALIZE ValidatorPlugin_Name
        }

        public override string Description
        {
            get { return Tobi_Plugin_Validator_Lang.ValidatorPlugin_Description; }    // TODO LOCALIZE  ValidatorPlugin_Description
        }

        private bool m_DialogIsShowing;

        private bool CanShowDialog()
        {
            return !m_DialogIsShowing
                && m_UrakawaSession.DocumentProject != null;
        }

        private void ShowDialog()
        {
            m_Logger.Log("ValidatorPlugin.ShowDialog", Category.Debug, Priority.Medium);

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Validator_Lang.CmdValidationCheck_ShortDesc),
                                                   m_ValidatorPaneView,
                                                   PopupModalWindow.DialogButtonsSet.Close,
                                                   PopupModalWindow.DialogButton.Close,
                                                   true, 700, 450, null, 0);
            windowPopup.ShowFloating(null);

            windowPopup.Closed += (o, args) =>
                                      {
                                          m_DialogIsShowing = false;

                                          // This line is not strictly necessary, but this way we make sure the CanShowDialog method (CanExecute) is called to refresh the command visual enabled/disabled status.
                                          CommandManager.InvalidateRequerySuggested();
                                      };

            m_DialogIsShowing = true;
        }
    }
}
