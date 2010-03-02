using System.ComponentModel.Composition;
using System.Windows.Input;
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
            
            m_ValidatorPaneView = view;

            CommandShowValidator = new RichDelegateCommand(
                "Validation Check",                                                // TODO LOCALIZE ValidationCheck
                "",
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
                RegionNames.MenuBar_Tools, PreferredPosition.Any, true,
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
                m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_Tools, m_MenuBarId_1);

                m_Logger.Log(@"ValidatorPlugin commands removed from menubar", Category.Debug, Priority.Medium);
            }
        }

        public override string Name
        {
            get { return @"Publication Validator."; }    // TODO LOCALIZE PublicationValidator
        }

        public override string Description
        {
            get { return @"A framework to validate the data model of an authored Urakawa SDK project."; }    // TODO LOCALIZE AFrameworkToValidateTheDataModelOfAnAuthoredUrakawaSDKProject
        }

        private bool m_DialogIsShowing;

        private bool CanShowDialog()
        {
            return !m_DialogIsShowing && m_UrakawaSession.DocumentProject != null && m_UrakawaSession.DocumentProject.Presentations.Count > 0;
        }

        private void ShowDialog()
        {
            m_Logger.Log("ValidatorPlugin.ShowDialog", Category.Debug, Priority.Medium);

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   UserInterfaceStrings.EscapeMnemonic("Validation Checker"),
                                                   m_ValidatorPaneView,
                                                   PopupModalWindow.DialogButtonsSet.Close,
                                                   PopupModalWindow.DialogButton.Close,
                                                   true, 700, 400);
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
