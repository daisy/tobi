using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Practices.Composite.Logging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections.ObjectModel;
using Tobi.Common;
using Tobi.Common.UI;
using Tobi.Common.Validation;
using Tobi.Plugin.Validator.Metadata;
using urakawa.metadata;
using urakawa.metadata.daisy;

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

        public ValidationItem ErrorWithFocus { get; set;}

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
            ErrorWithFocus = null;
            InitializeComponent();
        }

 
        public void Popup()
        {
            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_MetadataPane_Lang.CmdShowMetadata_ShortDesc),
                                                   this,
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 700, 400, null, 0);
            windowPopup.IgnoreEscape = true;

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

        //Sometimes the metadata dialog will be launched with the intention of highlighting a specific error
        private void FocusOnError()
        {
            if (ErrorWithFocus == null) return;

            //for missing item errors, add the missing item type
            if (ErrorWithFocus is MetadataMissingItemValidationError)
            {
                AddMissingItem((ErrorWithFocus as MetadataMissingItemValidationError).Definition.Name);
            }
            else
            {
                //for other errors, highlight the metadata item containing the error
                if (ErrorWithFocus is AbstractMetadataValidationErrorWithTarget &&
                    (ErrorWithFocus as AbstractMetadataValidationErrorWithTarget).Target != null)
                {
                    SetSelectedListItem((ErrorWithFocus as AbstractMetadataValidationErrorWithTarget).Target);
                }
            }
            ErrorWithFocus = null;
        }

        //add a metadata item and bring focus to it
        private void AddMissingItem(string name)
        {
            m_ViewModel.AddEmptyMetadata();
            ObservableCollection<NotifyingMetadataItem> metadataItems =
                m_ViewModel.MetadataCollection.Metadatas;
            if (metadataItems.Count > 0)
            {
                NotifyingMetadataItem selection = metadataItems[metadataItems.Count - 1];
                selection.Name = name;
                SetSelectedListItem(selection);
            }
        }

        private void SetSelectedListItem(Metadata metadata)
        {
            ObservableCollection<NotifyingMetadataItem> metadatas =
                    ((MetadataPaneViewModel)DataContext).MetadataCollection.Metadatas;
            IEnumerator<NotifyingMetadataItem> enumerator = metadatas.GetEnumerator();
            NotifyingMetadataItem selection = null;
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.UrakawaMetadata == metadata)
                {
                    selection = enumerator.Current;
                    break;
                }
            }

            if (selection != null) SetSelectedListItem(selection);

        }
        
        //this works for adding new items, but not for adding missing items or highlighting existing ones.  why?
        private void SetSelectedListItem(NotifyingMetadataItem selection)
        {   
            if (selection != null)
            {
               CollectionViewSource cvs = (CollectionViewSource)FindResource("MetadatasCVS");
               if (cvs != null) cvs.View.MoveCurrentTo(selection);

               //MetadataList.SelectedItem = selection;
               
               MetadataList.ScrollIntoView(selection);

               FocusHelper.Focus(FocusableItem);
            }
        }

        private UIElement FocusableItem
        {
            get
            {
                if (MetadataList.Focusable) return MetadataList;

                if (MetadataList.SelectedIndex != -1)
                {
                    return MetadataList.ItemContainerGenerator.ContainerFromIndex(MetadataList.SelectedIndex) as ListViewItem;
                }

                if (MetadataList.Items.Count > 0)
                {
                    return MetadataList.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
                }

                return null;
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
                SetSelectedListItem(metadata);
            }
             
        }

        private void OnContentGotFocus(object sender, EventArgs e)
        {
            //Debug.Print("Metadata pane: mouse focus");
            //e.Handled = true;
            SelectAllTextIfMagicEmptyString(sender as TextBox);
        }

        private void SelectAllTextIfMagicEmptyString(TextBox textBox)
        {
            if (textBox != null)
            {
                if (textBox.Text == SupportedMetadata_Z39862005.MagicStringEmpty)
                {
                    Debug.Print("Metadata pane: selecting all text");
                    Dispatcher.CurrentDispatcher.BeginInvoke(
                        new Action(textBox.SelectAll),
                        DispatcherPriority.Background);
                }
            }
        }


        private void ContentText_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Debug.Print("Metadata pane: mousedown focus");
            SelectAllTextIfMagicEmptyString(sender as TextBox);
        }

        private void MetadataPaneView_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (ErrorWithFocus != null) FocusOnError();
        }
    }
}