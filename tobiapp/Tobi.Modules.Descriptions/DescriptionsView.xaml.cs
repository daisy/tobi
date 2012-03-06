using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Practices.Composite;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Unity;
using Microsoft.Win32;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using Tobi.Common.UI.XAML;
using Tobi.Plugin.AudioPane;
using Tobi.Plugin.Urakawa;
using urakawa;
using urakawa.core;
using urakawa.daisy;
using urakawa.data;
using urakawa.exception;
using urakawa.media.data.audio;
using urakawa.media.data.audio.codec;
using urakawa.metadata;
using urakawa.metadata.daisy;
using urakawa.property.alt;
using urakawa.property.channel;
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
                    if (metadata.NameContentAttribute.Name == DiagramContentModelStrings.XmlId)
                    {
                        uid = metadata.NameContentAttribute.Value;
                    }
                    else if (metadata.NameContentAttribute.Name == DiagramContentModelStrings.DescriptionName)
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

                popup.ShowModal();
                return;
            }

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                  UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Descriptions_Lang.CmdEditDescriptions_ShortDesc),
                                                  this,
                                                  PopupModalWindow.DialogButtonsSet.OkCancel,
                                                  PopupModalWindow.DialogButton.Cancel,
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

            //GC.Collect();
            //GC.WaitForFullGCComplete();
        }

        private void OnLoaded_Panel(object sender, RoutedEventArgs e)
        {
            var win = Window.GetWindow(this);
            if (win is PopupModalWindow)
                OwnerWindow = (PopupModalWindow)win;


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
            if (MetadatasListView.IsVisible)
            {
                FocusHelper.Focus(MetadatasListView);
            }
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
            if (DescriptionsListView.IsVisible)
            {
                FocusHelper.Focus(DescriptionsListView);
            }
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

        private bool showMetadataAttributeEditorPopupDialog(string label1, string label2, MetadataAttribute metadataAttr, out string newName, out string newValue, bool isAltContentMetadata)
        {
            m_Logger.Log("Descriptions.MetadataAttributeEditor", Category.Debug, Priority.Medium);

            var label_Name = new TextBlock
            {
                Text = label1 + ": ",
                Margin = new Thickness(8, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap
            };

            var label_Value = new TextBlock
            {
                Text = label2 + ": ",
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

            var editBoxCombo_Name = new ComboBox //WithAutomationPeer
            {
                FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],
        
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
            if (isAltContentMetadata)
            {
                list.AddRange(DiagramContentModelStrings.MetadataNames_ForAltContentDescriptionInstance);
            }
            else
            {
                list.AddRange(DiagramContentModelStrings.MetadataNames_Generic);
            }
            editBoxCombo_Name.ItemsSource = list;

            //    col = new ObservableCollection<string> { "Eric", "Phillip" };
            //combo.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = col });

            var editBox_Value = new TextBoxReadOnlyCaretVisible
            {
                //Watermark = TEXTFIELD_WATERMARK,
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
                var textBox = ComboBoxWithAutomationPeer.GetTextBox(editBoxCombo_Name);
                if (textBox != null)
                {
                    textBox.FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"];
                    textBox.SelectAll();
                }

                FocusHelper.FocusBeginInvoke(editBoxCombo_Name);
            });
            editBox_Value.Loaded += new RoutedEventHandler((sender, ev) =>
            {
                editBox_Value.SelectAll();
                //FocusHelper.FocusBeginInvoke(editBox_Name);
            });

            WatermarkComboBoxBehavior.SetEnableWatermark(editBoxCombo_Name, true);
            WatermarkComboBoxBehavior.SetLabel(editBoxCombo_Name, TEXTFIELD_WATERMARK);

            Style style = (Style)Application.Current.Resources[@"WatermarkTextBoxStyle"];
            WatermarkComboBoxBehavior.SetLabelStyle(editBoxCombo_Name, style);


            WatermarkTextBoxBehavior.SetEnableWatermark(editBox_Value, true);
            WatermarkTextBoxBehavior.SetLabel(editBox_Value, TEXTFIELD_WATERMARK);

            //Style style = (Style)Application.Current.Resources[@"WatermarkTextBoxStyle"];
            WatermarkTextBoxBehavior.SetLabelStyle(editBox_Value, style);


            windowPopup.ShowModal();

            WatermarkComboBoxBehavior.SetEnableWatermark(editBoxCombo_Name, false);
            WatermarkTextBoxBehavior.SetEnableWatermark(editBox_Value, false);

            if (PopupModalWindow.IsButtonOkYesApply(windowPopup.ClickedDialogButton))
            {
                newName = editBoxCombo_Name.Text.Trim();
                newValue = editBox_Value.Text.Trim();

                return true;
            }

            newName = null;
            newValue = null;

            return false;
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
            if (MetadatasAltContentListView.SelectedIndex >= 0)
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
            string newName = null;
            string newValue = null;

            var mdAttrTEMP = new MetadataAttribute();
            mdAttrTEMP.Name = md.NameContentAttribute.Name;
            mdAttrTEMP.Value = md.NameContentAttribute.Value;

            bool ok = true;
            while (ok &&
                (
                string.IsNullOrEmpty(newName)
                || string.IsNullOrEmpty(newValue)
                || newName == md.NameContentAttribute.Name && newValue == md.NameContentAttribute.Value
                )
            )
            {
                ok = showMetadataAttributeEditorPopupDialog("Name", "Value", mdAttrTEMP, out newName, out newValue, true);
                mdAttrTEMP.Name = newName;
                mdAttrTEMP.Value = newValue;
            }
            if (!ok) return;

            //bool ok = showMetadataAttributeEditorPopupDialog(md.NameContentAttribute, out newName, out newValue, true);
            //if (ok &&
            //    (newName != md.NameContentAttribute.Name || newValue != md.NameContentAttribute.Value))
            //{
            foreach (Metadata m in altContent.Metadatas.ContentsAs_Enumerable)
            {
                if (md == m)
                {
                    continue;
                }

                if (m.NameContentAttribute.Name == newName)
                {
                    var label = new TextBlock
                    {
                        Text = "This attribute already exists.",
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

                    var windowPopup = new PopupModalWindow(m_ShellView,
                                                         UserInterfaceStrings.EscapeMnemonic("Duplicate attribute!"),
                                                         panel,
                                                         PopupModalWindow.DialogButtonsSet.Ok,
                                                         PopupModalWindow.DialogButton.Ok,
                                                         true, 300, 160, null, 0);
                    //view.OwnerWindow = windowPopup;

                    windowPopup.ShowModal();
                    return;
                }
            }

            m_ViewModel.SetMetadataAttr(null, altContent, md, null, newName, newValue);

            MetadatasAltContentListView.Items.Refresh();
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
            string newName = null;
            string newValue = null;

            var mdAttrTEMP = new MetadataAttribute();
            mdAttrTEMP.Name = md.NameContentAttribute.Name;
            mdAttrTEMP.Value = md.NameContentAttribute.Value;

            bool ok = true;
            while (ok &&
                (
                string.IsNullOrEmpty(newName)
                || string.IsNullOrEmpty(newValue)
                || newName == md.NameContentAttribute.Name && newValue == md.NameContentAttribute.Value
                )
            )
            {
                ok = showMetadataAttributeEditorPopupDialog("Property", "Content", mdAttrTEMP, out newName, out newValue, false);
                mdAttrTEMP.Name = newName;
                mdAttrTEMP.Value = newValue;
            }
            if (!ok) return;

            //bool ok = showMetadataAttributeEditorPopupDialog(md.NameContentAttribute, out newName, out newValue, false);
            //if (ok &&
            //    (newName != md.NameContentAttribute.Name || newValue != md.NameContentAttribute.Value))
            //{
            m_ViewModel.SetMetadataAttr(altProp, null, md, null, newName, newValue);

            MetadatasListView.Items.Refresh();
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
            string newName = null;
            string newValue = null;

            var mdAttrTEMP = new MetadataAttribute();
            mdAttrTEMP.Name = mdAttr.Name;
            mdAttrTEMP.Value = mdAttr.Value;

            bool ok = true;
            while (ok &&
                (
                string.IsNullOrEmpty(newName)
                || string.IsNullOrEmpty(newValue)
                || newName == mdAttr.Name && newValue == mdAttr.Value
                )
            )
            {
                ok = showMetadataAttributeEditorPopupDialog("Name", "Value", mdAttrTEMP, out newName, out newValue, false);
                mdAttrTEMP.Name = newName;
                mdAttrTEMP.Value = newValue;
            }
            if (!ok) return;

            //bool ok = showMetadataAttributeEditorPopupDialog(mdAttr, out newName, out newValue, false);
            //if (ok &&
            //    (newName != mdAttr.Name || newValue != mdAttr.Value))
            //{
            m_ViewModel.SetMetadataAttr(altProp, null, md, mdAttr, newName, newValue);

            MetadataAttributesListView.Items.Refresh();
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

            int selectedIndex = MetadatasAltContentListView.SelectedIndex;
            if (selectedIndex < 0) return;
            m_ViewModel.RemoveMetadata(null, altContent, (Metadata)MetadatasAltContentListView.SelectedItem);
            MetadatasAltContentListView.Items.Refresh();
            if (MetadatasAltContentListView.Items.Count > 0)
            {
                MetadatasAltContentListView.SelectedIndex = selectedIndex < MetadatasAltContentListView.Items.Count
                                                                ? selectedIndex
                                                                : MetadatasAltContentListView.Items.Count - 1;
            }
            //FocusHelper.FocusBeginInvoke(MetadatasAltContentListView);
        }

        private string TEXTFIELD_WATERMARK = "[enter text here]";

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
            mdAttr.Name = ""; // PROMPT_MD_NAME;
            mdAttr.Value = ""; // PROMPT_MD_VALUE;
            string newName = null;
            string newValue = null;

            bool ok = true;
            while (ok &&
                (
                string.IsNullOrEmpty(newName)
                || string.IsNullOrEmpty(newValue)
                //|| newName == PROMPT_MD_NAME
                //|| newValue == PROMPT_MD_VALUE
                )
            )
            {
                ok = showMetadataAttributeEditorPopupDialog("Name", "Value", mdAttr, out newName, out newValue, true);
                mdAttr.Name = newName;
                mdAttr.Value = newValue;
            }
            if (!ok) return;

            //bool ok = showMetadataAttributeEditorPopupDialog(mdAttr, out newName, out newValue, true);
            //if (ok &&
            //    newName != mdAttr.Name && newValue != mdAttr.Value)
            //{
            foreach (Metadata m in altContent.Metadatas.ContentsAs_Enumerable)
            {
                if (m.NameContentAttribute.Name == newName)
                {
                    var label = new TextBlock
                    {
                        Text = "This attribute already exists.",
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

                    var windowPopup = new PopupModalWindow(m_ShellView,
                                                         UserInterfaceStrings.EscapeMnemonic("Duplicate attribute!"),
                                                         panel,
                                                         PopupModalWindow.DialogButtonsSet.Ok,
                                                         PopupModalWindow.DialogButton.Ok,
                                                         true, 300, 160, null, 0);
                    //view.OwnerWindow = windowPopup;

                    windowPopup.ShowModal();
                    return;
                }
            }


            m_ViewModel.AddMetadata(null, altContent, newName, newValue);
            MetadatasAltContentListView.Items.Refresh();
            MetadatasAltContentListView.SelectedIndex = MetadatasAltContentListView.Items.Count - 1;
            //FocusHelper.FocusBeginInvoke(MetadatasAltContentListView);
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

            int selectedIndex = MetadatasListView.SelectedIndex;
            if (selectedIndex < 0) return;
            m_ViewModel.RemoveMetadata(altProp, null, (Metadata)MetadatasListView.SelectedItem);
            MetadatasListView.Items.Refresh();
            if (MetadatasListView.Items.Count > 0)
            {
                MetadatasListView.SelectedIndex = selectedIndex < MetadatasListView.Items.Count
                                                                ? selectedIndex
                                                                : MetadatasListView.Items.Count - 1;
            }
            //FocusHelper.FocusBeginInvoke(MetadatasListView);
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
            mdAttr.Name = ""; // PROMPT_MD_NAME;
            mdAttr.Value = ""; // PROMPT_MD_VALUE;
            string newName = null;
            string newValue = null;

            bool ok = true;
            while (ok &&
                (
                string.IsNullOrEmpty(newName)
                || string.IsNullOrEmpty(newValue)
                //|| newName == PROMPT_MD_NAME
                //|| newValue == PROMPT_MD_VALUE
                )
            )
            {
                ok = showMetadataAttributeEditorPopupDialog("Property", "Content", mdAttr, out newName, out newValue, false);
                mdAttr.Name = newName;
                mdAttr.Value = newValue;
            }
            if (!ok) return;

            //bool ok = showMetadataAttributeEditorPopupDialog(mdAttr, out newName, out newValue, false);
            //if (ok &&
            //    newName != mdAttr.Name && newValue != mdAttr.Value)
            //{
            m_ViewModel.AddMetadata(altProp, null, newName, newValue);
            MetadatasListView.Items.Refresh();
            MetadatasListView.SelectedIndex = MetadatasListView.Items.Count - 1;
            //FocusHelper.FocusBeginInvoke(MetadatasListView);
        }

        private void OnClick_ButtonRemoveMetadataAttr(object sender, RoutedEventArgs e)
        {
            int selectedIndex = MetadatasListView.SelectedIndex;
            if (selectedIndex < 0) return;
            Metadata md = (Metadata)MetadatasListView.SelectedItem;

            selectedIndex = MetadataAttributesListView.SelectedIndex;
            if (selectedIndex < 0) return;

            m_ViewModel.RemoveMetadataAttr(md, (MetadataAttribute)MetadataAttributesListView.SelectedItem);

            MetadataAttributesListView.Items.Refresh();
            if (MetadataAttributesListView.Items.Count > 0)
            {
                MetadataAttributesListView.SelectedIndex = selectedIndex < MetadataAttributesListView.Items.Count
                                                                ? selectedIndex
                                                                : MetadataAttributesListView.Items.Count - 1;
            }
            //FocusHelper.FocusBeginInvoke(MetadataAttributesListView);
        }

        private void OnClick_ButtonAddMetadataAttr(object sender, RoutedEventArgs e)
        {
            if (MetadatasListView.SelectedIndex < 0) return;
            Metadata md = (Metadata)MetadatasListView.SelectedItem;

            var mdAttr = new MetadataAttribute();
            mdAttr.Name = ""; // PROMPT_MD_NAME;
            mdAttr.Value = ""; // PROMPT_MD_VALUE;
            string newName = null;
            string newValue = null;

            bool ok = true;
            while (ok &&
                (
                string.IsNullOrEmpty(newName)
                || string.IsNullOrEmpty(newValue)
                //|| newName == PROMPT_MD_NAME
                //|| newValue == PROMPT_MD_VALUE
                )
            )
            {
                ok = showMetadataAttributeEditorPopupDialog("Name", "Value", mdAttr, out newName, out newValue, false);
                mdAttr.Name = newName;
                mdAttr.Value = newValue;
            }
            if (!ok) return;

            //bool ok = showMetadataAttributeEditorPopupDialog(mdAttr, out newName, out newValue, false);
            //if (ok &&
            //    newName != mdAttr.Name && newValue != mdAttr.Value)
            //{
            m_ViewModel.AddMetadataAttr(md, newName, newValue);
            MetadataAttributesListView.Items.Refresh();
            MetadataAttributesListView.SelectedIndex = MetadataAttributesListView.Items.Count - 1;
            //FocusHelper.FocusBeginInvoke(MetadataAttributesListView);
        }

        private bool isUniqueIdInvalid(string txt, string promptedTxt)
        {
            return txt == "" || txt == promptedTxt;
        }
        private bool isDescriptionNameInvalid(string txt, string promptedTxt)
        {
            return txt == "" || txt == promptedTxt;
        }

        private void OnClick_ButtonAddDescription(object sender, RoutedEventArgs e)
        {
            string txt = ""; // PROMPT_DescriptionName;
            string descriptionName = "";
            while (descriptionName != null && isDescriptionNameInvalid(descriptionName, "")) // PROMPT_DescriptionName))
            {
                // returns null only when dialog is cancelled, otherwise trimmed string (potentially empty)
                descriptionName = showLineEditorPopupDialog(txt, "Description name", DiagramContentModelStrings.MetadataValues_ForDescriptionName);
                txt = descriptionName;
            }
            if (descriptionName == null) return;

            txt = ""; // PROMPT_ID;
            string uid = "";
            while (uid != null && isUniqueIdInvalid(uid, "")) // PROMPT_ID))
            {
                // returns null only when dialog is cancelled, otherwise trimmed string (potentially empty)
                uid = showLineEditorPopupDialog(txt, "Unique identifier", null);
                txt = uid;
            }
            if (uid == null) return;

            m_ViewModel.AddDescription(uid, descriptionName);

            DescriptionsListView.Items.Refresh();
            DescriptionsListView.SelectedIndex = DescriptionsListView.Items.Count - 1;
            //FocusHelper.FocusBeginInvoke(DescriptionsListView);

            MetadatasAltContentListView.Items.Refresh();
            if (MetadatasAltContentListView.Items.Count > 0)
            {
                MetadatasAltContentListView.SelectedIndex = MetadatasAltContentListView.Items.Count - 1;
            }
            //FocusHelper.FocusBeginInvoke(MetadatasAltContentListView);

            //BindingExpression be = DescriptionTextBox.GetBindingExpression(TextBoxReadOnlyCaretVisible.TextReadOnlyProperty);
            //if (be != null) be.UpdateTarget();
        }

        private void OnClick_ButtonRemoveDescription(object sender, RoutedEventArgs e)
        {
            int selectedIndex = DescriptionsListView.SelectedIndex;
            if (selectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            m_ViewModel.RemoveDescription(altContent);

            DescriptionsListView.Items.Refresh();
            if (DescriptionsListView.Items.Count > 0)
            {
                DescriptionsListView.SelectedIndex = selectedIndex < DescriptionsListView.Items.Count
                                                                ? selectedIndex
                                                                : DescriptionsListView.Items.Count - 1;
            }
            //FocusHelper.FocusBeginInvoke(DescriptionsListView);
        }

        private void OnClick_ButtonEditText(object sender, RoutedEventArgs e)
        {
            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            string oldTxt = altContent.Text != null ? altContent.Text.Text : "";

            // returns null only when dialog is cancelled, otherwise trimmed string (potentially empty)
            string txt = showTextEditorPopupDialog(oldTxt, "Edit description text");

            if (txt == null) return; // cancel
            if (txt == "" || txt == oldTxt) return;

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


        // WE NEED TO CAPTURE TOGGLE COMMANDS SUCH AS PLAY/PAUSE, RECORD, MONITOR (same KeyGesture, different Commands).
        private PopupModalWindow m_AudioPopupModalWindow;

        private void OnClick_ButtonAddEditAudio(object sender, RoutedEventArgs e)
        {
            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            Tuple<TreeNode, TreeNode> selection = m_Session.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            if (altProp.AlternateContents.IndexOf(altContent) < 0) return;


            if (AudioMediaElement.Clock != null)
            {
                AudioMediaElement.Clock.Controller.Stop();
            }



            var pres = m_Session.DocumentProject.Presentations.Get(0);

            var project = new Project();
            project.SetPrettyFormat(m_Session.DocumentProject.IsPrettyFormat());

            // a proxy project/presentation/treenode (and UrakawaSession wrapper) to bridge the standard audio recording feature, without altering the main document.
            var presentation = new Presentation();
            presentation.Project = project;
            presentation.RootUri = pres.RootUri;
            int index = pres.DataProviderManager.DataFileDirectory.IndexOf(DataProviderManager.DefaultDataFileDirectorySeparator + DataProviderManager.DefaultDataFileDirectory);
            string prefix = pres.DataProviderManager.DataFileDirectory.Substring(0, index);
            string suffix = "--IMAGE_DESCRIPTIONS_TEMP_AUDIO";
            //DebugFix.Assert(Path.GetFileName(pres.RootUri.LocalPath) == prefix);
            presentation.DataProviderManager.SetDataFileDirectoryWithPrefix(prefix + suffix);
            presentation.MediaDataManager.DefaultPCMFormat = pres.MediaDataManager.DefaultPCMFormat.Copy();
            presentation.MediaDataManager.EnforceSinglePCMFormat = true;

            //DebugFix.Assert(presentation.DataProviderManager.DataFileDirectoryFullPath == pres.DataProviderManager.DataFileDirectoryFullPath + suffix);

            var audioChannel = presentation.ChannelFactory.CreateAudioChannel();
            audioChannel.Name = "The DESCRIPTION Audio Channel";

            project.Presentations.Insert(0, presentation);

            var treeNode = presentation.TreeNodeFactory.Create();
            presentation.RootNode = treeNode;

            if (altContent.Audio != null)
            {
                ManagedAudioMedia audio1 = presentation.MediaFactory.CreateManagedAudioMedia();
                AudioMediaData audioData1 = presentation.MediaDataFactory.CreateAudioMediaData();
                audio1.AudioMediaData = audioData1;

                // WARNING: WavAudioMediaData implementation differs from AudioMediaData:
                // the latter is naive and performs a stream binary copy, the latter is optimized and re-uses existing WavClips. 
                //  WARNING 2: The audio data from the given parameter gets emptied !
                //audio1.AudioMediaData.MergeWith(manMedia.AudioMediaData);

                if (!audio1.AudioMediaData.PCMFormat.Data.IsCompatibleWith(altContent.Audio.AudioMediaData.PCMFormat.Data))
                {
                    throw new InvalidDataFormatException(
                        "Can not merge description audio with a AudioMediaData with incompatible audio data");
                }
                Stream stream = altContent.Audio.AudioMediaData.OpenPcmInputStream();
                try
                {
                    audio1.AudioMediaData.AppendPcmData(stream, null); //manMedia.AudioMediaData.AudioDuration
                }
                finally
                {
                    stream.Close();
                }

                ChannelsProperty chProp = presentation.RootNode.GetOrCreateChannelsProperty();
                chProp.SetMedia(audioChannel, audio1);
            }


            var audioEventAggregator = new EventAggregator();

            var audioSession = new UrakawaSession(
                m_Logger,
                m_Container,
                audioEventAggregator, //m_EventAggregator,
                this //m_ShellView
                );
            audioSession.DocumentProject = project;

            var audioViewModel = new AudioPaneViewModel(
                m_Logger,
                audioEventAggregator, //m_EventAggregator,
                this, //m_ShellView,
                audioSession
                );
            audioViewModel.IsSimpleMode = true;
            audioViewModel.InputBindingManager = this; //m_ShellView
            //m_audioViewModel.PlaybackRate = xxx; TODO: copy from main document session

            var audioView = new AudioPaneView(
                m_Logger,
                audioEventAggregator, //m_EventAggregator,
                audioViewModel,
                this //m_ShellView
                );

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                  UserInterfaceStrings.EscapeMnemonic("(audio) " + Tobi_Plugin_Descriptions_Lang.CmdEditDescriptions_ShortDesc),
                                                  audioView,
                                                  PopupModalWindow.DialogButtonsSet.OkCancel,
                                                  PopupModalWindow.DialogButton.Cancel,
                                                  true, 850, 320, null, 0);

            windowPopup.IgnoreEscape = true;

            //UIElement win = TryFindParent<Window>(this);

            // WE HAND PICK THE COMMAND KEY BINDINGS INSTEAD OF RELYING ON AUTO REGISTRATION!

            windowPopup.AddInputBinding(audioViewModel.CommandPause.KeyBinding);
            windowPopup.AddInputBinding(audioViewModel.CommandPlay.KeyBinding);

            windowPopup.AddInputBinding(audioViewModel.CommandStopMonitor.KeyBinding);
            windowPopup.AddInputBinding(audioViewModel.CommandStartMonitor.KeyBinding);

            windowPopup.AddInputBinding(audioViewModel.CommandStopRecord.KeyBinding);
            windowPopup.AddInputBinding(audioViewModel.CommandStartRecord.KeyBinding);

            windowPopup.AddInputBinding(audioViewModel.CommandInsertFile.KeyBinding);

            windowPopup.AddInputBinding(audioViewModel.CommandPlayPreviewLeft.KeyBinding);
            windowPopup.AddInputBinding(audioViewModel.CommandPlayPreviewRight.KeyBinding);

            windowPopup.AddInputBinding(audioViewModel.CommandRewind.KeyBinding);
            windowPopup.AddInputBinding(audioViewModel.CommandFastForward.KeyBinding);

            windowPopup.AddInputBinding(audioViewModel.CommandGotoBegining.KeyBinding);
            windowPopup.AddInputBinding(audioViewModel.CommandGotoEnd.KeyBinding);

            windowPopup.AddInputBinding(audioViewModel.CommandDeleteAudioSelection.KeyBinding);

            windowPopup.AddInputBinding(audioViewModel.CommandSelectAll.KeyBinding);
            windowPopup.AddInputBinding(audioViewModel.CommandSelectLeft.KeyBinding);
            windowPopup.AddInputBinding(audioViewModel.CommandSelectRight.KeyBinding);

            windowPopup.AddInputBinding(audioViewModel.CommandClearSelection.KeyBinding);

            windowPopup.AddInputBinding(audioViewModel.CommandZoomFitFull.KeyBinding);
            windowPopup.AddInputBinding(audioViewModel.CommandZoomSelection.KeyBinding);

            windowPopup.AddInputBinding(audioSession.UndoCommand.KeyBinding);
            windowPopup.AddInputBinding(audioSession.RedoCommand.KeyBinding);

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
            //        windowPopup.AddInputBinding(keyBinding);
            //    }
            //}

            //windowPopup.InputBindings.AddRange(Application.Current.MainWindow.InputBindings);

            windowPopup.KeyUp += (object o, KeyEventArgs ev) =>
            {
                var key = (ev.Key == Key.System
                                ? ev.SystemKey
                                : (ev.Key == Key.ImeProcessed ? ev.ImeProcessedKey : ev.Key));

                if (key == Key.Escape)
                {
                    audioEventAggregator.GetEvent<EscapeEvent>().Publish(null);
                }
            };

            windowPopup.Closed += (o, ev) => Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                (Action)(() =>
                {
                    //
                }));

            //presentation.UndoRedoManager.StartTransaction
            //    ("(AUDIO) " + Tobi_Plugin_Descriptions_Lang.CmdEditDescriptions_ShortDesc,
            //    "(AUDIO) " + Tobi_Plugin_Descriptions_Lang.CmdEditDescriptions_LongDesc);



            windowPopup.Loaded += (o, ev) => Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                (Action)(() =>
                {
                    audioViewModel.OnProjectLoaded(audioSession.DocumentProject);

                    //Tuple<TreeNode, TreeNode> treeNodeSelection = m_Session.GetTreeNodeSelection();
                    //--
                    audioSession.PerformTreeNodeSelection(audioSession.DocumentProject.Presentations.Get(0).RootNode);
                    //--
                    //m_audioSession.ForceInitTreeNodeSelection(m_audioSession.DocumentProject.Presentations.Get(0).RootNode);
                    //
                    //var treeNodeSelection = new Tuple<TreeNode, TreeNode>(m_audioSession.DocumentProject.Presentations.Get(0).RootNode, null);
                    //var oldTreeNodeSelection = new Tuple<TreeNode, TreeNode>(treeNodeSelection.Item1.Parent, null);
                    //var tuple = new Tuple<Tuple<TreeNode, TreeNode>, Tuple<TreeNode, TreeNode>>(oldTreeNodeSelection, treeNodeSelection);
                    //m_audioViewModel.OnTreeNodeSelectionChanged(tuple);

                }));


            m_AudioPopupModalWindow = windowPopup;
            windowPopup.ShowModal();
            m_AudioPopupModalWindow = null;

            audioViewModel.OnProjectUnLoaded(audioSession.DocumentProject);

            //bool empty = m_audioSession.DocumentProject.Presentations.Get(0).UndoRedoManager.IsTransactionEmpty;



            if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Ok)
            {
                //presentation.UndoRedoManager.EndTransaction();

                //if (DescriptionsListView.SelectedIndex < 0) return;
                //AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

                // Can be null (or empty audio media data), but that's ok.
                ManagedAudioMedia manMedia_ = presentation.RootNode.GetManagedAudioMedia();

                m_ViewModel.SetDescriptionAudio(altContent, manMedia_);

                DescriptionsListView.Items.Refresh();

                //presentation.UndoRedoManager.Undo();
            }
            else
            {
                //presentation.UndoRedoManager.CancelTransaction();
            }

            //while (presentation.UndoRedoManager.CanUndo)
            //{
            //    presentation.UndoRedoManager.Undo();
            //}

            presentation.UndoRedoManager.FlushCommands();

            ManagedAudioMedia manMedia = presentation.RootNode.GetManagedAudioMedia();
            if (manMedia != null)
            {
                manMedia.AudioMediaData = null;
            }

            string deletedDataFolderPath = audioSession.DataCleanup(false);
            string[] files = Directory.GetFiles(deletedDataFolderPath);
            if (files.Length != 0)
            {
                //m_ShellView.ExecuteShellProcess(deletedDataFolderPath);

                //TODO: delete containing folder(s) ?
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }

            resetAudioPlayer();
        }

        private void OnClick_ButtonClearAudio(object sender, RoutedEventArgs e)
        {
            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            if (AudioMediaElement.Clock != null)
            {
                AudioMediaElement.Clock.Controller.Stop();
            }

            m_ViewModel.SetDescriptionAudio(altContent, null);

            DescriptionsListView.Items.Refresh();
        }

        private string showTextEditorPopupDialog(string editedText, String dialogTitle)
        {
            m_Logger.Log("showTextEditorPopupDialog", Category.Debug, Priority.Medium);

            var editBox = new TextBoxReadOnlyCaretVisible
            {
            FocusVisualStyle = (Style) Application.Current.Resources["MyFocusVisualStyle"],
        
                Text = editedText,
                TextWrapping = TextWrapping.WrapWithOverflow,
                AcceptsReturn = true
            };

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   dialogTitle,
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
                string str = editBox.Text == null ? "" : editBox.Text.Trim();
                //if (string.IsNullOrEmpty(str))
                //{
                //    return "";
                //}
                return str;
            }

            return null;
        }

        private string showLineEditorPopupDialog(string editedText, string dialogTitle, List<string> predefinedCandidates)
        {
            m_Logger.Log("showTextEditorPopupDialog", Category.Debug, Priority.Medium);

            if (predefinedCandidates == null)
            {
                var editBox = new TextBoxReadOnlyCaretVisible
                {
            FocusVisualStyle = (Style) Application.Current.Resources["MyFocusVisualStyle"],
        
                    //Watermark = TEXTFIELD_WATERMARK,
                    Text = editedText,
                    TextWrapping = TextWrapping.NoWrap,
                    AcceptsReturn = false
                };

                var panel = new StackPanel();
                panel.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
                panel.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
                editBox.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
                panel.Children.Add(editBox);

                var windowPopup = new PopupModalWindow(m_ShellView,
                                                       dialogTitle,
                                                       panel,
                                                       PopupModalWindow.DialogButtonsSet.OkCancel,
                                                       PopupModalWindow.DialogButton.Ok,
                                                       true, 300, 160, null, 40);
                editBox.SetValue(AutomationProperties.NameProperty, dialogTitle);

                editBox.Loaded += new RoutedEventHandler((sender, ev) =>
                                                             {
                                                                 editBox.SelectAll();
                                                                 FocusHelper.FocusBeginInvoke(editBox);
                                                             });

                WatermarkTextBoxBehavior.SetEnableWatermark(editBox, true);
                WatermarkTextBoxBehavior.SetLabel(editBox, TEXTFIELD_WATERMARK);

                Style style = (Style)Application.Current.Resources[@"WatermarkTextBoxStyle"];
                WatermarkTextBoxBehavior.SetLabelStyle(editBox, style);

                windowPopup.ShowModal();

                WatermarkTextBoxBehavior.SetEnableWatermark(editBox, false);

                if (PopupModalWindow.IsButtonOkYesApply(windowPopup.ClickedDialogButton))
                {
                    string str = editBox.Text == null ? "" : editBox.Text.Trim();
                    //if (string.IsNullOrEmpty(str))
                    //{
                    //    return "";
                    //}
                    return str;
                }

                return null;
            }
            else
            {
                var editBoxCombo_Name = new ComboBox //WithAutomationPeer
                {
                    FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],
        
                    Text = editedText,
                    IsEditable = true,
                    IsTextSearchEnabled = true,
#if NET40
                    IsTextSearchCaseSensitive = false
#endif //NET40
                };

                editBoxCombo_Name.ItemsSource = predefinedCandidates;

                var panel = new StackPanel();
                panel.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
                panel.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
                editBoxCombo_Name.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
                panel.Children.Add(editBoxCombo_Name);

                var windowPopup = new PopupModalWindow(m_ShellView,
                                                       dialogTitle,
                                                       panel,
                                                       PopupModalWindow.DialogButtonsSet.OkCancel,
                                                       PopupModalWindow.DialogButton.Ok,
                                                       true, 300, 160, null, 40);

                editBoxCombo_Name.Loaded += new RoutedEventHandler((sender, ev) =>
                {
                    var textBox = ComboBoxWithAutomationPeer.GetTextBox(editBoxCombo_Name);
                    if (textBox != null)
                    {
                        textBox.FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"];
                        textBox.SelectAll();
                    }

                    FocusHelper.FocusBeginInvoke(editBoxCombo_Name);
                });

                WatermarkComboBoxBehavior.SetEnableWatermark(editBoxCombo_Name, true);
                WatermarkComboBoxBehavior.SetLabel(editBoxCombo_Name, TEXTFIELD_WATERMARK);

                Style style = (Style)Application.Current.Resources[@"WatermarkTextBoxStyle"];
                WatermarkComboBoxBehavior.SetLabelStyle(editBoxCombo_Name, style);

                windowPopup.ShowModal();

                WatermarkComboBoxBehavior.SetEnableWatermark(editBoxCombo_Name, false);

                if (PopupModalWindow.IsButtonOkYesApply(windowPopup.ClickedDialogButton))
                {
                    string str = editBoxCombo_Name.Text == null ? "" : editBoxCombo_Name.Text.Trim();
                    //if (string.IsNullOrEmpty(str))
                    //{
                    //    return "";
                    //}
                    return str;
                }

                return null;
            }
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
            resetAudioPlayer();
        }




        public event PropertyChangedEventHandler PropertyChanged;
        public void DispatchPropertyChangedEvent(PropertyChangedEventArgs e)
        {
            m_ShellView.DispatchPropertyChangedEvent(e);
        }

        public RichDelegateCommand ExitCommand
        {
            get { return m_ShellView.ExitCommand; }
        }

        public void RaiseEscapeEvent()
        {
            m_ShellView.RaiseEscapeEvent();
        }

        public IActiveAware ActiveAware
        {
            get { return m_ShellView.ActiveAware; }
        }

        public event EventHandler DeviceRemoved;
        public event EventHandler DeviceArrived;

        public void PumpDispatcherFrames(DispatcherPriority prio)
        {
            m_ShellView.PumpDispatcherFrames(prio);
        }

        public bool IsUIAutomationDisabled
        {
            get { return m_ShellView.IsUIAutomationDisabled; }
        }

        public void Show()
        {
            m_ShellView.Show();
        }

        public bool SplitterDrag
        {
            get { return m_ShellView.SplitterDrag; }
        }

        public double MagnificationLevel
        {
            get { return m_ShellView.MagnificationLevel; }
            set { m_ShellView.MagnificationLevel = value; }
        }

        public VisualBrush LoadTangoIcon(string resourceKey)
        {
            return m_ShellView.LoadTangoIcon(resourceKey);
        }

        public VisualBrush LoadGnomeNeuIcon(string resourceKey)
        {
            return m_ShellView.LoadGnomeNeuIcon(resourceKey);
        }

        public VisualBrush LoadGnomeGionIcon(string resourceKey)
        {
            return m_ShellView.LoadGnomeGionIcon(resourceKey);
        }

        public VisualBrush LoadGnomeFoxtrotIcon(string resourceKey)
        {
            return m_ShellView.LoadGnomeFoxtrotIcon(resourceKey);
        }

        public void DimBackgroundWhile(Action action)
        {
            m_ShellView.DimBackgroundWhile(action);
        }

        public void ExecuteShellProcess(string shellCmd)
        {
            m_ShellView.ExecuteShellProcess(shellCmd);
        }

        public bool RunModalCancellableProgressTask(bool inSeparateThread, string title, IDualCancellableProgressReporter reporter, Action actionCancelled, Action actionCompleted)
        {
            return m_ShellView.RunModalCancellableProgressTask(inSeparateThread, title, reporter, actionCancelled, actionCompleted);
        }

        public void RegisterRichCommand(RichDelegateCommand command)
        {
            //AddInputBinding(command.KeyBinding);
        }
        public bool AddInputBinding(InputBinding inputBinding)
        {
            if (m_AudioPopupModalWindow != null)
            {
                return m_AudioPopupModalWindow.AddInputBinding(inputBinding);
            }
            return true;
        }
        public void RemoveInputBinding(InputBinding inputBinding)
        {
            if (m_AudioPopupModalWindow != null)
            {
                m_AudioPopupModalWindow.RemoveInputBinding(inputBinding);
            }
        }

        //        public void RemoveSubInputBindingManager(IInputBindingManager ibm)
        //        {
        //#if DEBUG
        //            Debugger.Break();
        //#endif // DEBUG
        //        }

        //        public void AddSubInputBindingManager(IInputBindingManager ibm)
        //        {
        //#if DEBUG
        //            Debugger.Break();
        //#endif // DEBUG
        //        }

        private void resetAudioPlayer()
        {
            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            Tuple<TreeNode, TreeNode> selection = m_Session.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            if (altProp.AlternateContents.IndexOf(altContent) < 0) return;



            if (AudioMediaElement.Clock != null)
            {
                AudioMediaElement.Clock.Controller.Stop();
                AudioMediaElement.Clock.Controller.Remove();

                AudioMediaElement.Close();
                AudioMediaElement.Clock = null;
            }


            //if (AudioMediaElement.Source != null)
            //{
            //    AudioMediaElement.Close();
            //    AudioMediaElement.Source = null;
            //}


            m_SliderValueChangeFromCode = true;
            AudioTimeSlider.Value = 0;

            if (altContent.Audio == null)
            {
                AudioTimeSlider.Maximum = 100;
                return;
            }

            string path = null;
            foreach (var dataProv in altContent.Audio.AudioMediaData.UsedDataProviders)
            {
                path = ((FileDataProvider)dataProv).DataFileFullPath;
                break;
            }
            if (String.IsNullOrEmpty(path))
            {
                return;
            }


            var mediaTimeline = new MediaTimeline(new Uri(path));
            mediaTimeline.CurrentTimeInvalidated += new EventHandler(mediaTimeline_CurrentTimeInvalidated);
            AudioMediaElement.Clock = mediaTimeline.CreateClock(true) as MediaClock;
            AudioMediaElement.Clock.Controller.Stop();

            //AudioMediaElement.Source = new Uri(path);
            //AudioMediaElement.Clock.CurrentTimeInvalidated += new EventHandler(mediaTimeline_CurrentTimeInvalidated);

            //if (mediaPlayer == null)
            //{
            //    mediaPlayer = new MediaPlayer();
            //    mediaPlayer.Volume = 1;
            //    mediaPlayer.MediaOpened += new EventHandler(AudioElement_MediaOpened);
            //    mediaPlayer.MediaEnded += new EventHandler(AudioElement_MediaEnded);
            //}
            //mediaPlayer.Open(new Uri(path));

        }

        //private MediaPlayer mediaPlayer;

        private bool m_SliderValueChangeFromCode;

        private void mediaTimeline_CurrentTimeInvalidated(object sender, EventArgs e)
        {
            if (m_SliderDragging) return;

            if (AudioMediaElement.Clock == null) return;

            if (AudioMediaElement.Clock.CurrentState == ClockState.Filling)
            {
                AudioElement_MediaEnded(null, null);
                return;
            }

            if (AudioMediaElement.Clock.CurrentTime.HasValue)
            {
                m_SliderValueChangeFromCode = true;
                AudioTimeSlider.Value = AudioMediaElement.Clock.CurrentTime.Value.TotalMilliseconds;
            }
        }

        private void AudioElement_MediaOpened(object sender, EventArgs e)
        {
            if (AudioMediaElement.NaturalDuration.HasTimeSpan)
            {
                AudioTimeSlider.Maximum = AudioMediaElement.NaturalDuration.TimeSpan.TotalMilliseconds;
            }

            if (AudioMediaElement.Clock == null) return;

            AudioMediaElement.Clock.Controller.Stop();
        }

        private void AudioElement_MediaEnded(object sender, EventArgs e)
        {
            if (AudioMediaElement.Clock == null) return;

            AudioMediaElement.Clock.Controller.Stop();

            m_SliderValueChangeFromCode = true;
            AudioTimeSlider.Value = 0;
        }


        private bool m_SliderDragging = false;

        private void OnDragCompleted_AudioTimeSlider(object sender, DragCompletedEventArgs e)
        {
            if (!AudioMediaElement.NaturalDuration.HasTimeSpan)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    resetAudioPlayer();
                }));

                return;
            }

            if (AudioMediaElement.Clock == null) return;

            if (AudioMediaElement.Clock.CurrentState == ClockState.Filling)
            {
                AudioMediaElement.Clock.Controller.Stop();
                AudioMediaElement.Clock.Controller.Pause();
            }

            int SliderValue = (int)AudioTimeSlider.Value;

            TimeSpan ts = new TimeSpan(0, 0, 0, 0, SliderValue);

            //AudioMediaElement.Clock.Controller.SeekAlignedToLastTick(ts, TimeSeekOrigin.BeginTime);
            AudioMediaElement.Clock.Controller.Seek(ts, TimeSeekOrigin.BeginTime);

            m_SliderDragging = false;
        }

        private void OnDragStarted_AudioTimeSlider(object sender, DragStartedEventArgs e)
        {
            if (AudioMediaElement.Clock == null) return;

            if (!AudioMediaElement.NaturalDuration.HasTimeSpan)
            {
                AudioMediaElement.Clock.Controller.Stop();
                return;
            }


            if (AudioMediaElement.Clock.CurrentState == ClockState.Active)
            {
                if (AudioMediaElement.Clock.IsPaused || AudioMediaElement.Clock.CurrentGlobalSpeed == 0.0)
                {
                }
                else
                {
                    AudioMediaElement.Clock.Controller.Pause();
                }
            }
            else if (AudioMediaElement.Clock.CurrentState == ClockState.Filling)
            {
                AudioMediaElement.Clock.Controller.Stop();
                AudioMediaElement.Clock.Controller.Pause();
            }

            m_SliderDragging = true;
        }

        private void OnAudioTimeSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (m_SliderValueChangeFromCode)
            {
                m_SliderValueChangeFromCode = false;
                return;
            }

            OnDragStarted_AudioTimeSlider(null, null);
            OnDragCompleted_AudioTimeSlider(null, null);
        }

        private void OnClick_ButtonAudioPlayPause(object sender, RoutedEventArgs e)
        {
            if (AudioMediaElement.Clock == null) return;

            if (!AudioMediaElement.NaturalDuration.HasTimeSpan)
            {
                AudioMediaElement.Clock.Controller.Stop();
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    resetAudioPlayer();
                }));

                return;
            }

            if (AudioMediaElement.Clock.CurrentState == ClockState.Active)
            {
                if (//AudioMediaElement.Clock.IsPaused ||
                    AudioMediaElement.Clock.CurrentGlobalSpeed == 0.0)
                {
                    AudioMediaElement.Clock.Controller.Resume();
                }
                else
                {
                    AudioMediaElement.Clock.Controller.Pause();
                }
            }
            else if (AudioMediaElement.Clock.CurrentState == ClockState.Stopped)
            {
                AudioMediaElement.Clock.Controller.Begin();
            }
            else if (AudioMediaElement.Clock.CurrentState == ClockState.Filling)
            {
                AudioElement_MediaEnded(null, null);
            }
        }
    }
}
