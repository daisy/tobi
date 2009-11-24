using System;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Events;
using Tobi.Common.Validation;

namespace Tobi.Plugin.Validator
{
    /// <summary>
    /// The view passively waits for validation refreshes and displays a list with clickable items.
    /// </summary>
    [Export(typeof(ValidatorPaneView)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class ValidatorPaneView : IPartImportsSatisfiedNotification
    {
#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied()
#pragma warning restore 1591
        {
            //#if DEBUG
            //            Debugger.Break();
            //#endif
        }

        private readonly IEventAggregator m_EventAggregator;
        private readonly ILoggerFacade m_Logger;

        private readonly Validator m_Validator;

        [ImportingConstructor]
        public ValidatorPaneView(
            ILoggerFacade logger,
            IEventAggregator eventAggregator,
            [Import(typeof(Validator), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = false, AllowDefault = false)]
            Validator validator
            )
        {
            m_EventAggregator = eventAggregator;
            m_Logger = logger;

            m_Validator = validator;
            m_Validator.ValidatorStateRefreshed += OnValidatorStateRefreshed;

            ValidationItems = new ObservableCollection<ValidationItem>();
            resetValidationItems(m_Validator);

            DataContext = this;
            InitializeComponent();
        }

        public ObservableCollection<ValidationItem> ValidationItems { get; set; }

        private void resetValidationItems(Validator validator)
        {
            ValidationItems.Clear();
            
            if (validator.ValidationItems == null) // metadataValidator.IsValid == true
            {
                return;
            }

            foreach (var validationItem in validator.ValidationItems)
            {
                ValidationItems.Add(validationItem);
            }
        }

        private void OnValidatorStateRefreshed(object sender, ValidatorStateRefreshedEventArgs e)
        {
            resetValidationItems((Validator)e.Validator);
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
