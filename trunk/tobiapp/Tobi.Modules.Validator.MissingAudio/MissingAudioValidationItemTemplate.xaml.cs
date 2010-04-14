using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using Tobi.Common.UI.XAML;
using Tobi.Common.Validation;
using urakawa.core;

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
            var obj = sender as Hyperlink;
            ((ValidationItem)obj.DataContext).TakeAction();
        }
    }
}
