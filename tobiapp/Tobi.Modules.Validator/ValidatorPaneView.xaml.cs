using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.ComponentModel.Composition;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Events;
using Tobi.Common;
using Tobi.Common.UI;
using Tobi.Common.UI.XAML;
using Tobi.Common.Validation;
using urakawa.core;

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

        private readonly ValidatorAggregator m_ValidatorAggregator;
        //public ValidatorAggregator ValidatorAggregator { get; private set;}

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

            m_ValidatorAggregator = validator;
            
            foreach (ResourceDictionary dict in resourceDictionaries)
            {
                Application.Current.Resources.MergedDictionaries.Add(dict);
            }

            DataContext = m_ValidatorAggregator;
            //DataContext = this;
            
            InitializeComponent();
            
            //foreach (var validator in m_ValidatorAggregator.Validators)
            //{
                
            //}

            m_EventAggregator.GetEvent<TreeNodeSelectionChangedEvent>().Subscribe(OnTreeNodeSelectionChanged, TreeNodeSelectionChangedEvent.THREAD_OPTION);
        }

        private void OnTreeNodeSelectionChanged(Tuple<Tuple<TreeNode, TreeNode>, Tuple<TreeNode, TreeNode>> obj)
        {
            Tuple<TreeNode, TreeNode> newTreeNodeSelection = obj.Item2;
            TreeNode treeNode = newTreeNodeSelection.Item2 ?? newTreeNodeSelection.Item1;
            SelectItemInListFromDocumentNodeSelection(treeNode);
        }

        private void ValidationItemsListBox_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var list = sender as ListBox;
            var validationItem = list.SelectedItem as ValidationItem;
            validationItem.TakeAction();
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
                MessageBox.Show(Tobi_Plugin_Validator_Lang.ContentsCopied);
            }
            catch (Exception ex)
            {
                Debug.Fail(string.Format("Clipboad exception: {0}", ex.Message));
            }
        }

        public void SelectItemInListFromDocumentNodeSelection(object target)
        {
            if (target == null) return;

            //get the visible tab
            IValidator validator = (IValidator) Tabs.SelectedItem;
            if (validator == null) return;

            ValidationItem selection = null;
            //look for a validationitem that has our target as its error.Target
            foreach (ValidationItem item in validator.ValidationItems)
            {
                if (target is TreeNode && item is ValidationItemWithTarget<TreeNode>)
                {
                    if ((item as ValidationItemWithTarget<TreeNode>).Target == target)
                    {
                        selection = item;
                        break;
                    }
                }
            }
            //select it
            if (selection != null)
            {
                SelectValidationItem(selection);
            }
        }

        private void SelectValidationItem(ValidationItem selection)
        {
            if (selection == null) return;

            DataTemplate template = Tabs.ContentTemplate;
            var contentPresenter = VisualLogicalTreeWalkHelper.FindObjectInVisualTreeWithMatchingType<ContentPresenter>(Tabs, null);
            var list = (ListBox)template.FindName("ValidationItemsListBox", contentPresenter);

            if (list != null)
            {
                list.SelectedItem = selection;
                list.ScrollIntoView(selection);
            }
        }

        private void OnClipboardCopyAllLinkClick(object sender, RoutedEventArgs e)
        {
            bool noErrors = true;
            string clipboardText = Tobi_Plugin_Validator_Lang.ClipboardListOfAll;
            IEnumerator<IValidator> enumerator = m_ValidatorAggregator.Validators.GetEnumerator();
            int validatorNumbering = 1;
            int totalErrorCount = 0;
            while(enumerator.MoveNext())
            {
                clipboardText += string.Format(Tobi_Plugin_Validator_Lang.ClipboardValidatorEntry, 
                    validatorNumbering, enumerator.Current.Name, enumerator.Current.Description);

                int errorNumbering = 1;

                foreach (ValidationItem item in enumerator.Current.ValidationItems)
                {
                    noErrors = false;
                    clipboardText += string.Format(Tobi_Plugin_Validator_Lang.ClipboardErrorEntry, 
                        validatorNumbering, errorNumbering, item.CompleteSummary);
                    errorNumbering++;
                    totalErrorCount++;
                }
                validatorNumbering++;

                if (noErrors)
                {
                    clipboardText += Tobi_Plugin_Validator_Lang.ClipboardNoErrors;
                }
                clipboardText += string.Format(Tobi_Plugin_Validator_Lang.ClipboardEndOfValidatorReport, enumerator.Current.Name);

            }
            clipboardText += string.Format(Tobi_Plugin_Validator_Lang.ClipboardTotalErrorCount, totalErrorCount);

            try
            {
                //this raises an exception the first time it is used
                Clipboard.SetText(clipboardText);
                MessageBox.Show(Tobi_Plugin_Validator_Lang.ContentsCopied);
            }
            catch (Exception ex)
            {
                Debug.Fail(string.Format("Clipboad exception: {0}", ex.Message));
            }
        }

        private void Tabs_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsVisible == false) return;
            if (e.OriginalSource != Tabs) return;
            //SelectFirstInValidationItemsList();
        }

        private void SelectFirstInValidationItemsList()
        {
            DataTemplate template = Tabs.ContentTemplate;
            var contentPresenter = VisualLogicalTreeWalkHelper.FindObjectInVisualTreeWithMatchingType<ContentPresenter>(Tabs, null);
            if (contentPresenter == null) return;
            var list = (ListBox)template.FindName("ValidationItemsListBox", contentPresenter);

            if (list == null) return;

            if (list.Items.Count > 0)
            {
                list.SelectedItem = list.Items[0];
            }
        }
    }

    [ValueConversion(typeof(ValidationSeverity), typeof(bool))]
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
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
