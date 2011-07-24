using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa.core;
using urakawa.property.alt;

namespace Tobi.Plugin.Descriptions
{
    [Export(typeof(IDescriptionsView)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class DescriptionsView : IDescriptionsView, IPartImportsSatisfiedNotification
    {
#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied()
#pragma warning restore 1591
        {
            //#if DEBUG
            //            Debugger.Break();
            //#endif
        }

        private readonly DescriptionsViewModel m_ViewModel;

        private readonly ILoggerFacade m_Logger;
        private readonly IShellView m_ShellView;
        private readonly IUrakawaSession m_Session;

        [ImportingConstructor]
        public DescriptionsView(
            ILoggerFacade logger,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession session,
            [Import(typeof(DescriptionsViewModel), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            DescriptionsViewModel viewModel)
        {
            m_Logger = logger;
            m_ShellView = shellView;
            m_Session = session;

            m_ViewModel = viewModel;

            m_Logger.Log("DescriptionsView.ctor", Category.Debug, Priority.Medium);

            DataContext = m_ViewModel;
            InitializeComponent();
        }

        public void Popup()
        {
            var windowPopup = new PopupModalWindow(m_ShellView,
                                                  UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Descriptions_Lang.CmdEditDescriptions_ShortDesc),
                                                  this,
                                                  PopupModalWindow.DialogButtonsSet.OkCancel,
                                                  PopupModalWindow.DialogButton.Ok,
                                                  true, 800, 500, null, 0);
            //view.OwnerWindow = windowPopup;

            m_Session.DocumentProject.Presentations.Get(0).UndoRedoManager.StartTransaction
                (Tobi_Plugin_Descriptions_Lang.CmdEditDescriptions_ShortDesc, Tobi_Plugin_Descriptions_Lang.CmdEditDescriptions_LongDesc);

            windowPopup.ShowModal();

            if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Ok)
            {
                m_Session.DocumentProject.Presentations.Get(0).UndoRedoManager.EndTransaction();
            }
            else
            {
                m_Session.DocumentProject.Presentations.Get(0).UndoRedoManager.CancelTransaction();
            }

            GC.Collect();
            GC.WaitForFullGCComplete();
        }

        private void OnLoaded_Panel(object sender, RoutedEventArgs e)
        {
            var win = Window.GetWindow(this);
            if (win is PopupModalWindow)
                OwnerWindow = (PopupModalWindow)win;

            FocusHelper.Focus(ButtonAdd);

            m_ViewModel.OnPanelLoaded();
        }

        private void OnUnloaded_Panel(object sender, RoutedEventArgs e)
        {
            if (m_OwnerWindow != null)
            {

            }
        }

        private void OnKeyDown_ListItem(object sender, KeyEventArgs e)
        {
            var key = (e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));

            // We capture only the RETURN KeyUp bubbling-up from UI descendants
            if (key != Key.Return) // || !(sender is ListViewItem))
            {
                return;
            }

            // We void the effect of the RETURN key
            // (which would normally close the parent dialog window by activating the default button: CANCEL)
            e.Handled = true;
        }
        ~DescriptionsView()
        {
#if DEBUG
            m_Logger.Log("DescriptionsView garbage collected.", Category.Debug, Priority.Medium);
#endif
        }

        private PopupModalWindow m_OwnerWindow;
        public PopupModalWindow OwnerWindow
        {
            get { return m_OwnerWindow; }
            private set
            {
                if (m_OwnerWindow != null)
                {
                    m_OwnerWindow.ActiveAware.IsActiveChanged -= OnOwnerWindowIsActiveChanged;
                }
                m_OwnerWindow = value;
                if (m_OwnerWindow == null) return;

                OnOwnerWindowIsActiveChanged(null, null);

                m_OwnerWindow.ActiveAware.IsActiveChanged += OnOwnerWindowIsActiveChanged;
            }
        }

        private void OnOwnerWindowIsActiveChanged(object sender, EventArgs e)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        private void OnSelectionChanged_MetadataList(object sender, SelectionChangedEventArgs e)
        {
            m_ViewModel.SetSelectedMetadata(MetadatasListBox.SelectedIndex);
        }
    }
}
