using System;
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

        private NotifyingMetadataItem m_NewlyAddedMetadataItem;

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
            m_NewlyAddedMetadataItem = null;
            InitializeComponent();
        }

 
        public void Popup()
        {
            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   UserInterfaceStrings.EscapeMnemonic(
                                                       Tobi_Plugin_MetadataPane_Lang.CmdShowMetadata_ShortDesc),
                                                   this,
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 700, 400, null, 0);
            
            m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.StartTransaction
                (Tobi_Plugin_MetadataPane_Lang.TransactionMetadataEdit_ShortDesc, Tobi_Plugin_MetadataPane_Lang.TransactionMetadataEdit_LongDesc);

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

        private void DeleteButton_OnClick(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            NotifyingMetadataItem metadata = button.DataContext as NotifyingMetadataItem;
            m_ViewModel.RemoveMetadata(metadata);
        }

        private void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            m_ViewModel.AddEmptyMetadata();
            ObservableCollection<NotifyingMetadataItem> metadataItems =
                m_ViewModel.MetadataCollection.Metadatas;
            if (metadataItems.Count > 0)
            {
                NotifyingMetadataItem metadata = metadataItems[metadataItems.Count - 1];
                CollectionViewSource cvs = (CollectionViewSource)this.FindResource("MetadatasCVS");
                cvs.View.MoveCurrentTo(metadata);
            }
        }

    }
}