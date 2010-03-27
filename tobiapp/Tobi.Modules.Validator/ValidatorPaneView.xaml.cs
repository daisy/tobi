using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Windows.Documents;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Events;
using Tobi.Common.UI.XAML;
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

        public ValidatorAggregator ValidatorAggregator { get; private set;}

        [ImportingConstructor]
        public ValidatorPaneView(
            ILoggerFacade logger,
            IEventAggregator eventAggregator,
            [Import(typeof(ValidatorAggregator), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = false, AllowDefault = false)]
            ValidatorAggregator validator, 
            [ImportMany(ValidationDataTemplateProperties.TypeIdentifier, typeof(ResourceDictionary), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = false)]
            IEnumerable<ResourceDictionary> resourceDictionaries)
        {
            m_EventAggregator = eventAggregator;
            m_Logger = logger;

            ValidatorAggregator = validator;
            ValidatorAggregator.ValidatorStateRefreshed += OnValidatorStateRefreshed;

            ValidationItems = new ObservableCollection<ValidationItem>();
            resetValidationItems(ValidatorAggregator);
            
            foreach (ResourceDictionary dict in resourceDictionaries)
            {
                Application.Current.Resources.MergedDictionaries.Add(dict);
            }
            
            DataContext = this;
            
            InitializeComponent();
        }

        public ObservableCollection<ValidationItem> ValidationItems
        { get;set;}
        
        private void resetValidationItems(ValidatorAggregator validator)
        {
            ValidationItems.Clear();
            
            if (validator.ValidationItems == null)
            {
                return;
            }

            foreach (var validationItem in validator.ValidationItems)
            {
                if (validationItem != null)
                    ValidationItems.Add(validationItem);
            }
            
        }
        
        private void OnValidatorStateRefreshed(object sender, ValidatorStateRefreshedEventArgs e)
        {
            resetValidationItems((ValidatorAggregator)e.Validator);
        }
        

        private void OnClipboardLinkClick(object sender, RoutedEventArgs e)
        {
            var obj = sender as Hyperlink;
            var err = obj.DataContext as ValidationItem;
            if (err == null) return;
            try
            {
                //this raises an exception the first time it is used
                Clipboard.SetText(err.CompleteSummary);
            }
            catch (Exception ex)
            {
                Debug.Fail(string.Format("Clipboad exception: {0}", ex.Message));
            }
        }

        private void OnListItemDoubleClick(object sender, RoutedEventArgs e)
        {
            var list = sender as ListBox;
            var validationItem = list.SelectedItem as ValidationItem;
            validationItem.TakeAction();
        }
    }


    [ValueConversion(typeof(ValidationSeverity), typeof(ValidationSeverity))]
    public class SeverityConverter : ValueConverterMarkupExtensionBase<SeverityConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "";
            var severity = (ValidationSeverity)value;
            return severity == ValidationSeverity.Error;
        }
    }

    [ValueConversion(typeof(ValidationItem), typeof(ValidationItem))]
    public class TestConverter : ValueConverterMarkupExtensionBase<TestConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;
            if (!(value is ValidationItem))
                return null;
            return value as ValidationItem;
        }
    }

    [ValueConversion(typeof(ValidationItem), typeof(Visibility))]
    public class VisibilityConverter : ValueConverterMarkupExtensionBase<VisibilityConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value == null ? Visibility.Hidden : Visibility.Visible;
        }
    }
}
