using System;
using System.ComponentModel.Composition;
using System.IO;
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
using Tobi.Plugin.Urakawa;
using urakawa.core;
using urakawa.daisy;
using urakawa.daisy.export;
using urakawa.data;
using urakawa.media.data.audio;
using urakawa.media.data.image;
using urakawa.metadata;
using urakawa.property.alt;
using urakawa.xuk;
using DialogResult = System.Windows.Forms.DialogResult;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

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
            m_ViewModel.ShellView = m_ShellView;

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
                                                 true, 300, 160, null, 0, m_DescriptionPopupModalWindow);

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
                    TextWrapping = TextWrapping.WrapWithOverflow
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
                                                     true, 340, 160, null, 0, null);

                popup.ShowModal();
                return;
            }

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                  UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Descriptions_Lang.CmdEditDescriptions_ShortDesc),
                                                  this,
                                                  PopupModalWindow.DialogButtonsSet.OkApplyCancel,
                                                  PopupModalWindow.DialogButton.Apply,
                                                  true, 1000, 600, null, 0, null);
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

            bool hadAltProp = true;
            var altProp = node.GetAlternateContentProperty(); //node.GetProperty<AlternateContentProperty>();
            if (altProp == null)
            {
                hadAltProp = false;

                altProp = node.GetOrCreateAlternateContentProperty();
                DebugFix.Assert(altProp != null);
            }

            m_DescriptionPopupModalWindow = windowPopup;

            windowPopup.ShowModal();

            m_DescriptionPopupModalWindow = null;

            if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Ok
                || windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Apply)
            {
                altProp = node.GetAlternateContentProperty(); //node.GetProperty<AlternateContentProperty>();
                if (altProp != null)
                {
                    removeEmptyDescriptions(altProp);
                }

                bool empty = m_Session.DocumentProject.Presentations.Get(0).UndoRedoManager.IsTransactionEmpty;

                m_Session.DocumentProject.Presentations.Get(0).UndoRedoManager.EndTransaction();

                if (empty)
                {
                    altProp = node.GetAlternateContentProperty(); //node.GetProperty<AlternateContentProperty>();
                    if (altProp != null && !hadAltProp)
                    {
#if DEBUG
                        DebugFix.Assert(altProp.IsEmpty);
#endif //DEBUG

                        node.RemoveProperty(altProp);
                    }
                }
            }
            else
            {
                m_Session.DocumentProject.Presentations.Get(0).UndoRedoManager.CancelTransaction();

                altProp = node.GetAlternateContentProperty(); //node.GetProperty<AlternateContentProperty>();
                if (altProp != null && !hadAltProp)
                {
#if DEBUG
                    DebugFix.Assert(altProp.IsEmpty);
#endif //DEBUG

                    node.RemoveProperty(altProp);
                }
            }


            if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Apply)
            {
                Popup();
            }
        }

        private void removeEmptyDescriptions(AlternateContentProperty altProp)
        {
            foreach (AlternateContent altContent in altProp.AlternateContents.ContentsAs_ListCopy)
            {
                if (altContent.IsEmpty ||
                    altContent.Text == null
                    &&
                    altContent.Image == null
                    &&
                    altContent.Audio == null
                    &&
                    !Daisy3_Export.AltContentHasSignificantMetadata(altContent)
                    )
                {
                    m_ViewModel.RemoveDescription(altContent);
                }
            }
        }

        private void OnKeyUp_ButtonExport(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None
                && e.Key == Key.Space)
            {
                OnClick_ButtonExport(null, null);
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

        private void OnClick_ButtonExport(object sender, RoutedEventArgs e)
        {
            m_Logger.Log("DescriptionView.OnClick_ButtonExport", Category.Debug, Priority.Medium);

            Tuple<TreeNode, TreeNode> selection = m_Session.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null
                || node.GetAlternateContentProperty() == null
                || node.GetImageMedia() == null
                || !(node.GetImageMedia() is ManagedImageMedia))
            {
                return;
            }


            SampleRate sampleRate = SampleRate.Hz22050;
            sampleRate = Urakawa.Settings.Default.AudioExportSampleRate;


            bool encodeToMp3 = true;
            encodeToMp3 = Urakawa.Settings.Default.AudioExportEncodeToMp3;


            var combo = new ComboBox
            {
                Margin = new Thickness(0, 0, 0, 12)
            };

            ComboBoxItem item1 = new ComboBoxItem();
            item1.Content = AudioLib.SampleRate.Hz11025.ToString();
            combo.Items.Add(item1);

            ComboBoxItem item2 = new ComboBoxItem();
            item2.Content = AudioLib.SampleRate.Hz22050.ToString();
            combo.Items.Add(item2);

            ComboBoxItem item3 = new ComboBoxItem();
            item3.Content = AudioLib.SampleRate.Hz44100.ToString();
            combo.Items.Add(item3);

            switch (sampleRate)
            {
                case AudioLib.SampleRate.Hz11025:
                    {
                        combo.SelectedItem = item1;
                        combo.Text = item1.Content.ToString();
                        break;
                    }
                case AudioLib.SampleRate.Hz22050:
                    {
                        combo.SelectedItem = item2;
                        combo.Text = item2.Content.ToString();
                        break;
                    }
                case AudioLib.SampleRate.Hz44100:
                    {
                        combo.SelectedItem = item3;
                        combo.Text = item3.Content.ToString();
                        break;
                    }
            }

            var checkBox = new CheckBox
            {
                IsThreeState = false,
                IsChecked = encodeToMp3,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var label_ = new TextBlock
            {
                Text = Tobi_Plugin_Urakawa_Lang.ExportEncodeMp3,
                Margin = new Thickness(8, 0, 8, 0),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap
            };


            var panel__ = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };
            panel__.Children.Add(label_);
            panel__.Children.Add(checkBox);

            var panel_ = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Vertical,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
            };
            panel_.Children.Add(combo);
            panel_.Children.Add(panel__);

            var windowPopup_ = new PopupModalWindow(m_ShellView,
                                                   UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Urakawa_Lang.ExportSettings),
                                                   panel_,
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   false, 300, 180, null, 40, m_DescriptionPopupModalWindow);

            windowPopup_.EnableEnterKeyDefault = true;

            windowPopup_.ShowModal();

            if (!PopupModalWindow.IsButtonOkYesApply(windowPopup_.ClickedDialogButton))
            {
                return;
            }

            encodeToMp3 = checkBox.IsChecked.Value;
            Urakawa.Settings.Default.AudioExportEncodeToMp3 = checkBox.IsChecked.Value;

            if (combo.SelectedItem == item1)
            {
                sampleRate = SampleRate.Hz11025;
                Urakawa.Settings.Default.AudioExportSampleRate = sampleRate;
            }
            else if (combo.SelectedItem == item2)
            {
                sampleRate = SampleRate.Hz22050;
                Urakawa.Settings.Default.AudioExportSampleRate = sampleRate;
            }
            else if (combo.SelectedItem == item3)
            {
                sampleRate = SampleRate.Hz44100;
                Urakawa.Settings.Default.AudioExportSampleRate = sampleRate;
            }



            string rootFolder = Path.GetDirectoryName(m_Session.DocumentFilePath);

            var dlg = new FolderBrowserDialog
            {
                RootFolder = Environment.SpecialFolder.MyComputer,
                SelectedPath = rootFolder,
                ShowNewFolderButton = true,
                Description = @"Tobi: " + UserInterfaceStrings.EscapeMnemonic("Export DIAGRAM XML")
            };

            DialogResult result = DialogResult.Abort;

            m_ShellView.DimBackgroundWhile(() => { result = dlg.ShowDialog(); });

            if (result != DialogResult.OK && result != DialogResult.Yes)
            {
                return;
            }
            if (!Directory.Exists(dlg.SelectedPath))
            {
                return;
            }


            ManagedImageMedia managedImage = (ManagedImageMedia)node.GetImageMedia();
            string exportImageName = FileDataProvider.EliminateForbiddenFileNameCharacters(managedImage.ImageMediaData.OriginalRelativePath);
            string imageDescriptionDirectoryPath = Daisy3_Export.GetAndCreateImageDescriptionDirectoryPath(false, exportImageName, dlg.SelectedPath);

            if (Directory.Exists(imageDescriptionDirectoryPath))
            {
                if (!m_Session.askUserConfirmOverwriteFileFolder(imageDescriptionDirectoryPath, true, m_DescriptionPopupModalWindow))
                {
                    return;
                }

                FileDataProvider.DeleteDirectory(imageDescriptionDirectoryPath);
            }

            FileDataProvider.CreateDirectory(imageDescriptionDirectoryPath);




            PCMFormatInfo audioFormat = node.Presentation.MediaDataManager.DefaultPCMFormat;
            AudioLibPCMFormat pcmFormat = audioFormat.Data;

            if ((ushort)sampleRate != pcmFormat.SampleRate)
            {
                pcmFormat.SampleRate = (ushort)sampleRate;
            }


            Application.Current.MainWindow.Cursor = Cursors.Wait;
            this.Cursor = Cursors.Wait; //m_ShellView

            try
            {
                string descriptionFile = Daisy3_Export.CreateImageDescription(
                    Urakawa.Settings.Default.AudioCodecDisableACM,
                    pcmFormat, encodeToMp3, 0,
                   imageDescriptionDirectoryPath, exportImageName,
                   node.GetAlternateContentProperty(),
                   null,
                   null,
                   null);
            }
            finally
            {
                Application.Current.MainWindow.Cursor = Cursors.Arrow;
                this.Cursor = Cursors.Arrow; //m_ShellView
            }


            m_ShellView.ExecuteShellProcess(imageDescriptionDirectoryPath);
        }


        private void OnClick_ButtonImport(object sender, RoutedEventArgs e)
        {
            m_Logger.Log("DescriptionView.OnClick_ButtonImport", Category.Debug, Priority.Medium);

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

            try
            {
                m_ViewModel.ImportDiagramXML(fullPath);

                forceRefreshDataUI();
            }
            finally
            {
                Application.Current.MainWindow.Cursor = Cursors.Arrow;
                this.Cursor = Cursors.Arrow; //m_ShellView
            }
        }



        private void OnClick_ButtonGoAdvanced(object sender, RoutedEventArgs e)
        {
            forceRefreshDataUI();
            m_ViewModel.ShowAdvancedEditor = true;
        }
        private void OnClick_ButtonGoBasic(object sender, RoutedEventArgs e)
        {
            forceRefreshDataUI();
            m_ViewModel.ShowAdvancedEditor = false;
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

        private ScalableGreyableImageProvider m_iconAudioHigh1 = null;
        private ScalableGreyableImageProvider m_iconAudioHigh2 = null;
        private ScalableGreyableImageProvider m_iconAudioHigh3 = null;
        private ScalableGreyableImageProvider m_iconAudioHigh4 = null;
        private ScalableGreyableImageProvider m_iconAudioHigh5 = null;

        private ScalableGreyableImageProvider m_iconAudioMuted1 = null;
        private ScalableGreyableImageProvider m_iconAudioMuted2 = null;
        private ScalableGreyableImageProvider m_iconAudioMuted3 = null;
        private ScalableGreyableImageProvider m_iconAudioMuted4 = null;
        private ScalableGreyableImageProvider m_iconAudioMuted5 = null;

        private void OnLoaded_Panel(object sender, RoutedEventArgs e)
        {
            var win = Window.GetWindow(this);

            //if (win is PopupModalWindow)
            //    OwnerWindow = (PopupModalWindow)win;

            OwnerWindow = win as PopupModalWindow; // can be NULL !!



            if (m_iconAudioHigh1 == null)
            {
                m_iconAudioHigh1 = new ScalableGreyableImageProvider(LoadTangoIcon("audio-volume-low"), m_ShellView.MagnificationLevel);
                ButtonAudio_LongDesc.Content = m_iconAudioHigh1.IconMedium;

                m_iconAudioHigh2 = new ScalableGreyableImageProvider(LoadTangoIcon("audio-volume-low"), m_ShellView.MagnificationLevel);
                ButtonAudio_Summary.Content = m_iconAudioHigh2.IconMedium;

                m_iconAudioHigh3 = new ScalableGreyableImageProvider(LoadTangoIcon("audio-volume-low"), m_ShellView.MagnificationLevel);
                ButtonAudio_SimplifiedLanguage.Content = m_iconAudioHigh3.IconMedium;

                m_iconAudioHigh4 = new ScalableGreyableImageProvider(LoadTangoIcon("audio-volume-low"), m_ShellView.MagnificationLevel);
                ButtonAudio_SimplifiedImage.Content = m_iconAudioHigh4.IconMedium;

                m_iconAudioHigh5 = new ScalableGreyableImageProvider(LoadTangoIcon("audio-volume-low"), m_ShellView.MagnificationLevel);
                ButtonAudio_TactileImage.Content = m_iconAudioHigh5.IconMedium;
            }

            if (m_iconAudioMuted1 == null)
            {
                m_iconAudioMuted1 = new ScalableGreyableImageProvider(LoadTangoIcon("audio-volume-muted"), m_ShellView.MagnificationLevel);
                ButtonNoAudio_LongDesc.Content = m_iconAudioMuted1.IconMedium;

                m_iconAudioMuted2 = new ScalableGreyableImageProvider(LoadTangoIcon("audio-volume-muted"), m_ShellView.MagnificationLevel);
                ButtonNoAudio_Summary.Content = m_iconAudioMuted2.IconMedium;

                m_iconAudioMuted3 = new ScalableGreyableImageProvider(LoadTangoIcon("audio-volume-muted"), m_ShellView.MagnificationLevel);
                ButtonNoAudio_SimplifiedLanguage.Content = m_iconAudioMuted3.IconMedium;

                m_iconAudioMuted4 = new ScalableGreyableImageProvider(LoadTangoIcon("audio-volume-muted"), m_ShellView.MagnificationLevel);
                ButtonNoAudio_SimplifiedImage.Content = m_iconAudioMuted4.IconMedium;

                m_iconAudioMuted5 = new ScalableGreyableImageProvider(LoadTangoIcon("audio-volume-muted"), m_ShellView.MagnificationLevel);
                ButtonNoAudio_TactileImage.Content = m_iconAudioMuted5.IconMedium;
            }

            m_iconAudioHigh1.IconDrawScale = m_ShellView.MagnificationLevel;
            m_iconAudioHigh2.IconDrawScale = m_ShellView.MagnificationLevel;
            m_iconAudioHigh3.IconDrawScale = m_ShellView.MagnificationLevel;
            m_iconAudioHigh4.IconDrawScale = m_ShellView.MagnificationLevel;
            m_iconAudioHigh5.IconDrawScale = m_ShellView.MagnificationLevel;

            m_iconAudioMuted1.IconDrawScale = m_ShellView.MagnificationLevel;
            m_iconAudioMuted2.IconDrawScale = m_ShellView.MagnificationLevel;
            m_iconAudioMuted3.IconDrawScale = m_ShellView.MagnificationLevel;
            m_iconAudioMuted4.IconDrawScale = m_ShellView.MagnificationLevel;
            m_iconAudioMuted5.IconDrawScale = m_ShellView.MagnificationLevel;

            m_ViewModel.OnPanelLoaded();

            forceRefreshDataUI();
        }

        private void OnUnloaded_Panel(object sender, RoutedEventArgs e)
        {
            forceRefreshUI();
            forceRefreshUI_Image();

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
