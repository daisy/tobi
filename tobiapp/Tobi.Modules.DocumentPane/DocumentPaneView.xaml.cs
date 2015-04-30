// MUST USE THIS !! OTHERWISE TextRange.ApplyPropertyValue modifies the FlowDocument content by inserting markup !!!
#define USE_WALKTREE_FOR_SELECT

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Annotations.Storage;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Practices.Composite;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Regions;
using SharpVectors.Converters;
using SharpVectors.Runtime;
using Tobi.Common;
using Tobi.Common._UnusedCode;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using Tobi.Common.UI.XAML;
using urakawa;
using urakawa.command;
using urakawa.commands;
using urakawa.core;
using urakawa.daisy;
using urakawa.daisy.export;
using urakawa.daisy.import;
using urakawa.data;
using urakawa.events.undo;
using urakawa.media;
using urakawa.property.alt;
using urakawa.property.channel;
using urakawa.property.xml;
using urakawa.undo;
using urakawa.xuk;
using Colors = System.Windows.Media.Colors;
using System.Xml;
using Tobi.Common.Validation;
using Tobi.Plugin.Validator.ContentDocument;

#if NET40
//using System.Windows.Shell;
#endif

namespace Tobi.Plugin.DocumentPane
{
    [ValueConversion(typeof(bool), typeof(Brush))]
    public class BooleanToBrushConverter : ValueConverterMarkupExtensionBase<BooleanToBrushConverter>
    {
        #region IValueConverter Members

        public override object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Brush))
                throw new InvalidOperationException("The target must be a Brush !");

            return ((bool)value ? Brushes.Red : SystemColors.ControlBrush);
        }

        public override object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be Boolean !");

            return true;
        }

        #endregion
    }


    [ValueConversion(typeof(Color), typeof(Brush))]
    public class BackgroundColorToBrushConverter : ValueConverterMarkupExtensionBase<BackgroundColorToBrushConverter>
    {
        #region IValueConverter Members

        private static BitmapImage m_ImageSource;

        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Brush)) return null;
            if (!(value is Color)) return null;

            if (((Color)value) == Colors.Transparent)
            {
                try
                {
                    if (m_ImageSource == null)
                    {
                        string dir = Path.GetDirectoryName(ApplicationConstants.LOG_FILE_PATH);
                        var filename = Path.Combine(dir, @"paper_tile_texture.jpg");
                        if (File.Exists(filename))
                        {
                            var uri = new Uri("file:///" + filename, UriKind.Absolute);


                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = uri;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.CreateOptions = BitmapCreateOptions.None;
                            bitmap.EndInit();

                            int ph = bitmap.PixelHeight;
                            int pw = bitmap.PixelWidth;
                            double dpix = bitmap.DpiX;
                            double dpiy = bitmap.DpiY;

                            //double zoom = ZoomSlider

                            m_ImageSource = bitmap;
                            m_ImageSource.Freeze();
                        }
                    }
                    if (m_ImageSource != null)
                    {
                        return new ImageBrush(m_ImageSource)
                        {
                            TileMode = TileMode.Tile,
                            Viewport = new Rect(0, 0, m_ImageSource.PixelWidth, m_ImageSource.PixelHeight),
                            ViewportUnits = BrushMappingMode.Absolute,
                            //Viewbox = new Rect(0, 0, m_ImageSource.PixelWidth, m_ImageSource.PixelHeight),
                            //ViewboxUnits = BrushMappingMode.Absolute,
                            //Viewbox = new Rect(0, 0, 1, 1),
                            //ViewboxUnits = BrushMappingMode.RelativeToBoundingBox,
                            Stretch = Stretch.Fill,
                        };
                    }
                }
                catch
                {
                    ;// default below
                }
            }
            //var scb = new SolidColorBrush((Color)value);
            return ColorBrushCache.Get((Color)value);
        }

        #endregion
    }
    public class FlowDocumentScrollViewerEx : FlowDocumentScrollViewer
    {
        private ScrollViewer m_ScrollViewer;
        public ScrollViewer ScrollViewer
        {
            get
            {
                if (m_ScrollViewer == null)
                {
                    m_ScrollViewer = VisualLogicalTreeWalkHelper.FindObjectInVisualTreeWithMatchingType<ScrollViewer>(this, null);
                }

                return m_ScrollViewer;
            }
        }
    }

    public class FontFamilyDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            ContentPresenter presenter = (ContentPresenter)container;

            if (presenter.TemplatedParent is ComboBox)
            {
                return (DataTemplate)presenter.FindResource("FontFamilyComboCollapsed");
            }

            // Templated parent is ComboBoxItem
            return (DataTemplate)presenter.FindResource("FontFamilyComboExpanded");
        }
    }

    /// <summary>
    /// Interaction logic for DocumentPaneView.xaml
    /// </summary>
    [Export(typeof(DocumentPaneView)), PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IDocumentViewModel))]
    public partial class DocumentPaneView : IPartImportsSatisfiedNotification, INotifyPropertyChangedEx, IInputBindingManager, UndoRedoManager.Hooker.Host, IDocumentViewModel
    {
        private readonly IRegionManager m_RegionManager;

        private PopupModalWindow m_DocumentNarratorWindow;
        //private FocusActiveAwareAdapter m_DocumentNarratorWindowActiveAware;
        private readonly object m_DocumentNarratorWindowLOCK = new object();

        public bool AddInputBinding(InputBinding inputBinding)
        {
            lock (m_DocumentNarratorWindowLOCK)
            {
                if (m_DocumentNarratorWindow != null)
                {
                    m_DocumentNarratorWindow.AddInputBinding(inputBinding);
                }
            }
            return m_ShellView.AddInputBinding(inputBinding);
        }

        public void RemoveInputBinding(InputBinding inputBinding)
        {
            lock (m_DocumentNarratorWindowLOCK)
            {
                if (m_DocumentNarratorWindow != null)
                {
                    m_DocumentNarratorWindow.RemoveInputBinding(inputBinding);
                }
            }
            m_ShellView.RemoveInputBinding(inputBinding);
        }

        public IInputBindingManager InputBindingManager
        {
            get { return this; }
        }

        [Import(typeof(IAudioViewModel), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = true, AllowDefault = true)]
        private IAudioViewModel m_AudioViewModel;
        public IAudioViewModel AudioViewModel
        {
            get
            {
                //        if (m_AudioViewModel == null)
                //        {
                //            m_AudioViewModel = m_Container.Resolve<IAudioViewModel>();
                //            m_Container.IsRegistered(typeof (IAudioViewModel));
                //        }
                return m_AudioViewModel;
            }
        }

        private bool m_AudioViewModelDone = false;
        private void tryAudioViewModel()
        {
            if (m_AudioViewModel == null || m_AudioViewModelDone)
            {
                return;
            }
            m_AudioViewModelDone = true;

            m_PropertyChangeHandler.RaisePropertyChanged(() => AudioViewModel);
        }

        private bool m_IsNarratorMode;
        public bool IsNarratorMode
        {
            get
            {
                return m_IsNarratorMode;
            }
            set
            {
                if (m_IsNarratorMode == value) return;
                m_IsNarratorMode = value;
                m_PropertyChangeHandler.RaisePropertyChanged(() => IsNarratorMode);

                lock (m_DocumentNarratorWindowLOCK)
                {
                    if (m_DocumentNarratorWindow != null)
                    {
                        var window = m_DocumentNarratorWindow;
                        m_DocumentNarratorWindow = null;
                        window.ForceClose(PopupModalWindow.DialogButton.ESC);
                    }
                }

                var region = m_RegionManager.Regions[RegionNames.DocumentPane];

                var listOfViews = new List<Object>();
                foreach (var view in region.Views)
                {
                    listOfViews.Add(view);
                }
                foreach (var view in listOfViews)
                {
                    region.Remove(view);
                }

                if (m_IsNarratorMode)
                {
                    var obj = region.GetView(DocumentPanePlugin.VIEW_NAME);
                    if (obj != null)
                    {
                        region.Remove(obj);
                    }
                    lock (m_DocumentNarratorWindowLOCK)
                    {
                        //                        m_DocumentNarratorWindow = new Window
                        //                        {
                        //                            Title = "Tobi",
                        //                            WindowStartupLocation = WindowStartupLocation.Manual,
                        //                            WindowState = WindowState.Normal,
                        //                            WindowStyle = WindowStyle.SingleBorderWindow,
                        //                            SizeToContent = SizeToContent.Manual,
                        //                            Topmost = false,
                        //                            ShowInTaskbar = true,
                        //                            ShowActivated = true,
                        //                            ResizeMode = ResizeMode.CanResizeWithGrip,
                        //                            AllowsTransparency = false,
                        //                            Width = 800,
                        //                            Height = 600,
                        //                            Top = 10,
                        //                            Left = 10,
                        //                            Content = this,
                        //                            //Owner = (Window)m_ShellView,
                        //                            Owner = Application.Current.MainWindow,
                        //#if NET40
                        //                            TaskbarItemInfo = new TaskbarItemInfo(),
                        //#endif
                        //                        };

                        m_DocumentNarratorWindow = new PopupModalWindow(m_ShellView,
                            Tobi_Plugin_DocumentPane_Lang.NarratorView,
                            this,
                            PopupModalWindow.DialogButtonsSet.None,
                            PopupModalWindow.DialogButton.Close,
                            true, 800, 600, null, 0, null);

                        m_DocumentNarratorWindow.IgnoreEscape = true;

                        var bindings = Application.Current.MainWindow.InputBindings;
                        foreach (var binding in bindings)
                        {
                            if (binding is KeyBinding)
                            {
                                var keyBinding = (KeyBinding)binding;
                                if (keyBinding.Command == m_ShellView.ExitCommand)
                                {
                                    continue;
                                }
                                m_DocumentNarratorWindow.InputBindings.Add(keyBinding);
                            }
                        }

                        //m_DocumentNarratorWindow.InputBindings.AddRange(Application.Current.MainWindow.InputBindings);

                        m_DocumentNarratorWindow.KeyUp += (object sender, KeyEventArgs e) =>
                            {
                                var key = (e.Key == Key.System
                                                ? e.SystemKey
                                                : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));

                                if (key == Key.Escape)
                                {
                                    m_EventAggregator.GetEvent<EscapeEvent>().Publish(null);
                                }
                            };

                        m_DocumentNarratorWindow.Closed += (sender, ev) => Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            (Action)(() =>
                            {
                                lock (m_DocumentNarratorWindowLOCK)
                                {
                                    if (m_DocumentNarratorWindow != null)
                                    {
                                        IsNarratorMode = false;
                                    }
                                }
                            }));
                    }

                    m_DocumentNarratorWindow.ShowFloating(null);
                    //m_DocumentNarratorWindow.ShowModal();
                }
                else
                {
                    m_RegionManager.RegisterNamedViewWithRegion(RegionNames.DocumentPane,
                        new PreferredPositionNamedView { m_viewInstance = this, m_viewName = DocumentPanePlugin.VIEW_NAME });
                }
            }
        }

        public void OnImportsSatisfied()
        {
            trySearchCommands();
            tryAudioViewModel();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void DispatchPropertyChangedEvent(PropertyChangedEventArgs e)
        {
            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        private PropertyChangedNotifyBase m_PropertyChangeHandler;

        public RichDelegateCommand CmdFindNextGlobal { get; private set; }
        public RichDelegateCommand CmdFindPreviousGlobal { get; private set; }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            SearchTerm = SearchBox.Text;
        }

        private void SearchTermDo()
        {
            m_SearchMatches = null;
            m_SearchCurrentIndex = -1;


            if (Settings.Default.Document_EnableInstantSearch)
            {
                var textElement = FindPreviousNext(false, false);
                if (textElement == null)
                {
                    m_SearchCurrentIndex = -1;
                    textElement = FindPreviousNext(true, false);
                    m_SearchCurrentIndex = -1;
                }

                TextRange selectionBackup = null;
                if (FlowDocReader.Selection != null)
                {
                    selectionBackup = new TextRange(FlowDocReader.Selection.Start, FlowDocReader.Selection.End);
                }


                AnnotationService service = AnnotationService.GetService(FlowDocReader);

                if (service != null && service.IsEnabled)
                {
                    foreach (var annotation in service.Store.GetAnnotations())
                    {
                        service.Store.DeleteAnnotation(annotation.Id);
                    }

                    service.Store.Flush();
                }

                //FlowDocReader.Selection.Select(FlowDocReader.Document.ContentStart,
                //                               FlowDocReader.Document.ContentEnd);
                //AnnotationHelper.ClearHighlightsForSelection(service);

                if (string.IsNullOrEmpty(m_SearchTerm) || m_SearchTerm.Length < 2)
                {
                    return;
                }

                //if (m_FindAndReplaceManager == null)
                //{
                //    m_FindAndReplaceManager = new FindAndReplaceManager(new TextRange(TheFlowDocument.ContentStart, TheFlowDocument.ContentEnd));
                //    m_SearchMatches = null;
                //}
                //if (m_SearchMatches == null)
                //{
                //    IEnumerable<TextRange> matches = m_FindAndReplaceManager.FindAll(SearchTerm, FindOptions.None);
                //    m_SearchMatches = new List<TextRange>(matches);
                //    m_SearchCurrentIndex = -1;
                //}

                if (m_SearchMatches == null || m_SearchMatches.Count == 0)
                {
                    return;
                }

                var brush = ColorBrushCache.Get(Common.Settings.Default.SearchHits_Color).Clone();
                brush.Opacity = .5;
                brush.Freeze();
                //var brush = new SolidColorBrush(Common.Settings.Default.SearchHits_Color) { Opacity = .5 };

                foreach (var textRange in m_SearchMatches)
                {
                    if (FlowDocReader.Selection != null)
                    {
                        FlowDocReader.Selection.Select(textRange.Start, textRange.End);
                    }

                    if (service != null && service.IsEnabled)
                    {
                        AnnotationHelper.CreateHighlightForSelection(service, "Tobi search hits", brush);
                    }
                }

                if (FlowDocReader.Selection != null && selectionBackup != null)
                {
                    FlowDocReader.Selection.Select(selectionBackup.Start, selectionBackup.End);
                }
            }
        }

        private DispatcherTimer _timer = null;

        private string m_SearchTerm;
        public string SearchTerm
        {
            get { return m_SearchTerm; }
            set
            {
                if (m_SearchTerm == value) { return; }
                m_SearchTerm = value;

                m_PropertyChangeHandler.RaisePropertyChanged(() => SearchTerm);

                if (_timer == null)
                {
                    _timer = new DispatcherTimer();
                    _timer.Tick += (object sender, EventArgs e) =>
                    {
                        _timer.Stop();
                        SearchTermDo();
                    };
                    _timer.Interval = new TimeSpan(0, 0, 0, 0, 500);
                }
                else
                {
                    _timer.Stop();
                    _timer.Start();
                }

                m_SearchMatches = null;
                m_SearchCurrentIndex = -1;

                //SearchTermDo();
            }
        }

        public bool IsSearchEnabled
        {
            get
            {
                return m_UrakawaSession.DocumentProject != null;
            }
        }
        private FindAndReplaceManager m_FindAndReplaceManager;
        private List<TextRange> m_SearchMatches;
        private int m_SearchCurrentIndex = -1;

        private TextElement FindPreviousNext(bool previous, bool select)
        {
            if (string.IsNullOrEmpty(SearchTerm)) return null;

            if (m_UrakawaSession.isAudioRecording)
            {
                return null;
            }

            if (m_FindAndReplaceManager == null)
            {
                m_FindAndReplaceManager = new FindAndReplaceManager(new TextRange(TheFlowDocument.ContentStart, TheFlowDocument.ContentEnd));
                m_SearchMatches = null;
            }
            if (m_SearchMatches == null)
            {
                IEnumerable<TextRange> matches = m_FindAndReplaceManager.FindAll(SearchTerm, FindOptions.None);
                m_SearchMatches = new List<TextRange>(matches);
                m_SearchCurrentIndex = -1;
            }
            if (m_SearchMatches.Count == 0) return null;

            if (m_SearchCurrentIndex == -1)
            {
                TextPointer textPointer;
                var textElement = m_lastHighlightedSub ?? m_lastHighlighted;
                if (textElement != null)
                {
                    textPointer = !previous ? textElement.ContentEnd : textElement.ContentStart;
                }
                else
                {
                    textPointer = TheFlowDocument.ContentStart;
                }
                //m_FindAndReplaceManager.CurrentPosition = textPointer;

                if (FlowDocReader.Selection != null && !FlowDocReader.Selection.IsEmpty)
                {
                    textPointer = !previous ? FlowDocReader.Selection.End : FlowDocReader.Selection.Start;
                }

                int position = -1;
                if (previous)
                {
                    int counter = -1;
                    foreach (var textRange in m_SearchMatches)
                    {
                        counter++;
                        if (textRange.Start.CompareTo(textPointer) >= 0)
                        {
                            position = counter;
                            break;
                        }
                    }
                }
                else // next
                {
                    for (int counter = m_SearchMatches.Count - 1; counter >= 0; counter--)
                    {
                        var textRange = m_SearchMatches[counter];

                        if (textRange.Start.CompareTo(textPointer) <= 0)
                        {
                            position = counter;
                            break;
                        }
                    }
                }

                m_SearchCurrentIndex = position;
            }

            TextRange hit = null;
            if (previous)
            {
                if (m_SearchCurrentIndex < 0)
                {
                    m_SearchCurrentIndex = m_SearchMatches.Count - 1;
                    hit = m_SearchMatches[m_SearchCurrentIndex];
                }
                else if (m_SearchCurrentIndex == 0)
                {
                    if (select)
                    {
                        AudioCues.PlayBeep();
                    }
                    hit = m_SearchMatches[m_SearchCurrentIndex];
                }
                else if (m_SearchCurrentIndex > 0)
                {
                    hit = m_SearchMatches[--m_SearchCurrentIndex];
                }
                else
                {
                    return null;
                }
            }
            else // next
            {
                //hit = m_FindAndReplaceManager.FindNext(SearchTerm, FindOptions.None);
                if (m_SearchCurrentIndex < 0)
                {
                    m_SearchCurrentIndex = 0;
                    hit = m_SearchMatches[m_SearchCurrentIndex];
                }
                else if (m_SearchCurrentIndex == m_SearchMatches.Count - 1)
                {
                    if (select)
                    {
                        AudioCues.PlayBeep();
                    }
                    hit = m_SearchMatches[m_SearchCurrentIndex];
                }
                else if (m_SearchCurrentIndex < (m_SearchMatches.Count - 1))
                {
                    hit = m_SearchMatches[++m_SearchCurrentIndex];
                }
                else
                {
                    return null;
                }
            }

            if (hit != null && !string.IsNullOrEmpty(hit.Text))
            {
                //DebugFix.Assert(hit.Text.ToLower() == SearchTerm.ToLower());

                if (FlowDocReader.Selection != null)
                {
                    if (select) // && !Settings.Default.Document_EnableInstantSearch)
                    {
                        FlowDocReader.Selection.Select(hit.Start, hit.End);

                        FocusHelper.FocusBeginInvoke(FlowDocReader); // otherwise the selection is invisible :(
                    }

                    var para = hit.Start.Paragraph;
                    var obj1 = hit.Start.GetAdjacentElement(LogicalDirection.Backward);
                    var obj2 = hit.Start.GetAdjacentElement(LogicalDirection.Forward);

                    object toScan = (obj1 ?? obj2) ?? para;
                    var textElement = getFirstAncestorWithTreeNodeTag(toScan);
                    if (textElement != null)
                    {
                        DebugFix.Assert(textElement.Tag is TreeNode);

                        if (select)
                        {
                            if (!m_UrakawaSession.isAudioRecording)
                            {
                                m_UrakawaSession.PerformTreeNodeSelection((TreeNode)textElement.Tag);
                            }
                        }
                        else
                        {
                            scrollToView(textElement);
                        }
                        return textElement;
                    }

                    if (para != null)
                    {
                        //FlowDocReader.Selection.Select(para.ContentStart, para.ContentEnd);

                        scrollToView(para);
                        return para;
                    }
                    if (obj1 != null && obj1 is TextElement)
                    {
                        scrollToView((TextElement)obj1);
                        return (TextElement)obj1;
                    }
                    if (obj2 != null && obj2 is TextElement)
                    {
                        scrollToView((TextElement)obj2);
                        return (TextElement)obj2;
                    }
                }
            }

            return null;
        }

        private void FindNext()
        {
            FindPreviousNext(false, true);
        }
        private void FindPrevious()
        {
            FindPreviousNext(true, true);
        }


        ~DocumentPaneView()
        {
            if (m_GlobalSearchCommand != null)
            {
                m_GlobalSearchCommand.CmdFindFocus.UnregisterCommand(CommandFindFocus);
                m_GlobalSearchCommand.CmdFindNext.UnregisterCommand(CommandFindNext);
                m_GlobalSearchCommand.CmdFindPrevious.UnregisterCommand(CommandFindPrev);
            }
#if DEBUG
            m_Logger.Log("DocumentPaneView garbage collected.", Category.Debug, Priority.Medium);
#endif
        }
        [Import(typeof(IGlobalSearchCommands), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = true, AllowDefault = true)]
        private IGlobalSearchCommands m_GlobalSearchCommand;

        private bool m_GlobalSearchCommandDone = false;
        private void trySearchCommands()
        {
            if (m_GlobalSearchCommand == null || m_GlobalSearchCommandDone)
            {
                return;
            }
            m_GlobalSearchCommandDone = true;

            m_GlobalSearchCommand.CmdFindFocus.RegisterCommand(CommandFindFocus);
            m_GlobalSearchCommand.CmdFindNext.RegisterCommand(CommandFindNext);
            m_GlobalSearchCommand.CmdFindPrevious.RegisterCommand(CommandFindPrev);

            CmdFindNextGlobal = m_GlobalSearchCommand.CmdFindNext;
            m_PropertyChangeHandler.RaisePropertyChanged(() => CmdFindNextGlobal);

            CmdFindPreviousGlobal = m_GlobalSearchCommand.CmdFindPrevious;
            m_PropertyChangeHandler.RaisePropertyChanged(() => CmdFindPreviousGlobal);
        }

        private string showDialogTextEdit(int i, int count, string text)
        {
            m_Logger.Log("showDialogTextEdit", Category.Debug, Priority.Medium);


            var editBox = new TextBoxReadOnlyCaretVisible
                              {
                                  Text = text,
                                  TextWrapping = TextWrapping.WrapWithOverflow
                              };

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_DocumentPane_Lang.CmdEditText_ShortDesc) + " (" + (i+1) + " / " + count + ")",
                                                   new ScrollViewer { Content = editBox },
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 300, 160, null, 40, null);

            windowPopup.EnableEnterKeyDefault = true;

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


        //public event PropertyChangedEventHandler PropertyChanged;
        //public void RaisePropertyChanged(PropertyChangedEventArgs e)
        //{
        //    var handler = PropertyChanged;

        //    if (handler != null)
        //    {
        //        handler(this, e);
        //    }
        //}

        //private PropertyChangedNotifyBase m_PropertyChangeHandler;

        //public NavigationPaneView()
        //{
        //    m_PropertyChangeHandler = new PropertyChangedNotifyBase();
        //    m_PropertyChangeHandler.InitializeDependentProperties(this);
        //}

        public RichDelegateCommand CommandSwitchPhrasePrevious { get; private set; }
        public RichDelegateCommand CommandSwitchPhraseNext { get; private set; }

        public RichDelegateCommand CommandEditText { get; private set; }

        public RichDelegateCommand CommandStructRemoveFragment { get; private set; }
        public RichDelegateCommand CommandStructInsertFragment { get; private set; }

        public RichDelegateCommand CommandStructInsertPageBreak { get; private set; }

        private TreeNode m_TreeNodeFragmentClipboard = null;

        public RichDelegateCommand CommandStructCopyFragment { get; private set; }
        public RichDelegateCommand CommandStructCutFragment { get; private set; }
        public RichDelegateCommand CommandStructPasteFragment { get; private set; }

        public RichDelegateCommand CommandFollowLink { get; private set; }
        public RichDelegateCommand CommandUnFollowLink { get; private set; }

        public RichDelegateCommand CommandStructureUp { get; private set; }
        public RichDelegateCommand CommandStructureDown { get; private set; }

        public RichDelegateCommand CommandFocus { get; private set; }

        public RichDelegateCommand CommandToggleSinglePhraseView { get; private set; }

        public RichDelegateCommand CommandToggleTextSyncGranularity { get; private set; }

        public RichDelegateCommand CommandSwitchNarratorView { get; private set; }

        private readonly ILoggerFacade m_Logger;

        private readonly IEventAggregator m_EventAggregator;
        private readonly IUrakawaSession m_UrakawaSession;

        private readonly IShellView m_ShellView;

        private bool checkDisplayStructEditWarning()
        {
            if (!m_ProjectLoadedFlag) return true;

            m_ProjectLoadedFlag = false;

            return checkWithUser(Tobi_Plugin_DocumentPane_Lang.StructEditConfirmTitle,
                Tobi_Plugin_DocumentPane_Lang.StructEditConfirmMessage,
                    Tobi_Plugin_DocumentPane_Lang.StructEditConfirmDetails);
        }


        public void BringIntoFocus()
        {
            if (FocusCollapsed.IsVisible)
            {
                FocusHelper.FocusBeginInvoke(FocusCollapsed);
            }
            else
            {
                FocusHelper.FocusBeginInvoke(FocusExpanded);
            }
        }

        private void OnMouseClickCheckBox(object sender, RoutedEventArgs e)
        {
            BringIntoFocus();
        }

        private void nextPrevious(bool previous)
        {
            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;

            //TreeNode nested;
            //TreeNode next = TreeNode.GetNextTreeNodeWithNoSignificantTextOnlySiblings(false, node, out nested);

            TreeNode nearestSignificantTextSibling;
            TreeNode next = TreeNode.NavigatePreviousNextSignificantText(previous, node, out nearestSignificantTextSibling);
            if (next == null)
            {
                AudioCues.PlayBeep();
            }
            else
            {
                TreeNode toSelect = next;
                TreeNode sub = null;

                TreeNode math = null;
                TreeNode svg = null;
                TreeNode adjustedGranularity = null;

                math = toSelect.GetFirstAncestorWithXmlElement("math");
                if (math != null)
                {
                    toSelect = math;
                }
                else
                {
                    svg = toSelect.GetFirstAncestorWithXmlElement("svg");
                    if (svg != null)
                    {
                        toSelect = svg;
                    }
                    else
                    {
                        adjustedGranularity = m_UrakawaSession.AdjustTextSyncGranularity(toSelect, node);
                        if (adjustedGranularity != null)
                        {
                            toSelect = adjustedGranularity;
                        }
                    }
                }

                TreeNode firstDescendantWithManagedAudio = toSelect.GetFirstDescendantWithManagedAudio();
                if (firstDescendantWithManagedAudio != null)
                {
                    sub = nearestSignificantTextSibling;

                    if (next.GetFirstDescendantWithManagedAudio() == null)
                    {
                        sub = next;
                    }

                    if (math != null && math.GetFirstDescendantWithManagedAudio() == null)
                    {
                        sub = math;
                    }

                    if (svg != null && math.GetFirstDescendantWithManagedAudio() == null)
                    {
                        sub = svg;
                    }

                    if (adjustedGranularity != null && adjustedGranularity.GetFirstDescendantWithManagedAudio() == null)
                    {
                        sub = adjustedGranularity;
                    }

                    TreeNode firstAncestorWithManagedAudio = sub.GetFirstAncestorWithManagedAudio();
                    if (firstAncestorWithManagedAudio != null)
                    {
                        sub = firstAncestorWithManagedAudio;
                    }
                }


                m_UrakawaSession.PerformTreeNodeSelection(toSelect, false, sub);
            }
        }

        private bool checkWithUserValidation(string title, string message, string info)
        {
            m_Logger.Log("DocumentPaneView.checkWithUserValidation", Category.Debug, Priority.Medium);

            var label = new TextBlock // TextBoxReadOnlyCaretVisible
            {
                //FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],

                //BorderThickness = new Thickness(1),
                //Padding = new Thickness(6),

                //TextReadOnly = message,
                Text = message,

                Margin = new Thickness(8, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap
            };


            var checkBox = new CheckBox
            {
                FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],
                IsThreeState = false,
                IsChecked = Settings.Default.InvalidStructEdit_DoNotAskAgain,
                VerticalAlignment = VerticalAlignment.Center,
                Content = Tobi_Common_Lang.DoNotShowMessageAgain,
                Margin = new Thickness(0, 16, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
            };

            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            //panel.Margin = new Thickness(8, 8, 8, 0);

            panel.Children.Add(label);
            panel.Children.Add(checkBox);


            //var iconProvider = new ScalableGreyableImageProvider(
            //    m_ShellView.LoadTangoIcon("help-browser"),
            //    m_ShellView.MagnificationLevel);

            //var panel = new StackPanel
            //{
            //    Orientation = Orientation.Horizontal,
            //    HorizontalAlignment = HorizontalAlignment.Left,
            //    VerticalAlignment = VerticalAlignment.Stretch,
            //};
            //panel.Children.Add(iconProvider.IconLarge);
            //panel.Children.Add(label);
            ////panel.Margin = new Thickness(8, 8, 8, 0);


            var details = info != null ? new TextBoxReadOnlyCaretVisible
            {
                FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],

                BorderThickness = new Thickness(1),
                Padding = new Thickness(6),
                TextReadOnly = info
            } : null;

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   title,
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.YesNo,
                                                   PopupModalWindow.DialogButton.Yes,
                                                   true, 340, 170, details, 40, null);

            windowPopup.ShowModal();

            Settings.Default.InvalidStructEdit_DoNotAskAgain = checkBox.IsChecked.Value;

            if (PopupModalWindow.IsButtonOkYesApply(windowPopup.ClickedDialogButton))
            {
                return true;
            }

            return false;
        }

        private bool checkWithUser(string title, string message, string info)
        {
            m_Logger.Log("DocumentPaneView.checkWithUser", Category.Debug, Priority.Medium);

            var label = new TextBlock // TextBoxReadOnlyCaretVisible
            {
                //FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],

                //BorderThickness = new Thickness(1),
                //Padding = new Thickness(6),

                //TextReadOnly = message,
                Text = message,

                Margin = new Thickness(8, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap
            };

            //var iconProvider = new ScalableGreyableImageProvider(
            //    m_ShellView.LoadTangoIcon("help-browser"),
            //    m_ShellView.MagnificationLevel);

            //var panel = new StackPanel
            //{
            //    Orientation = Orientation.Horizontal,
            //    HorizontalAlignment = HorizontalAlignment.Left,
            //    VerticalAlignment = VerticalAlignment.Stretch,
            //};
            //panel.Children.Add(iconProvider.IconLarge);
            //panel.Children.Add(label);
            ////panel.Margin = new Thickness(8, 8, 8, 0);


            var details = info != null ? new TextBoxReadOnlyCaretVisible
            {
                FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],

                BorderThickness = new Thickness(1),
                Padding = new Thickness(6),
                TextReadOnly = info
            } : null;

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   title,
                                                   label,
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 340, 260, details, 40, null);

            windowPopup.ShowModal();

            if (PopupModalWindow.IsButtonOkYesApply(windowPopup.ClickedDialogButton))
            {
                return true;
            }

            return false;
        }

        //private bool askUser(string title, string message, string info)
        //{
        //    m_Logger.Log("DocumentPaneView.askUser", Category.Debug, Priority.Medium);

        //    var label = new TextBlock
        //    {
        //        Text = message,
        //        Margin = new Thickness(8, 0, 8, 0),
        //        HorizontalAlignment = HorizontalAlignment.Center,
        //        VerticalAlignment = VerticalAlignment.Center,
        //        Focusable = true,
        //        TextWrapping = TextWrapping.Wrap
        //    };

        //    var iconProvider = new ScalableGreyableImageProvider(
        //        m_ShellView.LoadTangoIcon("help-browser"),
        //        m_ShellView.MagnificationLevel);

        //    var panel = new StackPanel
        //    {
        //        Orientation = Orientation.Horizontal,
        //        HorizontalAlignment = HorizontalAlignment.Left,
        //        VerticalAlignment = VerticalAlignment.Stretch,
        //    };
        //    panel.Children.Add(iconProvider.IconLarge);
        //    panel.Children.Add(label);
        //    //panel.Margin = new Thickness(8, 8, 8, 0);


        //    var details = info != null ? new TextBoxReadOnlyCaretVisible
        //    {
        //        FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],

        //        BorderThickness = new Thickness(1),
        //        Padding = new Thickness(6),
        //        TextReadOnly = info
        //    } : null;

        //    var windowPopup = new PopupModalWindow(m_ShellView,
        //                                           title,
        //                                           panel,
        //                                           PopupModalWindow.DialogButtonsSet.YesNo,
        //                                           PopupModalWindow.DialogButton.No,
        //                                           true, 360, 200, details, 40, null);

        //    windowPopup.ShowModal();

        //    if (PopupModalWindow.IsButtonOkYesApply(windowPopup.ClickedDialogButton))
        //    {
        //        return true;
        //    }

        //    return false;
        //}

        protected TreeNode buildTreeNodeFromXml(XmlNode xmlNode, Presentation presentation, TreeNode parentTreeNode)
        {
            XmlNodeType xmlType = xmlNode.NodeType;
            switch (xmlType)
            {
                case XmlNodeType.Document:
                    {
                        XmlDocument xmlDoc = ((XmlDocument)xmlNode);
                        TreeNode root = buildTreeNodeFromXml(xmlDoc.DocumentElement, presentation, parentTreeNode);
                        return root;
                    }
                case XmlNodeType.Element:
                    {
                        TreeNode treeNode = presentation.TreeNodeFactory.Create();

                        string prefix;
                        string localName;
                        XmlProperty.SplitLocalName(xmlNode.Name, out prefix, out localName);
                        string uri = "";
                        if (!string.IsNullOrEmpty(prefix))
                        {
                            uri = presentation.RootNode.GetXmlNamespaceUri(prefix);

                            if (string.IsNullOrEmpty(uri))
                            {
                                uri = xmlNode.GetNamespaceOfPrefix(prefix);

                                if (string.IsNullOrEmpty(uri))
                                {
                                    uri = xmlNode.NamespaceURI;
                                }
                            }
                        }

                        XmlProperty xmlProp = treeNode.GetOrCreateXmlProperty();
                        xmlProp.SetQName(xmlNode.LocalName, uri);

                        XmlAttributeCollection attributeCol = xmlNode.Attributes;
                        if (attributeCol != null)
                        {
                            for (int i = 0; i < attributeCol.Count; i++)
                            {
                                XmlNode xmlAttr = attributeCol.Item(i);
                                buildTreeNodeFromXml(xmlAttr, presentation, treeNode);
                            }
                        }

                        if (parentTreeNode != null)
                        {
                            parentTreeNode.AppendChild(treeNode);
                        }

                        foreach (XmlNode childXmlNode in xmlNode.ChildNodes)
                        {
                            buildTreeNodeFromXml(childXmlNode, presentation, treeNode);
                        }

                        return treeNode;
                    }
                case XmlNodeType.Attribute:
                    {
                        XmlProperty xmlProp = parentTreeNode.GetOrCreateXmlProperty();

                        string prefix;
                        string localName;
                        XmlProperty.SplitLocalName(xmlNode.Name, out prefix, out localName);
                        string uri = "";
                        if (!string.IsNullOrEmpty(prefix))
                        {
                            uri = presentation.RootNode.GetXmlNamespaceUri(prefix);

                            if (string.IsNullOrEmpty(uri))
                            {
                                uri = xmlNode.GetNamespaceOfPrefix(prefix);

                                if (string.IsNullOrEmpty(uri))
                                {
                                    uri = xmlNode.NamespaceURI;
                                }
                            }
                        }

                        xmlProp.SetAttribute(xmlNode.LocalName, uri, xmlNode.Value);

                        return null;
                    }
                //case XmlNodeType.Whitespace:
                //case XmlNodeType.CDATA:
                //case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.Text:
                    {
                        string text = xmlNode.Value;

                        if (string.IsNullOrEmpty(text))
                        {
#if DEBUG
                            Debugger.Break();
#endif // DEBUG
                            break; // switch+case
                        }

                        text = text.Replace(@"\r\n", @"\n");


                        TextMedia textMedia = presentation.MediaFactory.CreateTextMedia();
                        textMedia.Text = text;

                        ChannelsProperty cProp = presentation.PropertyFactory.CreateChannelsProperty();
                        cProp.SetMedia(presentation.ChannelsManager.GetOrCreateTextChannel(), textMedia);


                        bool atLeastOneSiblingElement = false;
                        foreach (XmlNode childXmlNode in xmlNode.ParentNode.ChildNodes)
                        {
                            XmlNodeType childXmlType = childXmlNode.NodeType;
                            if (childXmlType == XmlNodeType.Element)
                            {
                                atLeastOneSiblingElement = true;
                                break;
                            }
                        }

                        if (atLeastOneSiblingElement)
                        {
                            TreeNode txtWrapperNode = presentation.TreeNodeFactory.Create();
                            txtWrapperNode.AddProperty(cProp);
                            parentTreeNode.AppendChild(txtWrapperNode);
                        }
                        else
                        {
                            AbstractTextMedia txtMedia = parentTreeNode.GetTextMedia();
                            if (txtMedia == null)
                            {
                                parentTreeNode.AddProperty(cProp);
                            }
                            else
                            {
                                // Merge contiguous text chunks (occurs with script commented CDATA section in XHTML)
                                txtMedia.Text += text;
                            }
                        }

                        break; // switch+case
                    }
                default:
                    {
                        return null;
                    }
            }

            return null;
        }

        protected bool structureInsertDialog(TreeNode node, string title, string cmdShort, string cmdLong, string cmdId,
            TreeNode nodeToInsert,
            string initialNameInput, string initialTextInput, string labelNameInput, string labelTextInput, string labelXmlInput,
            Func<string, string, string, TreeNode> callback)
        {
            //m_EventAggregator.GetEvent<EscapeEvent>().Publish(null);

            int position = node.Children.Count; // append
            position = 0; // prepend

            TreeNode parent = node;

            var radioPrepend = new RadioButton()
            {
                Content = Tobi_Plugin_DocumentPane_Lang.InsertDocFragmentFirstInside,
                GroupName = "INSERT"
            };
            var radioAppend = new RadioButton()
            {
                Content = Tobi_Plugin_DocumentPane_Lang.InsertDocFragmentLastInside,
                GroupName = "INSERT"
            };
            var radioBefore = new RadioButton()
            {
                Content = Tobi_Plugin_DocumentPane_Lang.InsertDocFragmentBefore,
                GroupName = "INSERT"
            };
            var radioAfter = new RadioButton()
            {
                Content = Tobi_Plugin_DocumentPane_Lang.InsertDocFragmentAfter,
                GroupName = "INSERT"
            };

            radioPrepend.IsChecked = true;

            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
            };

            if (node.GetTextMedia() != null && string.IsNullOrEmpty(node.GetXmlElementLocalName())) // text-only node
            {
                radioBefore.IsChecked = true;
            }
            else
            {
                panel.Children.Add(radioPrepend);
                panel.Children.Add(radioAppend);
            }

            if (node.Parent != null)
            {
                panel.Children.Add(radioBefore);
                panel.Children.Add(radioAfter);
            }

            if (labelNameInput != null || labelTextInput != null)
            {
                panel.Children.Add(new Separator()
                {
                    Height = 3,
                    Margin = new Thickness(0, 8, 0, 0)
                });
            }

            var elementNameInput = new TextBox()
            {
                Text = string.IsNullOrEmpty(initialNameInput) ? "" : initialNameInput
            };
            var elementTextInput = new TextBox()
            {
                Text = string.IsNullOrEmpty(initialTextInput) ? "" : initialTextInput
            };

            if (labelNameInput != null)
            {
                var label_NameInput = new TextBlock()
                {
                    Text = labelNameInput + ": ",
                    Margin = new Thickness(0, 0, 8, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Focusable = true,
                    TextWrapping = TextWrapping.Wrap
                };

                var panel_NameInput = new DockPanel()
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                label_NameInput.SetValue(DockPanel.DockProperty, Dock.Left);
                panel_NameInput.Children.Add(label_NameInput);
                panel_NameInput.Children.Add(elementNameInput);

                panel.Children.Add(panel_NameInput);
            }

            if (labelTextInput != null)
            {
                var label_TextInput = new TextBlock()
                {
                    Text = labelTextInput + ": ",
                    Margin = new Thickness(0, 0, 8, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Focusable = true,
                    TextWrapping = TextWrapping.Wrap
                };

                var panel_TextInput = new DockPanel()
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 0)
                };
                if (labelNameInput == null)
                {
                    panel_TextInput.Margin = new Thickness(0, 10, 0, 0);
                }

                label_TextInput.SetValue(DockPanel.DockProperty, Dock.Left);
                panel_TextInput.Children.Add(label_TextInput);
                panel_TextInput.Children.Add(elementTextInput);

                panel.Children.Add(panel_TextInput);
            }


            var textbox = new TextBoxReadOnlyCaretVisible()
            {
                FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],

                BorderThickness = new Thickness(1),
                Padding = new Thickness(6),
                //TextReadOnly = info,

                Text = "",
                AcceptsReturn = true,
                //AcceptsTab = true,
                //TextWrapping = TextWrapping.WrapWithOverflow

                Height = 100
            };

            bool showXmlSourceInputBox = labelNameInput != null;
            if (showXmlSourceInputBox)
            {
                var label_XmlInput = new TextBlock()
                {
                    Text = labelXmlInput + ": ",
                    Margin = new Thickness(0, 20, 8, 4),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Focusable = true,
                    TextWrapping = TextWrapping.Wrap
                };

                panel.Children.Add(label_XmlInput);

                panel.Children.Add(textbox);
            }

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   title,
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 350, showXmlSourceInputBox ? 370 : (labelNameInput == null && labelTextInput == null ? 170 : 220), null, 200, null);
            windowPopup.ShowModal();

            if (windowPopup.ClickedDialogButton != PopupModalWindow.DialogButton.Ok)
            {
                return false;
            }


            if (nodeToInsert == null && callback != null)
            {
                nodeToInsert = callback(elementNameInput.Text, elementTextInput.Text, textbox.Text);
            }

            if (nodeToInsert == null)
            {
                return false;
            }


            bool isCompositeCommand = false;
            if ((Convert.ToBoolean(radioPrepend.IsChecked) || Convert.ToBoolean(radioAppend.IsChecked)) && node.GetTextMedia() != null)
            {
                DebugFix.Assert(!string.IsNullOrEmpty(node.GetXmlElementLocalName()));

                TreeNode nodeParent = node.Parent;
                if (nodeParent == null)
                {
#if DEBUG
                    Debugger.Break();
#endif
                    return false;
                }

                isCompositeCommand = true;
                nodeParent.Presentation.UndoRedoManager.StartTransaction(cmdShort, cmdLong, cmdId);

                var previous = node.GetPreviousSiblingWithText();
                var next = node.GetNextSiblingWithText();

                // Step (1) DETACH
                int pos = nodeParent.Children.IndexOf(node);
                var cmd1 = nodeParent.Presentation.CommandFactory.CreateTreeNodeRemoveCommand(node);
                nodeParent.Presentation.UndoRedoManager.Execute(cmd1);
                //node.Tag = null;

                if (previous != null)
                {
                    m_UrakawaSession.PerformTreeNodeSelection(previous);
                }
                else if (next != null)
                {
                    m_UrakawaSession.PerformTreeNodeSelection(next);
                }
                else if (nodeParent != null)
                {
                    m_UrakawaSession.PerformTreeNodeSelection(nodeParent);
                }
                else
                {
                    m_UrakawaSession.PerformTreeNodeSelection(nodeParent.Presentation.RootNode);
                }

                // Step (2) CLONE AND EMPTY TEXT
                TreeNode nodeClone = node.Copy(true, true);
                Channel textChannel = node.Presentation.ChannelsManager.GetOrCreateTextChannel();
                ChannelsProperty cloneChProp = nodeClone.GetOrCreateChannelsProperty();
                cloneChProp.SetMedia(textChannel, null);
                var cmd2 = nodeParent.Presentation.CommandFactory.CreateTreeNodeInsertCommand(nodeClone, nodeParent, pos);
                nodeParent.Presentation.UndoRedoManager.Execute(cmd2);

                // Step (3) RE-ADD TEXT AS CHILD
                TreeNode newTextNode = nodeParent.Presentation.TreeNodeFactory.Create();
                ChannelsProperty newChProp = newTextNode.GetOrCreateChannelsProperty();
                //Channel newTextChannel = node.Presentation.ChannelFactory.CreateTextChannel();
                TextMedia newTxtMedia = nodeParent.Presentation.MediaFactory.CreateTextMedia();
                newTxtMedia.Text = node.GetTextMedia().Text; //node.GetTextFlattened()
                newChProp.SetMedia(textChannel, newTxtMedia);
                var cmd3 = nodeParent.Presentation.CommandFactory.CreateTreeNodeInsertCommand(newTextNode, nodeClone, 0);
                nodeParent.Presentation.UndoRedoManager.Execute(cmd3);

                node = nodeClone;
            }

            if (Convert.ToBoolean(radioPrepend.IsChecked))
            {
                position = 0; // prepend

                parent = node;
            }
            else if (Convert.ToBoolean(radioAppend.IsChecked))
            {
                position = node.Children.Count; // append

                parent = node;
            }
            else if (Convert.ToBoolean(radioBefore.IsChecked))
            {
                parent = node.Parent;
                if (parent == null) return false;

                position = parent.Children.IndexOf(node);
            }
            else if (Convert.ToBoolean(radioAfter.IsChecked))
            {
                parent = node.Parent;
                if (parent == null) return false;

                position = parent.Children.IndexOf(node) + 1;
            }

            var cmd = node.Presentation.CommandFactory.CreateTreeNodeInsertCommand(nodeToInsert, parent, position);
            node.Presentation.UndoRedoManager.Execute(cmd);

            if (isCompositeCommand)
            {
                node.Presentation.UndoRedoManager.EndTransaction();
            }

            m_UrakawaSession.PerformTreeNodeSelection(nodeToInsert);

            return true;
        }

        public void stripXmlIds(TreeNode node)
        {
            XmlProperty xmlProp = node.GetXmlProperty();
            if (xmlProp != null)
            {
                urakawa.property.xml.XmlAttribute idAttr = xmlProp.GetAttribute(XmlReaderWriterHelper.XmlId, XmlReaderWriterHelper.NS_URL_XML);
                if (idAttr == null)
                {
                    idAttr = xmlProp.GetAttribute("id");
                }
                if (idAttr != null)
                {
                    xmlProp.RemoveAttribute(idAttr);
                }
            }

            foreach (var child in node.Children.ContentsAs_Enumerable)
            {
                stripXmlIds(child);
            }
        }

        private bool? m_valid = null;

        protected bool checkValid()
        {
            if (!Tobi.Common.Settings.Default.EnableMarkupValidation)
            {
                m_valid = true;
                return true;
            }

            bool thereIsAtLeastOneError = false;
            IValidator m_Validator = null;
            if (m_Validators != null)
            {
                foreach (var validator in m_Validators)
                {
                    if (!(validator is ContentDocumentValidator)) continue;
                    m_Validator = validator;
                    break;
                }
            }
            if (m_Validator != null)
            {
                if (m_valid == null) // first time since open
                {
                    m_valid = m_Validator.IsValid;
                }

                m_Validator.Validate();

                foreach (var validationItem in m_Validator.ValidationItems)
                {
                    if (validationItem.Severity == ValidationSeverity.Error)
                    {
                        thereIsAtLeastOneError = true;
                        break;
                    }
                }
            }

            if (thereIsAtLeastOneError && m_valid == true)
            {
                m_Logger.Log("Document structure edit VALIDATION error", Category.Debug, Priority.Medium);

                if (!Settings.Default.InvalidStructEdit_DoNotAskAgain
                    &&
                    //askUser(
                    checkWithUserValidation(
                    Tobi_Plugin_DocumentPane_Lang.StructureEditValidationErrorAskDisplayDialog,
                    Tobi_Plugin_DocumentPane_Lang.ConfirmDisplayValidationDialog,
                    null
                ))
                {
                    if (m_EventAggregator != null)
                    {
                        m_EventAggregator.GetEvent<ValidationReportRequestEvent>().Publish(typeof(ContentDocumentValidator).Name);
                    }
                }
            }

            m_valid = !thereIsAtLeastOneError;
            return thereIsAtLeastOneError;
        }

        [ImportMany(typeof(IValidator), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = true)]
        private IEnumerable<IValidator> m_Validators;

        //private ContentDocumentValidator m_Validator;

        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        [ImportingConstructor]
        public DocumentPaneView(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILoggerFacade logger,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession urakawaSession,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView
            //,
            //[Import(typeof(ContentDocumentValidator), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = false)]
            //ContentDocumentValidator validator
            )
        {
            //m_Validator = validator;

            m_PropertyChangeHandler = new PropertyChangedNotifyBase();
            m_PropertyChangeHandler.InitializeDependentProperties(this);

            m_RegionManager = regionManager;
            m_EventAggregator = eventAggregator;
            m_Logger = logger;

            m_UrakawaSession = urakawaSession;
            m_ShellView = shellView;

            DataContext = this;

            CommandFocus = new RichDelegateCommand(
                Tobi_Plugin_DocumentPane_Lang.CmdTxtFocus_ShortDesc,
                null,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("edit-select-all"),
                () =>
                {
                    BringIntoFocus();
                },
                () => true,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Focus_Txt));

            m_ShellView.RegisterRichCommand(CommandFocus);
            //
            CommandToggleSinglePhraseView = new RichDelegateCommand(
                Tobi_Plugin_DocumentPane_Lang.CmdTextOnlyViewToggle_ShortDesc,
                Tobi_Plugin_DocumentPane_Lang.CmdTextOnlyViewToggle_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadGnomeNeuIcon("Neu_preferences-desktop-font"),
                () =>
                {
                    Settings.Default.Document_SinglePhraseView = !Settings.Default.Document_SinglePhraseView;
                },
                () => true,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_ToggleSinglePhraseView));

            m_ShellView.RegisterRichCommand(CommandToggleSinglePhraseView);
            //
            CommandToggleTextSyncGranularity = new RichDelegateCommand(
                Tobi.Common.Tobi_Common_Lang.CmdToggleTextSyncGranularity_ShortDesc,
                Tobi.Common.Tobi_Common_Lang.CmdToggleTextSyncGranularity_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                //m_ShellView.LoadTangoIcon("internet-group-chat"),
                //m_ShellView.LoadGnomeGionIcon("Gion_text-x-authors"),
                m_ShellView.LoadGnomeGionIcon("Gion_text-x-script"),
                () =>
                {
                    Tobi.Common.Settings.Default.EnableTextSyncGranularity = !Tobi.Common.Settings.Default.EnableTextSyncGranularity;
                },
                () => true,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_ToggleTextSyncGranularity));

            m_ShellView.RegisterRichCommand(CommandToggleTextSyncGranularity);
            //
            CommandSwitchNarratorView = new RichDelegateCommand(
                Tobi_Plugin_DocumentPane_Lang.CmdSwitchNarratorView_ShortDesc,
                Tobi_Plugin_DocumentPane_Lang.CmdSwitchNarratorView_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("preferences-system-windows"),
                () =>
                {
                    IsNarratorMode = !IsNarratorMode;
                },
                () => !m_UrakawaSession.isAudioRecording,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_SwitchNarratorView));

            m_ShellView.RegisterRichCommand(CommandSwitchNarratorView);
            //
            CommandStructureDown = new RichDelegateCommand(
                Tobi_Plugin_DocumentPane_Lang.CmdStructureDown_ShortDesc,
                Tobi_Plugin_DocumentPane_Lang.CmdStructureDown_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadGnomeFoxtrotIcon("Foxtrot_go-bottom"),
                () =>
                {
                    Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();
                    List<TreeNode> pathToTreeNode =
                        getPathToTreeNode(treeNodeSelection.Item2 ?? treeNodeSelection.Item1);
                    int iTreeNode = pathToTreeNode.IndexOf(treeNodeSelection.Item1);
                    int iSubTreeNode = treeNodeSelection.Item2 == null
                                           ? -1
                                           : pathToTreeNode.IndexOf(treeNodeSelection.Item2);
                    if (iTreeNode == (pathToTreeNode.Count - 1))
                    {
                        AudioCues.PlayBeep();
                        return;
                    }
                    if (iSubTreeNode == -1) // down
                    {
                        m_UrakawaSession.PerformTreeNodeSelection(pathToTreeNode[iTreeNode + 1]);
                        return;
                    }
                    if (iTreeNode == iSubTreeNode - 1) // toggle
                    {
                        m_UrakawaSession.PerformTreeNodeSelection(treeNodeSelection.Item1);
                        //pathToTreeNode[iTreeNode]
                        return;
                    }
                    m_UrakawaSession.PerformTreeNodeSelection(treeNodeSelection.Item1);
                    m_UrakawaSession.PerformTreeNodeSelection(pathToTreeNode[iTreeNode + 1]);
                },
                () =>
                {
                    if (m_UrakawaSession.DocumentProject == null) return false;
                    if (m_UrakawaSession.isAudioRecording) return false;

                    Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                    return selection.Item1 != null;
                },
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_StructureSelectDown));

            m_ShellView.RegisterRichCommand(CommandStructureDown);

            //
            CommandFollowLink = new RichDelegateCommand(
                Tobi_Plugin_DocumentPane_Lang.CmdFollowLink_ShortDesc,
                Tobi_Plugin_DocumentPane_Lang.CmdFollowLink_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadGnomeNeuIcon("Neu_emblem-symbolic-link"),
                () =>
                {
                    TextElement hyperlink = m_lastHighlightedSub ?? m_lastHighlighted;
                    do
                    {
                        if (hyperlink is Hyperlink && ((Hyperlink)hyperlink).NavigateUri != null
                            && hyperlink.Tag != null && hyperlink.Tag is TreeNode)
                        {
                            NavigateUri(((Hyperlink)hyperlink).NavigateUri);
                            return;
                        }
                        hyperlink = hyperlink.Parent as TextElement;
                    } while (hyperlink != null);

                    AudioCues.PlayAsterisk();
                },
                () =>
                {
                    if (m_UrakawaSession.isAudioRecording) return false;

                    TextElement textElement = m_lastHighlightedSub ?? m_lastHighlighted;
                    return textElement != null;
                },
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_FollowLink));

            m_ShellView.RegisterRichCommand(CommandFollowLink);

            //
            //
            CommandUnFollowLink = new RichDelegateCommand(
                Tobi_Plugin_DocumentPane_Lang.CmdUnfollowLink_ShortDesc,
                Tobi_Plugin_DocumentPane_Lang.CmdUnfollowLink_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadGnomeNeuIcon("Neu_edit-undo"),
                () =>
                {

                    if (m_UrakawaSession.DocumentProject == null) return;

                    Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                    TreeNode treeNode_ = selection.Item2 ?? selection.Item1;
                    if (treeNode_ == null)
                    {
                        AudioCues.PlayAsterisk();
                        return;
                    }

                    TreeNode treeNode = ensureTreeNodeIsNoteAnnotation(treeNode_);
                    if (treeNode == null)
                    {
                        AudioCues.PlayAsterisk();
                        return;
                    }

                    string uid = treeNode.GetXmlElementId();
                    if (string.IsNullOrEmpty(uid))
                    {
                        AudioCues.PlayAsterisk();
                        return;
                    }

                    //string id = XukToFlowDocument.IdToName(uid);

                    TextElement textElement = null;

                    TextElement te;
                    m_idLinkTargets.TryGetValue(uid, out te);

                    if (te != null) //m_idLinkTargets.ContainsKey(uid))
                    {
                        textElement = te; // m_idLinkTargets[uid];
                    }
                    //            if (textElement == null)
                    //            {
                    //#if DEBUG
                    //                Debugger.Break();
                    //#endif //DEBUG
                    //                textElement = TheFlowDocument.FindName(uid) as TextElement;
                    //            }
                    if (textElement != null)
                    {
                        if (textElement.Tag is TreeNode)
                        {
                            DebugFix.Assert(treeNode == (TreeNode)textElement.Tag);
                        }
                    }

                    List<TextElement> lte;
                    m_idLinkSources.TryGetValue(uid, out lte);

                    if (lte != null) //m_idLinkSources.ContainsKey(uid))
                    {
                        var list = lte; // m_idLinkSources[uid];
#if DEBUG
                        if (list.Count > 1) Debugger.Break();
#endif //DEBUG
                        textElement = list[0];//TODO: popup list of choices when several reference sources
                    }
                    if (textElement != null)
                    {
                        if (textElement.Tag is TreeNode)
                        {
                            m_UrakawaSession.PerformTreeNodeSelection((TreeNode)textElement.Tag);
                            return;
                        }
                        else
                        {
#if DEBUG
                            Debugger.Break();
#endif //DEBUG
                            Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(textElement.BringIntoView));

                        }
                    }

                    AudioCues.PlayAsterisk();
                },
                () =>
                {
                    if (m_UrakawaSession.isAudioRecording) return false;

                    TextElement textElement = m_lastHighlightedSub ?? m_lastHighlighted;
                    return textElement != null;
                },
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_UnfollowLink));

            m_ShellView.RegisterRichCommand(CommandUnFollowLink);

            //

            CommandStructCutFragment = new RichDelegateCommand(
                Tobi_Plugin_DocumentPane_Lang.CmdStructEditCutFragment_ShortDesc,
                Tobi_Plugin_DocumentPane_Lang.CmdStructEditCutFragment_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                null, //m_ShellView.LoadTangoIcon("accessories-text-editor"),
                () =>
                {
                    Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();

                    //TreeNode node = selection.Item2 ?? selection.Item1;
                    TreeNode node = selection.Item1; // TOP LEVEL!

                    if (node == null) return;

                    if (!checkDisplayStructEditWarning()) return;

                    //m_EventAggregator.GetEvent<EscapeEvent>().Publish(null);

                    if (m_TreeNodeFragmentClipboard != null)
                    {
                        if (
                            !
                            //askUser(
                            checkWithUser(
                                Tobi_Plugin_DocumentPane_Lang.CmdStructEditCutFragment_ShortDesc,
                                Tobi_Plugin_DocumentPane_Lang.ConfirmStructureClipboard,
                                null
                                ))
                        {
                            return;
                        }
                        m_TreeNodeFragmentClipboard = null;
                    }

                    var previous = node.GetPreviousSiblingWithText();
                    var next = node.GetNextSiblingWithText();
                    var nodeParent = node.Parent;

                    var cmd = node.Presentation.CommandFactory.CreateTreeNodeRemoveCommand(node);
                    node.Presentation.UndoRedoManager.Execute(cmd);

                    //checkValid();

                    if (previous != null)
                    {
                        m_UrakawaSession.PerformTreeNodeSelection(previous);
                    }
                    else if (next != null)
                    {
                        m_UrakawaSession.PerformTreeNodeSelection(next);
                    }
                    else if (nodeParent != null)
                    {
                        m_UrakawaSession.PerformTreeNodeSelection(nodeParent);
                    }
                    else
                    {
                        m_UrakawaSession.PerformTreeNodeSelection(node.Presentation.RootNode);
                    }

                    // clone looses Tags and text cache (FlowDocument TextElement mapping gets recreated at paste time)
                    m_TreeNodeFragmentClipboard = node.Copy(true, true);
                },
                () =>
                {
                    return CommandStructRemoveFragment.CanExecute();
                },
                Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_StructEditCutFragment)
                );

            m_ShellView.RegisterRichCommand(CommandStructCutFragment);

            //
            CommandStructCopyFragment = new RichDelegateCommand(
                Tobi_Plugin_DocumentPane_Lang.CmdStructEditCopyFragment_ShortDesc,
                Tobi_Plugin_DocumentPane_Lang.CmdStructEditCopyFragment_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                null, //m_ShellView.LoadTangoIcon("accessories-text-editor"),
                () =>
                {
                    Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();

                    //TreeNode node = selection.Item2 ?? selection.Item1;
                    TreeNode node = selection.Item1; // TOP LEVEL!

                    if (node == null) return;

                    if (!checkDisplayStructEditWarning()) return;

                    //m_EventAggregator.GetEvent<EscapeEvent>().Publish(null);

                    if (m_TreeNodeFragmentClipboard != null)
                    {
                        if (
                            !
                            //askUser(
                            checkWithUser(
                                Tobi_Plugin_DocumentPane_Lang.CmdStructEditCopyFragment_ShortDesc,
                                Tobi_Plugin_DocumentPane_Lang.ConfirmStructureClipboard,
                                null
                                ))
                        {
                            return;
                        }
                        m_TreeNodeFragmentClipboard = null;
                    }

                    // clone looses Tags and text cache (FlowDocument TextElement mapping gets recreated at paste time)
                    m_TreeNodeFragmentClipboard = node.Copy(true, true);

                    stripXmlIds(m_TreeNodeFragmentClipboard);
                },
                () =>
                {
                    return CommandStructRemoveFragment.CanExecute();
                },
                Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_StructEditCutFragment)
                );

            m_ShellView.RegisterRichCommand(CommandStructCopyFragment);

            //

            CommandStructPasteFragment = new RichDelegateCommand(
                Tobi_Plugin_DocumentPane_Lang.CmdStructEditPasteFragment_ShortDesc,
                Tobi_Plugin_DocumentPane_Lang.CmdStructEditPasteFragment_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                null, //m_ShellView.LoadTangoIcon("accessories-text-editor"),
                () =>
                {
                    Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();

                    //TreeNode node = selection.Item2 ?? selection.Item1;
                    TreeNode node = selection.Item1; // TOP LEVEL!

                    if (node == null) return;

                    if (!checkDisplayStructEditWarning()) return;

                    if (structureInsertDialog(node,
                        UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_DocumentPane_Lang.CmdStructEditPasteFragment_ShortDesc),
                        "Extract text into new child TreeNode, then insert new node",
                        "Extract text into child mixed XML content, and insert new TreeNode",
                        "DOC_TEXT_ONLY_NODE_INSERT-PASTE",
                        m_TreeNodeFragmentClipboard,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null
                        ))
                    {
                        m_TreeNodeFragmentClipboard = null;

                        //checkValid();
                    }
                },
                () =>
                {
                    return m_TreeNodeFragmentClipboard != null && CommandStructInsertFragment.CanExecute();
                },
                Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_StructEditPasteFragment)
                );

            m_ShellView.RegisterRichCommand(CommandStructPasteFragment);

            //

            CommandStructRemoveFragment = new RichDelegateCommand(
                Tobi_Plugin_DocumentPane_Lang.CmdStructEditRemoveFragment_ShortDesc,
                Tobi_Plugin_DocumentPane_Lang.CmdStructEditRemoveFragment_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                null, //m_ShellView.LoadTangoIcon("accessories-text-editor"),
                () =>
                {
                    Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();

                    //TreeNode node = selection.Item2 ?? selection.Item1;
                    TreeNode node = selection.Item1; // TOP LEVEL!

                    if (node == null) return;

                    if (!checkDisplayStructEditWarning()) return;

                    //m_EventAggregator.GetEvent<EscapeEvent>().Publish(null);

                    var previous = node.GetPreviousSiblingWithText();
                    var next = node.GetNextSiblingWithText();
                    var nodeParent = node.Parent;

                    var cmd = node.Presentation.CommandFactory.CreateTreeNodeRemoveCommand(node);
                    node.Presentation.UndoRedoManager.Execute(cmd);

                    //checkValid();

                    if (previous != null)
                    {
                        m_UrakawaSession.PerformTreeNodeSelection(previous);
                    }
                    else if (next != null)
                    {
                        m_UrakawaSession.PerformTreeNodeSelection(next);
                    }
                    else if (nodeParent != null)
                    {
                        m_UrakawaSession.PerformTreeNodeSelection(nodeParent);
                    }
                    else
                    {
                        m_UrakawaSession.PerformTreeNodeSelection(node.Presentation.RootNode);
                    }
                },
                () =>
                {
                    if (m_UrakawaSession.DocumentProject == null) return false;

                    if (m_UrakawaSession.isAudioRecording) return false;

                    Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                    TreeNode node = selection.Item2 ?? selection.Item1;
                    return node != null && node.Parent != null;
                },
                Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_StructEditRemoveFragment)
                );

            m_ShellView.RegisterRichCommand(CommandStructRemoveFragment);

            //

            CommandStructInsertFragment = new RichDelegateCommand(
                Tobi_Plugin_DocumentPane_Lang.CmdStructEditInsertFragment_ShortDesc,
                Tobi_Plugin_DocumentPane_Lang.CmdStructEditInsertFragment_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                null, //m_ShellView.LoadTangoIcon("accessories-text-editor"),
                () =>
                {
                    Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();

                    //TreeNode node = selection.Item2 ?? selection.Item1;
                    TreeNode node = selection.Item1; // TOP LEVEL!

                    if (node == null) return;

                    if (!checkDisplayStructEditWarning()) return;

                    bool html = node.Presentation.RootNode.GetXmlElementLocalName()
                        .Equals("body", StringComparison.OrdinalIgnoreCase);

                    if (structureInsertDialog(node,
                        UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_DocumentPane_Lang.CmdStructEditInsertFragment_ShortDesc),
                        "Extract text into new child TreeNode, then insert new node",
                        "Extract text into child mixed XML content, and insert new TreeNode",
                        "DOC_TEXT_ONLY_NODE_INSERT-CUSTOM",
                        null,
                        html ? "span" : "sent",
                        "TXT",
                        Tobi_Plugin_DocumentPane_Lang.ElementName,
                        Tobi_Plugin_DocumentPane_Lang.TextContent,
                        Tobi_Plugin_DocumentPane_Lang.AdvancedXmlInsert,
(elementName, elementText, xmlSource) =>
{
    if (!String.IsNullOrEmpty(xmlSource))
    {
        string xmlns_mathml = XmlReaderWriterHelper.NS_PREFIX_XMLNS + ":" + DiagramContentModelHelper.NS_PREFIX_MATHML + "=\"" + DiagramContentModelHelper.NS_URL_MATHML + "\"";
        string xmlns_svg = XmlReaderWriterHelper.NS_PREFIX_XMLNS + ":" + DiagramContentModelHelper.NS_PREFIX_SVG + "=\"" + DiagramContentModelHelper.NS_URL_SVG + "\"";
        string xmlns_epub = XmlReaderWriterHelper.NS_PREFIX_XMLNS + ":" + DiagramContentModelHelper.NS_PREFIX_EPUB + "=\"" + DiagramContentModelHelper.NS_URL_EPUB + "\"";

        string xmlns_xhtml = "xmlns=\"" + DiagramContentModelHelper.NS_URL_XHTML + "\"";
        string xmlns_dtbook = "xmlns=\"" + "http://www.daisy.org/z3986/2005/dtbook/" + "\"";

        string xmlSourceString = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
        xmlSourceString += ("<root "
            //+ XmlReaderWriterHelper.NS_PREFIX_XMLNS + "=\"" + node.Presentation.RootNode.GetXmlNamespaceUri() + "\""
            + " "
            + (html ? xmlns_xhtml : xmlns_dtbook)
            + " "
            + xmlns_mathml
            + " "
            + xmlns_svg
            + " "
            + xmlns_epub

            + " >");

        string strippedNS = xmlSource.Replace(xmlns_mathml, " ");
        strippedNS = strippedNS.Replace(xmlns_svg, " ");
        strippedNS = strippedNS.Replace(xmlns_epub, " ");
        xmlSourceString += strippedNS;
        xmlSourceString += "</root>";

        //byte[] xmlSourceString_RawEncoded = Encoding.UTF8.GetBytes(xmlSourceString);
        //MemoryStream stream = new MemoryStream();
        //stream.Write(xmlSourceString_RawEncoded, 0, xmlSourceString_RawEncoded.Length);

        //stream.Flush();

        //stream.Seek(0, SeekOrigin.Begin);
        //stream.Position = 0;

        //XmlDocument fragmentDoc = new XmlDocument();
        //fragmentDoc.XmlResolver = null;

        ////XmlTextReader reader = new XmlTextReader(stream);
        ////fragmentDoc.Load(reader);

        //fragmentDoc.Load(stream);

        ////fragmentDoc.LoadXml(xmlSourceString);


        //XmlNode tobi = fragmentDoc.ChildNodes[1]; // skip XML declaration
        //XmlNodeList children = tobi.ChildNodes;
        //XmlNode[] xmlNodes = new XmlNode[children.Count];
        //int i = 0;
        //foreach (XmlNode child in children)
        //{
        //    xmlNodes[i] = child;
        //    i++;
        //}
        //for (i = 0; i < xmlNodes.Length; i++)
        //{
        //    XmlNode child = xmlNodes[i];
        //    XmlNode imported = htmlDocument.ImportNode(child, true);
        //    tobi.RemoveChild(child);
        //    textParentNode.AppendChild(imported);
        //}

        //normalizedDescriptionText = textParentNode.InnerXml;

        XmlDocument xmldoc = XmlReaderWriterHelper.ParseXmlDocumentFromString(xmlSourceString, false, false);

        //        try
        //        {
        //        }
        //        catch (Exception ex)
        //        {
        //#if DEBUG
        //            Debugger.Break();
        //#endif
        //            return null;
        //        }

        TreeNode root = null;
        if (xmldoc != null && xmldoc.DocumentElement != null && xmldoc.DocumentElement.FirstChild != null)
        {
            try
            {
                TreeNode.EnableTextCache = false;
                root = buildTreeNodeFromXml(xmldoc.DocumentElement.FirstChild, node.Presentation, null);
            }
            finally
            {
                TreeNode.EnableTextCache = true;
            }
        }
        if (root != null)
        {
            stripXmlIds(root);
        }
        return root;
    }

    if (String.IsNullOrEmpty(elementName))
    {
        return null;
    }

    TreeNode newNode = node.Presentation.TreeNodeFactory.Create();

    if (!string.IsNullOrEmpty(elementText))
    {
        ChannelsProperty chProp = newNode.GetOrCreateChannelsProperty();
        //Channel textChannel = node.Presentation.ChannelFactory.CreateTextChannel();
        Channel textChannel = node.Presentation.ChannelsManager.GetOrCreateTextChannel();
        TextMedia txtMedia = node.Presentation.MediaFactory.CreateTextMedia();
        txtMedia.Text = elementText;
        chProp.SetMedia(textChannel, txtMedia);
    }

    if (!String.IsNullOrEmpty(elementName))
    {
        XmlProperty xmlProp = newNode.GetOrCreateXmlProperty();
        string uri = node.GetXmlNamespaceUri();
        if (string.IsNullOrEmpty(uri) && node.Parent != null)
        {
            uri = node.Parent.GetXmlNamespaceUri();
        }
        if (string.IsNullOrEmpty(uri))
        {
#if DEBUG
            Debugger.Break();
#endif
            uri = "";
        }
        xmlProp.SetQName(elementName, uri);
    }

    return newNode;
}
                        ))
                    {
                        //checkValid();
                    }
                },
                () =>
                {
                    if (m_UrakawaSession.DocumentProject == null) return false;

                    if (m_UrakawaSession.isAudioRecording) return false;

                    Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                    TreeNode node = selection.Item2 ?? selection.Item1;
                    return node != null
                        //&& node.GetTextMedia() == null
                        //&& node.GetText() == null
                        ; // && node.Parent != null;
                },
                Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_StructEditRemoveFragment)
                );

            m_ShellView.RegisterRichCommand(CommandStructInsertFragment);
            //

            CommandStructInsertPageBreak = new RichDelegateCommand(
                Tobi_Plugin_DocumentPane_Lang.CmdStructEditInsertPageBreak_ShortDesc,
                Tobi_Plugin_DocumentPane_Lang.CmdStructEditInsertPageBreak_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                null, //m_ShellView.LoadTangoIcon("accessories-text-editor"),
                () =>
                {
                    Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();

                    //TreeNode node = selection.Item2 ?? selection.Item1;
                    TreeNode node = selection.Item1; // TOP LEVEL!

                    if (node == null) return;

                    if (!checkDisplayStructEditWarning()) return;

                    bool html = node.Presentation.RootNode.GetXmlElementLocalName()
                        .Equals("body", StringComparison.OrdinalIgnoreCase);

                    if (structureInsertDialog(node,
                        UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_DocumentPane_Lang.CmdStructEditInsertFragment_ShortDesc),
                        "Extract text into new child TreeNode, then insert new node",
                        "Extract text into child mixed XML content, and insert new TreeNode",
                        "DOC_TEXT_ONLY_NODE_INSERT-PAGEBREAK",
                        null,
                        null,
                        "1",
                        null,
                        Tobi_Plugin_DocumentPane_Lang.PageLabel,
                        null,
(elementName, elementText, xmlSource) =>
{
    if (string.IsNullOrEmpty(elementText))
    {
        return null;
    }

    TreeNode newNode = node.Presentation.TreeNodeFactory.Create();

    XmlProperty xmlProp = newNode.GetOrCreateXmlProperty();
    string uri = node.GetXmlNamespaceUri();
    if (string.IsNullOrEmpty(uri) && node.Parent != null)
    {
        uri = node.Parent.GetXmlNamespaceUri();
    }
    if (string.IsNullOrEmpty(uri))
    {
#if DEBUG
        Debugger.Break();
#endif
        uri = "";
    }
    xmlProp.SetQName(html ? "span" : "pagenum", uri);

    if (html)
    {
        xmlProp.SetAttribute("epub:type", DiagramContentModelHelper.NS_URL_EPUB, "pagebreak");
        xmlProp.SetAttribute("title", null, elementText);
    }
    else
    {
        xmlProp.SetAttribute("page", null, "normal");

        ChannelsProperty chProp = newNode.GetOrCreateChannelsProperty();
        //Channel textChannel = node.Presentation.ChannelFactory.CreateTextChannel();
        Channel textChannel = node.Presentation.ChannelsManager.GetOrCreateTextChannel();
        TextMedia txtMedia = node.Presentation.MediaFactory.CreateTextMedia();
        txtMedia.Text = elementText;
        chProp.SetMedia(textChannel, txtMedia);
    }

    return newNode;
}
                        ))
                    {
                        //checkValid();
                    }
                },
                () =>
                {
                    if (m_UrakawaSession.DocumentProject == null) return false;

                    if (m_UrakawaSession.isAudioRecording) return false;

                    Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                    TreeNode node = selection.Item2 ?? selection.Item1;
                    return node != null
                        //&& node.GetTextMedia() == null
                        //&& node.GetText() == null
                        ; // && node.Parent != null;
                },
                Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_StructEditRemoveFragment)
                );

            m_ShellView.RegisterRichCommand(CommandStructInsertPageBreak);

            //
            //
            CommandEditText = new RichDelegateCommand(
                Tobi_Plugin_DocumentPane_Lang.CmdEditText_ShortDesc,
                Tobi_Plugin_DocumentPane_Lang.CmdEditText_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("accessories-text-editor"),
                () =>
                {
                    List<TreeNode> nodes = new List<TreeNode>(1);

                    if (m_TextElementForEdit != null)
                    {
                        DebugFix.Assert(m_TextElementForEdit.Tag is TreeNode);
                        TreeNode node = (TreeNode)m_TextElementForEdit.Tag;
                        m_TextElementForEdit = null;

                        if (node != null)
                        {
                            nodes.Add(node);
                        }
                    }
                    else
                    {
                        Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                        //node = selection.Item2 ?? selection.Item1;
                        TreeNode node = selection.Item1;

                        if (node != null)
                        {
                            string text = TreeNodeChangeTextCommand.GetText(node);

                            if (!string.IsNullOrEmpty(text))
                            {
                                nodes.Add(node);
                            }
                            else
                            {
                                TreeNode descendantNode = node.GetFirstDescendantWithText();
                                if (descendantNode != null)
                                {
                                    string descendantNodeText = TreeNodeChangeTextCommand.GetText(descendantNode);
                                    if (descendantNodeText != null)
                                    {
                                        descendantNodeText = descendantNodeText.Trim();
                                    }

                                    if (!string.IsNullOrEmpty(descendantNodeText))
                                    {
                                        nodes.Add(descendantNode);
                                    }

                                    TreeNode siblingNode = descendantNode;
                                    while ((siblingNode = siblingNode.GetNextSiblingWithText()) != null)
                                    {
                                        if (!siblingNode.IsDescendantOf(node))
                                        {
                                            break;
                                        }

                                        string siblingNodeText = TreeNodeChangeTextCommand.GetText(siblingNode);
                                        if (siblingNodeText != null)
                                        {
                                            siblingNodeText = siblingNodeText.Trim();
                                        }
                                        if (string.IsNullOrEmpty(siblingNodeText))
                                        {
                                            continue;
                                        }

                                        nodes.Add(siblingNode);
                                    }
                                }
                            }
                        }
                    }


                    for (int i = 0; i < nodes.Count; i++)
                    {
                        TreeNode n = nodes[i];

                        string oldTxt = TreeNodeChangeTextCommand.GetText(n);

                        if (string.IsNullOrEmpty(oldTxt))
                        {
                            oldTxt = "";
                        }

                        string txt = showDialogTextEdit(i, nodes.Count, oldTxt);

                        if (txt == oldTxt) continue;
                        if (string.IsNullOrEmpty(txt)) break;

                        m_EventAggregator.GetEvent<EscapeEvent>().Publish(null);

                        var cmd = n.Presentation.CommandFactory.CreateTreeNodeChangeTextCommand(n, txt);
                        n.Presentation.UndoRedoManager.Execute(cmd);
                    }
                },
                () =>
                {
                    if (m_UrakawaSession.DocumentProject == null) return false;

                    if (m_UrakawaSession.isAudioRecording) return false;

                    Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                    //TreeNode node = selection.Item2 ?? selection.Item1;
                    TreeNode node = selection.Item1;
                    return m_TextElementForEdit != null
                            || node != null

                            // Note: allow selection of ancestor, as leaf node will be automatically selected
                        //&& !string.IsNullOrEmpty(TreeNodeChangeTextCommand.GetText(node))
                            ;
                },
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_EditPhraseText));

            m_ShellView.RegisterRichCommand(CommandEditText);

            //
            CommandStructureUp = new RichDelegateCommand(
                Tobi_Plugin_DocumentPane_Lang.CmdStructureUp_ShortDesc,
                Tobi_Plugin_DocumentPane_Lang.CmdStructureUp_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadGnomeFoxtrotIcon("Foxtrot_go-top"),
                () =>
                {
                    Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();
                    TreeNode nodeToNavigate = treeNodeSelection.Item1.Parent;
                    if (nodeToNavigate == null)
                        AudioCues.PlayBeep();
                    else
                        m_UrakawaSession.PerformTreeNodeSelection(nodeToNavigate);

                },
                () =>
                {
                    if (m_UrakawaSession.DocumentProject == null) return false;
                    if (m_UrakawaSession.isAudioRecording) return false;

                    Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                    return selection.Item1 != null;
                },
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_StructureSelectUp));

            m_ShellView.RegisterRichCommand(CommandStructureUp);
            //
            CommandSwitchPhrasePrevious = new RichDelegateCommand(
                Tobi_Plugin_DocumentPane_Lang.CmdEventSwitchPrevious_ShortDesc,
                Tobi_Plugin_DocumentPane_Lang.CmdEventSwitchPrevious_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadGnomeFoxtrotIcon("Foxtrot_go-first"),
                () =>
                {
                    nextPrevious(true);

                    //if (CurrentTreeNode == CurrentSubTreeNode)
                    //{
                    //    TreeNode nextNode = CurrentTreeNode.GetPreviousSiblingWithText();
                    //    if (nextNode != null)
                    //    {
                    //        m_Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] DocumentPaneView.SwitchPhrasePrevious",
                    //                   Category.Debug, Priority.Medium);

                    //        m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(nextNode);
                    //        return;
                    //    }
                    //}
                    //else
                    //{
                    //    TreeNode nextNode = CurrentSubTreeNode.GetPreviousSiblingWithText(CurrentTreeNode);
                    //    if (nextNode != null)
                    //    {
                    //        m_Logger.Log("-- PublishEvent [SubTreeNodeSelectedEvent] DocumentPaneView.SwitchPhrasePrevious",
                    //                   Category.Debug, Priority.Medium);

                    //        m_EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(nextNode);
                    //        return;
                    //    }
                    //    else
                    //    {
                    //        nextNode = CurrentTreeNode.GetPreviousSiblingWithText();
                    //        if (nextNode != null)
                    //        {
                    //            m_Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] DocumentPaneView.SwitchPhrasePrevious",
                    //                       Category.Debug, Priority.Medium);

                    //            m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(nextNode);
                    //            return;
                    //        }
                    //    }
                    //}

                },
                () =>
                {
                    if (m_UrakawaSession.DocumentProject == null) return false;
                    if (m_UrakawaSession.isAudioRecording) return false;

                    Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                    return selection.Item1 != null;
                },
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(
                    () => Settings_KeyGestures.Default.Keyboard_Doc_Event_SwitchPrevious));

            m_ShellView.RegisterRichCommand(CommandSwitchPhrasePrevious);
            //
            CommandSwitchPhraseNext = new RichDelegateCommand(
                Tobi_Plugin_DocumentPane_Lang.CmdEventSwitchNext_ShortDesc,
                Tobi_Plugin_DocumentPane_Lang.CmdEventSwitchNext_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadGnomeFoxtrotIcon("Foxtrot_go-last"),
                () =>
                {
                    nextPrevious(false);

                    //if (CurrentTreeNode == CurrentSubTreeNode)
                    //{
                    //    TreeNode nextNode = CurrentTreeNode.GetNextSiblingWithText();
                    //    if (nextNode != null)
                    //    {
                    //        m_Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] DocumentPaneView.SwitchPhraseNext",
                    //                   Category.Debug, Priority.Medium);

                    //        m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(nextNode);
                    //        return;
                    //    }
                    //}
                    //else
                    //{
                    //    TreeNode nextNode = CurrentSubTreeNode.GetNextSiblingWithText(CurrentTreeNode);
                    //    if (nextNode != null)
                    //    {
                    //        m_Logger.Log("-- PublishEvent [SubTreeNodeSelectedEvent] DocumentPaneView.SwitchPhraseNext",
                    //                   Category.Debug, Priority.Medium);

                    //        m_EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(nextNode);
                    //        return;
                    //    }
                    //    else
                    //    {
                    //        nextNode = CurrentTreeNode.GetNextSiblingWithText();
                    //        if (nextNode != null)
                    //        {
                    //            m_Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] DocumentPaneView.SwitchPhraseNext",
                    //                       Category.Debug, Priority.Medium);

                    //            m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(nextNode);
                    //            return;
                    //        }
                    //    }
                    //}

                },
                () =>
                {
                    if (m_UrakawaSession.DocumentProject == null) return false;
                    if (m_UrakawaSession.isAudioRecording) return false;

                    Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                    return selection.Item1 != null;
                },
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Doc_Event_SwitchNext));

            m_ShellView.RegisterRichCommand(CommandSwitchPhraseNext);
            //
            //
            CommandFindFocus = new RichDelegateCommand(
                @"DOCVIEW CommandFindFocus DUMMY TXT",
                @"DOCVIEW CommandFindFocus DUMMY TXT",
                null, // KeyGesture set only for the top-level CompositeCommand
                null,
                () =>
                {
                    m_ShellView.RaiseEscapeEvent();

                    IsSearchVisible = true;
                    FocusHelper.Focus(SearchBox);
                    SearchBox.SelectAll();
                },
                () => SearchBox.IsEnabled
                && SearchBox.Visibility == Visibility.Visible,
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_PageFindNext)
                );
            //
            CommandFindNext = new RichDelegateCommand(
                @"DOCVIEW CommandFindNext DUMMY TXT",
                @"DOCVIEW CommandFindNext DUMMY TXT",
                null, // KeyGesture set only for the top-level CompositeCommand
                null,
                () =>
                {
                    m_ShellView.RaiseEscapeEvent();

                    FindNext();
                },
                () => !string.IsNullOrEmpty(SearchTerm),
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_PageFindNext)
                );
            CommandFindPrev = new RichDelegateCommand(
                @"DOCVIEW CommandFindPrevious DUMMY TXT",
                @"DOCVIEW CommandFindPrevious DUMMY TXT",
                null, // KeyGesture set only for the top-level CompositeCommand
                null,
                () =>
                {
                    m_ShellView.RaiseEscapeEvent();

                    FindPrevious();
                },
                () => !string.IsNullOrEmpty(SearchTerm),
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_PageFindNext)
                );
            //
            InitializeComponent();

#if NET40
            //FlowDocReader.SelectionBrush = new SolidColorBrush(Settings.Default.Document_Color_Selection_Back1);
            //FlowDocReader.SelectionOpacity = 0.7;
#endif

            //FlowDocReader
            TheFlowDocument.MouseLeave += (sender, e) => restoreMouseOverHighlight();

            ////// tesing with Got/LostCapture only 
            FlowDocReader.AddHandler(ContentElement.MouseDownEvent, new RoutedEventHandler(OnFlowDocViewerMouseDown), true);
            FlowDocReader.AddHandler(ContentElement.MouseUpEvent, new RoutedEventHandler(OnFlowDocViewerMouseUp), true);

            FlowDocReader.AddHandler(ContentElement.GotMouseCaptureEvent, new RoutedEventHandler(OnFlowDocGotMouseCapture), true);
            FlowDocReader.AddHandler(ContentElement.LostMouseCaptureEvent, new RoutedEventHandler(OnFlowDocLostMouseCapture), true);

            //FlowDocReaderSimple.InputBindings.Clear();
            //TheFlowDocumentSimple.InputBindings.Clear();

            //FlowDocReader.InputBindings.Clear();
            //TheFlowDocument.InputBindings.Clear();

            //FlowDocReader.AddHandler(ContentElement.KeyDownEvent, new RoutedEventHandler(OnFlowDocViewerKeyDown), true);
            //FlowDocReader.PreviewKeyDown += new KeyEventHandler(OnFlowDocViewerPreviewKeyDown);

            //DocumentViewer dv1 = LogicalTreeHelper.FindLogicalNode(window, "dv1") as DocumentViewer;
            //var cc = FlowDocReader.Template.FindName("PART_FindToolBarHost", FlowDocReader) as ContentControl;
            //if (cc != null)
            //{
            //    cc.Visibility = Visibility.Collapsed;
            //}

            //var fontConverter = new FontFamilyConverter();
            //var fontFamily = (FontFamily)fontConverter.ConvertFrom("Times New Roman");

            //UserInterfaceStrings.No_Document);
            //setTextDecoration_ErrorUnderline(run);//comboListOfFonts.SelectedItem = fontFamily;

            m_MouseDownTextElement = null;
            TheFlowDocument.Blocks.Clear();
            TheFlowDocument.Blocks.Add(createWelcomeEmptyFlowDoc());

            TheFlowDocumentSimple.Blocks.Clear();
            TheFlowDocumentSimple.Blocks.Add(createWelcomeEmptyFlowDoc());

            //m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Subscribe(OnTreeNodeSelected, TreeNodeSelectedEvent.THREAD_OPTION);
            //m_EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Subscribe(OnSubTreeNodeSelected, SubTreeNodeSelectedEvent.THREAD_OPTION);
            m_EventAggregator.GetEvent<TreeNodeSelectionChangedEvent>().Subscribe(OnTreeNodeSelectionChanged, TreeNodeSelectionChangedEvent.THREAD_OPTION);


            m_EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(OnProjectLoaded, ProjectLoadedEvent.THREAD_OPTION);
            m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(OnProjectUnLoaded, ProjectUnLoadedEvent.THREAD_OPTION);

            m_EventAggregator.GetEvent<EscapeEvent>().Subscribe(OnEscape, EscapeEvent.THREAD_OPTION);


            ActiveAware = new FocusActiveAwareAdapter(this);
            ActiveAware.IsActiveChanged += (sender, e) => refreshCommandsIsActive();
            m_ShellView.ActiveAware.IsActiveChanged += (sender, e) => refreshCommandsIsActive();

            Settings.Default.PropertyChanged += OnSettingsPropertyChanged;
        }

        private Block createXukSpineEmptyFlowDoc(Project project)
        {
            try
            {
                Presentation pres = project.Presentations.Get(0);
                string titleMain = Daisy3_Import.GetTitle(pres);


                var block = new Paragraph();
                block.TextAlignment = TextAlignment.Center;

                var run1 = new Run(titleMain)
                {
                    FontWeight = FontWeights.Heavy
                };
                run1.FontSize *= 2;
                block.Inlines.Add(run1);

                block.Inlines.Add(new LineBreak());

                var run2 = new Run(@"(" + pres.RootNode.Children.Count + " spine items)");
                block.Inlines.Add(run2);

                //block.Inlines.Add(new LineBreak());
                //block.Inlines.Add(new LineBreak());

                foreach (var treeNode in pres.RootNode.Children.ContentsAs_Enumerable)
                {
                    TextMedia txtMedia = treeNode.GetTextMedia() as TextMedia;
                    if (txtMedia == null) continue;
                    string path = txtMedia.Text;

                    XmlProperty xmlProp = treeNode.GetXmlProperty();
                    if (xmlProp == null) continue;

                    string name = treeNode.GetXmlElementLocalName();
                    if (name != "metadata") continue;

                    string title = null;
                    string xukFileName = null;
                    bool hasXuk = false;
                    foreach (var xmlAttr in xmlProp.Attributes.ContentsAs_Enumerable)
                    {
                        if (xmlAttr.LocalName == "xuk" && xmlAttr.Value == "true")
                        {
                            hasXuk = true;
                        }

                        if (xmlAttr.LocalName == "title")
                        {
                            title = xmlAttr.Value;
                        }

                        if (xmlAttr.LocalName == "xukFileName")
                        {
                            xukFileName = xmlAttr.Value;
                        }
                    }

                    if (!hasXuk) continue;

                    //string title_ = Daisy3_Import.GetTitle(presentation);
                    //DebugFix.Assert(title_ == title);

                    string rootDir = Path.GetDirectoryName(m_UrakawaSession.DocumentFilePath);

                    string fullXukPath = null;
                    if (!string.IsNullOrEmpty(xukFileName))
                    {
                        fullXukPath = Path.Combine(rootDir, xukFileName);
                    }
                    else
                    {
                        //old project format
                        fullXukPath = Daisy3_Import.GetXukFilePath_SpineItem(rootDir, path, title, -1);
                    }

                    if (!File.Exists(fullXukPath))
                    {
#if DEBUG
                        Debugger.Break();
#endif //DEBUG
                        continue;
                    }

                    var runA = new Run(!string.IsNullOrEmpty(title) ? title : "NO TITLE");
                    runA.FontWeight = FontWeights.Heavy;
                    runA.FontSize *= 1.5;

                    Uri uri = new Uri(fullXukPath, UriKind.Absolute);

                    var link = new Hyperlink(runA);
                    link.Focusable = false;
                    link.NavigateUri = uri;
                    link.ToolTip = link.NavigateUri.ToString();

                    link.RequestNavigate +=
                        (object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
                            =>
                        {
                            //m_EventAggregator.GetEvent<OpenFileRequestEvent>().Publish(link.NavigateUri.LocalPath);
                            m_UrakawaSession.TryOpenFile(link.NavigateUri.LocalPath);
                        };

                    var runB = new Run("(" + Path.GetFileName(fullXukPath).Replace(".xuk", "") + ")");
                    //runB.FontSize *= 0.85;

                    block.Inlines.Add(new LineBreak());
                    block.Inlines.Add(new LineBreak());
                    block.Inlines.Add(link);
                    block.Inlines.Add(new LineBreak());
                    block.Inlines.Add(runB);
                }

                return block;
            }
            catch
            {
                return new Paragraph(new Run(" "));
            }
        }

        private Block createWelcomeEmptyFlowDoc()
        {
            string dirPath = Path.GetDirectoryName(ApplicationConstants.LOG_FILE_PATH);

            string imgPath = Path.Combine(dirPath, "daisy.svg");
            ImageSource imageSource = AutoGreyableImage.GetSVGOrBitmapImageSource(imgPath);
            if (imageSource == null)
            {
#if DEBUG
                Debugger.Break();
#endif //DEBUG
                imgPath = Path.Combine(dirPath, "daisy_01.png");
                imageSource = AutoGreyableImage.GetSVGOrBitmapImageSource(imgPath);
                if (imageSource == null)
                {
#if DEBUG
                    Debugger.Break();
#endif //DEBUG
                }
            }

            string dirPathEPUB = Path.GetDirectoryName(ApplicationConstants.LOG_FILE_PATH);

            string imgPathEPUB = Path.Combine(dirPath, "EPUB.svg");
            ImageSource imageSourceEPUB = AutoGreyableImage.GetSVGOrBitmapImageSource(imgPathEPUB);
            if (imageSourceEPUB == null)
            {
#if DEBUG
                Debugger.Break();
#endif //DEBUG
            }

            try
            {
                Image image = null;
                if (imageSource != null)
                {
                    image = new Image
                                {
                                    Source = imageSource,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Top,
                                    Stretch = Stretch.Uniform,
                                    StretchDirection = StretchDirection.DownOnly
                                };
                    image.MaxWidth = 150;
                }

                Image imageEPUB = null;
                if (imageSourceEPUB != null)
                {
                    imageEPUB = new Image
                    {
                        Source = imageSourceEPUB,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Top,
                        Stretch = Stretch.Uniform,
                        StretchDirection = StretchDirection.DownOnly
                    };
                    imageEPUB.MaxWidth = 100;
                }
                //var block = new BlockUIContainer(image);

                var block = new Paragraph();
                block.TextAlignment = TextAlignment.Center;

                var run1 = new Run(@"Tobi v" + ApplicationConstants.APP_VERSION)
                               {
                                   FontWeight = FontWeights.Heavy
                               };
                run1.FontSize *= 2;
                block.Inlines.Add(run1);
                block.Inlines.Add(new LineBreak());

                var run2 = new Run(@"Authoring Tool for Talking Books");
                block.Inlines.Add(run2);

                block.Inlines.Add(new LineBreak());
                block.Inlines.Add(new LineBreak());

                if (image != null)
                {
                    var inline = new InlineUIContainer(image)
                    {
                        BaselineAlignment = BaselineAlignment.Top
                    };

                    block.Inlines.Add(inline);
                }

                block.Inlines.Add(new Run("    "));

                if (imageEPUB != null)
                {
                    var inlineEPUB = new InlineUIContainer(imageEPUB)
                    {
                        BaselineAlignment = BaselineAlignment.Top
                    };

                    block.Inlines.Add(inlineEPUB);
                }

                return block;
            }
            catch
            {
                return new Paragraph(new Run(" "));
            }
        }

        private void OnSearchLostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchBox.Text))
            {
                IsSearchVisible = false;
            }
        }
        private bool m_IsSearchVisible;
        public bool IsSearchVisible
        {
            get
            {
                return m_IsSearchVisible;
            }
            set
            {
                if (value == m_IsSearchVisible) return;
                m_IsSearchVisible = value;
                m_PropertyChangeHandler.RaisePropertyChanged(() => IsSearchVisible);
            }
        }
        public IActiveAware ActiveAware { get; private set; }

        private void refreshCommandsIsActive()
        {
            lock (m_DocumentNarratorWindowLOCK)
            {
                CommandFindFocus.IsActive = m_DocumentNarratorWindow != null
                                                ? m_DocumentNarratorWindow.ActiveAware.IsActive
                                                : m_ShellView.ActiveAware.IsActive && ActiveAware.IsActive;
                CommandFindNext.IsActive = m_DocumentNarratorWindow != null
                                               ? m_DocumentNarratorWindow.ActiveAware.IsActive
                                               : m_ShellView.ActiveAware.IsActive && ActiveAware.IsActive;
                CommandFindPrev.IsActive = m_DocumentNarratorWindow != null
                                               ? m_DocumentNarratorWindow.ActiveAware.IsActive
                                               : m_ShellView.ActiveAware.IsActive && ActiveAware.IsActive;
            }
        }

        public RichDelegateCommand CommandFindFocus { get; private set; }
        public RichDelegateCommand CommandFindNext { get; private set; }
        public RichDelegateCommand CommandFindPrev { get; private set; }

        private TextElement getFirstAncestorWithTreeNodeTag(object obj)
        {
            var textElement = obj as TextElement;
            if (textElement == null)
            {
                var uiElement = obj as UIElement;
                if (uiElement != null)
                {
                    DependencyObject parent = uiElement;
                    do
                    {
                        if (parent is BlockUIContainer)
                        {
                            textElement = (BlockUIContainer)parent;
                            break;
                        }
                        else if (parent is InlineUIContainer)
                        {
                            textElement = (InlineUIContainer)parent;
                            break;
                        }
                        //parent = VisualTreeHelper.GetParent(parent);
                        parent = LogicalTreeHelper.GetParent(parent);
                    } while (parent != null);
                }
            }

            if (textElement != null)
            {
                do
                {
                    if (textElement.Tag != null && textElement.Tag is TreeNode)
                    {
                        return textElement;
                    }
                    textElement = textElement.Parent as TextElement;
                } while (textElement != null);
            }

            return null;
        }

        public void OnFlowDocViewerMouseDown(object sender, RoutedEventArgs e)
        {
            if (!(e is MouseButtonEventArgs))
            {
#if DEBUG
                Debugger.Break();
#endif
                return;
            }

            var ev = e as MouseButtonEventArgs;
            if (ev.RightButton == MouseButtonState.Pressed
                //Mouse.RightButton
                )
            {
                OnFlowDocGotMouseCapture(sender, e);
            }
        }

        public void OnFlowDocViewerMouseUp(object sender, RoutedEventArgs e)
        {
            if (!(e is MouseButtonEventArgs))
            {
#if DEBUG
                Debugger.Break();
#endif
                return;
            }

            var ev = e as MouseButtonEventArgs;
            if (ev.ChangedButton == MouseButton.Right
                //Mouse.RightButton
                )
            {
                OnFlowDocLostMouseCapture(sender, null);
            }
        }

        private DependencyObject m_MouseDownTextElement;// TextElement Image, Panel (UIElement)
        private void OnFlowDocGotMouseCapture(object sender, RoutedEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed
                || Mouse.RightButton == MouseButtonState.Pressed)
            {
                var textElement = getFirstAncestorWithTreeNodeTag(Mouse.DirectlyOver);
                if (textElement != null)
                {
                    m_MouseDownTextElement = textElement;
                    m_TextElementForEdit = null;
                }
            }
        }

        private TextElement m_TextElementForEdit;
        private void OnFlowDocLostMouseCapture(object sender, RoutedEventArgs e)
        {
            var mouseDownTextElement = m_MouseDownTextElement;
            m_MouseDownTextElement = null;

            var action = (Action)(() =>
                {
                    if (mouseDownTextElement == null) return;

                    IInputElement el = Mouse.DirectlyOver;
                    if (el == null || el is ContextMenu)
                    {
                        el = Mouse.Captured;
                    }

                    var textElement = getFirstAncestorWithTreeNodeTag(el);

                    if (textElement == null) return;

                    DebugFix.Assert(textElement.Tag != null);

                    if (textElement != mouseDownTextElement)
                    {
                        m_TextElementForEdit = null;
                    }
                    else
                    {
                        //var before = (m_lastHighlightedSub ?? m_lastHighlighted);
                        m_TextElementForEdit = textElement;
                        if (isAltKeyDown())
                        {
                            if (isControlKeyDown())
                            {
                                //CommandEditDescription.Execute();
                            }
                            else
                            {
                                CommandEditText.Execute();
                            }
                        }
                        else
                        {
                            CommandManager.InvalidateRequerySuggested();
                        }
                        //var after = (m_lastHighlightedSub ?? m_lastHighlighted);
                        //if (before != after) return; // selection already performed

                        if (textElement != (m_lastHighlightedSub ?? m_lastHighlighted))
                        {
                            if (!m_UrakawaSession.isAudioRecording)
                            {
                                m_UrakawaSession.PerformTreeNodeSelection((TreeNode)textElement.Tag);
                            }
                        }

                        if (m_lastHighlighted != null)
                        {
                            textElement = m_lastHighlightedSub ?? m_lastHighlighted;

                            if (isAltKeyDown())
                            {
                                // See above (we edit before treenode selection, otherwise we may miss innaccessible Runs because of audio higher up in the hierarchy => selection redirection by Urakawa Session)
                                //CommandEditText.Execute();
                            }
                            else if (isControlKeyDown())
                            {
                                CommandFollowLink.Execute();

                                //                                // Fallback:
                                //                                Dispatcher.BeginInvoke(DispatcherPriority.Background,
                                //(Action)(() =>
                                //{
                                //    if (
                                //        FlowDocReader.
                                //            Selection !=
                                //        null)
                                //        FlowDocReader.
                                //            Selection.Select
                                //            (textElement.
                                //                    ContentStart,
                                //                textElement.
                                //                    ContentEnd);
                                //})
                                //                                    );
                            }
                            else
                            {
                                //if (Mouse.RightButton == MouseButtonState.Pressed
                                //    && TheFlowDocument.ContextMenu != null)
                                //{
                                //    TheFlowDocument.ContextMenu.PlacementTarget = FlowDocReader;
                                //    TheFlowDocument.ContextMenu.Placement = PlacementMode.Bottom;
                                //    var p = Mouse.GetPosition(FlowDocReader);
                                //    TheFlowDocument.ContextMenu.PlacementRectangle = new Rect(p.X, p.Y, 2, 2);
                                //    TheFlowDocument.ContextMenu.IsOpen = true;
                                //}
                            }
                        }
                    }
                });

            //action.Invoke();

            if (e == null)
            {
                Dispatcher.Invoke(DispatcherPriority.Input, action);
            }
            else
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Input, action);
            }
        }


        private void refreshTextOnlyViewColors()
        {
            if (m_TextOnlyViewRun != null)
            {
                m_TextOnlyViewRun.Foreground = ColorBrushCache.Get(Settings.Default.Document_Color_Font_TextOnly);
            }
        }

        private void OnSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!e.PropertyName.StartsWith(@"Document_Color_")
                && !e.PropertyName.StartsWith(@"Document_Highlight")
                && !e.PropertyName.StartsWith(@"Document_Back")
                ) return;

            refreshTextOnlyViewColors();

            if (e.PropertyName.StartsWith(@"Document_Color_Selection_"))
            {
                refreshHighlightedColors();
            }
            else
            {
                refreshDocumentColors(null);
                refreshHighlightedColors();
            }

            //if (e.PropertyName == PropertyChangedNotifyBase.GetMemberName(() => Settings.Default.Document_Color_Font_Audio))
            //{
            //}
        }

        private TreeNode ensureTreeNodeIsNoteAnnotation(TreeNode treeNode)
        {
            if (treeNode == null || !treeNode.HasXmlProperty) return null;

            string localName = treeNode.GetXmlElementLocalName();
            if (localName == "annotation"
                || localName == "note")
            {
                return treeNode;
            }

            XmlProperty xmlProp = treeNode.GetXmlProperty();

            urakawa.property.xml.XmlAttribute attrEpubType = xmlProp.GetAttribute("epub:type", DiagramContentModelHelper.NS_URL_EPUB);
            bool isNote = attrEpubType != null &&
                        (
                        "note".Equals(attrEpubType.Value, StringComparison.OrdinalIgnoreCase)
                        || "rearnote".Equals(attrEpubType.Value, StringComparison.OrdinalIgnoreCase)
                        || "footnote".Equals(attrEpubType.Value, StringComparison.OrdinalIgnoreCase)
                        );
            if (isNote)
            {
                return treeNode;
            }

            string uid = treeNode.GetXmlElementId();
            if (!string.IsNullOrEmpty(uid) && HasLinkSource(uid))
            {
                return treeNode;
            }

            return ensureTreeNodeIsNoteAnnotation(treeNode.Parent);
        }

        private void OnEscape(object obj)
        {
            if (!Dispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action<object>)OnEscape, obj);
                return;
            }

        }


        private List<TreeNode> m_PathToTreeNode;
        private List<TreeNode> getPathToTreeNode(TreeNode treeNodeSel)
        {
            if (m_PathToTreeNode == null || !m_PathToTreeNode.Contains(treeNodeSel))
            {
                m_PathToTreeNode = new List<TreeNode>();
                TreeNode treeNode = treeNodeSel;
                do
                {
                    m_PathToTreeNode.Add(treeNode);
                } while ((treeNode = treeNode.Parent) != null);

                m_PathToTreeNode.Reverse();
            }
            return m_PathToTreeNode;
        }

        // NEEDED for Settings.Default.Document_EnableInstantSearch
        private void annotationsOff()
        {
            AnnotationService service = AnnotationService.GetService(FlowDocReader);

            if (service != null && service.IsEnabled)
            {
                foreach (var annotation in service.Store.GetAnnotations())
                {
                    service.Store.DeleteAnnotation(annotation.Id);
                }

                service.Store.Flush();
                service.Disable();
                //AnnotationStream.Close();
                service.Store.Dispose();
            }
        }

        // NEEDED for Settings.Default.Document_EnableInstantSearch
        private void annotationsOn()
        {
            AnnotationService service = AnnotationService.GetService(FlowDocReader);
            if (service != null && service.IsEnabled)
            {
                service.Disable();
            }
            if (service == null)
            {
                service = new AnnotationService(FlowDocReader);
            }
            if (!service.IsEnabled)
            {
                string dir = Path.GetDirectoryName(ApplicationConstants.LOG_FILE_PATH);
                var filename = Path.Combine(dir, @"annotations.xml");

                try
                {
                    Stream annoStream = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    AnnotationStore store = new XmlStreamStore(annoStream);

                    service.Enable(store);
                }
                catch
                {
                    ;//ignore.
                }
            }
        }


        //private FlowDocument m_FlowDoc;


        private TextElement m_lastHighlighted;
        //private Brush m_lastHighlighted_Background;
        //private Brush m_lastHighlighted_Foreground;
        //private Brush m_lastHighlighted_BorderBrush;
        //private Thickness m_lastHighlighted_BorderThickness;

        private TextElement m_lastHighlightedSub;
        //private Brush m_lastHighlightedSub_Background;
        //private Brush m_lastHighlightedSub_Foreground;
        //private Brush m_lastHighlightedSub_BorderBrush;
        //private Thickness m_lastHighlightedSub_BorderThickness;


        private Dictionary<string, TextElement> m_idLinkTargets;
        private Dictionary<string, List<TextElement>> m_idLinkSources;

        //private bool m_FlowDocSelectionHooked;

        private UndoRedoManager.Hooker m_UndoRedoManagerHooker = null;

        private bool m_ProjectLoadedFlag = true;

        private void OnProjectLoaded(Project project)
        {
            if (!Dispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action<Project>)OnProjectLoaded, project);
                return;
            }

            Settings.Default.InvalidStructEdit_DoNotAskAgain = false;

            m_valid = null;
            //checkValid();

            m_ProjectLoadedFlag = true;

            m_FindAndReplaceManager = null;

            m_PathToTreeNode = null;

            if (m_idLinkTargets != null)
            {
                m_idLinkTargets.Clear();
            }
            m_idLinkTargets = new Dictionary<string, TextElement>();

            if (m_idLinkSources != null)
            {
                m_idLinkSources.Clear();
            }
            m_idLinkSources = new Dictionary<string, List<TextElement>>();

            m_lastHighlighted = null;
            m_lastHighlightedSub = null;
            m_SearchCurrentIndex = -1;

            m_MouseDownTextElement = null;

            annotationsOff();

            TheFlowDocument.Blocks.Clear();
            TheFlowDocumentSimple.Blocks.Clear();

            if (FlowDocReader.Selection != null)
            {
                FlowDocReader.Selection.Select(TheFlowDocument.ContentStart, TheFlowDocument.ContentEnd);
            }

            m_PropertyChangeHandler.RaisePropertyChanged(() => IsSearchEnabled);

            if (project == null)
            {
                m_TreeNodeFragmentClipboard = null;

                SearchBox.Text = "";
                SearchTerm = null;
                CommandFocus.Execute();

#if false && DEBUG
                FlowDocReader.Document = new FlowDocument(new Paragraph(new Run("Testing FlowDocument (DEBUG) （１）このテキストDAISY図書は，レベル５まであります。")));
#else
                TheFlowDocument.Blocks.Add(createWelcomeEmptyFlowDoc());
                TheFlowDocumentSimple.Blocks.Add(createWelcomeEmptyFlowDoc());
#endif //DEBUG

                //GC.Collect();
                //GC.WaitForFullGCComplete();
                return;
            }
            else
            {
                if (m_UrakawaSession.IsXukSpine)
                {
                    TheFlowDocument.Blocks.Add(createXukSpineEmptyFlowDoc(project));
                    TheFlowDocumentSimple.Blocks.Add(createXukSpineEmptyFlowDoc(project));
                    return;
                }

                m_UndoRedoManagerHooker = project.Presentations.Get(0).UndoRedoManager.Hook(this);
            }

            createFlowDocumentFromXuk(project);

            if (FlowDocReader.Selection != null)
            {
                FlowDocReader.Selection.Select(TheFlowDocument.ContentStart, TheFlowDocument.ContentStart);
            }

            annotationsOn();

            //if (FlowDocReader.Selection != null && !m_FlowDocSelectionHooked)
            //{
            //    m_FlowDocSelectionHooked = true;
            //    FlowDocReader.Selection.Changed += new EventHandler(OnFlowDocSelectionChanged);
            //}

            /*
            string dirPath = Path.GetDirectoryName(FilePath);
            string fullPath = Path.Combine(dirPath, "FlowDocument.xaml");

            using (FileStream stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                try
                {
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Encoding = Encoding.UTF8;
                    settings.NewLineHandling = NewLineHandling.Replace;
                    settings.NewLineChars = Environment.NewLine;
                    settings.Indent = true;
                    settings.IndentChars = "\t";
                    settings.NewLineOnAttributes = true;

                    XmlWriter xmlWriter = XmlWriter.Create(stream, settings);

                    XamlWriter.Save(m_FlowDoc, xmlWriter);
                }
                finally
                {
                    stream.Close();
                }
            }*/

        }

        private void OnProjectUnLoaded(Project project)
        {
            if (!Dispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action<Project>)OnProjectUnLoaded, project);
                return;
            }


            if (m_UndoRedoManagerHooker != null) m_UndoRedoManagerHooker.UnHook();
            m_UndoRedoManagerHooker = null;

            OnProjectLoaded(null);
        }

        //private void resetFlowDocument()
        //{
        //    //FlowDocReader.Document = new FlowDocument(new Paragraph(new Run(UserInterfaceStrings.No_Document)))
        //    //{
        //    //    IsEnabled = false,
        //    //    IsHyphenationEnabled = false,
        //    //    IsOptimalParagraphEnabled = false,
        //    //    ColumnWidth = Double.PositiveInfinity,
        //    //    IsColumnWidthFlexible = false,
        //    //    TextAlignment = TextAlignment.Center
        //    //};
        //    //FlowDocReader.Document.Blocks.Add(new Paragraph(new Run("Use 'new' or 'open' from the menu bar.")));

        //    TheFlowDocument.Blocks.Clear();

        //    GC.Collect();
        //}

        //private TreeNode m_CurrentTreeNode;
        //public TreeNode CurrentTreeNode
        //{
        //    get
        //    {
        //        return m_CurrentTreeNode;
        //    }
        //    set
        //    {
        //        if (m_CurrentTreeNode == value) return;

        //        m_CurrentTreeNode = value;
        //        //RaisePropertyChanged(() => CurrentTreeNode);
        //    }
        //}

        //private TreeNode m_CurrentSubTreeNode;
        //public TreeNode CurrentSubTreeNode
        //{
        //    get
        //    {
        //        return m_CurrentSubTreeNode;
        //    }
        //    set
        //    {
        //        if (m_CurrentSubTreeNode == value) return;
        //        m_CurrentSubTreeNode = value;

        //        //RaisePropertyChanged(() => CurrentSubTreeNode);
        //    }
        //}


        private void OnTreeNodeSelectionChanged(Tuple<Tuple<TreeNode, TreeNode>, Tuple<TreeNode, TreeNode>> oldAndNewTreeNodeSelection)
        {
            Tuple<TreeNode, TreeNode> oldTreeNodeSelection = oldAndNewTreeNodeSelection.Item1;
            Tuple<TreeNode, TreeNode> newTreeNodeSelection = oldAndNewTreeNodeSelection.Item2;

            TreeNode selectedTreeNode = newTreeNodeSelection.Item2 ?? newTreeNodeSelection.Item1;
            updateSimpleTextView(selectedTreeNode);

            int max = (int)Math.Floor(Settings.Default.Document_MathML_LoadSelection);
            XukToFlowDocument.checkLoadMathMLIntoImage_(m_ShellView, m_UrakawaSession, this, selectedTreeNode, ref max);

            TextElement textElement1 = null;
            if (m_lastHighlighted != null && m_lastHighlighted.Tag == newTreeNodeSelection.Item1)
            {
                textElement1 = m_lastHighlighted;
            }
            if (m_lastHighlightedSub != null && m_lastHighlightedSub.Tag == newTreeNodeSelection.Item1)
            {
                textElement1 = m_lastHighlightedSub;
            }
            if (textElement1 == null)
            {
                textElement1 = FindTextElement(newTreeNodeSelection.Item1);
            }
            if (textElement1 == null)
            {
#if DEBUG
                //Debugger.Break();
#endif //DEBUG
                Console.WriteLine(@"TextElement not rendered for TreeNode: " + newTreeNodeSelection.Item1.ToString());
                return;
            }

            TextElement textElement2 = null;
            if (newTreeNodeSelection.Item2 != null)
            {
                if (m_lastHighlighted != null && m_lastHighlighted.Tag == newTreeNodeSelection.Item2)
                {
                    textElement2 = m_lastHighlighted;
                }
                if (m_lastHighlightedSub != null && m_lastHighlightedSub.Tag == newTreeNodeSelection.Item2)
                {
                    textElement2 = m_lastHighlightedSub;
                }
                if (textElement2 == null)
                {
                    textElement2 = FindTextElement(newTreeNodeSelection.Item2);
                }
                if (textElement2 == null)
                {
#if DEBUG
                    Debugger.Break();
#endif //DEBUG
                    Console.WriteLine(@"TextElement not rendered for TreeNode: " + newTreeNodeSelection.Item2.ToString());
                    return;
                }
            }

            clearLastHighlighteds();

            m_TextElementForEdit = null;

            if (textElement2 == null)
            {
                //m_TextElementForEdit = textElement1;
                //DebugFix.Assert(m_TextElementForEdit.Tag != null);

                doLastHighlightedOnly(textElement1, false);

                scrollToView(textElement1);
            }
            else
            {
                //m_TextElementForEdit = textElement2;
                //DebugFix.Assert(m_TextElementForEdit.Tag != null);

                doLastHighlightedAndSub(textElement1, textElement2, false);

                scrollToView(textElement2);
            }
        }

        private Run m_TextOnlyViewRun;
        private void updateSimpleTextView(TreeNode treeNode)
        {
            if (m_TextOnlyViewRun == null || TheFlowDocumentSimple.Blocks.Count == 0)
            {
                TheFlowDocumentSimple.Blocks.Clear();

                m_TextOnlyViewRun = new Run("");
                var block = new Paragraph(m_TextOnlyViewRun);

                m_TextOnlyViewRun.FontSize *= 2;
                m_TextOnlyViewRun.FontStyle = FontStyles.Normal;
                m_TextOnlyViewRun.FontWeight = FontWeights.Heavy;
                m_TextOnlyViewRun.FontStretch = FontStretches.Normal;

                refreshTextOnlyViewColors();

                //block.TextAlignment = TextAlignment.Center;

                TheFlowDocumentSimple.Blocks.Add(block);
            }

            string str = treeNode.GetTextFlattened();
            if (string.IsNullOrEmpty(str))
            {
                m_TextOnlyViewRun.Text = "";
                return;
            }

            m_TextOnlyViewRun.Text = str;
        }

        //private Stopwatch m_ScrollToViewStopwatch = null;
        private DispatcherTimer m_scrollRefreshIntervalTimer = null;
        private TextElement m_scrollTextElement = null;
        private void scrollToView(TextElement textElement)
        {
            m_scrollTextElement = textElement;

            if (FlowDocReader.ScrollViewer == null)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(textElement.BringIntoView));
            }
            else
            {
                //if (m_ScrollToViewStopwatch == null)
                //{
                //    m_ScrollToViewStopwatch = Stopwatch.StartNew(); // new Stopwatch();
                //}
                //DebugFix.Assert(m_ScrollToViewStopwatch.IsRunning);

                //if (m_ScrollToViewStopwatch.ElapsedMilliseconds > 1000)
                //{
                //    m_ScrollToViewStopwatch.Stop();

                //}

                //m_ScrollToViewStopwatch.Reset();
                //m_ScrollToViewStopwatch.Start();

                //Dispatcher.Invoke(DispatcherPriority.Input, (Action)(m_scrollTextElement.BringIntoView));

                if (m_scrollRefreshIntervalTimer == null)
                {
                    m_scrollRefreshIntervalTimer = new DispatcherTimer(DispatcherPriority.Background);
                    m_scrollRefreshIntervalTimer.Interval = TimeSpan.FromMilliseconds(350);
                    m_scrollRefreshIntervalTimer.Tick += (oo, ee) =>
                    {
                        m_scrollRefreshIntervalTimer.Stop();
                        //m_scrollRefreshIntervalTimer = null;

                        //textElement.BringIntoView();
                        //Dispatcher.Invoke(DispatcherPriority.Render, (Action)(textElement.BringIntoView));

                        if (m_scrollTextElement != null)
                        {
                            scrollToView_(m_scrollTextElement);
                        }
                        //Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action<TextElement>)(scrollToView_), textElement);
                    };
                    m_scrollRefreshIntervalTimer.Start();
                }
                else if (m_scrollRefreshIntervalTimer.IsEnabled)
                {
                    //restart
                    m_scrollRefreshIntervalTimer.Stop();
                    m_scrollRefreshIntervalTimer.Start();
                }
                else
                {
                    m_scrollRefreshIntervalTimer.Start();
                }
            }
        }

        private void scrollToView_(TextElement textElement)
        {
            //m_Logger.Log("@@@@@@@@@ SCROLL", Category.Debug, Priority.Medium);

            try
            {
                textElement.BringIntoView();

                //DebugFix.Assert(FlowDocReader.ScrollViewer.ScrollableHeight == FlowDocReader.ScrollViewer.ExtentHeight - FlowDocReader.ScrollViewer.ViewportHeight);
                if (FlowDocReader.ScrollViewer.ScrollableHeight !=
                    FlowDocReader.ScrollViewer.ExtentHeight - FlowDocReader.ScrollViewer.ViewportHeight)
                {
                    double diff = FlowDocReader.ScrollViewer.ExtentHeight - FlowDocReader.ScrollViewer.ViewportHeight -
                                  FlowDocReader.ScrollViewer.ScrollableHeight;
                    Console.WriteLine("FlowDocument Scroll area diff: " + diff);
                }

                TextPointer textPointerStart = textElement.ContentStart;
                TextPointer textPointerEnd = textElement.ContentEnd;

                double left = Double.MaxValue, top = Double.MaxValue;
                double right = Double.MinValue, bottom = Double.MinValue;

                bool found = false;

                TextPointer textPointerCurrent = null;
                do
                {
                    if (textPointerCurrent == null)
                    {
                        textPointerCurrent = textPointerStart;
                    }
                    else
                    {
                        textPointerCurrent = textPointerCurrent.GetNextContextPosition(LogicalDirection.Forward);
                    }

                    Rect rect = textPointerCurrent.GetCharacterRect(LogicalDirection.Forward);
                    if (rect.Left == Double.MaxValue || rect.Top == Double.MaxValue
                        || rect.Right == Double.MinValue || rect.Bottom == Double.MinValue)
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }
                    if (rect.Left < left) left = rect.Left;
                    if (rect.Top < top) top = rect.Top;
                    if (rect.Right > right) right = rect.Right;
                    if (rect.Bottom > bottom) bottom = rect.Bottom;

                    //textPointerCurrent = textPointerCurrent.GetNextInsertionPosition(LogicalDirection.Forward);

                    //int result = textPointerCurrent.CompareTo(textPointerEnd);
                    //result = textPointerEnd.CompareTo(textPointerCurrent);
                    //if (result==0)
                    //{
                    //    bool b = textPointerEnd == textPointerCurrent;
                    //}

                    if (textPointerCurrent.CompareTo(textPointerEnd) == 0)
                    {
                        if (left == Double.MaxValue || top == Double.MaxValue
                            || right == Double.MinValue || bottom == Double.MinValue)
                        {
#if DEBUG
                            //Debugger.Break();
#endif
                        }
                        found = true;
                        break;
                    }

                } while (textPointerCurrent != null);

                double width = right - left;
                double height = bottom - top;
                if (width <= 0 || height <= 0)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(textElement.BringIntoView));
                    return;
                }
                var rectBoundingBox = new Rect(left, top, width, height);

                if (!found)
                {
#if DEBUG
                    Debugger.Break();
#endif
                    Rect rectStart = textPointerStart.GetCharacterRect(LogicalDirection.Forward);
                    Rect rectEnd = textPointerEnd.GetCharacterRect(LogicalDirection.Backward);

                    rectBoundingBox = new Rect(rectStart.Left, rectStart.Top, rectStart.Width, rectStart.Height);
                    rectBoundingBox.Union(rectEnd);

                    double textTotalHeight_ = rectEnd.Top + rectEnd.Height - rectStart.Top;
                    double textTotalHeight = rectBoundingBox.Height;
                    DebugFix.Assert(textTotalHeight_ == textTotalHeight);
                }

                //Rect rectDocStart = FlowDocReader.Document.ContentStart.GetCharacterRect(LogicalDirection.Forward);
                double boxTopRelativeToDoc = -20 + FlowDocReader.ScrollViewer.VerticalOffset + rectBoundingBox.Top;

                double offsetToTop = boxTopRelativeToDoc;
                double offsetToCenter = boxTopRelativeToDoc -
                                        (FlowDocReader.ScrollViewer.ViewportHeight - rectBoundingBox.Height) / 2;
                double offsetToBottom = offsetToTop + FlowDocReader.ScrollViewer.ViewportHeight - rectBoundingBox.Height;

                if (rectBoundingBox.Height > FlowDocReader.ScrollViewer.ViewportHeight)
                {
                    offsetToCenter = offsetToTop;
                    offsetToBottom = offsetToTop;
                }

                double offset = offsetToCenter; //TODO: choose based on app pref
                if (offset < 0)
                {
                    offset = 0;
                }
                else if (offset > FlowDocReader.ScrollViewer.ScrollableHeight)
                {
                    offset = FlowDocReader.ScrollViewer.ScrollableHeight;
                }

                Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    (Action)(() => FlowDocReader.ScrollViewer.ScrollToVerticalOffset(offset)));
            }
            catch (Exception ex)
            {
                // TextPointer API?
#if DEBUG
                Debugger.Break();
#endif
            }
        }

        //public static TextPointer AlignTextPointer(TextPointer start, int x)
        //{
        //    var ret = start;
        //    var i = 0;
        //    while (i < x && ret != null)
        //    {
        //        if (ret.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.Text
        //            || ret.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.None)
        //        {
        //            i++;
        //        }
        //        if (ret.GetPositionAtOffset(1, LogicalDirection.Forward) == null)
        //        {
        //            return ret;
        //        }
        //        ret = ret.GetPositionAtOffset(1, LogicalDirection.Forward);
        //    }
        //    return ret;
        //}

        ////// EXMAPLE of use:
        ////    var run = new Run("test");
        ////    SelectAndColorize(TheFlowDocument.ContentStart.GetOffsetToPosition(run.ContentStart), run.Text.Length, Colors.Blue);
        //public void SelectAndColorize(int offset, int length, Color color)
        //{
        //    var start = TheFlowDocument.ContentStart;
        //    var startPos = AlignTextPointer(start, offset);
        //    var endPos = AlignTextPointer(start, offset + length);

        //    var textRange = FlowDocReader.Selection;
        //    if (textRange == null) return;

        //    textRange.Select(startPos, endPos);
        //    textRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
        //    //textRange.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
        //}

        private void refreshDocumentColors(TextElement textElement)
        {
            Action<TextElement> del = data =>
            {
                if (m_MouseOverTextElement == data)
                {
                    restoreMouseOverHighlight();
                    m_MouseOverTextElement = null;
                }

                XukToFlowDocument.SetBorderAndBackColorBasedOnTreeNodeTag(this, data);

                if (data.Tag is TreeNode)
                {
                    XukToFlowDocument.SetForegroundColorAndCursorBasedOnTreeNodeTag(this, data, false);
                }

                //if (data == m_lastHighlightedSub)
                //{
                //    m_lastHighlightedSub_Background = data.Background;
                //    m_lastHighlightedSub_Foreground = data.Foreground;
                //    if (data is Block)
                //    {
                //        m_lastHighlightedSub_BorderBrush = ((Block)data).BorderBrush;
                //        m_lastHighlightedSub_BorderThickness = ((Block)data).BorderThickness;
                //    }
                //}
                //else if (data == m_lastHighlighted)
                //{
                //    m_lastHighlighted_Background = data.Background;
                //    m_lastHighlighted_Foreground = data.Foreground;
                //    if (data is Block)
                //    {
                //        m_lastHighlighted_BorderBrush = ((Block)data).BorderBrush;
                //        m_lastHighlighted_BorderThickness = ((Block)data).BorderThickness;
                //    }
                //}
            };

            if (textElement == null)
                WalkDocumentTree(del);
            else
                WalkDocumentTree(textElement, del);
        }

        private void refreshHighlightedColors()
        {
#if NET40
            //FlowDocReader.SelectionBrush = new SolidColorBrush(Settings.Default.Document_Color_Selection_Back1);
#endif
            if (m_lastHighlighted == null) return;

            if (m_lastHighlightedSub != null)
            {
                doLastHighlightedAndSub(m_lastHighlighted, m_lastHighlightedSub, true);
            }
            else
            {
                doLastHighlightedOnly(m_lastHighlighted, true);
            }
        }

        private void doLastHighlightedOnly(TextElement textElement, bool onlyUpdateColors)
        {
            Brush brushFont = ColorBrushCache.Get(Settings.Default.Document_Color_Selection_Font);
            Brush brushBorder = ColorBrushCache.Get(Settings.Default.Document_Color_Selection_Border);
            Brush brushBack2 = ColorBrushCache.Get(Settings.Default.Document_Color_Selection_Back2);

            if (!onlyUpdateColors)
            {
                m_lastHighlighted = textElement;
                m_SearchCurrentIndex = -1;
            }

            if (m_lastHighlighted is Block)
            {
                //if (!onlyUpdateColors)
                //{
                //    m_lastHighlighted_BorderBrush = ((Block)m_lastHighlighted).BorderBrush;
                //    m_lastHighlighted_BorderThickness = ((Block)m_lastHighlighted).BorderThickness;
                //}

                ((Block)m_lastHighlighted).BorderBrush = brushBorder;
                ((Block)m_lastHighlighted).BorderThickness = new Thickness(1);
            }

            //if (!onlyUpdateColors)
            //{
            //    m_lastHighlighted_Background = m_lastHighlighted.Background;
            //    m_lastHighlighted_Foreground = m_lastHighlighted.Foreground;
            //}

#if USE_WALKTREE_FOR_SELECT
            WalkDocumentTree(textElement,
                             data =>
                             {
                                 //if (!(data is Run) && !(data is InlineUIContainer) && !(data is BlockUIContainer)) return;

                                 if (m_MouseOverTextElement == data)
                                 {
                                     restoreMouseOverHighlight();
                                     m_MouseOverTextElement = null;
                                 }

                                 data.Background = brushBack2;
                                 if (data.Tag != null && data.Tag is TreeNode)
                                 {
                                     data.Foreground = brushFont;
                                 }
                             });
#else //USE_WALKTREE_FOR_SELECT
#if USE_FLOWDOCSELECTION_FOR_SELECT
            var textRange = null;
            if (FlowDocReader.Selection != null)
            {
                FlowDocReader.Selection.Select(textElement.ContentStart, textElement.ContentEnd);
                textRange = FlowDocReader.Selection;
            }
            else
            {
#if DEBUG
                Debugger.Break();
#endif //DEBUG
                return;
            }
#else //USE_FLOWDOCSELECTION_FOR_SELECT
            var textRange = new TextRange(textElement.ContentStart, textElement.ContentEnd);

#if DEBUG
            string txt1 = textRange.Text;
#endif //DEBUG

#endif //USE_FLOWDOCSELECTION_FOR_SELECT
            textRange.ApplyPropertyValue(TextElement.ForegroundProperty, brushFont);
            textRange.ApplyPropertyValue(TextElement.BackgroundProperty, brushBack2);
#if DEBUG
            var textRange2 = new TextRange(textElement.ContentStart, textElement.ContentEnd);
            string txt2 = textRange2.Text;

            if (txt1 != txt2)
            {
                Debugger.Break();
            }
            if (textElement is Span && ((Span)textElement).Inlines.Count == 0)
            {
                Debugger.Break();
            }
#endif //DEBUG
#endif //USE_WALKTREE_FOR_SELECT


            //m_lastHighlighted.Background = brushBack2;
            //m_lastHighlighted.Foreground = brushFont;

            setOrRemoveTextDecoration_SelectUnderline(m_lastHighlighted, false, false);
        }

        private void doLastHighlightedAndSub(TextElement textElement1, TextElement textElement2, bool onlyUpdateColors)
        {
            Brush brushFont = ColorBrushCache.Get(Settings.Default.Document_Color_Selection_Font);
            Brush brushBorder = ColorBrushCache.Get(Settings.Default.Document_Color_Selection_Border);
            Brush brushBack1 = ColorBrushCache.Get(Settings.Default.Document_Color_Selection_Back1);
            Brush brushBack2 = ColorBrushCache.Get(Settings.Default.Document_Color_Selection_Back2);

            if (!onlyUpdateColors)
            {
                m_lastHighlighted = textElement1;
                m_SearchCurrentIndex = -1;

                //m_lastHighlighted_Background = m_lastHighlighted.Background;
                //m_lastHighlighted_Foreground = m_lastHighlighted.Foreground;
            }

            if (Settings.Default.Document_EnableDrawTopSelection)
            {
                m_lastHighlighted.Background = brushBack1;
                //m_lastHighlighted.Foreground = brushFont;

                if (m_lastHighlighted is Block)
                {
                    if (!onlyUpdateColors)
                    {
                        //m_lastHighlighted_BorderBrush = ((Block)m_lastHighlighted).BorderBrush;
                        //m_lastHighlighted_BorderThickness = ((Block)m_lastHighlighted).BorderThickness;
                    }
                    ((Block)m_lastHighlighted).BorderBrush = brushBorder;
                    ((Block)m_lastHighlighted).BorderThickness = new Thickness(1);
                }
            }

            if (!onlyUpdateColors)
            {
                m_lastHighlightedSub = textElement2;
                m_SearchCurrentIndex = -1;

                //m_lastHighlightedSub_Background = m_lastHighlightedSub.Background;
                //m_lastHighlightedSub_Foreground = m_lastHighlightedSub.Foreground;
            }

#if USE_WALKTREE_FOR_SELECT
            WalkDocumentTree(textElement2,
                             data =>
                             {
                                 //if (!(data is Run) && !(data is InlineUIContainer) && !(data is BlockUIContainer)) return;

                                 if (m_MouseOverTextElement == data)
                                 {
                                     restoreMouseOverHighlight();
                                     m_MouseOverTextElement = null;
                                 }

                                 data.Background = brushBack2;
                                 if (data.Tag != null && data.Tag is TreeNode)
                                 {
                                     data.Foreground = brushFont;
                                 }
                             });
#else //USE_WALKTREE_FOR_SELECT
#if USE_FLOWDOCSELECTION_FOR_SELECT
            var textRange = null;
            if (FlowDocReader.Selection != null)
            {
                FlowDocReader.Selection.Select(textElement2.ContentStart, textElement2.ContentEnd);
                textRange = FlowDocReader.Selection;
            }
            else
            {
#if DEBUG
                Debugger.Break();
#endif //DEBUG
                return;
            }
#else //USE_FLOWDOCSELECTION_FOR_SELECT
            var textRange = new TextRange(textElement2.ContentStart, textElement2.ContentEnd);
#endif //USE_FLOWDOCSELECTION_FOR_SELECT

#if DEBUG
            string txt1 = textRange.Text;
#endif //DEBUG

            textRange.ApplyPropertyValue(TextElement.ForegroundProperty, brushFont);
            textRange.ApplyPropertyValue(TextElement.BackgroundProperty, brushBack2);
#if DEBUG
            var textRange2 = new TextRange(textElement2.ContentStart, textElement2.ContentEnd);
            string txt2 = textRange2.Text;

            if (txt1 != txt2)
            {
                Debugger.Break();
            }

            if (textElement2 is Span && ((Span)textElement2).Inlines.Count == 0)
            {
                Debugger.Break();
            }
#endif //DEBUG
#endif //USE_WALKTREE_FOR_SELECT

            //m_lastHighlightedSub.Background = brushBack2;
            //m_lastHighlightedSub.Foreground = brushFont;

            if (m_lastHighlightedSub is Block)
            {
                //if (!onlyUpdateColors)
                //{
                //    m_lastHighlightedSub_BorderBrush = ((Block)m_lastHighlightedSub).BorderBrush;
                //    m_lastHighlightedSub_BorderThickness = ((Block)m_lastHighlightedSub).BorderThickness;
                //}

                ((Block)m_lastHighlightedSub).BorderBrush = brushBorder;
                ((Block)m_lastHighlightedSub).BorderThickness = new Thickness(1);
            }

            setOrRemoveTextDecoration_SelectUnderline(m_lastHighlightedSub, false, false);
        }

        private void clearLastHighlighteds()
        {
            if (m_lastHighlighted == null)
            {
                return;
            }

            bool enable = m_lastHighlightedSub == null || Settings.Default.Document_EnableDrawTopSelection;

            if (enable)
            {
                setOrRemoveTextDecoration_SelectUnderline(m_lastHighlighted, true, false);
            }

#if !USE_WALKTREE_FOR_SELECT
#if USE_FLOWDOCSELECTION_FOR_SELECT
            var textRange = null;
            if (FlowDocReader.Selection != null)
            {
                FlowDocReader.Selection.Select(m_lastHighlighted.ContentStart, m_lastHighlighted.ContentEnd);
                textRange = FlowDocReader.Selection;
            }
            else
            {
#if DEBUG
                Debugger.Break();
#endif //DEBUG
                return;
            }
#else //USE_FLOWDOCSELECTION_FOR_SELECT
            var textRange = new TextRange(m_lastHighlighted.ContentStart, m_lastHighlighted.ContentEnd);
#endif //USE_FLOWDOCSELECTION_FOR_SELECT

#if DEBUG
            string txt1 = textRange.Text;
#endif //DEBUG

            textRange.ApplyPropertyValue(TextElement.ForegroundProperty, ColorBrushCache.Get(Settings.Default.Document_Color_Font_NoAudio));
            textRange.ApplyPropertyValue(TextElement.BackgroundProperty, ColorBrushCache.Get(Settings.Default.Document_Back));
#if DEBUG
            var textRange2 = new TextRange(m_lastHighlighted.ContentStart, m_lastHighlighted.ContentEnd);
            string txt2 = textRange2.Text;

            if (txt1 != txt2)
            {
                Debugger.Break();
            }
            if (m_lastHighlighted is Span && ((Span)m_lastHighlighted).Inlines.Count == 0)
            {
                Debugger.Break();
            }
#endif //DEBUG
#endif //!USE_WALKTREE_FOR_SELECT

            if (m_lastHighlightedSub != null)
            {
                setOrRemoveTextDecoration_SelectUnderline(m_lastHighlightedSub, true, false);
            }

            TextElement backup = m_lastHighlightedSub != null && !enable ? m_lastHighlightedSub : m_lastHighlighted;
            m_lastHighlighted = null;
            m_lastHighlightedSub = null;
            m_SearchCurrentIndex = -1;

            refreshDocumentColors(backup);

            //if (m_lastHighlighted is Block)
            //{
            //    ((Block)m_lastHighlighted).BorderBrush = m_lastHighlighted_BorderBrush;
            //    ((Block)m_lastHighlighted).BorderThickness = m_lastHighlighted_BorderThickness;
            //}

            //m_lastHighlighted.Background = m_lastHighlighted_Background;
            //m_lastHighlighted.Foreground = m_lastHighlighted_Foreground;

            //

            //m_lastHighlighted = null;


            //if (m_lastHighlightedSub != null)
            //{
            //    //setOrRemoveTextDecoration_SelectUnderline(m_lastHighlightedSub, true);

            //    //if (m_lastHighlightedSub is Block)
            //    //{
            //    //    ((Block)m_lastHighlightedSub).BorderBrush = m_lastHighlightedSub_BorderBrush;
            //    //    ((Block)m_lastHighlightedSub).BorderThickness = m_lastHighlightedSub_BorderThickness;
            //    //}

            //    //m_lastHighlightedSub.Background = m_lastHighlightedSub_Background;
            //    //m_lastHighlightedSub.Foreground = m_lastHighlightedSub_Foreground;



            //    //m_lastHighlightedSub = null;
            //}
        }

        //private void OnSubTreeNodeSelected(TreeNode node)
        //{
        //    if (node == null || CurrentTreeNode == null)
        //    {
        //        return;
        //    }
        //    if (CurrentSubTreeNode == node)
        //    {
        //        return;
        //    }
        //    if (!node.IsDescendantOf(CurrentTreeNode))
        //    {
        //        return;
        //    }
        //    CurrentSubTreeNode = node;

        //    BringIntoViewAndHighlightSub(node);
        //}

        //private void OnTreeNodeSelected(TreeNode node)
        //{
        //    if (node == null)
        //    {
        //        return;
        //    }
        //    if (CurrentTreeNode == node)
        //    {
        //        return;
        //    }

        //    TreeNode subTreeNode = null;

        //    if (CurrentTreeNode != null)
        //    {
        //        if (CurrentSubTreeNode == CurrentTreeNode)
        //        {
        //            if (node.IsAncestorOf(CurrentTreeNode))
        //            {
        //                subTreeNode = CurrentTreeNode;
        //            }
        //        }
        //        else
        //        {
        //            if (node.IsAncestorOf(CurrentSubTreeNode))
        //            {
        //                subTreeNode = CurrentSubTreeNode;
        //            }
        //            else if (node.IsDescendantOf(CurrentTreeNode))
        //            {
        //                subTreeNode = node;
        //            }
        //        }
        //    }

        //    if (subTreeNode == node)
        //    {
        //        CurrentTreeNode = node;
        //        CurrentSubTreeNode = CurrentTreeNode;
        //        BringIntoViewAndHighlight(node);
        //    }
        //    else
        //    {
        //        CurrentTreeNode = node;
        //        CurrentSubTreeNode = CurrentTreeNode;
        //        BringIntoViewAndHighlight(node);

        //        if (subTreeNode != null)
        //        {
        //            m_Logger.Log("-- PublishEvent [SubTreeNodeSelectedEvent] DocumentPaneView.OnTreeNodeSelected",
        //                       Category.Debug, Priority.Medium);

        //            m_EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(subTreeNode);
        //        }
        //    }
        //}
        //DependencyObject FindVisualTreeRoot(DependencyObject initial)
        //{
        //    DependencyObject current = initial;
        //    DependencyObject result = initial;

        //    while (current != null)
        //    {
        //        result = current;
        //        if (current is Visual) // || current is Visual3D)
        //        {
        //            current = VisualTreeHelper.GetParent(current);
        //        }
        //        else
        //        {
        //            // If we're in Logical Land then we must walk 
        //            // up the logical tree until we find a 
        //            // Visual/Visual3D to get us back to Visual Land.
        //            current = LogicalTreeHelper.GetParent(current);
        //        }
        //    }

        //    return result;
        //}

        //public DelegateOnMouseDownTextElementWithNode m_DelegateOnMouseDownTextElementWithNode;
        //public DelegateOnRequestNavigate m_DelegateOnRequestNavigate;

        //public DelegateAddIdLinkTarget m_DelegateAddIdLinkTarget;
        //public DelegateAddIdLinkSource m_DelegateAddIdLinkSource;


        public void AddIdLinkTarget(string name, TextElement textElement)
        {
            m_idLinkTargets.Add(name, textElement);
        }

        public bool HasLinkSource(string name)
        {
            List<TextElement> lte;
            m_idLinkSources.TryGetValue(name, out lte);
            return lte != null;
        }

        public void AddIdLinkSource(string name, TextElement textElement)
        {
            List<TextElement> lte;
            m_idLinkSources.TryGetValue(name, out lte);

            if (lte != null) // m_idLinkSources.ContainsKey(name))
            {
                var list = lte; // m_idLinkSources[name];
                list.Add(textElement);
            }
            else
            {
                var list = new List<TextElement>(1) { textElement };
                m_idLinkSources.Add(name, list);
            }
        }

        private TextElement m_MouseOverTextElement;

        //private Brush m_MouseOverTextElementBackground;
        public void OnTextElementMouseEnter(object sender, MouseEventArgs e)
        {
            restoreMouseOverHighlight();

            var textElem = (TextElement)sender;
            m_MouseOverTextElement = textElem;
            //m_MouseOverTextElementBackground = null;
            //WalkDocumentTree(m_MouseOverTextElement,
            //                 data =>
            //                 {
            //                     m_MouseOverTextElementBackground = data.Background;

            //                     data.Background = ColorBrushCache.Get(Settings.Default.Document_Color_Selection_Back1);
            //                 });

            setOrRemoveTextDecoration_SelectUnderline(m_MouseOverTextElement, false, true);
        }

        private void restoreMouseOverHighlight()
        {
            if (m_MouseOverTextElement != null)
            {
                //WalkDocumentTree(m_MouseOverTextElement,
                //                data =>
                //                {
                //                    data.Background = m_MouseOverTextElementBackground ?? null;
                //                });

                setOrRemoveTextDecoration_SelectUnderline(m_MouseOverTextElement, true, true);
            }
        }

        //public void OnTextElementMouseLeave(object sender, MouseEventArgs e)
        //{
        //    var textElem = (TextElement)sender;
        //    if (m_MouseOverTextElementBackground != null)
        //    {
        //        DebugFix.Assert(textElem == m_MouseOverTextElement);
        //        textElem.Background = m_MouseOverTextElementBackground;
        //    }
        //    m_MouseOverTextElement = null;
        //    m_MouseOverTextElementBackground = null;
        //}

        private void NavigateUri(Uri uri)
        {
            if (uri.ToString().StartsWith("#"))
            {
                string id = uri.ToString().Substring(1);
                PerformTreeNodeSelection(id);
            }
        }

        //public void OnTextElementRequestNavigate(object sender, RequestNavigateEventArgs e)
        //{
        //    m_Logger.Log("DocumentPaneView.OnRequestNavigate: " + e.Uri.ToString(), Category.Debug, Priority.Medium);
        //    m_MouseDownTextElement = null;
        //    NavigateUri(e.Uri);
        //    m_MouseDownTextElement = null;
        //}

        //        public void OnTextElementHyperLinkMouseDown(object sender, RoutedEventArgs e)
        //        {
        //            if (!(e is MouseButtonEventArgs))
        //            {
        //#if DEBUG
        //                Debugger.Break();
        //#endif
        //                return;
        //            }

        //            var hyperlink = (Hyperlink)sender;
        //            var ev = e as MouseButtonEventArgs;

        //            if (ev.ClickCount == 2 && ev.ChangedButton == MouseButton.Left)
        //            {
        //                NavigateUri(hyperlink.NavigateUri);
        //            }
        //        }

        //        public void OnTextElementMouseDown(object sender, RoutedEventArgs e)
        //        {
        //            return; // tesing with Got/LostCapture only 

        //            if (!(e is MouseButtonEventArgs))
        //            {
        //#if DEBUG
        //                Debugger.Break();
        //#endif
        //                return;
        //            }

        //            var textElem = (TextElement)sender;
        //            var ev = e as MouseButtonEventArgs;

        //            if (ev.ClickCount == 1 && ev.ChangedButton == MouseButton.Left)
        //            {
        //                m_MouseDownTextElement = textElem;
        //            }
        //            else
        //            {
        //                m_MouseDownTextElement = null;
        //            }
        //        }

        private void OnFlowDocDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.All;
            e.Handled = true;
        }

        //private void OnFlowDocSelectionChanged(object sender, EventArgs e)
        //{
        //    var textSelection = sender as TextSelection;
        //    if (textSelection == null) return;

        //    Console.WriteLine("Captured: " + Mouse.Captured);
        //    Console.WriteLine("DirectlyOver: " + Mouse.DirectlyOver);
        //    if (Mouse.LeftButton == MouseButtonState.Pressed)
        //    {
        //        Console.WriteLine("Mouse.LeftButton");
        //        return;
        //    }
        //    Console.WriteLine("TEXT: " + textSelection.Text);
        //}

        //private void OnFlowDocViewerKeyDown(object sender, RoutedEventArgs e)
        //{
        //    //FlowDocReader
        //    if (e is KeyEventArgs)
        //    {
        //        OnFlowDocViewerPreviewKeyDown(sender, (KeyEventArgs)e);
        //    }
        //}

        //private void OnFlowDocViewerPreviewKeyDown(object sender, KeyEventArgs e)
        //{
        //    //FlowDocReader
        //    if (e.Key == Key.F3 || e.Key == Key.F && isControlKeyDown())
        //    {
        //        e.Handled = true;
        //    }
        //}


        //        public void OnTextElementMouseUp(object sender, RoutedEventArgs e)
        //        {
        //            return; // tesing with Got/LostCapture only 

        //            if (!(e is MouseButtonEventArgs))
        //            {
        //#if DEBUG
        //                Debugger.Break();
        //#endif
        //                return;
        //            }

        //            var textElem = (TextElement)sender;
        //            var ev = e as MouseButtonEventArgs;

        //            if (m_MouseDownTextElement != textElem)
        //            {
        //                return;
        //            }

        //            if (ev.ClickCount != 1 || ev.ChangedButton != MouseButton.Left)
        //            {
        //                return;
        //            }

        //            //var obj = FindVisualTreeRoot(textElem);

        //            var node = textElem.Tag as TreeNode;
        //            if (node == null)
        //            {
        //#if DEBUG
        //                Debugger.Break();
        //#endif
        //                return;
        //            }

        //            m_UrakawaSession.PerformTreeNodeSelection(node);

        //        }

        public static bool isAltKeyDown()
        {
            return (Keyboard.Modifiers &
                    (ModifierKeys.Alt
                //| ModifierKeys.Shift
                    )
                    ) != ModifierKeys.None;

            //Keyboard.IsKeyDown(Key.LeftShift)
            //System.Windows.Forms.Control.ModifierKeys == Keys.Control;
            // (System.Windows.Forms.Control.ModifierKeys & Keys.Control) != Keys.None;
        }
        public static bool isControlKeyDown()
        {
            return (Keyboard.Modifiers &
                    (ModifierKeys.Control
                //| ModifierKeys.Shift
                    )
                    ) != ModifierKeys.None;

            //Keyboard.IsKeyDown(Key.LeftShift)
            //System.Windows.Forms.Control.ModifierKeys == Keys.Control;
            // (System.Windows.Forms.Control.ModifierKeys & Keys.Control) != Keys.None;
        }

        private void createFlowDocumentFromXuk(Project project)
        {
            TreeNode nodeBook = project.Presentations.Get(0).RootNode;

            //TreeNode nodeBook = root.GetFirstChildWithXmlElementName("book");
            //if (nodeBook == null)
            //{
            //    nodeBook = root.GetFirstChildWithXmlElementName("body");
            //}

            //DebugFix.Assert(root == nodeBook);

            if (nodeBook == null)
            {
                Debug.Fail("No 'book' / 'body' root element ??");
                return;
            }

            DebugFix.Assert(nodeBook.GetXmlElementLocalName().Equals("book", StringComparison.OrdinalIgnoreCase) || nodeBook.GetXmlElementLocalName().Equals("body", StringComparison.OrdinalIgnoreCase));

            var converter = new XukToFlowDocument(this,
                nodeBook, TheFlowDocument,
                m_Logger, m_EventAggregator, m_ShellView,
                m_UrakawaSession
                //OnMouseUpFlowDoc,
                //m_DelegateOnMouseDownTextElementWithNode,
                //m_DelegateOnRequestNavigate,
                );

            //try
            //{
            //    converter.DoWork();
            //}
            //catch (Exception ex)
            //{
            //    ExceptionHandler.Handle(ex, false, m_ShellView);
            //}


            var action = (Action)(() =>
                             {
                                 // NOTE: raises the UNLOAD + LOAD event sequence for the FlowDocument
                                 FlowDocReader.Document = TheFlowDocument;
                                 //converter.m_FlowDoc;


                                 foreach (var ev in FlowDocumentUnLoadedEvents)
                                 {
                                     TheFlowDocument.Unloaded -= new RoutedEventHandler(ev);
                                 }
                                 FlowDocumentUnLoadedEvents.Clear();



                                 FlowDocumentUnLoadedEvents.AddRange(converter.FlowDocumentUnLoadedEvents);
                                 converter.FlowDocumentUnLoadedEvents.Clear();

                                 FlowDocumentLoadedEvents.AddRange(converter.FlowDocumentLoadedEvents);
                                 converter.FlowDocumentLoadedEvents.Clear();

                                 if (converter.MathMLs > 0 && Settings.Default.Document_MathML_LoadReminderMessage)
                                 {
                                     var label = new TextBlock
                                     {
                                         Text = Tobi_Plugin_DocumentPane_Lang.MathMLSelectToShow,
                                         Margin = new Thickness(8, 0, 8, 0),
                                         HorizontalAlignment = HorizontalAlignment.Center,
                                         VerticalAlignment = VerticalAlignment.Center,
                                         Focusable = true,
                                         TextWrapping = TextWrapping.Wrap
                                     };

                                     var iconProvider = new ScalableGreyableImageProvider(
                                         m_ShellView.LoadGnomeNeuIcon("Neu_image-loading"),
                                         m_ShellView.MagnificationLevel);

                                     var panel = new StackPanel
                                     {
                                         Orientation = Orientation.Horizontal,
                                         HorizontalAlignment = HorizontalAlignment.Left,
                                         VerticalAlignment = VerticalAlignment.Stretch,
                                     };
                                     panel.Children.Add(iconProvider.IconLarge);
                                     panel.Children.Add(label);
                                     //panel.Margin = new Thickness(8, 8, 8, 0);







                                     var checkBox = new CheckBox
                                     {
                                         FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],
                                         IsThreeState = false,
                                         IsChecked = !Settings.Default.Document_MathML_LoadReminderMessage,
                                         VerticalAlignment = VerticalAlignment.Center,
                                         Content = Tobi_Common_Lang.DoNotShowMessageAgain,
                                         Margin = new Thickness(0, 16, 0, 0),
                                         HorizontalAlignment = HorizontalAlignment.Left,
                                     };





                                     var mainPanel = new StackPanel
                                     {
                                         Orientation = Orientation.Vertical,
                                     };
                                     mainPanel.Children.Add(panel);
                                     mainPanel.Children.Add(checkBox);





                                     var windowPopup = new PopupModalWindow(m_ShellView,
                                                                            "MathML",
                                                                            mainPanel,
                                                                            PopupModalWindow.DialogButtonsSet.Ok,
                                                                            PopupModalWindow.DialogButton.Ok,
                                                                            true, 360, 180, null, 40, null);

                                     windowPopup.ShowModal();

                                     if (checkBox.IsChecked.Value)
                                     {
                                         Settings.Default.Document_MathML_LoadReminderMessage = false;
                                     }

                                     //if (PopupModalWindow.IsButtonOkYesApply(windowPopup.ClickedDialogButton))
                                     //{
                                     //}
                                 }
                             });

            foreach (var ev in FlowDocumentLoadedEvents)
            {
                TheFlowDocument.Loaded -= new RoutedEventHandler(ev);
            }
            FlowDocumentLoadedEvents.Clear();


            FlowDocReader.Document = new FlowDocument(new Paragraph(new Run(Tobi_Plugin_DocumentPane_Lang.CreatingFlowDocument)));


            // WE CAN'T USE A THREAD BECAUSE FLOWDOCUMENT CANNOT BE FROZEN FOR INTER-THREAD INSTANCE EXCHANGE !! :(
            bool error = m_ShellView.RunModalCancellableProgressTask(false,
                Tobi_Plugin_DocumentPane_Lang.CreatingFlowDocument,
                converter,
                action,
                action
                );


            //GC.Collect();
            //GC.WaitForFullGCComplete();
            //GC.WaitForPendingFinalizers();
        }

        public List<Action<object, RoutedEventArgs>> FlowDocumentLoadedEvents = new List<Action<object, RoutedEventArgs>>();
        public List<Action<object, RoutedEventArgs>> FlowDocumentUnLoadedEvents = new List<Action<object, RoutedEventArgs>>();

        //private void selectNode(TreeNode node)
        //{
        //    if (node == CurrentTreeNode)
        //    {
        //        var treeNode = node.GetFirstDescendantWithText();
        //        if (treeNode != null)
        //        {
        //            m_Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] DocumentPaneView.selectNode",
        //                       Category.Debug, Priority.Medium);

        //            m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(treeNode);
        //        }

        //        return;
        //    }

        //    if (CurrentTreeNode != null && CurrentSubTreeNode != CurrentTreeNode
        //        && node.IsDescendantOf(CurrentTreeNode))
        //    {
        //        m_Logger.Log(
        //            "-- PublishEvent [SubTreeNodeSelectedEvent] DocumentPaneView.OnMouseDownTextElement",
        //            Category.Debug, Priority.Medium);

        //        m_EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(node);
        //    }
        //    else
        //    {
        //        m_Logger.Log(
        //            "-- PublishEvent [TreeNodeSelectedEvent] DocumentPaneView.OnMouseDownTextElement",
        //            Category.Debug, Priority.Medium);

        //        m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(node);
        //    }
        //}

        //private void OnMouseUpFlowDoc()
        //{
        //    m_Logger.Log("DocumentPaneView.OnMouseUpFlowDoc", Category.Debug, Priority.Medium);

        //    TextSelection selection = FlowDocReader.Selection;
        //    if (selection != null && !selection.IsEmpty)
        //    {
        //        TextPointer startPointer = selection.Start;
        //        TextPointer endPointer = selection.End;
        //        TextRange selectedRange = new TextRange(startPointer, endPointer);


        //        TextPointer leftPointer = startPointer;

        //        while (leftPointer != null
        //            && (leftPointer.GetPointerContext(LogicalDirection.Backward) != TextPointerContext.ElementStart
        //            || !(leftPointer.Parent is Run)))
        //        {
        //            leftPointer = leftPointer.GetNextContextPosition(LogicalDirection.Backward);
        //        }
        //        if (leftPointer == null
        //            || (leftPointer.GetPointerContext(LogicalDirection.Backward) != TextPointerContext.ElementStart
        //            || !(leftPointer.Parent is Run)))
        //        {
        //            return;
        //        }

        //        //BringIntoViewAndHighlight((TextElement)leftPointer.Parent);
        //    }
        //}


        public void PerformTreeNodeSelection(string uid)
        {
            //string id = XukToFlowDocument.IdToName(uid);

            TextElement textElement = null;

            TextElement te;
            m_idLinkTargets.TryGetValue(uid, out te);

            if (te != null) //m_idLinkTargets.ContainsKey(uid))
            {
                textElement = te; // m_idLinkTargets[uid];
            }
            //            if (textElement == null)
            //            {
            //#if DEBUG
            //                Debugger.Break();
            //#endif //DEBUG
            //                textElement = TheFlowDocument.FindName(uid) as TextElement;
            //            }
            if (textElement != null)
            {
                if (textElement.Tag is TreeNode)
                {
                    //m_Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] DocumentPaneView.BringIntoViewAndHighlight", Category.Debug, Priority.Medium);

                    m_UrakawaSession.PerformTreeNodeSelection((TreeNode)textElement.Tag);
                    //m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish((TreeNode)(textElement.Tag));
                }
                else
                {
                    Debug.Fail("Hyperlink not to TreeNode ??");
                }
            }
        }

        private void setOrRemoveTextDecoration_SelectUnderline(TextElement textElement, bool remove, bool overrideUseDottedSelect)
        {
            //if (m_UrakawaSession.PerformanceFlag)
            //{
            //    return;
            //}

            if (!remove && !overrideUseDottedSelect && !Settings.Default.Document_UseDottedSelect)
            {
                return;
            }

            if (textElement is ListItem) // TEXT_ELEMENT
            {
                var blocks = ((ListItem)textElement).Blocks;
                foreach (var block in blocks)
                {
                    setOrRemoveTextDecoration_SelectUnderline(block, remove, overrideUseDottedSelect);
                }
            }
            else if (textElement is TableRowGroup) // TEXT_ELEMENT
            {
                var rows = ((TableRowGroup)textElement).Rows;
                foreach (var row in rows)
                {
                    setOrRemoveTextDecoration_SelectUnderline(row, remove, overrideUseDottedSelect);
                }
            }
            else if (textElement is TableRow) // TEXT_ELEMENT
            {
                var cells = ((TableRow)textElement).Cells;
                foreach (var cell in cells)
                {
                    setOrRemoveTextDecoration_SelectUnderline(cell, remove, overrideUseDottedSelect);
                }
            }
            else if (textElement is TableCell) // TEXT_ELEMENT
            {
                var blocks = ((TableCell)textElement).Blocks;
                foreach (var block in blocks)
                {
                    setOrRemoveTextDecoration_SelectUnderline(block, remove, overrideUseDottedSelect);
                }
            }
            else if (textElement is Table) // BLOCK
            {
                var rowGs = ((Table)textElement).RowGroups;
                foreach (var rowG in rowGs)
                {
                    setOrRemoveTextDecoration_SelectUnderline(rowG, remove, overrideUseDottedSelect);
                }
            }
            else if (textElement is Paragraph) // BLOCK
            {
                var inlines = ((Paragraph)textElement).Inlines;
                foreach (var inline in inlines)
                {
                    setOrRemoveTextDecoration_SelectUnderline(inline, remove, overrideUseDottedSelect);
                }
            }
            else if (textElement is Section) // BLOCK
            {
                var blocks = ((Section)textElement).Blocks;
                foreach (var block in blocks)
                {
                    setOrRemoveTextDecoration_SelectUnderline(block, remove, overrideUseDottedSelect);
                }
            }
            else if (textElement is List) // BLOCK
            {
                var lis = ((List)textElement).ListItems;
                foreach (var li in lis)
                {
                    setOrRemoveTextDecoration_SelectUnderline(li, remove, overrideUseDottedSelect);
                }
            }
            else if (textElement is BlockUIContainer) // BLOCK
            {
                // ((BlockUIContainer)textElement).Child => not to be underlined !
            }
            else if (textElement is Span) // INLINE
            {
                var inlines = ((Span)textElement).Inlines;
                foreach (var inline in inlines)
                {
                    setOrRemoveTextDecoration_SelectUnderline(inline, remove, overrideUseDottedSelect);
                }
            }
            else if (textElement is Floater) // INLINE
            {
                var blocks = ((Floater)textElement).Blocks;
                foreach (var block in blocks)
                {
                    setOrRemoveTextDecoration_SelectUnderline(block, remove, overrideUseDottedSelect);
                }
            }
            else if (textElement is Figure) // INLINE
            {
                var blocks = ((Figure)textElement).Blocks;
                foreach (var block in blocks)
                {
                    setOrRemoveTextDecoration_SelectUnderline(block, remove, overrideUseDottedSelect);
                }
            }
            else if (textElement is Inline) // includes InlineUIContainer, LineBreak and Run
            {
                setOrRemoveTextDecoration_SelectUnderline_((Inline)textElement, remove);
            }
            else
            {
#if DEBUG
                Debugger.Break();
#endif
            }
        }

        private TextDecorationCollection m_TextDecorationCollection = null;

        private void setOrRemoveTextDecoration_SelectUnderline_(Inline inline, bool remove)
        {
            if (remove)
            {
                inline.TextDecorations = null;
                return;
            }

            if (m_TextDecorationCollection == null)
            {
                Brush brush = ColorBrushCache.Get(Settings.Default.Document_Color_Selection_UnderOverLine);

                var decUnder = new TextDecoration(
                    TextDecorationLocation.Underline,
                    new Pen(brush, 1)
                    {
                        DashStyle = DashStyles.Solid
                    },
                    2,
                    TextDecorationUnit.Pixel,
                    TextDecorationUnit.Pixel
                    );

                var decOver = new TextDecoration(
                    TextDecorationLocation.OverLine,
                    new Pen(brush, 1)
                    {
                        DashStyle = DashStyles.Solid
                    },
                    0,
                    TextDecorationUnit.Pixel,
                    TextDecorationUnit.Pixel
                    );

                m_TextDecorationCollection = new TextDecorationCollection { decUnder, decOver };
            }

            inline.TextDecorations = m_TextDecorationCollection;
        }

        //private void setTextDecoration_ErrorUnderline(Inline inline)
        //{
        //    //if (textDecorations == null || !textDecorations.Equals(System.Windows.TextDecorations.Underline))
        //    //{
        //    //    textDecorations = System.Windows.TextDecorations.Underline;
        //    //}
        //    //else
        //    //{
        //    //    textDecorations = new TextDecorationCollection(); // or null
        //    //}

        //    var dec = new TextDecoration(
        //        TextDecorationLocation.Underline,
        //        new Pen(Brushes.Red, 1)
        //        {
        //            DashStyle = DashStyles.Dot
        //        },
        //        1,
        //        TextDecorationUnit.FontRecommended,
        //        TextDecorationUnit.FontRecommended
        //    );

        //    //var decs = new TextDecorationCollection { dec };
        //    var decs = new TextDecorationCollection(TextDecorations.OverLine) { dec };

        //    inline.TextDecorations = decs;
        //}

        //public List<object> GetVisibleTextElements()
        //{
        //    m_FoundVisible = false;
        //    temp_ParagraphVisualCount = 0;
        //    temp_ContainerVisualCount = 0;
        //    temp_OtherCount = 0;
        //    //List<object> list = GetVisibleTextObjects_Logical(TheFlowDocument);
        //    List<object> list = GetVisibleTextObjects_Visual(FlowDocReader);
        //    foreach (object obj in list)
        //    {
        //        //TODO: find the TextElement objects, and ultimately, the urakawa Nodes that correspond to this list
        //        //how to find a logical object from a visual one?
        //    }
        //    return list;
        //}

        //private bool m_FoundVisible;
        //private ScrollViewer m_ScrollViewer;

        //private List<object> GetVisibleTextObjects_Logical(DependencyObject obj)
        //{
        //    List<object> elms = new List<object>();

        //    IEnumerable children = LogicalTreeHelper.GetChildren(obj);
        //    IEnumerator enumerator = children.GetEnumerator();

        //    while (enumerator.MoveNext())
        //    {    
        //        if (enumerator.Current is TextElement && IsTextObjectInView((TextElement)enumerator.Current))
        //        {
        //            elms.Add(enumerator.Current);
        //        }
        //        if (enumerator.Current is DependencyObject)
        //        {
        //            List<object> list = GetVisibleTextObjects_Logical((DependencyObject)enumerator.Current);
        //            elms.AddRange(list);
        //        }
        //    }
        //    return elms;
        //}


        ////just for testing purposes
        //private int temp_ContainerVisualCount;
        //private int temp_ParagraphVisualCount;
        //private int temp_OtherCount;

        //private List<object> GetVisibleTextObjects_Visual(DependencyObject obj)
        //{
        //    if (obj.DependencyObjectType.Name == "ParagraphVisual") temp_ParagraphVisualCount++;
        //    else if (obj is ContainerVisual) temp_ContainerVisualCount++;
        //    else temp_OtherCount++;

        //    if (obj is ScrollContentPresenter)
        //    {
        //        object view = ((ScrollContentPresenter) obj).Content;
        //    }
        //    List<object> elms = new List<object>();

        //    int childcount = VisualTreeHelper.GetChildrenCount(obj);

        //    for (int i = 0; i<childcount; i++)
        //    {
        //        DependencyObject child = VisualTreeHelper.GetChild(obj, i);
        //        if (child is ScrollViewer) m_ScrollViewer = (ScrollViewer) child;
        //        if (child != null)
        //        {
        //            //there may be more types
        //            if (child.DependencyObjectType.Name == "ParagraphVisual")
        //            {
        //                if (IsTextObjectInView((Visual)child))
        //                {
        //                    m_FoundVisible = true;
        //                    elms.Add(child);
        //                    List<object> list = GetVisibleTextObjects_Visual(child);
        //                    if (list != null) elms.AddRange(list);
        //                }
        //                else
        //                {
        //                    //if this is our first non-visible object
        //                    //after encountering one or more visible objects, assume we are out of the viewable region
        //                    //since it should only show contiguous objects
        //                    if (m_FoundVisible)
        //                    {
        //                        return null;
        //                    }
        //                    //else, we haven't found any visible text objects yet, so keep looking
        //                    else
        //                    {
        //                        List<object> list = GetVisibleTextObjects_Visual(child);
        //                        if (list != null) elms.AddRange(list);
        //                    }
        //                }
        //            }
        //            //just recurse for non-text objects
        //            else
        //            {
        //                List<object> list = GetVisibleTextObjects_Visual(child);
        //                if (list != null) elms.AddRange(list);
        //            }
        //        }

        //    }
        //    return elms;
        //}
        ////say whether the text object is in view on the screen.  assumed: obj is a text visual
        //private bool IsTextObjectInView(Visual obj)
        //{
        //    //ParagraphVisual objects are also ContainerVisual
        //    if (obj is ContainerVisual)
        //    {
        //        ContainerVisual cv = (ContainerVisual) obj;
        //        //express the visual object's coordinates in terms of the flow doc reader
        //        GeneralTransform paraTransform = obj.TransformToAncestor(m_ScrollViewer);
        //        Rect rect;
        //        if (cv.Children.Count > 0)
        //            rect = cv.DescendantBounds;
        //        else
        //            rect = cv.ContentBounds;
        //        Rect rectTransformed = paraTransform.TransformBounds(rect);

        //        //then figure out if these coordinates are in the currently visible document portion))
        //        Rect viewportRect = new Rect(0, 0, m_ScrollViewer.ViewportWidth, m_ScrollViewer.ViewportHeight);
        //        if (viewportRect.Contains(rectTransformed))
        //            return true;
        //        else
        //            return false;
        //    }
        //    return false;

        //}
        //private bool IsTextObjectInView(TextElement obj)
        //{
        //    //how to find visibility information from a logical object??
        //    DependencyObject test = obj;
        //    while (test != null)
        //    {
        //        test = LogicalTreeHelper.GetParent(test);
        //        if (test is Visual)
        //        {
        //            break;
        //        }
        //    }
        //    if (drillDown(test) != null)
        //    {
        //        return true;
        //    }
        //    return true;
        //}

        //private DependencyObject drillDown(DependencyObject test)
        //{
        //    IEnumerable children = LogicalTreeHelper.GetChildren(test);
        //    foreach (DependencyObject obj in children)
        //    {
        //        if (obj is Visual)
        //            return obj;
        //        else
        //            return drillDown(obj);
        //    }
        //    return null;
        //}
        //private void TestButton_Click(object sender, RoutedEventArgs e)
        //{
        //    List<object> list = GetVisibleTextElements();
        //    string str = "The visible text objects, perhaps with some redundancies:\n";
        //    foreach (object obj in list)
        //    {
        //        str += obj.ToString();
        //        str += "\n";

        //    }
        //    MessageBox.Show(str);
        //}
        //private void OnFontSelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (e.AddedItems != null && e.AddedItems.Count > 0)
        //        DebugFix.Assert(comboListOfFonts.SelectedItem == e.AddedItems[0]);
        //    //FlowDocReader.FontFamily = (FontFamily)comboListOfFonts.SelectedItem;
        //    if (comboListOfFonts.SelectedItem != null)
        //        TheFlowDocument.FontFamily = (FontFamily)comboListOfFonts.SelectedItem;
        //}
        private void OnToolbarToggleVisible(object sender, MouseButtonEventArgs e)
        {
            Settings.Default.Document_ButtonBarVisible = !Settings.Default.Document_ButtonBarVisible;
        }

        private void OnToolbarToggleVisibleKeyboard(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) // || e.Key == Key.Space)
            {
                Settings.Default.Document_ButtonBarVisible = !Settings.Default.Document_ButtonBarVisible;
                FocusHelper.FocusBeginInvoke(Settings.Default.Document_ButtonBarVisible ? FocusExpanded : FocusCollapsed);
            }
        }

        private void OnDocKeyUp(object sender, KeyEventArgs e)
        {
            var key = (e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));

            if (key == Key.Escape)
            {
                //CommandUnFollowLink.Execute();
            }
        }

        private void OnSearchBoxKeyUp(object sender, KeyEventArgs e)
        {
            var key = (e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));

            if (e.Key == Key.Return && CommandFindNext.CanExecute())
            {
                CommandFindNext.Execute();
            }

            if (key == Key.Escape)
            {
                SearchBox.Text = "";
                SearchTerm = null;
                CommandFocus.Execute();
            }
        }

        public void OnHyperLinkGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            scrollToView((Hyperlink)sender);
        }

        //private void BuiltinCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        //{
        //    //e.Handled = true;
        //}

        //private void OnPreviewKeyDown_DocViewer(object sender, KeyEventArgs e)
        //{
        //    if (Keyboard.Modifiers == ModifierKeys.Control
        //        && (e.Key == Key.P || e.Key == Key.A || e.Key == Key.C || e.Key == Key.X || e.Key == Key.V))
        //    {
        //        e.Handled = true;
        //    }
        //}
    }
}
