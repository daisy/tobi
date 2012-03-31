using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using AudioLib;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Unity;
using Microsoft.Win32;
using Tobi.Common;
using Tobi.Common.UI;
using Tobi.Common.UI.XAML;
using urakawa.core;
using urakawa.daisy;
using urakawa.data;
using urakawa.metadata;
using urakawa.property.alt;
using urakawa.xuk;

namespace Tobi.Plugin.Descriptions
{

    [Export(typeof(IDescriptionsView)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class DescriptionsView : IDescriptionsView, IPartImportsSatisfiedNotification,
        //
        IShellView // IShellView: because UrakawaSession and AudioPaneViewModel and AudioPaneView require it.
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
        private readonly IUnityContainer m_Container;
        private readonly IEventAggregator m_EventAggregator;

        [ImportingConstructor]
        public DescriptionsView(
            ILoggerFacade logger,
            IEventAggregator eventAggregator,
            IUnityContainer container,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession session,
            [Import(typeof(DescriptionsViewModel), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            DescriptionsViewModel viewModel)
        {
            m_EventAggregator = eventAggregator;
            m_Logger = logger;
            m_Container = container;
            m_ShellView = shellView;
            m_Session = session;

            m_ViewModel = viewModel;

            m_Logger.Log("DescriptionsView.ctor", Category.Debug, Priority.Medium);

            DataContext = m_ViewModel;
            InitializeComponent();
        }

        public bool AskUserRenameXmlID()
        {
            var label = new TextBlock
            {
                Text = "Automatically rename linked identifiers?\n(recommended)",
                Margin = new Thickness(8, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap
            };

            var iconProvider = new ScalableGreyableImageProvider(m_ShellView.LoadTangoIcon("help-browser"), m_ShellView.MagnificationLevel);

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            panel.Children.Add(iconProvider.IconLarge);
            panel.Children.Add(label);
            //panel.Margin = new Thickness(8, 8, 8, 0);

            var popup = new PopupModalWindow(m_ShellView,
                                                 UserInterfaceStrings.EscapeMnemonic("Refactor identifiers?"),
                                                 panel,
                                                 PopupModalWindow.DialogButtonsSet.YesNo,
                                                 PopupModalWindow.DialogButton.Yes,
                                                 true, 300, 160, null, 0);

            popup.ShowModal();

            popup.IgnoreEscape = true;

            return (popup.ClickedDialogButton == PopupModalWindow.DialogButton.Yes);
        }

        public void Popup()
        {
            var navView = m_Container.Resolve<DescriptionsNavigationView>();
            if (navView != null) navView.UpdateTreeNodeSelectionFromListItem();

            Tuple<TreeNode, TreeNode> selection = m_Session.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var navModel = m_Container.Resolve<DescriptionsNavigationViewModel>();
            if (navModel.DescriptionsNavigator == null) return;

            bool found = false;
            foreach (DescribableTreeNode dnode in navModel.DescriptionsNavigator.DescribableTreeNodes)
            {
                found = dnode.TreeNode == node;
                if (found) break;
            }
            if (!found)
            {
                var label = new TextBlock
                {
                    Text = "You must first select an image to describe.",
                    Margin = new Thickness(8, 0, 8, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Focusable = true,
                    TextWrapping = TextWrapping.Wrap
                };

                var iconProvider = new ScalableGreyableImageProvider(m_ShellView.LoadTangoIcon("dialog-warning"), m_ShellView.MagnificationLevel);

                var panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Stretch,
                };
                panel.Children.Add(iconProvider.IconLarge);
                panel.Children.Add(label);
                //panel.Margin = new Thickness(8, 8, 8, 0);

                var popup = new PopupModalWindow(m_ShellView,
                                                     UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Descriptions_Lang.CmdEditDescriptions_ShortDesc),
                                                     panel,
                                                     PopupModalWindow.DialogButtonsSet.Ok,
                                                     PopupModalWindow.DialogButton.Ok,
                                                     true, 300, 160, null, 0);

                popup.ShowModal();
                return;
            }

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                  UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Descriptions_Lang.CmdEditDescriptions_ShortDesc),
                                                  this,
                                                  PopupModalWindow.DialogButtonsSet.OkApplyCancel,
                                                  PopupModalWindow.DialogButton.Apply,
                                                  true, 800, 500, null, 0);
            //this.OwnerWindow = windowPopup; DONE in ON PANEL LOADED EVENT

            windowPopup.IgnoreEscape = true;

            //var bindings = Application.Current.MainWindow.InputBindings;
            //foreach (var binding in bindings)
            //{
            //    if (binding is KeyBinding)
            //    {
            //        var keyBinding = (KeyBinding)binding;
            //        if (keyBinding.Command == m_ShellView.ExitCommand)
            //        {
            //            continue;
            //        }
            //        windowPopup.InputBindings.Add(keyBinding);
            //    }
            //}

            //windowPopup.InputBindings.AddRange(Application.Current.MainWindow.InputBindings);

            //windowPopup.KeyUp += (object sender, KeyEventArgs e) =>
            //    {
            //        var key = (e.Key == Key.System
            //                        ? e.SystemKey
            //                        : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));

            //        if (key == Key.Escape)
            //        {
            //            m_EventAggregator.GetEvent<EscapeEvent>().Publish(null);
            //        }
            //    };

            //windowPopup.Closed += (sender, ev) => Dispatcher.BeginInvoke(
            //    DispatcherPriority.Background,
            //    (Action)(() =>
            //    {
            //        //
            //    }));

            m_Session.DocumentProject.Presentations.Get(0).UndoRedoManager.StartTransaction
                (Tobi_Plugin_Descriptions_Lang.CmdEditDescriptions_ShortDesc, Tobi_Plugin_Descriptions_Lang.CmdEditDescriptions_LongDesc);


            //Tuple<TreeNode, TreeNode> selection = m_Session.GetTreeNodeSelection();
            //TreeNode node = selection.Item2 ?? selection.Item1;
            //if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null)
            {
                altProp = node.GetOrCreateAlternateContentProperty();
                DebugFix.Assert(altProp != null);
            }

            windowPopup.ShowModal();

            if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Ok
                || windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Apply)
            {
                bool empty = m_Session.DocumentProject.Presentations.Get(0).UndoRedoManager.IsTransactionEmpty;

                m_Session.DocumentProject.Presentations.Get(0).UndoRedoManager.EndTransaction();

                if (empty)
                {
                    altProp = node.GetProperty<AlternateContentProperty>();
                    if (altProp != null && altProp.IsEmpty)
                    {
                        node.RemoveProperty(altProp);
                    }
                }
            }
            else
            {
                m_Session.DocumentProject.Presentations.Get(0).UndoRedoManager.CancelTransaction();

                altProp = node.GetProperty<AlternateContentProperty>();
                if (altProp != null && altProp.IsEmpty)
                {
                    node.RemoveProperty(altProp);
                }
            }


            if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Apply)
            {
                Popup();
            }
        }



        private void OnKeyUp_ButtonImport(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None
                && e.Key == Key.Space)
            {
                OnClick_ButtonImport(null, null);
            }
        }

        private void OnClick_ButtonImport(object sender, RoutedEventArgs e)
        {
            m_Logger.Log("DescriptionView.OpenFileDialog (XML)", Category.Debug, Priority.Medium);

            var dlg = new OpenFileDialog
            {
                FileName = "",
                DefaultExt = ".xml",
                Filter = @"XML (*.xml)|*.xml",
                CheckFileExists = false,
                CheckPathExists = false,
                AddExtension = true,
                DereferenceLinks = true,
                Title = "Tobi: " + "Open DIAGRAM XML file"
            };

            bool? result = false;

            m_ShellView.DimBackgroundWhile(() => { result = dlg.ShowDialog(); });

            if (result == false)
            {
                return;
            }

            string fullPath = "";
            fullPath = dlg.FileName;

            if (string.IsNullOrEmpty(fullPath)) return;

            Application.Current.MainWindow.Cursor = Cursors.Wait;
            this.Cursor = Cursors.Wait; //m_ShellView

            m_ViewModel.ImportDiagramXML(fullPath);

            forceRefreshDataUI();

            Application.Current.MainWindow.Cursor = Cursors.Arrow;
            this.Cursor = Cursors.Arrow; //m_ShellView
        }

        private void forceRefreshDataUI()
        {
            MetadatasListView.Items.Refresh();
            if (MetadatasListView.Items.Count > 0)
            {
                MetadatasListView.SelectedIndex = 0;
            }
            MetadataAttributesListView.Items.Refresh();
            if (MetadataAttributesListView.Items.Count > 0)
            {
                MetadataAttributesListView.SelectedIndex = 0;
            }
            //if (MetadatasListView.IsVisible)
            //{
            //    FocusHelper.Focus(MetadatasListView);
            //}
            OnSelectionChanged_MetadataList(null, null);

            DescriptionsListView.Items.Refresh();
            if (DescriptionsListView.Items.Count > 0)
            {
                DescriptionsListView.SelectedIndex = 0;
            }
            MetadatasAltContentListView.Items.Refresh();
            if (MetadatasAltContentListView.Items.Count > 0)
            {
                MetadatasAltContentListView.SelectedIndex = 0;
            }
            //if (DescriptionsListView.IsVisible)
            //{
            //    FocusHelper.Focus(DescriptionsListView);
            //}
            OnSelectionChanged_DescriptionsList(null, null);

            FocusHelper.Focus(ImageLabel);
        }

        private void OnLoaded_Panel(object sender, RoutedEventArgs e)
        {
            var win = Window.GetWindow(this);
            if (win is PopupModalWindow)
                OwnerWindow = (PopupModalWindow)win;



            m_ViewModel.OnPanelLoaded();

            forceRefreshDataUI();
        }

        private void OnUnloaded_Panel(object sender, RoutedEventArgs e)
        {
            BindingExpression be = DescriptionImage.GetBindingExpression(Image.SourceProperty);
            if (be != null) be.UpdateTarget();

            if (m_OwnerWindow != null)
            {
            }
        }

        private string TEXTFIELD_WATERMARK = "[enter text here]";

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

    }


    [ValueConversion(typeof(AlternateContent), typeof(string))]
    public class AlternateContentToImagePathConverter : ValueConverterMarkupExtensionBase<AlternateContentToImagePathConverter>
    {
        #region IValueConverter Members

        public override object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Object) && targetType != typeof(String))
                throw new InvalidOperationException("The target must be Object or String !");

            var altContent = value as AlternateContent;
            if (altContent != null && altContent.Image != null)
            {
                return ((FileDataProvider)altContent.Image.ImageMediaData.DataProvider).DataFileFullPath;
            }

            return "[no image]";
        }

        public override object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return null;
        }

        #endregion
    }

    [ValueConversion(typeof(AlternateContent), typeof(string))]
    public class AlternateContentToTextConverter : ValueConverterMarkupExtensionBase<AlternateContentToTextConverter>
    {
        #region IValueConverter Members

        public override object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Object) && targetType != typeof(String))
                throw new InvalidOperationException("The target must be Object or String !");

            var altContent = value as AlternateContent;
            if (altContent != null && altContent.Text != null)
            {
                return altContent.Text.Text;
            }

            return "[no text]";
        }

        public override object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return null;
        }

        #endregion
    }

    [ValueConversion(typeof(AlternateContent), typeof(string))]
    public class AlternateContentToSummaryConverter : ValueConverterMarkupExtensionBase<AlternateContentToSummaryConverter>
    {
        #region IValueConverter Members

        public override object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Object) && targetType != typeof(String))
                throw new InvalidOperationException("The target must be Object or String !");

            var altContent = value as AlternateContent;

            //string txt = "[no text]";

            //if (altContent != null && altContent.Text != null) // && !string.IsNullOrEmpty(altContent.Text.Text))
            //{
            //    txt = altContent.Text.Text;
            //}

            string txt = "";

            string uid = null;
            string descriptionName = null;

            if (altContent != null && altContent.Metadatas != null && altContent.Metadatas.Count > 0)
            {
                foreach (Metadata metadata in altContent.Metadatas.ContentsAs_Enumerable)
                {
                    if (metadata.NameContentAttribute.Name == XmlReaderWriterHelper.XmlId)
                    {
                        uid = metadata.NameContentAttribute.Value;
                    }
                    else if (metadata.NameContentAttribute.Name == DiagramContentModelHelper.DiagramElementName)
                    {
                        descriptionName = metadata.NameContentAttribute.Value;
                    }
                }
            }

            return (descriptionName != null ? " {" + descriptionName + "} " : "") + txt + (uid != null ? " (ID: " + uid + ")" : "");
        }

        public override object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return null;
        }

        #endregion
    }

}
