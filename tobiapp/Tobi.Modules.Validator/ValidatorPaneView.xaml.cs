using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tobi.Common;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Unity;

namespace Tobi.Modules.Validator
{
    /// <summary>
    /// Interaction logic for ValidatorPaneView.xaml
    /// </summary>
    public partial class ValidatorPaneView
    {
        [ImportMany(typeof(IValidator))]
        public IEnumerable<IValidator> Validators { get; set;}
        public ObservableCollection<ValidationItem> ReportResults { get; set;}
        public RichDelegateCommand CommandShowValidator { get; private set; }

        protected IEventAggregator EventAggregator { get; private set; }
        public ILoggerFacade Logger { get; private set; }
        public IUnityContainer Container { get; private set; }
        
        
        private void initializeCommands()
        {
            Logger.Log("ValidatorPaneView.initializeCommands", Category.Debug, Priority.Medium);

            var shellPresenter = Container.Resolve<IShellPresenter>();

            CommandShowValidator = new RichDelegateCommand(
                "Show Validator",
                "",
                new KeyGesture(Key.V, ModifierKeys.Shift | ModifierKeys.Control),
                shellPresenter.LoadGnomeNeuIcon("preferences-user-information"),
                ShowDialog,
                CanShowDialog);

            shellPresenter.RegisterRichCommand(CommandShowValidator);

            var toolbars = Container.Resolve<IToolBarsView>();
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
            Logger.Log("ValidatorPaneView.ShowDialog", Category.Debug, Priority.Medium);

            //do the validation!!!
            Validate();

            //show the results of the validation!!!
            var shellPresenter_ = Container.Resolve<IShellPresenter>();
            var windowPopup = new PopupModalWindow(shellPresenter_,
                                                   UserInterfaceStrings.EscapeMnemonic(
                                                   "Validator"),
                                                   this,
                                                   PopupModalWindow.DialogButtonsSet.Close,
                                                   PopupModalWindow.DialogButton.Close,
                                                   true, 700, 400);
            windowPopup.ShowFloating(null);
            
        }

        public ValidatorPaneView(IUnityContainer container, IEventAggregator eventAggregator, ILoggerFacade logger)
        {
            EventAggregator = eventAggregator;
            Logger = logger;
            Container = container;
            initializeCommands();
            DataContext = this;
            InitializeComponent();
        }

        //run all the validator plugins
        public bool Validate()
        {
            bool isValid = true;
            ReportResults.Clear();
            foreach (IValidator v in Validators)
            {
                isValid = isValid && v.Validate();
                if (!isValid)
                {
                    foreach (ValidationItem item in v.ValidationItems)
                    {
                        ReportResults.Add(item);
                    }
                }
            }
            return isValid;
        }
    }
}
