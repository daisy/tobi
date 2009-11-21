using System.Windows.Input;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Composite.Presentation.Events;
using Microsoft.Practices.Composite.UnityExtensions;
using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa;

namespace Tobi.Plugin.Validator
{
    ///<summary>
    /// The top-level validator engine that aggregates other plugin-contributed concrete validators.
    ///</summary>
    public class ValidatorModule : IModule
    {
        private RichDelegateCommand CommandShowValidator;

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
        }

        private void OnProjectLoaded(Project project)
        {
            EventAggregator.GetEvent<ProjectLoadedEvent>().Unsubscribe(OnProjectLoaded);

            Container.Resolve<Validator>();
        }

        public void Initialize()
        {
            //Container.RegisterType<ValidatorPaneView>(new ContainerControlledLifetimeManager());

            EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(OnProjectLoaded, ThreadOption.UIThread);

            var shellView = Container.Resolve<IShellView>();

            CommandShowValidator = new RichDelegateCommand(
                "Validation Check",
                "",
                new KeyGesture(Key.V, ModifierKeys.Shift | ModifierKeys.Control),
                shellView.LoadGnomeNeuIcon("Neu_preferences-user-information"),
                ShowDialog,
                CanShowDialog);

            shellView.RegisterRichCommand(CommandShowValidator);

            Logger.Log("ValidatorModule.Initialize", Category.Debug, Priority.Medium);

            var toolbars = Container.TryResolve<IToolBarsView>();
            if (toolbars != null)
            {
                int uid = toolbars.AddToolBarGroup(new[] { CommandShowValidator });
            }
        }

        private bool CanShowDialog()
        {
            var session = Container.Resolve<IUrakawaSession>();
            return session.DocumentProject != null && session.DocumentProject.Presentations.Count > 0;
        }

        private void ShowDialog()
        {
            Logger.Log("ValidatorModule.ShowDialog", Category.Debug, Priority.Medium);

            var view = Container.Resolve<ValidatorPaneView>();
            var shellView_ = Container.Resolve<IShellView>();
            var windowPopup = new PopupModalWindow(shellView_,
                                                   UserInterfaceStrings.EscapeMnemonic("Validation Checker"),
                                                   view,
                                                   PopupModalWindow.DialogButtonsSet.Close,
                                                   PopupModalWindow.DialogButton.Close,
                                                   true, 700, 400);
            windowPopup.ShowFloating(null);
        }
    }
}
