using System;
using System.Diagnostics;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Unity;
using Tobi.Common.Validation;

namespace Tobi.Modules.Validator
{
    /// <summary>
    /// Interaction logic for ValidatorPaneView.xaml
    /// </summary>
    [Export(typeof(ValidatorPaneView)), PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class ValidatorPaneView : IPartImportsSatisfiedNotification
    {
        public ObservableCollection<ValidationItem> ValidationItems { get; set; }

        protected IEventAggregator EventAggregator { get; private set; }
        protected ILoggerFacade Logger { get; private set; }
        protected IUnityContainer Container { get; private set; }

        private readonly Validator Validator;

        [ImportingConstructor]
        public ValidatorPaneView(Validator validator, IUnityContainer container, IEventAggregator eventAggregator, ILoggerFacade logger)
        {
            Container = container;
            EventAggregator = eventAggregator;
            Logger = logger;

            Validator = validator;
            Validator.ValidatorStateRefreshed += OnValidatorStateRefreshed;

            ValidationItems = new ObservableCollection<ValidationItem>();

            resetValidationItems(Validator);

            DataContext = this;
            InitializeComponent();
        }


        private void resetValidationItems(Validator metadataValidator)
        {
            ValidationItems.Clear();
            
            if (metadataValidator.ValidationItems == null) // metadataValidator.IsValid == true
            {
                return;
            }

            foreach (var validationItem in metadataValidator.ValidationItems)
            {
                ValidationItems.Add(validationItem);
            }
        }

        private void OnValidatorStateRefreshed(object sender, ValidatorStateRefreshedEventArgs e)
        {
            resetValidationItems((Validator)e.Validator);
        }

        public void OnImportsSatisfied()
        {
//#if DEBUG
//            Debugger.Break();
//#endif
        }
    }

    public class SeverityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "";
            var severity = (ValidationSeverity)value;
            return severity == ValidationSeverity.Error;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException
                ("NO !");
        }
    }
}
