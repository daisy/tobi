using System.ComponentModel.Composition;
using System.Windows;
using Tobi.Common.Validation;


namespace Tobi.Plugin.Validator.Metadata
{
    [Export(ValidationDataTemplateProperties.TypeIdentifier, typeof(ResourceDictionary)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class MetadataValidationItemTemplate : ResourceDictionary
    {
        public MetadataValidationItemTemplate()
        {
            InitializeComponent();
        }
    }
}
