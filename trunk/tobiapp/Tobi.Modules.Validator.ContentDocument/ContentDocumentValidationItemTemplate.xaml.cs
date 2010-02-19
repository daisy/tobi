using System.ComponentModel.Composition;
using System.Windows;

namespace Tobi.Plugin.Validator.ContentDocument
{
    [Export(typeof(ResourceDictionary)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class ContentDocumentValidationItemTemplate : ResourceDictionary
    {
        public ContentDocumentValidationItemTemplate()
        {
            InitializeComponent();
        }
    }
}
