using System.ComponentModel.Composition;
using System.Windows.Input;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;

namespace Tobi.Plugin.Validator
{
    ///<summary>
    /// The validation framework includes a top-level UI to display all publication errors as they are detected.
    ///</summary>
    [Export(typeof(ITobiPlugin)), PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class ValidatorPlugin : AbstractTobiPlugin, IPartImportsSatisfiedNotification
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
        private readonly IShellView m_ShellView;
        private readonly IUrakawaSession m_UrakawaSession;
        
        private readonly ValidatorPaneView m_ValidatorPaneView;

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
                "Validation Check",
                "",
                new KeyGesture(Key.V, ModifierKeys.Shift | ModifierKeys.Control),
                m_ShellView.LoadGnomeNeuIcon(@"Neu_preferences-user-information"),
                ShowDialog,
                CanShowDialog);

            m_ShellView.RegisterRichCommand(CommandShowValidator);

            m_Logger.Log(@"ValidatorPlugin init", Category.Debug, Priority.Medium);
        }
    
        private readonly RichDelegateCommand CommandShowValidator;

        private int m_ToolBarId_1;
        private bool m_ToolBarCommandsDone;
        private void tryToolbarCommands()
        {
            if (!m_ToolBarCommandsDone && m_ToolBarsView != null)
            {
                m_ToolBarId_1 = m_ToolBarsView.AddToolBarGroup(new[] { CommandShowValidator });

                m_ToolBarCommandsDone = true;

                m_Logger.Log(@"ValidatorPlugin commands pushed to toolbar", Category.Debug, Priority.Medium);
            }
        }

        private int m_MenuBarId_1;
        private bool m_MenuBarCommandsDone;
        private void tryMenubarCommands()
        {
            if (!m_MenuBarCommandsDone && m_MenuBarView != null)
            {
                m_MenuBarId_1 = m_MenuBarView.AddMenuBarGroup(RegionNames.MenuBar_Tools, new[] { CommandShowValidator }, null, false);
                
                m_MenuBarCommandsDone = true;

                m_Logger.Log(@"ValidatorPlugin commands pushed to menubar", Category.Debug, Priority.Medium);
            }
        }

        public override void Dispose()
        {
            if (m_ToolBarCommandsDone)
            {
                m_ToolBarsView.RemoveToolBarGroup(m_ToolBarId_1);

                m_ToolBarCommandsDone = false;

                m_Logger.Log(@"ValidatorPlugin commands removed from toolbar", Category.Debug, Priority.Medium);
            }

            if (m_MenuBarCommandsDone)
            {
                m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_Tools, m_MenuBarId_1);

                m_MenuBarCommandsDone = false;

                m_Logger.Log(@"ValidatorPlugin commands removed from menubar", Category.Debug, Priority.Medium);
            }
        }

        public override string Name
        {
            get { return @"Publication Validator."; }
        }

        public override string Description
        {
            get { return @"A framework to validate the data model of an authored Urakawa SDK project."; }
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
