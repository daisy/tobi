using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
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
using urakawa.metadata.daisy;
using urakawa.property.alt;
using BooleanToVisibilityConverter = System.Windows.Controls.BooleanToVisibilityConverter;

namespace Tobi.Plugin.Descriptions
{
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

            string txt = "[no text]";

            if (altContent != null && altContent.Text != null)
            {
                txt = altContent.Text.Text;
            }

            if (altContent != null && altContent.Metadatas != null && altContent.Metadatas.Count > 0)
            {
                foreach (Metadata metadata in altContent.Metadatas.ContentsAs_Enumerable)
                {
                    if (metadata.NameContentAttribute.Name == DaigramContentModelStrings.XmlId)
                    {
                        txt = txt + " (ID: " + metadata.NameContentAttribute.Value + ")";
                    }
                }
            }

            return txt;
        }

        public override object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return null;
        }

        #endregion
    }
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
        private readonly IUnityContainer m_Container;

        [ImportingConstructor]
        public DescriptionsView(
            ILoggerFacade logger,
            IUnityContainer container,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession session,
            [Import(typeof(DescriptionsViewModel), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            DescriptionsViewModel viewModel)
        {
            m_Logger = logger;
            m_Container = container;
            m_ShellView = shellView;
            m_Session = session;

            m_ViewModel = viewModel;

            m_Logger.Log("DescriptionsView.ctor", Category.Debug, Priority.Medium);

            DataContext = m_ViewModel;
            InitializeComponent();
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
                    Text = "Please select an image to describe.",
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
                //view.OwnerWindow = windowPopup;

                popup.ShowModal();
                return;
            }

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
                bool empty = m_Session.DocumentProject.Presentations.Get(0).UndoRedoManager.IsTransactionEmpty;

                m_Session.DocumentProject.Presentations.Get(0).UndoRedoManager.EndTransaction();

                if (empty)
                {
                    var altProp = node.GetProperty<AlternateContentProperty>();
                    if (altProp != null && altProp.IsEmpty)
                    {
                        node.RemoveProperty(altProp);
                    }
                }
            }
            else
            {
                m_Session.DocumentProject.Presentations.Get(0).UndoRedoManager.CancelTransaction();

                var altProp = node.GetProperty<AlternateContentProperty>();
                if (altProp != null && altProp.IsEmpty)
                {
                    node.RemoveProperty(altProp);
                }
            }

            GC.Collect();
            GC.WaitForFullGCComplete();
        }

        private void OnLoaded_Panel(object sender, RoutedEventArgs e)
        {
            var win = Window.GetWindow(this);
            if (win is PopupModalWindow)
                OwnerWindow = (PopupModalWindow)win;

            MetadatasListView.Items.Refresh();
            MetadataAttributesListView.Items.Refresh();
            if (MetadatasListView.IsVisible) FocusHelper.Focus(MetadatasListView);
            OnSelectionChanged_MetadataList(null, null);

            DescriptionsListView.Items.Refresh();
            MetadatasAltContentListView.Items.Refresh();
            if (DescriptionsListView.IsVisible) FocusHelper.Focus(DescriptionsListView);
            OnSelectionChanged_DescriptionsList(null, null);

            m_ViewModel.OnPanelLoaded();
        }

        private void OnUnloaded_Panel(object sender, RoutedEventArgs e)
        {
            BindingExpression be = DescriptionImage.GetBindingExpression(Image.SourceProperty);
            if (be != null) be.UpdateTarget();

            if (m_OwnerWindow != null)
            {

            }
        }

        private bool showMetadataAttributeEditorPopupDialog(MetadataAttribute metadataAttr, out string newName, out string newValue)
        {
            m_Logger.Log("Descriptions.MetadataAttributeEditor", Category.Debug, Priority.Medium);

            var label_Name = new TextBlock
            {
                Text = "Name: ",
                Margin = new Thickness(8, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap
            };

            var label_Value = new TextBlock
            {
                Text = "Value: ",
                Margin = new Thickness(8, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap
            };

            //var editBox_Name = new TextBox
            //{
            //    Text = metadataAttr.Name,
            //    TextWrapping = TextWrapping.WrapWithOverflow
            //};

            var editBoxCombo_Name = new ComboBoxWithAutomationPeer
            {
                Text = metadataAttr.Name,
                IsEditable = true,
                IsTextSearchEnabled = true,
#if NET40
                IsTextSearchCaseSensitive = false
#endif //NET40
            };

            //var binding = new Binding
            //{
            //    Mode = BindingMode.OneWay,
            //    Source = new RelativeSource(RelativeSourceMode.Self),
            //    Path = new PropertyPath("SelectedItem")
            //};
            ////var expr = editBoxCombo_Name.SetBinding(AutomationProperties.NameProperty, binding);
            //editBoxCombo_Name.SetValue(AutomationProperties.NameProperty, "daniel");

            //editBoxCombo_Name.SelectionChanged += new SelectionChangedEventHandler(
            //    (object sender, SelectionChangedEventArgs e) =>
            //    {
            //        //var expr = editBoxCombo_Name.GetBindingExpression(AutomationProperties.NameProperty);
            //        //expr.UpdateTarget();
            //        //editBoxCombo_Name.NotifyScreenReaderAutomationIfKeyboardFocused();
                    
            //        //var txt = editBoxCombo_Name.Text;
            //        //editBoxCombo_Name.Text = "mike";
            //        //editBoxCombo_Name.Text = txt;

            //        editBoxCombo_Name.NotifyScreenReaderAutomation();

            //        m_Logger.Log("UP TRAGET", Category.Debug, Priority.High);

            //        }
            //    );

            var list = new List<String>();
            list.AddRange(DaigramContentModelStrings.MetadataNames);
            foreach (var def in SupportedMetadata_Z39862005.DefinitionSet.Definitions)
            {
                list.Add(def.Name.ToLower());
                if (def.Synonyms != null)
                {
                    foreach (var syn in def.Synonyms)
                    {
                        list.Add(syn.ToLower());
                    }
                }
            }
            
            editBoxCombo_Name.ItemsSource = list;

            //    col = new ObservableCollection<string> { "Eric", "Phillip" };
            //combo.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = col });

            var editBox_Value = new TextBox
            {
                Text = metadataAttr.Value,
                TextWrapping = TextWrapping.WrapWithOverflow
            };

            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var panelName = new DockPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                LastChildFill = true
            };
            panelName.Margin = new Thickness(0, 0, 0, 8);
            label_Name.SetValue(DockPanel.DockProperty, Dock.Left);
            panelName.Children.Add(label_Name);
            panelName.Children.Add(editBoxCombo_Name);

            var panelValue = new DockPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
            };
            label_Value.SetValue(DockPanel.DockProperty, Dock.Left);
            panelValue.Children.Add(label_Value);
            panelValue.Children.Add(editBox_Value);

            panel.Children.Add(panelName);
            panel.Children.Add(panelValue);

            //var details = new TextBoxReadOnlyCaretVisible
            //                  {
            //    TextReadOnly = Tobi_Lang.ExitConfirm
            //};

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   UserInterfaceStrings.EscapeMnemonic("Edit attribute"),
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 300, 160, null, 40);

            editBoxCombo_Name.Loaded += new RoutedEventHandler((sender, ev) =>
            {
                var textBox = FindChild(editBoxCombo_Name, "PART_EditableTextBox", typeof(TextBox)) as TextBox;
                if (textBox != null)
                    textBox.SelectAll();

                FocusHelper.FocusBeginInvoke(editBoxCombo_Name);
            });
            editBox_Value.Loaded += new RoutedEventHandler((sender, ev) =>
            {
                editBox_Value.SelectAll();
                //FocusHelper.FocusBeginInvoke(editBox_Name);
            });

            windowPopup.ShowModal();

            if (PopupModalWindow.IsButtonOkYesApply(windowPopup.ClickedDialogButton))
            {
                newName = editBoxCombo_Name.Text;
                newValue = editBox_Value.Text;

                return true;
            }

            newName = null;
            newValue = null;

            return false;
        }

        public static DependencyObject FindChild(DependencyObject reference, string childName, Type childType)
        {
            DependencyObject foundChild = null;
            if (reference != null)
            {
                int childrenCount = VisualTreeHelper.GetChildrenCount(reference);
                for (int i = 0; i < childrenCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(reference, i);
                    // If the child is not of the request child type child
                    if (child.GetType() != childType)
                    {
                        // recursively drill down the tree
                        foundChild = FindChild(child, childName, childType);
                    }
                    else if (!string.IsNullOrEmpty(childName))
                    {
                        var frameworkElement = child as FrameworkElement;
                        // If the child's name is set for search
                        if (frameworkElement != null && frameworkElement.Name == childName)
                        {
                            // if the child's name is of the request name
                            foundChild = child;
                            break;
                        }
                    }
                    else
                    {
                        // child element found.
                        foundChild = child;
                        break;
                    }
                }
            }
            return foundChild;
        }
        private void OnKeyDown_ListItemMetadataAltContent(object sender, KeyEventArgs e)
        {
            var key = (e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));

            // We capture only the RETURN KeyUp bubbling-up from UI descendants
            if (key != Key.Return) // || !(sender is ListViewItem))
            {
                return;
            }

            OnMouseDoubleClick_ListItemMetadataAltContent(null, null);

            // We void the effect of the RETURN key
            // (which would normally close the parent dialog window by activating the default button: CANCEL)
            e.Handled = true;
        }

        private void OnKeyDown_ListItemMetadata(object sender, KeyEventArgs e)
        {
            var key = (e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));

            // We capture only the RETURN KeyUp bubbling-up from UI descendants
            if (key != Key.Return) // || !(sender is ListViewItem))
            {
                return;
            }

            OnMouseDoubleClick_ListItemMetadata(null, null);

            // We void the effect of the RETURN key
            // (which would normally close the parent dialog window by activating the default button: CANCEL)
            e.Handled = true;
        }

        private void OnKeyDown_ListItemMetadataAttr(object sender, KeyEventArgs e)
        {
            var key = (e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));

            // We capture only the RETURN KeyUp bubbling-up from UI descendants
            if (key != Key.Return) // || !(sender is ListViewItem))
            {
                return;
            }

            OnMouseDoubleClick_ListItemMetadataAttr(null, null);

            // We void the effect of the RETURN key
            // (which would normally close the parent dialog window by activating the default button: CANCEL)
            e.Handled = true;
        }

        private void OnClick_ButtonEditMetadataAltContent(object sender, RoutedEventArgs e)
        {
            if (MetadatasListView.SelectedIndex >= 0)
                OnMouseDoubleClick_ListItemMetadataAltContent(null, null);
        }
        private void OnClick_ButtonEditMetadata(object sender, RoutedEventArgs e)
        {
            if (MetadatasListView.SelectedIndex >= 0)
                OnMouseDoubleClick_ListItemMetadata(null, null);
        }
        private void OnClick_ButtonEditMetadataAttr(object sender, RoutedEventArgs e)
        {
            if (MetadataAttributesListView.SelectedIndex >= 0)
                OnMouseDoubleClick_ListItemMetadataAttr(null, null);
        }

        private void OnMouseDoubleClick_ListItemMetadataAltContent(object sender, MouseButtonEventArgs e)
        {
            Tuple<TreeNode, TreeNode> selection = m_Session.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            if (altProp.AlternateContents.IndexOf(altContent) < 0) return;

            if (MetadatasAltContentListView.SelectedIndex < 0) return;
            Metadata md = (Metadata)MetadatasAltContentListView.SelectedItem;
            string newName, newValue;
            bool ok = showMetadataAttributeEditorPopupDialog(md.NameContentAttribute, out newName, out newValue);
            if (ok)
            {
                m_ViewModel.SetMetadataAttribute(null, altContent, md, md.NameContentAttribute, newName, newValue);

                MetadatasAltContentListView.Items.Refresh();
            }
        }

        private void OnMouseDoubleClick_ListItemMetadata(object sender, MouseButtonEventArgs e)
        {
            Tuple<TreeNode, TreeNode> selection = m_Session.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            if (MetadatasListView.SelectedIndex < 0) return;
            Metadata md = (Metadata)MetadatasListView.SelectedItem;
            string newName, newValue;
            bool ok = showMetadataAttributeEditorPopupDialog(md.NameContentAttribute, out newName, out newValue);
            if (ok)
            {
                m_ViewModel.SetMetadataAttribute(altProp, null, md, md.NameContentAttribute, newName, newValue);

                MetadatasListView.Items.Refresh();
            }
        }

        private void OnMouseDoubleClick_ListItemMetadataAttr(object sender, MouseButtonEventArgs e)
        {
            Tuple<TreeNode, TreeNode> selection = m_Session.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            if (MetadatasListView.SelectedIndex < 0) return;
            Metadata md = (Metadata)MetadatasListView.SelectedItem;
            MetadataAttribute mdAttr = (MetadataAttribute)MetadataAttributesListView.SelectedItem;
            string newName, newValue;
            bool ok = showMetadataAttributeEditorPopupDialog(mdAttr, out newName, out newValue);
            if (ok)
            {
                m_ViewModel.SetMetadataAttribute(altProp, null, md, mdAttr, newName, newValue);

                MetadataAttributesListView.Items.Refresh();
            }
        }

        private void OnClick_ButtonRemoveMetadataAltContent(object sender, RoutedEventArgs e)
        {
            Tuple<TreeNode, TreeNode> selection = m_Session.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            if (altProp.AlternateContents.IndexOf(altContent) < 0) return;

            if (MetadatasAltContentListView.SelectedIndex < 0) return;
            m_ViewModel.RemoveMetadata(null, altContent, (Metadata)MetadatasAltContentListView.SelectedItem);
            MetadatasAltContentListView.Items.Refresh();
            MetadatasAltContentListView.SelectedIndex = 0;
            FocusHelper.FocusBeginInvoke(MetadatasAltContentListView);
        }

        private string PROMPT_MD_NAME = "[enter a name]";
        private string PROMPT_MD_VALUE = "[enter a value]";
        private string PROMPT_ID = "[enter a unique identifier]";
        private string PROMPT_DescriptionName = "[enter the description name]";

        private void OnClick_ButtonAddMetadataAltContent(object sender, RoutedEventArgs e)
        {
            Tuple<TreeNode, TreeNode> selection = m_Session.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            if (altProp.AlternateContents.IndexOf(altContent) < 0) return;

            var mdAttr = new MetadataAttribute();
            mdAttr.Name = PROMPT_MD_NAME;
            mdAttr.Value = PROMPT_MD_VALUE;
            string newName, newValue;
            bool ok = showMetadataAttributeEditorPopupDialog(mdAttr, out newName, out newValue);
            if (ok)
            {
                m_ViewModel.AddMetadata(null, altContent, newName, newValue);
                MetadatasAltContentListView.Items.Refresh();
                MetadatasAltContentListView.SelectedIndex = MetadatasAltContentListView.Items.Count - 1;
                FocusHelper.FocusBeginInvoke(MetadatasAltContentListView);
            }
        }

        private void OnClick_ButtonRemoveMetadata(object sender, RoutedEventArgs e)
        {
            Tuple<TreeNode, TreeNode> selection = m_Session.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            //if (DescriptionsListView.SelectedIndex < 0) return;
            //AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            //if (altProp.AlternateContents.IndexOf(altContent) < 0) return;

            if (MetadatasListView.SelectedIndex < 0) return;
            m_ViewModel.RemoveMetadata(altProp, null, (Metadata)MetadatasListView.SelectedItem);
            MetadatasListView.Items.Refresh();
            MetadatasListView.SelectedIndex = 0;
            FocusHelper.FocusBeginInvoke(MetadatasListView);
        }

        private void OnClick_ButtonAddMetadata(object sender, RoutedEventArgs e)
        {
            Tuple<TreeNode, TreeNode> selection = m_Session.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetOrCreateAlternateContentProperty();
            //if (altProp == null) return;

            //if (DescriptionsListView.SelectedIndex < 0) return;
            //AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            //if (altProp.AlternateContents.IndexOf(altContent) < 0) return;

            var mdAttr = new MetadataAttribute();
            mdAttr.Name = PROMPT_MD_NAME;
            mdAttr.Value = PROMPT_MD_VALUE;
            string newName, newValue;
            bool ok = showMetadataAttributeEditorPopupDialog(mdAttr, out newName, out newValue);
            if (ok)
            {
                m_ViewModel.AddMetadata(altProp, null, newName, newValue);
                MetadatasListView.Items.Refresh();
                MetadatasListView.SelectedIndex = MetadatasListView.Items.Count - 1;
                FocusHelper.FocusBeginInvoke(MetadatasListView);
            }
        }

        private void OnClick_ButtonRemoveMetadataAttr(object sender, RoutedEventArgs e)
        {
            if (MetadatasListView.SelectedIndex < 0) return;
            Metadata md = (Metadata)MetadatasListView.SelectedItem;
            if (MetadataAttributesListView.SelectedIndex < 0) return;
            m_ViewModel.RemoveMetadataAttr(md, (MetadataAttribute)MetadataAttributesListView.SelectedItem);
            MetadataAttributesListView.Items.Refresh();
            MetadataAttributesListView.SelectedIndex = 0;
            FocusHelper.FocusBeginInvoke(MetadataAttributesListView);
        }

        private void OnClick_ButtonAddMetadataAttr(object sender, RoutedEventArgs e)
        {
            if (MetadatasListView.SelectedIndex < 0) return;
            Metadata md = (Metadata)MetadatasListView.SelectedItem;
            var mdAttr = new MetadataAttribute();
            mdAttr.Name = PROMPT_MD_NAME;
            mdAttr.Value = PROMPT_MD_VALUE;
            string newName, newValue;
            bool ok = showMetadataAttributeEditorPopupDialog(mdAttr, out newName, out newValue);
            if (ok)
            {
                m_ViewModel.AddMetadataAttr(md, newName, newValue);
                MetadataAttributesListView.Items.Refresh();
                MetadataAttributesListView.SelectedIndex = MetadataAttributesListView.Items.Count - 1;
                FocusHelper.FocusBeginInvoke(MetadataAttributesListView);
            }
        }


        private void OnClick_ButtonAddDescription(object sender, RoutedEventArgs e)
        {
            string txt = " ";
            string descriptionName = "";

            while (txt != null && txt.Trim() == "")
                txt = showLineEditorPopupDialog(PROMPT_ID, "Unique identifier");
            if (txt == null) return;

            while (descriptionName != null && descriptionName.Trim() == "")
            descriptionName = showLineEditorPopupDialog(PROMPT_DescriptionName, "description-name");

            if (txt == null || descriptionName == null) return;

            m_ViewModel.AddDescription(txt, descriptionName);
            
            DescriptionsListView.Items.Refresh();
            DescriptionsListView.SelectedIndex = DescriptionsListView.Items.Count - 1;
            FocusHelper.FocusBeginInvoke(DescriptionsListView);

            MetadatasAltContentListView.Items.Refresh();
            MetadatasAltContentListView.SelectedIndex = MetadatasAltContentListView.Items.Count - 1;
            //FocusHelper.FocusBeginInvoke(MetadatasAltContentListView);

            //BindingExpression be = DescriptionTextBox.GetBindingExpression(TextBoxReadOnlyCaretVisible.TextReadOnlyProperty);
            //if (be != null) be.UpdateTarget();
        }

        private void OnClick_ButtonRemoveDescription(object sender, RoutedEventArgs e)
        {
            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            m_ViewModel.RemoveDescription(altContent);

            DescriptionsListView.Items.Refresh();
            DescriptionsListView.SelectedIndex = 0;
            FocusHelper.FocusBeginInvoke(DescriptionsListView);
        }

        private void OnClick_ButtonEditText(object sender, RoutedEventArgs e)
        {
            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            string oldTxt = altContent.Text != null ? altContent.Text.Text : "";
            string txt = showTextEditorPopupDialog(oldTxt, "Edit description text");

            if (string.IsNullOrEmpty(txt) || txt == oldTxt) return;

            m_ViewModel.SetDescriptionText(altContent, txt);

            DescriptionsListView.Items.Refresh();

            BindingExpression be = DescriptionTextBox.GetBindingExpression(TextBoxReadOnlyCaretVisible.TextReadOnlyProperty);
            if (be != null) be.UpdateTarget();
        }

        private void OnClick_ButtonClearText(object sender, RoutedEventArgs e)
        {
            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            m_ViewModel.SetDescriptionText(altContent, null);

            DescriptionsListView.Items.Refresh();

            BindingExpression be = DescriptionTextBox.GetBindingExpression(TextBoxReadOnlyCaretVisible.TextReadOnlyProperty);
            if (be != null) be.UpdateTarget();
        }

        private void OnClick_ButtonOpenImage(object sender, RoutedEventArgs e)
        {
            string fullPath = "";
            m_Logger.Log("DescriptionImage.OpenFileDialog", Category.Debug, Priority.Medium);

            var dlg = new OpenFileDialog
            {
                FileName = "",
                DefaultExt = ".jpg",
                Filter = @"JPEG, PNG, BMP (*.jpeg, *.jpg, *.png, *.bmp)|*.jpeg;*.jpg;*.png;*.bmp",
                CheckFileExists = false,
                CheckPathExists = false,
                AddExtension = true,
                DereferenceLinks = true,
                Title = "Tobi: " + "Open image"
            };

            bool? result = false;

            m_ShellView.DimBackgroundWhile(() => { result = dlg.ShowDialog(); });

            if (result == false)
            {
                return;
            }

            fullPath = dlg.FileName;

            if (string.IsNullOrEmpty(fullPath)) return;

            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            m_ViewModel.SetDescriptionImage(altContent, fullPath);

            DescriptionsListView.Items.Refresh();

            BindingExpression be = DescriptionImage.GetBindingExpression(Image.SourceProperty);
            if (be != null) be.UpdateTarget();
        }

        private void OnClick_ButtonClearImage(object sender, RoutedEventArgs e)
        {
            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            m_ViewModel.SetDescriptionImage(altContent, null);

            DescriptionsListView.Items.Refresh();

            BindingExpression be = DescriptionImage.GetBindingExpression(Image.SourceProperty);
            if (be != null) be.UpdateTarget();
        }

        private string showTextEditorPopupDialog(string text, String title)
        {
            m_Logger.Log("showTextEditorPopupDialog", Category.Debug, Priority.Medium);

            var editBox = new TextBox
            {
                Text = text,
                TextWrapping = TextWrapping.WrapWithOverflow,
                AcceptsReturn = true
            };

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   title,
                                                   new ScrollViewer { Content = editBox },
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 300, 160, null, 40);

            editBox.Loaded += new RoutedEventHandler((sender, ev) =>
            {
                editBox.SelectAll();
                FocusHelper.FocusBeginInvoke(editBox);
            });

            windowPopup.ShowModal();


            if (PopupModalWindow.IsButtonOkYesApply(windowPopup.ClickedDialogButton))
            {
                if (string.IsNullOrEmpty(editBox.Text))
                {
                    return null;
                }
                return editBox.Text;
            }

            return null;
        }

        private string showLineEditorPopupDialog(string text, String title)
        {
            m_Logger.Log("showTextEditorPopupDialog", Category.Debug, Priority.Medium);

            var editBox = new TextBox
            {
                Text = text,
                TextWrapping = TextWrapping.NoWrap,
                AcceptsReturn = false
            };

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   title,
                                                   editBox,
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 300, 160, null, 40);
            editBox.SetValue(AutomationProperties.NameProperty, title);

            editBox.Loaded += new RoutedEventHandler((sender, ev) =>
            {
                editBox.SelectAll();
                FocusHelper.FocusBeginInvoke(editBox);
            });

            windowPopup.ShowModal();


            if (PopupModalWindow.IsButtonOkYesApply(windowPopup.ClickedDialogButton))
            {
                if (string.IsNullOrEmpty(editBox.Text))
                {
                    return "";
                }
                return editBox.Text;
            }

            return null;
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
            m_ViewModel.SetSelectedMetadata((Metadata)MetadatasListView.SelectedItem);
        }
        private void OnSelectionChanged_DescriptionsList(object sender, SelectionChangedEventArgs e)
        {
            m_ViewModel.SetSelectedAlternateContent((AlternateContent)DescriptionsListView.SelectedItem);
        }
    }
}
