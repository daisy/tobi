using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using Tobi.Common;
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
        [ImportMany(typeof(IValidator))]
        public IEnumerable<IValidator> Validators { get; set; }

        public ObservableCollection<ValidationItem> ValidationItems { get; set; }

        protected IEventAggregator EventAggregator { get; private set; }
        protected ILoggerFacade Logger { get; private set; }
        protected IUnityContainer Container { get; private set; }
        
        [ImportingConstructor]
        public ValidatorPaneView(IUnityContainer container, IEventAggregator eventAggregator, ILoggerFacade logger)
        {
            Container = container;
            EventAggregator = eventAggregator;
            Logger = logger;

            ValidationItems = new ObservableCollection<ValidationItem>();

            DataContext = this;
            InitializeComponent();
        }

        private bool Validate()
        {
            bool isValid = true;
            ValidationItems.Clear();
            foreach (IValidator v in Validators)
            {
                isValid = isValid && v.Validate();
                if (!isValid)
                {
                    foreach (ValidationItem item in v.ValidationItems)
                    {
                        ValidationItems.Add(item);
                    }
                }
            }
            return isValid;
        }

        private void OnClick_ValidateButton(object sender, RoutedEventArgs e)
        {
            Validate();
        }

        public void OnImportsSatisfied()
        {
#if DEBUG
            Debugger.Break();
#endif
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
