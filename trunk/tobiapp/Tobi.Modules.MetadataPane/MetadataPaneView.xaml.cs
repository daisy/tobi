using Microsoft.Practices.Composite.Logging;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using urakawa.metadata;

namespace Tobi.Modules.MetadataPane
{
    /// <summary>
    /// Interaction logic for MetadataPaneView.xaml
    /// The backing ViewModel is injected in the constructor ("passive" view design pattern)
    /// </summary>
    public partial class MetadataPaneView : IMetadataPaneView
    {
        #region Construction

        public MetadataPaneViewModel ViewModel { get; private set; }

        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        public MetadataPaneView(MetadataPaneViewModel viewModel)
        {
            ViewModel = viewModel;
            ViewModel.SetView(this);

            ViewModel.Logger.Log("MetadataPaneView.ctor", Category.Debug, Priority.Medium);

            DataContext = ViewModel;

            InitializeComponent();
        }

        #endregion Construction
    }


   
}

//if this class is in the Tobi.Modules.MetadataPane namespace, the XAML doesn't "see" it
//there must be an easy fix, but for today ... 
namespace Frustration
{
    public class MetadataTypeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DefaultTemplate { get; set; }
        public DataTemplate PrettyTemplate { get; set; }
        public DataTemplate DateTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            System.Diagnostics.Debug.Assert(item is Metadata);

            Metadata metadata= (Metadata)item;
            if (metadata.Name == "dc:Date")
                return DateTemplate;
            else if (metadata.Name == "dc:Language")
                return PrettyTemplate;
            else
                return DefaultTemplate;
        }
    }

    //YYYY-MM-DD is the required format
    public class DateValidationRule : ValidationRule
    {
        public string ErrorMessage { get; set;}

        public override ValidationResult Validate(object obj, System.Globalization.CultureInfo cultureInfo)
        {
            ValidationResult result = new ValidationResult(true, null);

            string date = (string) obj;
            string[] dateArray = date.Split('-');
            if (dateArray.Length != 3)
            {
                result = new ValidationResult(false, ErrorMessage);
                return result;
            }
            string year = dateArray[0];
            string month = dateArray[1];
            string day = dateArray[2];

            try
            {
                DateTime testDate = new DateTime(
                    Convert.ToInt32(year), Convert.ToInt32(month), Convert.ToInt32(day));
            }
            catch
            {
                result = new ValidationResult(false, ErrorMessage);
            }
            return result;
        }
    }



}