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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Practices.Composite;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
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
using urakawa.events.undo;
using urakawa.xuk;
using Colors = System.Windows.Media.Colors;

namespace Tobi.Plugin.DocumentPane
{
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
            var scb = new SolidColorBrush((Color)value);
            return scb;
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
    public partial class DocumentPaneView : IPartImportsSatisfiedNotification, INotifyPropertyChangedEx
    {
        public void OnImportsSatisfied()
        {
            trySearchCommands();
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

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e) { SearchTerm = SearchBox.Text; }

        private string m_SearchTerm;
        public string SearchTerm
        {
            get { return m_SearchTerm; }
            set
            {
                if (m_SearchTerm == value) { return; }
                m_SearchTerm = value;

                m_PropertyChangeHandler.RaisePropertyChanged(() => SearchTerm);

                m_SearchMatches = null;
                m_SearchCurrentIndex = -1;

                var textElement = FindPreviousNext(false, false);
                if (textElement == null)
                {
                    m_SearchCurrentIndex = -1;
                    textElement = FindPreviousNext(true, false);
                    m_SearchCurrentIndex = -1;
                }

                if (Settings.Default.Document_EnableInstantSearch)
                {
                    if (FlowDocReader.Selection == null)
                    {
                        return;
                    }
                    var selectionBackup = new TextRange(FlowDocReader.Selection.Start, FlowDocReader.Selection.End);

                    AnnotationService service = AnnotationService.GetService(FlowDocReader);

                    if (service == null || !service.IsEnabled)
                    {
                        return;
                    }

                    //FlowDocReader.Selection.Select(FlowDocReader.Document.ContentStart,
                    //                               FlowDocReader.Document.ContentEnd);
                    //AnnotationHelper.ClearHighlightsForSelection(service);

                    foreach (var annotation in service.Store.GetAnnotations())
                    {
                        service.Store.DeleteAnnotation(annotation.Id);
                    }

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

                    var brush = GetCachedBrushForColor(Common.Settings.Default.SearchHits_Color);
                    brush.Opacity = .5;
                    //var brush = new SolidColorBrush(Common.Settings.Default.SearchHits_Color) { Opacity = .5 };

                    foreach (var textRange in m_SearchMatches)
                    {
                        FlowDocReader.Selection.Select(textRange.Start, textRange.End);
                        AnnotationHelper.CreateHighlightForSelection(service, "Tobi search hits", brush);
                    }

                    FlowDocReader.Selection.Select(selectionBackup.Start, selectionBackup.End);
                }
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
                //Debug.Assert(hit.Text.ToLower() == SearchTerm.ToLower());

                if (FlowDocReader.Selection != null)
                {
                    if (select && !Settings.Default.Document_EnableInstantSearch)
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
                        Debug.Assert(textElement.Tag is TreeNode);

                        if (select)
                        {
                            m_UrakawaSession.PerformTreeNodeSelection((TreeNode)textElement.Tag);
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

        private string showDialogTextEdit(string text)
        {
            m_Logger.Log("showDialogTextEdit", Category.Debug, Priority.Medium);


            var editBox = new TextBox
                              {
                                  Text = text,
                                  TextWrapping = TextWrapping.WrapWithOverflow
                              };

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_DocumentPane_Lang.CmdEditText_ShortDesc),
                                                   new ScrollViewer { Content = editBox },
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 300, 160, null, 40);
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

        public RichDelegateCommand CommandStructureUp { get; private set; }
        public RichDelegateCommand CommandStructureDown { get; private set; }

        public RichDelegateCommand CommandFocus { get; private set; }

        public RichDelegateCommand CommandToggleTextOnlyView { get; private set; }

        private readonly ILoggerFacade m_Logger;

        private readonly IEventAggregator m_EventAggregator;
        private readonly IUrakawaSession m_UrakawaSession;

        private readonly IShellView m_ShellView;

        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        [ImportingConstructor]
        public DocumentPaneView(
            IEventAggregator eventAggregator,
            ILoggerFacade logger,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession urakawaSession,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView)
        {
            m_PropertyChangeHandler = new PropertyChangedNotifyBase();
            m_PropertyChangeHandler.InitializeDependentProperties(this);

            m_UrakawaSession = urakawaSession;
            m_EventAggregator = eventAggregator;
            m_Logger = logger;
            m_ShellView = shellView;

            DataContext = this;

            CommandFocus = new RichDelegateCommand(
                Tobi_Plugin_DocumentPane_Lang.CmdTxtFocus_ShortDesc,
                null,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("edit-select-all"),
                () =>
                {
                    if (FocusCollapsed.IsVisible)
                    {
                        FocusHelper.FocusBeginInvoke(FocusCollapsed);
                    }
                    else
                    {
                        FocusHelper.FocusBeginInvoke(FocusExpanded);
                    }
                },
                () => true,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Focus_Txt));

            m_ShellView.RegisterRichCommand(CommandFocus);
            //
            CommandToggleTextOnlyView = new RichDelegateCommand(
                Tobi_Plugin_DocumentPane_Lang.CmdTextOnlyViewToggle_ShortDesc,
                Tobi_Plugin_DocumentPane_Lang.CmdTextOnlyViewToggle_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadGnomeNeuIcon("Neu_preferences-desktop-font"),
                () =>
                {
                    Settings.Default.Document_ShowTextOnlyView = !Settings.Default.Document_ShowTextOnlyView;
                },
                () => true,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_ToggleTextOnly));

            m_ShellView.RegisterRichCommand(CommandToggleTextOnlyView);
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

                    Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                    return selection.Item1 != null;
                },
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_StructureSelectDown));

            m_ShellView.RegisterRichCommand(CommandStructureDown);
            //

            CommandEditText = new RichDelegateCommand(
                Tobi_Plugin_DocumentPane_Lang.CmdEditText_ShortDesc,
                Tobi_Plugin_DocumentPane_Lang.CmdEditText_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("accessories-text-editor"),
                () =>
                {
                    TreeNode node = null;
                    if (m_MouseDownTextElementForEdit != null)
                    {
                        Debug.Assert(m_MouseDownTextElementForEdit.Tag is TreeNode);
                        node = (TreeNode)m_MouseDownTextElementForEdit.Tag;
                        m_MouseDownTextElementForEdit = null;
                    }
                    else
                    {
                        Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                        node = selection.Item2 ?? selection.Item1;
                    }
                    if (node == null) return;

                    string oldTxt = TreeNodeChangeTextCommand.GetText(node);

                    if (string.IsNullOrEmpty(oldTxt)) return;

                    string txt = showDialogTextEdit(oldTxt);

                    if (string.IsNullOrEmpty(txt) || txt == oldTxt) return;

                    m_EventAggregator.GetEvent<EscapeEvent>().Publish(null);

                    var cmd = node.Presentation.CommandFactory.CreateTreeNodeChangeTextCommand(node, txt);
                    node.Presentation.UndoRedoManager.Execute(cmd);
                },
                () =>
                {
                    if (m_UrakawaSession.DocumentProject == null) return false;

                    Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                    TreeNode node = selection.Item2 ?? selection.Item1;
                    return node != null && !string.IsNullOrEmpty(TreeNodeChangeTextCommand.GetText(node))
                        || m_MouseDownTextElementForEdit != null;
                },
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_EditText));

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
                    Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                    TreeNode node = selection.Item2 ?? selection.Item1;
                    TreeNode nodeToNavigate = node.GetPreviousSiblingWithText(true);
                    if (nodeToNavigate == null)
                        AudioCues.PlayBeep();
                    else
                        m_UrakawaSession.PerformTreeNodeSelection(nodeToNavigate);

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
                    Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                    TreeNode node = selection.Item2 ?? selection.Item1;
                    TreeNode nodeToNavigate = node.GetNextSiblingWithText(true);
                    if (nodeToNavigate == null)
                        AudioCues.PlayBeep();
                    else
                        m_UrakawaSession.PerformTreeNodeSelection(nodeToNavigate);

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
                FindNext,
                () => !string.IsNullOrEmpty(SearchTerm),
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_PageFindNext)
                );
            CommandFindPrev = new RichDelegateCommand(
                @"DOCVIEW CommandFindPrevious DUMMY TXT",
                @"DOCVIEW CommandFindPrevious DUMMY TXT",
                null, // KeyGesture set only for the top-level CompositeCommand
                null,
                FindPrevious,
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
            ////FlowDocReader.AddHandler(ContentElement.MouseUpEvent, new RoutedEventHandler(OnFlowDocViewerMouseUp), true);

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

        private Block createWelcomeEmptyFlowDoc()
        {
            string dirPath = Path.GetDirectoryName(ApplicationConstants.LOG_FILE_PATH);
            string imgPath = Path.Combine(dirPath, "daisy_01.png");
            try
            {
                FileStream imageStream = File.OpenRead(imgPath);
                var iconDecoder = new PngBitmapDecoder(imageStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                ImageSource imageSource = iconDecoder.Frames[0];
                var image = new Image
                    {
                        Source = imageSource,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Top,
                        Stretch = Stretch.Uniform,
                        StretchDirection = StretchDirection.DownOnly
                    };
                image.MaxWidth = 240;

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

                var run2 = new Run(@"Open-Source DAISY Multimedia Authoring");
                block.Inlines.Add(run2);
                block.Inlines.Add(new LineBreak());

                var inline = new InlineUIContainer(image)
                    {
                        BaselineAlignment = BaselineAlignment.Top
                    };
                block.Inlines.Add(inline);

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
            CommandFindFocus.IsActive = m_ShellView.ActiveAware.IsActive && ActiveAware.IsActive;
            CommandFindNext.IsActive = m_ShellView.ActiveAware.IsActive && ActiveAware.IsActive;
            CommandFindPrev.IsActive = m_ShellView.ActiveAware.IsActive && ActiveAware.IsActive;
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

        private DependencyObject m_MouseDownTextElement;// TextElement Image, Panel (UIElement)
        private void OnFlowDocGotMouseCapture(object sender, RoutedEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                var textElement = getFirstAncestorWithTreeNodeTag(Mouse.DirectlyOver);

                if (textElement != null)
                {
                    m_MouseDownTextElement = textElement;
                    m_MouseDownTextElementForEdit = null;
                }
            }
        }

        private TextElement m_MouseDownTextElementForEdit;
        private void OnFlowDocLostMouseCapture(object sender, RoutedEventArgs e)
        {
            var mouseDownTextElement = m_MouseDownTextElement;
            m_MouseDownTextElement = null;

            Dispatcher.BeginInvoke(DispatcherPriority.Input, (Action)(() =>
            {
                if (mouseDownTextElement == null) return;

                var textElement = getFirstAncestorWithTreeNodeTag(Mouse.DirectlyOver);

                if (textElement == null) return;

                Debug.Assert(textElement.Tag != null);

                if (textElement == mouseDownTextElement)
                {
                    var before = (m_lastHighlightedSub ?? m_lastHighlighted);
                    if (isAltKeyDown())
                    {
                        m_MouseDownTextElementForEdit = textElement;
                        CommandEditText.Execute();
                    }
                    var after = (m_lastHighlightedSub ?? m_lastHighlighted);
                    if (before != after) return; // selection already performed

                    if (textElement != (m_lastHighlightedSub ?? m_lastHighlighted))
                    {
                        m_UrakawaSession.PerformTreeNodeSelection((TreeNode)textElement.Tag);
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
                            TextElement hyperlink = textElement;
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

                            // Fallback:
                            Dispatcher.BeginInvoke(DispatcherPriority.Background,
                                (Action)(() =>
                                {
                                    if (FlowDocReader.Selection != null)
                                        FlowDocReader.Selection.Select(textElement.ContentStart, textElement.ContentEnd);
                                })
                                );
                        }
                    }
                }
            }));
        }


        private void refreshTextOnlyViewColors()
        {
            if (m_TextOnlyViewRun != null)
            {
                m_TextOnlyViewRun.Foreground = GetCachedBrushForColor(Settings.Default.Document_Color_Font_TextOnly);
            }
        }

        private void OnSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!e.PropertyName.StartsWith(@"Document_Color_")
                //&& !e.PropertyName.StartsWith(@"Document_")
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
            if (treeNode == null) return null;
            QualifiedName qname = treeNode.GetXmlElementQName();
            if (qname == null) return null;

            if (qname.LocalName == "annotation"
                || qname.LocalName == "note")
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
            if (m_UrakawaSession.DocumentProject == null) return;

            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode treeNode_ = selection.Item2 ?? selection.Item1;
            if (treeNode_ == null) return;

            TreeNode treeNode = ensureTreeNodeIsNoteAnnotation(treeNode_);
            if (treeNode == null) return;

            string uid = treeNode.GetXmlElementId();
            if (string.IsNullOrEmpty(uid)) return;

            //string id = XukToFlowDocument.IdToName(uid);

            TextElement textElement = null;
            if (m_idLinkTargets.ContainsKey(uid))
            {
                textElement = m_idLinkTargets[uid];
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
                    Debug.Assert(treeNode == (TreeNode)textElement.Tag);
                }
            }
            if (m_idLinkSources.ContainsKey(uid))
            {
                var list = m_idLinkSources[uid];
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
                }
                else
                {
#if DEBUG
                    Debugger.Break();
#endif //DEBUG
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(textElement.BringIntoView));
                }
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

        private void findAndUpdateTreeNodeAudioTextStatus(Command cmd, bool done)
        {
            if (cmd is TreeNodeChangeTextCommand)
            {
                var command = (TreeNodeChangeTextCommand)cmd;
                findAndUpdateTreeNodeText(command, done);
            }
            else if (cmd is ManagedAudioMediaInsertDataCommand)
            {
                var command = (ManagedAudioMediaInsertDataCommand)cmd;
                findAndUpdateTreeNodeAudioStatus(command.TreeNode);
            }
            else if (cmd is TreeNodeSetManagedAudioMediaCommand)
            {
                var command = (TreeNodeSetManagedAudioMediaCommand)cmd;
                findAndUpdateTreeNodeAudioStatus(command.TreeNode);
            }
            else if (cmd is TreeNodeAudioStreamDeleteCommand)
            {
                var command = (TreeNodeAudioStreamDeleteCommand)cmd;
                findAndUpdateTreeNodeAudioStatus(command.SelectionData.m_TreeNode);
            }
            else if (cmd is CompositeCommand)
            {
                foreach (var childCommand in ((CompositeCommand)cmd).ChildCommands.ContentsAs_YieldEnumerable)
                {
                    findAndUpdateTreeNodeAudioTextStatus(childCommand, done);
                }
            }
        }

        private void OnUndoRedoManagerChanged(object sender, UndoRedoManagerEventArgs eventt)
        {
            if (!Dispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action<object, UndoRedoManagerEventArgs>)OnUndoRedoManagerChanged, sender, eventt);
                return;
            }

            //m_Logger.Log("DocumentPaneViewModel.OnUndoRedoManagerChanged", Category.Debug, Priority.Medium);

            if (!(eventt is DoneEventArgs
                           || eventt is UnDoneEventArgs
                           || eventt is ReDoneEventArgs
                           || eventt is TransactionEndedEventArgs))
            {
                Debug.Fail("This should never happen !!");
                return;
            }

            if (m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.IsTransactionActive)
            {
                Debug.Assert(eventt is DoneEventArgs || eventt is TransactionEndedEventArgs);
                //m_Logger.Log("DocumentPaneViewModel.OnUndoRedoManagerChanged (exit: ongoing TRANSACTION...)", Category.Debug, Priority.Medium);
                return;
            }

            bool done = eventt is DoneEventArgs || eventt is ReDoneEventArgs || eventt is TransactionEndedEventArgs;

            Command cmd = eventt.Command;

            findAndUpdateTreeNodeAudioTextStatus(cmd, done);
        }

        private bool bTreeNodeNeedsAudio(TreeNode node)
        {
            QualifiedName qname = node.GetXmlElementQName();
            if (node.GetTextMedia() != null
                || qname != null && qname.LocalName.ToLower() == "img")
            {
                return true;
            }

            return false;
        }

        private void findAndUpdateTreeNodeText(TreeNodeChangeTextCommand cmd, bool done)
        {
            TreeNode node = cmd.TreeNode;

            TextElement text = null;
            if (m_lastHighlighted != null && m_lastHighlighted.Tag == node)
            {
                text = m_lastHighlighted;
            }
            if (m_lastHighlightedSub != null && m_lastHighlightedSub.Tag == node)
            {
                text = m_lastHighlightedSub;
            }
            if (text == null)
            {
                text = FindTextElement(node);
            }
            if (text != null)
            {
                Debug.Assert(node == text.Tag);
                if (node == text.Tag)
                {
                    //var media = node.GetTextMedia();
                    //Debug.Assert(media != null);
                    //Debug.Assert(!string.IsNullOrEmpty(media.Text));

                    //Run run = VisualLogicalTreeWalkHelper.FindObjectInLogicalTreeWithMatchingType<Run>(text, null);
                    Run run = null;
                    foreach (var run_ in VisualLogicalTreeWalkHelper.FindObjectsInLogicalTreeWithMatchingType<Run>(text, null))
                    {
                        if (run != null)
                        {
                            run = null;
                            Debug.Fail("WTF ?");
                            break;
                        }
                        run = run_;
                    }
                    if (run != null)
                    {
                        //ThreadPool.QueueUserWorkItem(obj =>
                        Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(() =>
                        {
                            run.Text = done ? cmd.NewText : cmd.OldText;
                        }));
                    }
                    else
                    {
#if DEBUG // Normally, the TextBlock's Run is picked-up with the code above, no need for the code below
                        Debugger.Break();
#endif
                        TextBlock tb = null;
                        foreach (var tb_ in VisualLogicalTreeWalkHelper.FindObjectsInLogicalTreeWithMatchingType<TextBlock>(text, null))
                        {
                            if (tb != null)
                            {
                                tb = null;
                                Debug.Fail("WTF ?");
                                break;
                            }
                            tb = tb_;
                        }
                        if (tb != null)
                        {
                            //ThreadPool.QueueUserWorkItem(obj =>
                            Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(() =>
                            {
                                tb.Text = done ? cmd.NewText : cmd.OldText;
                            }));
                        }
                    }


                    Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                    TreeNode selected = selection.Item2 ?? selection.Item1;
                    if (selected != node)
                    {
                        m_UrakawaSession.PerformTreeNodeSelection(node);
                    }
                    else
                    {
                        updateSimpleTextView(selected);
                    }
                }
            }
        }

        private void findAndUpdateTreeNodeAudioStatus(TreeNode node)
        {
            foreach (var childTreeNode in node.Children.ContentsAs_YieldEnumerable)
            {
                findAndUpdateTreeNodeAudioStatus(childTreeNode);
            }

            if (!bTreeNodeNeedsAudio(node))
            {
                return;
            }

            TextElement text = null;
            if (m_lastHighlighted != null && m_lastHighlighted.Tag == node)
            {
                text = m_lastHighlighted;
            }
            if (m_lastHighlightedSub != null && m_lastHighlightedSub.Tag == node)
            {
                text = m_lastHighlightedSub;
            }
            if (text == null)
            {
                text = FindTextElement(node);
            }
            if (text != null)
            {
                Debug.Assert(node == text.Tag);
                if (node == text.Tag)
                {
                    XukToFlowDocument.SetForegroundColorAndCursorBasedOnTreeNodeTag(this, text, false);

                    //Debug.Assert(noAudio == !node.HasOrInheritsAudio());

                    //if (m_lastHighlighted == text)
                    //{
                    //    m_lastHighlighted_Foreground = text.Foreground;
                    //}
                    //if (m_lastHighlightedSub == text)
                    //{
                    //    m_lastHighlightedSub_Foreground = text.Foreground;
                    //}
                }
            }
            ////ThreadPool.QueueUserWorkItem(obj =>
            //Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(() =>
            //{

            //}));
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

            project.Presentations.Get(0).UndoRedoManager.CommandDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandReDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandUnDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.TransactionEnded -= OnUndoRedoManagerChanged;

            OnProjectLoaded(null);
        }

        //private bool m_FlowDocSelectionHooked;

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
                SearchBox.Text = "";
                SearchTerm = null;
                CommandFocus.Execute();

#if false && DEBUG
                FlowDocReader.Document = new FlowDocument(new Paragraph(new Run("Testing FlowDocument (DEBUG) （１）このテキストDAISY図書は，レベル５まであります。")));
#else
                TheFlowDocument.Blocks.Add(createWelcomeEmptyFlowDoc());
                TheFlowDocumentSimple.Blocks.Add(createWelcomeEmptyFlowDoc());
#endif //DEBUG

                GC.Collect();
                GC.WaitForFullGCComplete();
                return;
            }
            else
            {
                project.Presentations.Get(0).UndoRedoManager.CommandDone += OnUndoRedoManagerChanged;
                project.Presentations.Get(0).UndoRedoManager.CommandReDone += OnUndoRedoManagerChanged;
                project.Presentations.Get(0).UndoRedoManager.CommandUnDone += OnUndoRedoManagerChanged;
                project.Presentations.Get(0).UndoRedoManager.TransactionEnded += OnUndoRedoManagerChanged;
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
                Debugger.Break();
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

            if (textElement2 == null)
            {
                doLastHighlightedOnly(textElement1, false);

                scrollToView(textElement1);
            }
            else
            {
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

            string str = treeNode.GetTextMediaFlattened(true);
            if (string.IsNullOrEmpty(str))
            {
                m_TextOnlyViewRun.Text = "";
                return;
            }

            m_TextOnlyViewRun.Text = str;
        }

        private void scrollToView(TextElement textElement)
        {
            if (FlowDocReader.ScrollViewer == null)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(textElement.BringIntoView));
            }
            else
            {
                Dispatcher.Invoke(DispatcherPriority.Render, (Action)(textElement.BringIntoView));
                //textElement.BringIntoView();
                Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action<TextElement>)(scrollToView_), textElement);
            }
        }

        private void scrollToView_(TextElement textElement)
        {
            //Debug.Assert(FlowDocReader.ScrollViewer.ScrollableHeight == FlowDocReader.ScrollViewer.ExtentHeight - FlowDocReader.ScrollViewer.ViewportHeight);
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
                        Debugger.Break();
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
                Debug.Assert(textTotalHeight_ == textTotalHeight);
            }

            //Rect rectDocStart = FlowDocReader.Document.ContentStart.GetCharacterRect(LogicalDirection.Forward);
            double boxTopRelativeToDoc = -20 + FlowDocReader.ScrollViewer.VerticalOffset + rectBoundingBox.Top;

            double offsetToTop = boxTopRelativeToDoc;
            double offsetToCenter = boxTopRelativeToDoc - (FlowDocReader.ScrollViewer.ViewportHeight - rectBoundingBox.Height) / 2;
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

            Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(() => FlowDocReader.ScrollViewer.ScrollToVerticalOffset(offset)));
        }

        private Dictionary<String, SolidColorBrush> m_SolidColorBrushCache;
        public SolidColorBrush GetCachedBrushForColor(Color color)
        {
            if (m_SolidColorBrushCache == null)
            {
                m_SolidColorBrushCache = new Dictionary<String, SolidColorBrush>();
            }

            string colorString = color.ToString();

            if (!m_SolidColorBrushCache.ContainsKey(colorString))
            {
                bool found = false;
                foreach (PropertyInfo propertyInfo in typeof(Brushes).GetProperties(BindingFlags.Public | BindingFlags.Static))
                {
                    if (propertyInfo.PropertyType == typeof(SolidColorBrush))
                    {
                        var brush = (SolidColorBrush)propertyInfo.GetValue(null, null);
                        if (brush.Color.ToString() == colorString)
                        {
                            found = true;
                            m_SolidColorBrushCache.Add(colorString, brush);
                            break;
                        }
                    }
                }
                if (!found)
                {
                    m_SolidColorBrushCache.Add(colorString, new SolidColorBrush(color));
                }
            }

            return m_SolidColorBrushCache[colorString];
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
            Brush brushFont = GetCachedBrushForColor(Settings.Default.Document_Color_Selection_Font);
            Brush brushBorder = GetCachedBrushForColor(Settings.Default.Document_Color_Selection_Border);
            Brush brushBack2 = GetCachedBrushForColor(Settings.Default.Document_Color_Selection_Back2);

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
            Brush brushFont = GetCachedBrushForColor(Settings.Default.Document_Color_Selection_Font);
            Brush brushBorder = GetCachedBrushForColor(Settings.Default.Document_Color_Selection_Border);
            Brush brushBack1 = GetCachedBrushForColor(Settings.Default.Document_Color_Selection_Back1);
            Brush brushBack2 = GetCachedBrushForColor(Settings.Default.Document_Color_Selection_Back2);

            if (!onlyUpdateColors)
            {
                m_lastHighlighted = textElement1;
                m_SearchCurrentIndex = -1;

                //m_lastHighlighted_Background = m_lastHighlighted.Background;
                //m_lastHighlighted_Foreground = m_lastHighlighted.Foreground;
            }
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

            setOrRemoveTextDecoration_SelectUnderline(m_lastHighlighted, true, false);

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

            textRange.ApplyPropertyValue(TextElement.ForegroundProperty, GetCachedBrushForColor(Settings.Default.Document_Color_Font_NoAudio));
            textRange.ApplyPropertyValue(TextElement.BackgroundProperty, GetCachedBrushForColor(Settings.Default.Document_Back));
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

            TextElement backup = m_lastHighlighted;
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
        public void AddIdLinkSource(string name, TextElement textElement)
        {
            if (m_idLinkSources.ContainsKey(name))
            {
                var list = m_idLinkSources[name];
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

            //                     data.Background = GetCachedBrushForColor(Settings.Default.Document_Color_Selection_Back1);
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
        //        Debug.Assert(textElem == m_MouseOverTextElement);
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

        //        public void OnFlowDocViewerMouseUp(object sender, RoutedEventArgs e)
        //        {
        //            return; // tesing with Got/LostCapture only 

        //            if (!(e is MouseButtonEventArgs))
        //            {
        //#if DEBUG
        //                Debugger.Break();
        //#endif
        //                return;
        //            }

        //            var ev = e as MouseButtonEventArgs;
        //            var flowDocView = (FlowDocumentScrollViewer)sender;
        //            if (m_MouseDownTextElement != null)
        //            {
        //                OnTextElementMouseUp(m_MouseDownTextElement, ev);
        //            }
        //        }

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
            TreeNode root = project.Presentations.Get(0).RootNode;
            TreeNode nodeBook = root.GetFirstChildWithXmlElementName("book");

            Debug.Assert(root == nodeBook);

            if (nodeBook == null)
            {
                Debug.Fail("No 'book' root element ??");
                return;
            }

            var converter = new XukToFlowDocument(this,
                nodeBook, TheFlowDocument,
                m_Logger, m_EventAggregator, m_ShellView
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
                                 FlowDocReader.Document = TheFlowDocument;
                                 //converter.m_FlowDoc;
                             });
            FlowDocReader.Document = new FlowDocument(new Paragraph(new Run(Tobi_Plugin_DocumentPane_Lang.CreatingFlowDocument)));

            // WE CAN'T USE A THREAD BECAUSE FLOWDOCUMENT CANNOT BE FROZEN FOR INTER-THREAD INSTANCE EXCHANGE !! :(
            m_ShellView.RunModalCancellableProgressTask(false,
                Tobi_Plugin_DocumentPane_Lang.CreatingFlowDocument,
                converter,
                action,
                action
                );

            GC.Collect();
            GC.WaitForFullGCComplete();
            GC.WaitForPendingFinalizers();
        }

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
            if (m_idLinkTargets.ContainsKey(uid))
            {
                textElement = m_idLinkTargets[uid];
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

        private void setOrRemoveTextDecoration_SelectUnderline_(Inline inline, bool remove)
        {
            if (remove)
            {
                inline.TextDecorations = null;
                return;
            }

            Brush brush = GetCachedBrushForColor(Settings.Default.Document_Color_Selection_UnderOverLine);

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

            var decs = new TextDecorationCollection { decUnder, decOver };

            inline.TextDecorations = decs;
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
        //        Debug.Assert(comboListOfFonts.SelectedItem == e.AddedItems[0]);
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
            if (e.Key == Key.Return || e.Key == Key.Space)
            {
                Settings.Default.Document_ButtonBarVisible = !Settings.Default.Document_ButtonBarVisible;
                FocusHelper.FocusBeginInvoke(Settings.Default.Document_ButtonBarVisible ? FocusExpanded : FocusCollapsed);
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
    }
}
