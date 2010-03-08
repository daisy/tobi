using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Logging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections.ObjectModel;
using Tobi.Common;
using Tobi.Common.UI;
using Tobi.Plugin.Validator.Metadata;

namespace Tobi.Plugin.MetadataPane
{
    /// <summary>
    /// Interaction logic for MetadataPaneView.xaml
    /// The backing ViewModel is injected in the constructor ("passive" view design pattern)
    /// </summary>
    [Export(typeof(IMetadataPaneView)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class MetadataPaneView : IMetadataPaneView
    {
        private readonly MetadataPaneViewModel m_ViewModel;

        private readonly ILoggerFacade m_Logger;

        private readonly IUrakawaSession m_UrakawaSession;
        private readonly IShellView m_ShellView;

        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        [ImportingConstructor]
        public MetadataPaneView(
            ILoggerFacade logger,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession session,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView,
            [Import(typeof(MetadataPaneViewModel), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            MetadataPaneViewModel viewModel)
        {
            m_Logger = logger;
            m_UrakawaSession = session;
            m_ShellView = shellView;

            m_ViewModel = viewModel;

            m_Logger.Log("MetadataPaneView.ctor", Category.Debug, Priority.Medium);

            DataContext = m_ViewModel;

            InitializeComponent();
        }

        private void Add_Metadata_Button_Click(object sender, RoutedEventArgs e)
        {
            m_ViewModel.AddEmptyMetadata();
            ObservableCollection<NotifyingMetadataItem> metadataItems =
                m_ViewModel.MetadataCollection.Metadatas;
            if (metadataItems.Count > 0)
            {
                NotifyingMetadataItem metadata = metadataItems[metadataItems.Count - 1];
                CollectionViewSource cvs = (CollectionViewSource) this.FindResource("MetadatasCVS");
                cvs.View.MoveCurrentTo(metadata);
            }
            namesComboBox.Focus();
        }
        
        private void Remove_Metadata_Button_Click(object sender, RoutedEventArgs e)
        {
            CollectionViewSource cvs = (CollectionViewSource)this.FindResource("MetadatasCVS");
            NotifyingMetadataItem metadata = (NotifyingMetadataItem)cvs.View.CurrentItem;
            m_ViewModel.RemoveMetadata(metadata);
        }

        //select the corresponding metadata from the item double-clicked in the errors list
        private void errorsList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (errorsList.SelectedItem != null && 
                errorsList.SelectedItem is MetadataValidationError)
            {
                MetadataValidationError error = (MetadataValidationError) errorsList.SelectedItem;
                if (error.ErrorType == MetadataErrorType.FormatError)
                {
                    NotifyingMetadataItem metadataItem = m_ViewModel.MetadataCollection.Find(error.Target);
                    CollectionViewSource cvs = (CollectionViewSource) this.FindResource("MetadatasCVS");
                    if (metadataItem != null) cvs.View.MoveCurrentTo(metadataItem);
                }
            }

        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] != null)
            {
                CollectionViewSource cvs = (CollectionViewSource)this.FindResource("MetadatasCVS");
                NotifyingMetadataItem metadata = (NotifyingMetadataItem)cvs.View.CurrentItem;

                //checkbox visibility
                PrimaryIdentifierConverter primaryIdentifierConverter = new PrimaryIdentifierConverter();
                primaryIdentifierCheckBox.Visibility = 
                    (System.Windows.Visibility)primaryIdentifierConverter.Convert(metadata, null, null, null);
                
                //the remove button's enabled-ness
                IsNotRequiredOccurrenceConverter requiredOccurrenceConverter = new IsNotRequiredOccurrenceConverter();
                removeButton.IsEnabled = (bool)requiredOccurrenceConverter.Convert(metadata, null, null, null);
            
            }
        }

        private void MetadataListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            m_ViewModel.SelectionChanged();
        }

        public void Popup()
        {
            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   UserInterfaceStrings.EscapeMnemonic(
                                                       Tobi_Plugin_MetadataPane_Lang.ShowMetadata),
                                                   this,
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 700, 400, null, 0);
            
            m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.StartTransaction
                (Tobi_Plugin_MetadataPane_Lang.OpenMetadataEditor, Tobi_Plugin_MetadataPane_Lang.DialogOpen);            // TODO LOCALIZE OpenMetadataEditor
                                                                                                     // TODO LOCALIZE DialogOpen

            windowPopup.ShowModal();

            //if the user presses "Ok", then save the changes.  otherwise, don't save them.
            if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Ok)
            {
                m_ViewModel.removeEmptyMetadata();
                m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.EndTransaction();
            }
            else
            {
                m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.CancelTransaction();
            }
        }
    }
}