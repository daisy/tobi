using Microsoft.Practices.Composite.Logging;
using System.Windows;
using System.Windows.Controls;
using urakawa.metadata;
using System.Collections.Generic;

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
        public DataTemplate OptionalStringTemplate { get; set; }
        public DataTemplate OptionalDateTemplate { get; set; }
        public DataTemplate RequiredStringTemplate { get; set; }
        public DataTemplate RequiredDateTemplate { get; set; }
        public DataTemplate ReadOnlyTemplate { get; set;}
        public DataTemplate DefaultTemplate { get; set; }

        private static readonly List<string> _requiredDateFields = new List<string>
                                                                       {
                                                                           "dc:Date"
                                                                       };

        private static readonly List<string> _optionalDateFields = new List<string>
                                                                       {
                                                                           "dtb:sourceDate",
                                                                           "dtb:producedDate",
                                                                           "dtb:revisionDate"
                                                                       };

        private static readonly List<string> _requiredStringFields = new List<string>
                                                                         {
                                                                             "dc:Title",
                                                                             "dc:Publisher",
                                                                             "dc:Identifier",
                                                                             "dc:Language",
                                                                             "dtb:totalTime"
                                                                         };

        private static readonly List<string> _optionalStringFields = new List<string>
                                                                         {
                                                                             "dc:Creator",
                                                                             "dc:Subject",
                                                                             "dc:Description",
                                                                             "dc:Contributor",
                                                                             "dc:Source",
                                                                             "dc:Relation",
                                                                             "dc:Coverage",
                                                                             "dc:Rights",
                                                                             "dtb:sourceEdition",
                                                                             "dtb:sourcePublisher",
                                                                             "dtb:sourceRights",
                                                                             "dtb:sourceTitle",
                                                                             "dtb:narrator",
                                                                             "dtb:producer",
                                                                             "dtb:revision",
                                                                             "dtb:revisionDescription"
                                                                         };

        private static readonly List<string> _readonlyStringFields = new List<string>
                                                                         {
                                                                             "dc:Format",
                                                                             "dtb:multimediaType",
                                                                             "dtb:multimediaContent",
                                                                             "dtb:totalTime",
                                                                             "dc:Type",
                                                                             "dtb:audioFormat"
                                                                         };

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            System.Diagnostics.Debug.Assert(item is Metadata);

            Metadata metadata = (Metadata)item;

            string res = _requiredDateFields.Find(s => s == metadata.Name);
            string res2 = _requiredStringFields.Find(s => s == metadata.Name);

            if (_requiredDateFields.Find(s => s == metadata.Name) != null)
                return RequiredDateTemplate;
            else if (_requiredStringFields.Find(s => s == metadata.Name) != null)
                return RequiredStringTemplate;
            else if (_optionalDateFields.Find(s => s == metadata.Name) != null)
                return OptionalDateTemplate;
            else if (_optionalStringFields.Find(s => s == metadata.Name) != null)
                return OptionalStringTemplate;
            else if (_readonlyStringFields.Find(s => s == metadata.Name) != null)
                return ReadOnlyTemplate;
            else
                return DefaultTemplate;
        }
    }
}