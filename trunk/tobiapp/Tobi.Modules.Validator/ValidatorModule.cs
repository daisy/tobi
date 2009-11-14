using System.Windows.Input;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Composite.UnityExtensions;
using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;

namespace Tobi.Modules.Validator
{
    ///<summary>
    /// The top-level validator engine that aggregates other plugin-contributed concrete validators.
    ///</summary>
    public class ValidatorModule : IModule
    {
        private readonly RichDelegateCommand CommandShowValidator;


        private readonly IUnityContainer Container;
        private readonly IEventAggregator EventAggregator;
        private readonly ILoggerFacade Logger;
        
        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        public ValidatorModule(IUnityContainer container, IEventAggregator eventAggregator, ILoggerFacade logger)
        {
            Container = container;
            EventAggregator = eventAggregator;
            Logger = logger;


            var shellPresenter = Container.Resolve<IShellPresenter>();

            CommandShowValidator = new RichDelegateCommand(
                "Validation Check",
                "",
                new KeyGesture(Key.V, ModifierKeys.Shift | ModifierKeys.Control),
                shellPresenter.LoadGnomeNeuIcon("Neu_preferences-user-information"),
                ShowDialog,
                CanShowDialog);

            shellPresenter.RegisterRichCommand(CommandShowValidator);
        }

        ///<summary>
        /// Registers the <see cref="ValidatorPaneView"/> into the Dependency Injection container
        ///</summary>
        public void Initialize()
        {
            //Container.RegisterType<ValidatorPaneView>(new ContainerControlledLifetimeManager());

            Logger.Log("ValidatorModule.Initialize", Category.Debug, Priority.Medium);

            var toolbars = Container.TryResolve<IToolBarsView>();
            if (toolbars != null)
            {
                int uid = toolbars.AddToolBarGroup(new[] { CommandShowValidator });
            }
        }

        bool CanShowDialog()
        {
            var session = Container.Resolve<IUrakawaSession>();
            return session.DocumentProject != null && session.DocumentProject.Presentations.Count > 0;
        }

        void ShowDialog()
        {
            Logger.Log("ValidatorModule.ShowDialog", Category.Debug, Priority.Medium);

            var view = Container.Resolve<ValidatorPaneView>();
            var shellPresenter_ = Container.Resolve<IShellPresenter>();
            var windowPopup = new PopupModalWindow(shellPresenter_,
                                                   UserInterfaceStrings.EscapeMnemonic("Validation Checker"),
                                                   view,
                                                   PopupModalWindow.DialogButtonsSet.Close,
                                                   PopupModalWindow.DialogButton.Close,
                                                   true, 700, 400);
            windowPopup.ShowFloating(null);
        }
    }
}
