using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Tobi.Common.Validation;

namespace Tobi.Plugin.Validator.MissingAudio
{
    /// <summary>
    /// Interaction logic for MissingAudioValidationItemTemplate.xaml
    /// </summary>
    [Export(ValidationDataTemplateProperties.TypeIdentifier, typeof(ResourceDictionary)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class MissingAudioValidationItemTemplate : ResourceDictionary
    {
        public MissingAudioValidationItemTemplate()
        {
            InitializeComponent();
        }

        private void OnViewLinkClick(object sender, RoutedEventArgs e)
        {
            var obj = sender as Button;
            ((ValidationItem)obj.DataContext).TakeAction();
        }
    }
}
