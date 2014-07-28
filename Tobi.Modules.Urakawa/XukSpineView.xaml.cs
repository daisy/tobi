using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ListViewItem = System.Windows.Controls.ListViewItem;

namespace Tobi.Plugin.Urakawa
{
    public class XukSpineItemWrapper : INotifyPropertyChangedEx //, IDataErrorInfo
    {
        //public Uri Uri
        //{
        //    get;
        //    private set;
        //}

        public readonly XukSpineItemData Data;
        private readonly XukSpineView View;

        public XukSpineItemWrapper(XukSpineItemData data, int index, XukSpineView view)
        {
            Data = data;
            Index = index;
            View = view;

            m_PropertyChangeHandler = new PropertyChangedNotifyBase();
            m_PropertyChangeHandler.InitializeDependentProperties(this);
        }


        public int Index { get; private set; }

        //public bool CheckFileExists()
        //{
        //    m_FileFound = Uri.IsFile && File.Exists(Uri.LocalPath);

        //    //m_PropertyChangeHandler.RaisePropertyChanged(() => FileFound);
        //    //m_PropertyChangeHandler.RaisePropertyChanged(() => FullDescription);

        //    return m_FileFound.Value;
        //}

        //public void UpdateFileExists()
        //{
        //    m_PropertyChangeHandler.RaisePropertyChanged(() => FileFound);
        //    m_PropertyChangeHandler.RaisePropertyChanged(() => FullDescription);
        //}

        //[NotifyDependsOn("Uri")]
        [NotifyDependsOn("FilePath")]
        public string FullDescription
        {
            get
            {
                //string str = Uri.IsFile ? Uri.LocalPath : Uri.ToString();
                //if (!FileFound)
                //{
                //    str = "[" + Tobi_Common_Lang.NotFound + "] " + str;
                //}
                String filePath = FilePath;
                return "#" + Index + " " + ShortDescription + " [" + Size + "kB] (" + FileName + ") -- " + filePath;
            }
            private set { }
        }

        private bool? m_cachedMergedExist = null;
        public string FilePath
        {
            get
            {
                string str = Data.Uri.IsFile ? Data.Uri.LocalPath : Data.Uri.ToString();
                //if (!FileFound)
                //{
                //    str = "[" + Tobi_Common_Lang.NotFound + "] " + str;
                //}
                if (View.check.IsChecked.GetValueOrDefault())
                {
                    string parentDir = Path.GetDirectoryName(str);
                    string fileNameWithoutExtn = Path.GetFileNameWithoutExtension(str);

                    string mergedDirName = UrakawaSession.MERGE_PREFIX + @"_" + fileNameWithoutExtn;
                    string mergedDir = Path.Combine(parentDir, mergedDirName);

                    String filePath = Path.Combine(mergedDir, Path.GetFileName(str));

                    if (m_cachedMergedExist != null)
                    {
                        if (m_cachedMergedExist.GetValueOrDefault())
                        {
                            str = filePath;
                            m_SplitMerged = true;
                        }
                        else
                        {
                            m_SplitMerged = false;
                        }
                    }
                    else
                    {
                        if (File.Exists(filePath)) // Directory.Exists(mergedDir)
                        {
                            str = filePath;

                            m_cachedMergedExist = true;
                            m_SplitMerged = true;
                        }
                        else
                        {
                            m_cachedMergedExist = false;
                            m_SplitMerged = false;
                        }
                    }
                }
                else
                {
                    m_SplitMerged = false;
                }

                return str;
            }
        }

        private bool m_SplitMerged = false;

        [NotifyDependsOn("FilePath")]
        public bool SplitMerged
        {
            get
            {
                String filePath = FilePath;
                return m_SplitMerged;
            }
        }

        private decimal m_cachedSize = -1;

        //[NotifyDependsOn("FilePath")]
        [NotifyDependsOnEx("FilePath", typeof(XukSpineItemWrapper))]
        public decimal Size
        {
            get
            {
                if (m_cachedSize > -1)
                {
                    return m_cachedSize;
                }

                decimal size = 0;

                string path = FilePath;

                try
                {
                    var file = new FileInfo(path);
                    decimal kB = file.Length / (decimal)(1024.0); // * 1024.0
                    size = Math.Round(kB, 2, MidpointRounding.ToEven);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }

                m_cachedSize = size;
                return size;
            }
            private set { }
        }

        [NotifyDependsOn("FilePath")]
        public string FileName
        {
            get { return Path.GetFileName(FilePath).Replace(".xuk", ""); }
            private set { }
        }

        //STRUCT! readonly fields, not getter/setter [NotifyDependsOnEx("Title", typeof(XukSpineItemData))]
        [NotifyDependsOn("FileName")]
        [NotifyDependsOn("SplitMerged")]
        public string ShortDescription
        {
            get
            {
                String prefix = (SplitMerged ? "[" + Tobi_Plugin_Urakawa_Lang.Merged + "] " : "");
                if (!string.IsNullOrEmpty(Data.Title))
                {
                    return prefix + Data.Title; // + " (" + Path.GetFileName(FilePath).Replace(".xuk", "") + ")";
                }
                else
                {
                    return prefix + FileName; //Path.GetFileName(FilePath);
                }
            }
            private set { }
        }

        //private bool? m_FileFound = null;
        ////[NotifyDependsOn("Uri")]
        //public bool FileFound
        //{
        //    get
        //    {
        //        return m_FileFound == null || m_FileFound.Value;
        //    }
        //}

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

        public void Changed()
        {
            m_cachedMergedExist = null;
            m_cachedSize = -1;
            m_SplitMerged = false;
            m_PropertyChangeHandler.RaisePropertyChanged(() => FilePath);
        }

        private bool m_isMatch;
        public bool SearchMatch
        {
            get { return m_isMatch; }
            set
            {
                if (m_isMatch == value) { return; }
                m_isMatch = value;
                m_PropertyChangeHandler.RaisePropertyChanged(() => SearchMatch);
            }
        }

        private bool m_isSelected;
        public bool IsSelected
        {
            get { return m_isSelected; }
            set
            {
                if (m_isSelected == value) { return; }
                m_isSelected = value;
                m_PropertyChangeHandler.RaisePropertyChanged(() => IsSelected);
            }
        }


        //public string this[string propertyName]
        //{
        //    get
        //    {
        //        if (propertyName == PropertyChangedNotifyBase.GetMemberName(() => Value))
        //        {
        //            if (Value.GetType() == typeof(Double))
        //            {
        //                if ((Double)Value < 0.0
        //                    || (Double)Value > 10000.0)
        //                return "Invalid screen pixel value";
        //            }
        //        }

        //        return null; // no error
        //    }
        //}

        //public string Error
        //{
        //    get { throw new NotImplementedException(); }
        //}
    }

    [Export(typeof(XukSpineView)), PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class XukSpineView : IPartImportsSatisfiedNotification, INotifyPropertyChangedEx
    {
#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied()
#pragma warning restore 1591
        {
            trySearchCommands();

            //#if DEBUG
            //            Debugger.Break();
            //#endif
        }

        private readonly ILoggerFacade m_Logger;
        private readonly IShellView m_ShellView;
        private readonly IUrakawaSession m_Session;

        public ObservableCollection<XukSpineItemWrapper> XukSpineItems
        {
            get;
            private set;
        }

        [ImportingConstructor]
        public XukSpineView(
            ILoggerFacade logger,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession session)
        {
            m_PropertyChangeHandler = new PropertyChangedNotifyBase();
            m_PropertyChangeHandler.InitializeDependentProperties(this);

            m_Logger = logger;
            m_ShellView = shellView;
            m_Session = session;

            resetList();

            DataContext = this;
            InitializeComponent();

            intializeCommands();
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

        private void OnLoaded_Panel(object sender, RoutedEventArgs e)
        {
            var win = Window.GetWindow(this);
            if (win is PopupModalWindow)
                OwnerWindow = (PopupModalWindow)win;

            //m_interruptFileExistCheck = false;

            //foreach (var rf in XukSpineItems)
            //{
            //    if (m_interruptFileExistCheck) break;

            //    ThreadPool.QueueUserWorkItem(
            //        delegate(Object o) // or: (foo) => {} (LAMBDA)
            //        {
            //            var xukSpineItem = (XukSpineItemWrapper)o;

            //            //m_Logger.Log("... " + xukSpineItem.Uri, Category.Debug, Priority.High);

            //            bool exists = xukSpineItem.CheckFileExists(); // Can be time-consuming, because of network timeout.

            //            if (m_interruptFileExistCheck) return;

            //            //m_Logger.Log("EXISTS: " + exists, Category.Debug, Priority.High);

            //            if (!exists)
            //            {
            //                Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
            //                    (DispatcherOperationCallback)delegate(object recFile)
            //                    {
            //                        if (m_interruptFileExistCheck) return null;

            //                        var rec = (XukSpineItemWrapper)recFile;

            //                        //m_Logger.Log("UPDATE: " + rec.Uri.ToString(), Category.Debug, Priority.High);

            //                        rec.UpdateFileExists();
            //                        return null;
            //                    }, xukSpineItem);
            //                //Dispatcher.BeginInvoke(
            //                //    DispatcherPriority.Background,
            //                //    (MethodInvoker)(() =>
            //                //                        {

            //                //                        })
            //                //    ); // new Action(LAMBDA)
            //            }
            //        }, rf);
            //}
        }

        public RichDelegateCommand CmdFindNextGlobal { get; private set; }
        public RichDelegateCommand CmdFindPreviousGlobal { get; private set; }

        //private bool m_interruptFileExistCheck = false;

        private void OnUnloaded_Panel(object sender, RoutedEventArgs e)
        {
            //m_interruptFileExistCheck = true;

            if (m_GlobalSearchCommand != null)
            {
                m_GlobalSearchCommand.CmdFindFocus.UnregisterCommand(CommandFindFocus);
                m_GlobalSearchCommand.CmdFindNext.UnregisterCommand(CommandFindNext);
                m_GlobalSearchCommand.CmdFindPrevious.UnregisterCommand(CommandFindPrev);

                if (m_OwnerWindow != null)
                {
                    m_OwnerWindow.InputBindings.Remove(m_GlobalSearchCommand.CmdFindFocus.KeyBinding);
                    m_OwnerWindow.InputBindings.Remove(m_GlobalSearchCommand.CmdFindNext.KeyBinding);
                    m_OwnerWindow.InputBindings.Remove(m_GlobalSearchCommand.CmdFindPrevious.KeyBinding);
                }
            }
        }

        private void resetList()
        {
            XukSpineItems = new ObservableCollection<XukSpineItemWrapper>();

            if (m_Session.XukSpineItems == null || m_Session.XukSpineItems.Count <= 0) return;

            for (int i = 0; i < m_Session.XukSpineItems.Count; i++)
            //foreach (var fileUri in m_Session.XukSpineItems)
            {
                XukSpineItemData data = m_Session.XukSpineItems[i];
                XukSpineItems.Add(new XukSpineItemWrapper(data, i, this));
            }
        }

        private string m_SearchTerm;
        public string SearchTerm
        {
            get { return m_SearchTerm; }
            set
            {
                if (m_SearchTerm == value) { return; }
                m_SearchTerm = value;
                FlagSearchMatches();
                m_PropertyChangeHandler.RaisePropertyChanged(() => SearchTerm);
            }
        }
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e) { SearchTerm = SearchBox.Text; }

        private XukSpineItemWrapper FindNext(bool select)
        {
            XukSpineItemWrapper nextMatch = FindNextSetting();
            if (nextMatch != null)
            {
                if (select)
                {
                    nextMatch.IsSelected = true;
                }
                else
                {
                    var listItem = VisualLogicalTreeWalkHelper.FindObjectInVisualTreeWithMatchingType<ListViewItem>(
                        XukSpineItemsList,
                        child =>
                        {
                            object dc = child.GetValue(FrameworkElement.DataContextProperty);
                            return dc != null && dc == nextMatch;
                        });
                    if (listItem != null)
                    {
                        listItem.BringIntoView();
                    }
                }
            }
            else
            {
                AudioCues.PlayBeep();
            }
            return nextMatch;
        }

        private XukSpineItemWrapper FindPrevious(bool select)
        {
            XukSpineItemWrapper previousMatch = FindPrevSetting();
            if (previousMatch != null)
            {
                if (select)
                {
                    previousMatch.IsSelected = true;
                }
                else
                {
                    var listItem = VisualLogicalTreeWalkHelper.FindObjectInVisualTreeWithMatchingType<ListViewItem>(
                        XukSpineItemsList,
                        child =>
                        {
                            object dc = child.GetValue(FrameworkElement.DataContextProperty);
                            return dc != null && dc == previousMatch;
                        });
                    if (listItem != null)
                    {
                        listItem.BringIntoView();
                    }
                }
            }
            else
            {
                AudioCues.PlayBeep();
            }
            return previousMatch;
        }

        ~XukSpineView()
        {
#if DEBUG
            m_Logger.Log("XukSpineView garbage collected.", Category.Debug, Priority.Medium);
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

        public RichDelegateCommand CommandFindFocus { get; private set; }
        public RichDelegateCommand CommandFindNext { get; private set; }
        public RichDelegateCommand CommandFindPrev { get; private set; }
        private void intializeCommands()
        {
            m_Logger.Log("HeadingPaneViewModel.initializeCommands", Category.Debug, Priority.Medium);
            //
            CommandFindFocus = new RichDelegateCommand(
                @"SETTINGS CommandFindFocus DUMMY TXT",
                @"SETTINGS CommandFindFocus DUMMY TXT",
                null, // KeyGesture set only for the top-level CompositeCommand
                null,
                () =>
                {
                    m_ShellView.RaiseEscapeEvent();

                    FocusHelper.Focus(SearchBox);
                    SearchBox.SelectAll();
                },
                () => SearchBox.Visibility == Visibility.Visible,
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_PageFindNext)
                );
            //
            CommandFindNext = new RichDelegateCommand(
                @"SETTINGS CommandFindNext DUMMY TXT",
                @"SETTINGS CommandFindNext DUMMY TXT",
                null, // KeyGesture set only for the top-level CompositeCommand
                null,
                () =>
                {
                    m_ShellView.RaiseEscapeEvent();

                    FindNext(true);
                },
                () => !string.IsNullOrEmpty(SearchTerm),
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_PageFindNext)
                );
            CommandFindPrev = new RichDelegateCommand(
                @"SETTINGS CommandFindPrevious DUMMY TXT",
                @"SETTINGS CommandFindPrevious DUMMY TXT",
                null, // KeyGesture set only for the top-level CompositeCommand
                null,
                () =>
                {
                    m_ShellView.RaiseEscapeEvent();

                    FindPrevious(true);
                },
                () => !string.IsNullOrEmpty(SearchTerm),
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_PageFindNext)
                );
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

                if (m_GlobalSearchCommand == null) return;

                m_OwnerWindow.InputBindings.Add(m_GlobalSearchCommand.CmdFindFocus.KeyBinding);
                m_OwnerWindow.InputBindings.Add(m_GlobalSearchCommand.CmdFindNext.KeyBinding);
                m_OwnerWindow.InputBindings.Add(m_GlobalSearchCommand.CmdFindPrevious.KeyBinding);
            }
        }

        private void OnOwnerWindowIsActiveChanged(object sender, EventArgs e)
        {
            CommandFindFocus.IsActive = OwnerWindow.ActiveAware.IsActive;
            CommandFindNext.IsActive = OwnerWindow.ActiveAware.IsActive;
            CommandFindPrev.IsActive = OwnerWindow.ActiveAware.IsActive;

            CommandManager.InvalidateRequerySuggested();
        }

        private void FlagSearchMatches()
        {
            if (string.IsNullOrEmpty(SearchTerm))
            {
                foreach (XukSpineItemWrapper wrapper in XukSpineItems)
                {
                    wrapper.SearchMatch = false;
                }
                return;
            }

            bool atLeastOneFound = false;
            foreach (XukSpineItemWrapper wrapper in XukSpineItems)
            {
                bool found = !string.IsNullOrEmpty(wrapper.FullDescription)
                    && wrapper.FullDescription.IndexOf(SearchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
                wrapper.SearchMatch = found;
                if (found)
                {
                    atLeastOneFound = true;
                }
            }

            if (atLeastOneFound)
            {
                XukSpineItemWrapper sw = FindNext(false);
                if (sw == null)
                {
                    sw = FindPrevious(false);
                }
            }
        }

        private XukSpineItemWrapper FindNextSetting()
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(XukSpineItemsList.ItemsSource);
            IEnumerator enumerator = dataView.GetEnumerator();

            XukSpineItemWrapper firstMatch = null;
            while (enumerator.MoveNext())
            {
                var current = (XukSpineItemWrapper)enumerator.Current;
                if (current.SearchMatch && firstMatch == null)
                {
                    firstMatch = current;
                }
                if (!current.IsSelected)
                {
                    continue;
                }

                // from here we know we are after the selected item

                while (enumerator.MoveNext())
                {
                    current = (XukSpineItemWrapper)enumerator.Current;
                    if (current.SearchMatch)
                    {
                        return current; // the first match after the selected item
                    }
                }

                return null; // no match after => we don't cycle
            }

            return firstMatch; // no selection => first one that matched, if any
        }

        private XukSpineItemWrapper FindPrevSetting()
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(XukSpineItemsList.ItemsSource);
            IEnumerator enumerator = dataView.GetEnumerator();

            XukSpineItemWrapper lastMatch = null;
            while (enumerator.MoveNext())
            {
                var current = (XukSpineItemWrapper)enumerator.Current;

                if (current.IsSelected)
                {
                    return lastMatch;
                }

                if (current.SearchMatch)
                {
                    lastMatch = current;
                }
            }

            return lastMatch;
        }

        private void OnSearchBoxKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && CommandFindNext.CanExecute())
            {
                CommandFindNext.Execute();
            }
        }

        private void OnLoaded_ListView(object sender, RoutedEventArgs e)
        {
            FocusHelper.FocusBeginInvoke(check); //XukSpineItemsList
        }

        private void OnMouseDoubleClick_ListItem(object sender, MouseButtonEventArgs e)
        {
            var item = XukSpineItemsList.SelectedItem as XukSpineItemWrapper;
            if (item == null) return;

            if (OwnerWindow == null) return;

            OwnerWindow.ForceClose(PopupModalWindow.DialogButton.Ok);
        }

        private void OnExportClick(object sender, RoutedEventArgs e)
        {
            OwnerWindow.ForceClose(PopupModalWindow.DialogButton.Apply);
        }

        private void OnMergeClick(object sender, RoutedEventArgs e)
        {
            OwnerWindow.ForceClose(PopupModalWindow.DialogButton.Close);
        }

        private void OnSpineClick(object sender, RoutedEventArgs e)
        {
            OwnerWindow.ForceClose(PopupModalWindow.DialogButton.No);
        }

        public bool IsSpine
        {
            get { return m_Session.IsXukSpine; }
        }

        private void OnCheck(object sender, RoutedEventArgs e)
        {
            foreach (XukSpineItemWrapper wrapper in XukSpineItems)
            {
                wrapper.Changed();
            }

            //var listItem = VisualLogicalTreeWalkHelper.FindObjectInVisualTreeWithMatchingType<ListViewItem>(
            //               XukSpineItemsList,
            //               child =>
            //               {
            //                   child.InvalidateVisual();
            //                   //object dc = child.GetValue(FrameworkElement.DataContextProperty);
            //                   return false; // continue
            //               });

            //ICollectionView dataView = CollectionViewSource.GetDefaultView(XukSpineItemsList.ItemsSource);
            //dataView.Refresh();

            //XukSpineItemsList.Items.Refresh();
        }
    }
}
