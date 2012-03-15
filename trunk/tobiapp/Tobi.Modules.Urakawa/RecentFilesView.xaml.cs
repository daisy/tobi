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
    public class RecentFileWrapper : INotifyPropertyChangedEx //, IDataErrorInfo
    {
        //public Uri Uri
        //{
        //    get;
        //    private set;
        //}

        public readonly Uri Uri;

        public RecentFileWrapper(Uri uri)
        {
            Uri = uri;

            m_PropertyChangeHandler = new PropertyChangedNotifyBase();
            m_PropertyChangeHandler.InitializeDependentProperties(this);
        }

        public bool CheckFileExists()
        {
            m_FileFound = Uri.IsFile && File.Exists(Uri.LocalPath);

            //m_PropertyChangeHandler.RaisePropertyChanged(() => FileFound);
            //m_PropertyChangeHandler.RaisePropertyChanged(() => FullDescription);

            return m_FileFound.Value;
        }

        public void UpdateFileExists()
        {
            m_PropertyChangeHandler.RaisePropertyChanged(() => FileFound);
            m_PropertyChangeHandler.RaisePropertyChanged(() => FullDescription);
        }

        //[NotifyDependsOn("Uri")]
        public string FullDescription
        {
            get
            {
                string str = Uri.IsFile ? Uri.LocalPath : Uri.ToString();
                if (!FileFound)
                {
                    str = "[" + Tobi_Common_Lang.NotFound + "] " + str;
                }
                return str;
            }
        }

        private bool? m_FileFound = null;
        //[NotifyDependsOn("Uri")]
        public bool FileFound
        {
            get
            {
                return m_FileFound == null || m_FileFound.Value;
            }
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

        private bool m_isChecked;
        public bool IsChecked
        {
            get { return m_isChecked; }
            set
            {
                if (m_isChecked == value) { return; }
                m_isChecked = value;
                m_PropertyChangeHandler.RaisePropertyChanged(() => IsChecked);
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

    [Export(typeof(RecentFilesView)), PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class RecentFilesView : IPartImportsSatisfiedNotification, INotifyPropertyChangedEx
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

        public ObservableCollection<RecentFileWrapper> RecentFiles
        {
            get;
            private set;
        }

        [ImportingConstructor]
        public RecentFilesView(
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

            m_interruptFileExistCheck = false;

            foreach (var rf in RecentFiles)
            {
                if (m_interruptFileExistCheck) break;

                ThreadPool.QueueUserWorkItem(
                    delegate(Object o) // or: (foo) => {} (LAMBDA)
                    {
                        var recentFile = (RecentFileWrapper)o;

                        //m_Logger.Log("... " + recentFile.Uri, Category.Debug, Priority.High);

                        bool exists = recentFile.CheckFileExists(); // Can be time-consuming, because of network timeout.

                        if (m_interruptFileExistCheck) return;

                        //m_Logger.Log("EXISTS: " + exists, Category.Debug, Priority.High);

                        if (!exists)
                        {
                            Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
                                (DispatcherOperationCallback)delegate(object recFile)
                                {
                                    if (m_interruptFileExistCheck) return null;

                                    var rec = (RecentFileWrapper)recFile;

                                    //m_Logger.Log("UPDATE: " + rec.Uri.ToString(), Category.Debug, Priority.High);

                                    rec.UpdateFileExists();
                                    return null;
                                }, recentFile);
                            //Dispatcher.BeginInvoke(
                            //    DispatcherPriority.Background,
                            //    (MethodInvoker)(() =>
                            //                        {

                            //                        })
                            //    ); // new Action(LAMBDA)
                        }
                    }, rf);
            }
        }

        public RichDelegateCommand CmdFindNextGlobal { get; private set; }
        public RichDelegateCommand CmdFindPreviousGlobal { get; private set; }

        private bool m_interruptFileExistCheck = false;

        private void OnUnloaded_Panel(object sender, RoutedEventArgs e)
        {
            m_interruptFileExistCheck = true;

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
            RecentFiles = new ObservableCollection<RecentFileWrapper>();

            if (m_Session.RecentFiles.Count <= 0) return;

            for (int i = m_Session.RecentFiles.Count - 1; i >= 0; i--)
            //foreach (var recentFileUri in m_Session.RecentFiles)
            {
                var recentFileUri = m_Session.RecentFiles[i];
                RecentFiles.Add(new RecentFileWrapper(recentFileUri));
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

        private RecentFileWrapper FindNext(bool select)
        {
            RecentFileWrapper nextMatch = FindNextSetting();
            if (nextMatch != null)
            {
                if (select)
                {
                    nextMatch.IsSelected = true;
                }
                else
                {
                    var listItem = VisualLogicalTreeWalkHelper.FindObjectInVisualTreeWithMatchingType<ListViewItem>(
                        RecentFilesList,
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

        private RecentFileWrapper FindPrevious(bool select)
        {
            RecentFileWrapper previousMatch = FindPrevSetting();
            if (previousMatch != null)
            {
                if (select)
                {
                    previousMatch.IsSelected = true;
                }
                else
                {
                    var listItem = VisualLogicalTreeWalkHelper.FindObjectInVisualTreeWithMatchingType<ListViewItem>(
                        RecentFilesList,
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

        ~RecentFilesView()
        {
#if DEBUG
            m_Logger.Log("RecentFilesView garbage collected.", Category.Debug, Priority.Medium);
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
                () => FindNext(true),
                () => !string.IsNullOrEmpty(SearchTerm),
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_PageFindNext)
                );
            CommandFindPrev = new RichDelegateCommand(
                @"SETTINGS CommandFindPrevious DUMMY TXT",
                @"SETTINGS CommandFindPrevious DUMMY TXT",
                null, // KeyGesture set only for the top-level CompositeCommand
                null,
                () => FindPrevious(true),
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
                foreach (RecentFileWrapper wrapper in RecentFiles)
                {
                    wrapper.SearchMatch = false;
                }
                return;
            }

            bool atLeastOneFound = false;
            foreach (RecentFileWrapper wrapper in RecentFiles)
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
                RecentFileWrapper sw = FindNext(false);
                if (sw == null)
                {
                    sw = FindPrevious(false);
                }
            }
        }

        private RecentFileWrapper FindNextSetting()
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(RecentFilesList.ItemsSource);
            IEnumerator enumerator = dataView.GetEnumerator();

            RecentFileWrapper firstMatch = null;
            while (enumerator.MoveNext())
            {
                var current = (RecentFileWrapper)enumerator.Current;
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
                    current = (RecentFileWrapper)enumerator.Current;
                    if (current.SearchMatch)
                    {
                        return current; // the first match after the selected item
                    }
                }

                return null; // no match after => we don't cycle
            }

            return firstMatch; // no selection => first one that matched, if any
        }

        private RecentFileWrapper FindPrevSetting()
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(RecentFilesList.ItemsSource);
            IEnumerator enumerator = dataView.GetEnumerator();

            RecentFileWrapper lastMatch = null;
            while (enumerator.MoveNext())
            {
                var current = (RecentFileWrapper)enumerator.Current;

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

        private void OnCheckAll(object sender, RoutedEventArgs e)
        {
            foreach (var wrapper in RecentFiles)
            {
                wrapper.IsChecked = true;
            }
        }
        private void OnUnCheckAll(object sender, RoutedEventArgs e)
        {
            foreach (var wrapper in RecentFiles)
            {
                wrapper.IsChecked = false;
            }
        }

        private void OnClick_DeleteRecentFile(object sender, RoutedEventArgs e)
        {
            var listToRemove = new List<RecentFileWrapper>(RecentFiles);
            foreach (var wrapper in listToRemove)
            {
                if (wrapper.IsChecked)
                {
                    int index = RecentFiles.IndexOf(wrapper);
                    foreach (var uri in m_Session.RecentFiles)
                    {
                        if (uri.ToString() == wrapper.Uri.ToString())
                        {
                            int index_ = m_Session.RecentFiles.IndexOf(uri);
                            //DebugFix.Assert(index == index_);
                            index = index_;
                            break;
                        }
                    }
                    m_Session.RecentFiles.RemoveAt(index);
                    RecentFiles.Remove(wrapper);
                }
            }
            m_Session.SaveRecentFiles();
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
            FocusHelper.FocusBeginInvoke(RecentFilesList);
        }

        private void OnMouseDoubleClick_ListItem(object sender, MouseButtonEventArgs e)
        {
            var recent = RecentFilesList.SelectedItem as RecentFileWrapper;
            if (recent == null) return;

            if (OwnerWindow == null) return;

            OwnerWindow.ForceClose(PopupModalWindow.DialogButton.Ok);
        }
    }
}
