using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;

namespace Tobi.Plugin.Settings
{
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
    [Export(typeof(SettingsView)), PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class SettingsView : IPartImportsSatisfiedNotification, INotifyPropertyChangedEx
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

        public readonly ISettingsAggregator m_SettingsAggregator;

        public List<SettingWrapper> AggregatedSettings
        {
            get;
            private set;
        }

        [ImportingConstructor]
        public SettingsView(
            ILoggerFacade logger,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView,
            [Import(typeof(ISettingsAggregator), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = false)]
            ISettingsAggregator settingsAggregator)
        {
            m_PropertyChangeHandler = new PropertyChangedNotifyBase();
            m_PropertyChangeHandler.InitializeDependentProperties(this);

            m_Logger = logger;
            m_ShellView = shellView;

            m_SettingsAggregator = settingsAggregator;

            resetList();

            InitializeComponent();
            intializeCommands();
            // NEEDS to be after InitializeComponent() in order for the DataContext bridge to work.
            DataContext = this;
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var settingBase = (ApplicationSettingsBase)sender;
            string name = e.PropertyName;

            foreach (var aggregatedSetting in AggregatedSettings)
            {
                if (aggregatedSetting.Name == name
                    && aggregatedSetting.m_settingBase == settingBase)
                {
                    aggregatedSetting.NotifyValueChanged();
                }
            }
        }

        public bool HasValidationErrors
        {
            get { return m_nErrors > 0; }
        }

        private int m_nErrors = 0;

        private void OnError(object sender, ValidationErrorEventArgs e)
        {
            if (e.Action == ValidationErrorEventAction.Added)
                m_nErrors++;
            else
                m_nErrors--;

            m_PropertyChangeHandler.RaisePropertyChanged(() => HasValidationErrors);

            //var element = sender as FrameworkElement;
            //if (element == null) return;

            //var settingWrapper = element.DataContext as SettingWrapper;
            //if (settingWrapper == null) return;

            //settingWrapper.NotifyValueChanged();
        }


        private void OnKeyUp_ListItem(object sender, KeyEventArgs e)
        {
            var key = (e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));

            // We capture only the ESCAPE KeyUp bubbling-up from UI descendants
            if (key != Key.Escape || !(sender is ListViewItem))
            {
                return;
            }

            // We re-focus on the ListItem,
            // only if the Key press happened within one of the known editors
            if (true || // Let's do it all the time !
                e.OriginalSource is KeyGestureSinkBox
                || e.OriginalSource is CheckBox
                || e.OriginalSource is TextBox
                || e.OriginalSource is ComboBoxColor)
            {
                FocusHelper.Focus((UIElement)sender);

                // We void the effect of the ESCAPE key
                // (which would normally close the parent dialog window: CANCEL action)
                e.Handled = true;
            }
        }

        private void OnKeyDown_ListItem(object sender, KeyEventArgs e)
        {
            var key = (e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));

            // We capture only the RETURN KeyUp bubbling-up from UI descendants
            if (key != Key.Return || !(sender is ListViewItem))
            {
                return;
            }

            // If RETURN was pressed on the ListItem itself,
            // then we focus down into the editor UI (using a TAB gesture)
            if (e.OriginalSource is ListViewItem)
            {
                //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(FUNC));
                Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    (Action)(() =>
                    {
                        var ev = new KeyEventArgs(
                            Keyboard.PrimaryDevice,
                            Keyboard.PrimaryDevice.ActiveSource, //PresentationSource.FromVisual(this),
                            0, //Environment.TickCount, //(int)new DateTime(Environment.TickCount, DateTimeKind.Utc).Ticks
                            Key.Tab) { RoutedEvent = Keyboard.KeyDownEvent };

                        //RaiseEvent(ev);
                        //OnKeyDown(ev);

                        InputManager.Current.ProcessInput(ev);
                    }));

                // We void the effect of the RETURN key
                // (which would normally close the parent dialog window by activating the default button: CANCEL)
                e.Handled = true;
            }
            // If RETURN was pressed on one of the editors,
            // then we re-focus on the ListItem
            else if (true || // Let's do it all the time !
                e.OriginalSource is KeyGestureSinkBox
                || e.OriginalSource is CheckBox
                || e.OriginalSource is TextBox
                || e.OriginalSource is ComboBoxColor)
            {
                FocusHelper.Focus((UIElement)sender);

                // We void the effect of the RETURN key
                // (which would normally close the parent dialog window by activating the default button: CANCEL)
                e.Handled = true;
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

        [Conditional("DEBUG")]
        public void CheckParseScanWalkUiTreeThing()
        {
            Stopwatch startRecursiveDepth = Stopwatch.StartNew();
            VisualLogicalTreeWalkHelper.GetElements(this, false, false, false);
            startRecursiveDepth.Stop();
            TimeSpan timeRecursiveDepth = startRecursiveDepth.Elapsed;

            Stopwatch startRecursiveLeaf = Stopwatch.StartNew();
            VisualLogicalTreeWalkHelper.GetElements(this, false, true, false);
            startRecursiveLeaf.Stop();
            TimeSpan timeRecursiveLeaf = startRecursiveLeaf.Elapsed;

            Stopwatch startNonRecursiveDepth = Stopwatch.StartNew();
            VisualLogicalTreeWalkHelper.GetElements(this, true, false, false);
            startNonRecursiveDepth.Stop();
            TimeSpan timeNonRecursiveDepth = startNonRecursiveDepth.Elapsed;

            Stopwatch startNonRecursiveLeaf = Stopwatch.StartNew();
            VisualLogicalTreeWalkHelper.GetElements(this, true, true, false);
            startNonRecursiveLeaf.Stop();
            TimeSpan timeNonRecursiveLeaf = startNonRecursiveLeaf.Elapsed;

#if DEBUG
            int nVisualLeafNoError = ValidationErrorTreeSearch.CheckTreeWalking(this, false, true, false);
            int nVisualDepthNoError = ValidationErrorTreeSearch.CheckTreeWalking(this, false, false, false);

            int nLogicalLeafNoError = ValidationErrorTreeSearch.CheckTreeWalking(this, false, true, true);
            int nLogicalDepthNoError = ValidationErrorTreeSearch.CheckTreeWalking(this, false, false, true);

            MessageBox.Show(String.Format(
                "VisualLeafNoError={0}"
                + Environment.NewLine
                + "VisualDepthNoError={1}"
                + Environment.NewLine
                + "LogicalLeafNoError={2}"
                + Environment.NewLine
                + "LogicalDepthNoError={3}"
                + Environment.NewLine + Environment.NewLine
                + "timeNonRecursiveDepth={4}"
                + Environment.NewLine
                + "timeNonRecursiveLeaf={5}"
                + Environment.NewLine
                + "timeRecursiveDepth={6}"
                + Environment.NewLine
                + "timeRecursiveLeaf={7}"
                + Environment.NewLine
                , nVisualLeafNoError, nVisualDepthNoError, nLogicalLeafNoError, nLogicalDepthNoError, timeNonRecursiveDepth, timeNonRecursiveLeaf, timeRecursiveDepth, timeRecursiveLeaf));
#endif
        }

        //private void OnLoaded_Panel(object sender, RoutedEventArgs e)
        //{
        //    //CheckParseScanWalkUiTreeThing();

        //    //var dispatcherOperation = Application.Current.Dispatcher.BeginInvoke(
        //    //       DispatcherPriority.Normal,
        //    //       (Action)delegate()
        //    //       {
        //    //           IEnumerable<DependencyObject> enm = ValidationErrorTreeSearch.GetElementsWithErrors(SettingsList, false, false, false);
        //    //           var list = new List<DependencyObject>(enm);
        //    //           var size = list.Count;
        //    //       });

        //    foreach (var settingWrapper in AggregatedSettings)
        //    {
        //        settingWrapper.NotifyValueChanged();
        //    }
        //}

        private void OnUnloaded_Panel(object sender, RoutedEventArgs e)
        {
            if (m_GlobalSearchCommand != null)
            {
                m_GlobalSearchCommand.CmdFindFocus.UnregisterCommand(CommandFindFocus);
                m_GlobalSearchCommand.CmdFindNext.UnregisterCommand(CommandFindNext);
                m_GlobalSearchCommand.CmdFindPrevious.UnregisterCommand(CommandFindPrev);
            }

            foreach (var settingsProvider in m_SettingsAggregator.Settings)
            {
                settingsProvider.PropertyChanged -= Settings_PropertyChanged;
            }

            //DataContext = null;
        }

        private void resetList()
        {
            AggregatedSettings = new List<SettingWrapper>();

            foreach (var settingsProvider in m_SettingsAggregator.Settings)
            {
                settingsProvider.PropertyChanged += Settings_PropertyChanged;

                SettingsPropertyCollection col1 = settingsProvider.Properties;
                IEnumerator enume1 = col1.GetEnumerator();
                while (enume1.MoveNext())
                {
                    var current = (SettingsProperty)enume1.Current;
                    AggregatedSettings.Add(new SettingWrapper(settingsProvider, current));

                    //(current.IsReadOnly ? "[readonly] " : "")
                    //+ current.Name + " = " + settingsProvider.Settings[current.Name]
                    // + "(" + current.DefaultValue + ")
                    // [" + current.PropertyType + "] ");
                }
            }

            //SettingsExpanded.Add("---");

            //foreach (var settingsProvider in SettingsProviders)
            //{
            //    SettingsPropertyValueCollection col2 = settingsProvider.Settings.PropertyValues;
            //    IEnumerator enume2 = col2.GetEnumerator();
            //    while (enume2.MoveNext())
            //    {
            //        var current = (SettingsPropertyValue)enume2.Current;
            //        SettingsExpanded.Add(current.Name + " = " + current.PropertyValue + "---" + current.SerializedValue + " (" + (current.UsingDefaultValue ? "default" : "user") + ")");
            //    }
            //}
        }

        //private void OnItemEditorLoaded(object sender, RoutedEventArgs e)
        //{
        //    var depObj = sender as DependencyObject;
        //    if (depObj == null)
        //    {
        //        return;
        //    }

        //    ValidationErrorTreeSearch.CheckAllValidationErrors(depObj);
        //}
        private string m_SearchTerm;
        public string SearchTerm
        {
            get { return m_SearchTerm; }
            set
            {
                if (m_SearchTerm == value) { return; }
                m_SearchTerm = value;
                FlagSearchMatches(AggregatedSettings, m_SearchTerm);
                m_PropertyChangeHandler.RaisePropertyChanged(() => SearchTerm);
            }
        }
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e) { SearchTerm = SearchBox.Text; }

        private void FindNext()
        {
            SettingWrapper nextMatch = FindNextSetting(AggregatedSettings);
            if (nextMatch != null) { nextMatch.IsSelected = true; } else { AudioCues.PlayAsterisk(); }

        }
        private void FindPrevious()
        {
            SettingWrapper nextMatch = FindPrevSetting(AggregatedSettings);
            if (nextMatch != null) { nextMatch.IsSelected = true; } else { AudioCues.PlayAsterisk(); }
        }

        ~SettingsView()
        {
#if DEBUG
            m_Logger.Log("SettingsView garbage collected.", Category.Debug, Priority.Medium);
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

            InputBindings.Add(m_GlobalSearchCommand.CmdFindFocus.KeyBinding);
            InputBindings.Add(m_GlobalSearchCommand.CmdFindNext.KeyBinding);
            InputBindings.Add(m_GlobalSearchCommand.CmdFindPrevious.KeyBinding);
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
                () => FocusHelper.Focus(SearchBox),
                () => OwnerWindow.ActiveAware.IsActive && SearchBox.Visibility == Visibility.Visible,
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_PageFindNext)
                );
            //
            CommandFindNext = new RichDelegateCommand(
                @"SETTINGS CommandFindNext DUMMY TXT",
                @"SETTINGS CommandFindNext DUMMY TXT",
                null, // KeyGesture set only for the top-level CompositeCommand
                null,
                FindNext,
                () => !string.IsNullOrEmpty(SearchTerm),
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_PageFindNext)
                );
            CommandFindPrev = new RichDelegateCommand(
                @"SETTINGS CommandFindPrevious DUMMY TXT",
                @"SETTINGS CommandFindPrevious DUMMY TXT",
                null, // KeyGesture set only for the top-level CompositeCommand
                null,
                FindPrevious,
                () => !string.IsNullOrEmpty(SearchTerm),
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_PageFindNext)
                );
        }

        private PopupModalWindow m_OwnerWindow;
        public PopupModalWindow OwnerWindow
        {
            get { return m_OwnerWindow; }
            set
            {
                if (m_OwnerWindow != null)
                {
                    m_OwnerWindow.ActiveAware.IsActiveChanged -= OnOwnerWindowIsActiveChanged;
                }
                m_OwnerWindow = value;
                if (m_OwnerWindow == null) return;
                m_OwnerWindow.ActiveAware.IsActiveChanged += OnOwnerWindowIsActiveChanged;
            }
        }

        private void OnOwnerWindowIsActiveChanged(object sender, EventArgs e)
        {
            //var focusAware = new FocusActiveAwareAdapter(this);
            // ALWAYS ACTIVE ! CommandFindFocusPage.IsActive = focusAware.IsActive;
            CommandFindNext.IsActive = OwnerWindow.ActiveAware.IsActive; // && focusAware.IsActive;
            CommandFindPrev.IsActive = OwnerWindow.ActiveAware.IsActive; // && focusAware.IsActive;
        }

        private static void FlagSearchMatches(List<SettingWrapper> settingWrappers, string searchTerm)
        {
            foreach (SettingWrapper wrapper in settingWrappers)
            {
                wrapper.SearchMatch = !string.IsNullOrEmpty(searchTerm) &&
                   !string.IsNullOrEmpty(wrapper.Name) &&
                   wrapper.Name.ToLower().Contains(searchTerm.ToLower());
            }
        }

        private static SettingWrapper FindNextSetting(List<SettingWrapper> settingWrappers)
        {
            SettingWrapper pResult = null;
            int iStarting = -1;
            for (int i = 0; i < settingWrappers.Count; i++)
            {
                if (settingWrappers[i].SearchMatch && iStarting == -1) { iStarting = i; }
                if (!settingWrappers[i].IsSelected) { continue; }
                iStarting = i;
                break;
            }
            if (iStarting < 0) { return null; }
            if (!settingWrappers[iStarting].IsSelected && settingWrappers[iStarting].SearchMatch) { pResult = settingWrappers[iStarting]; }
            if (pResult == null)
            {
                for (int i = iStarting + 1; i < settingWrappers.Count; i++)
                {
                    if (!settingWrappers[i].SearchMatch)
                        continue;
                    pResult = settingWrappers[i];
                    break;
                }
            }
            return pResult;
        }

        private static SettingWrapper FindPrevSetting(List<SettingWrapper> settingWrappers)
        {
            SettingWrapper pResult = null;
            int iStarting = -1;
            for (int i = settingWrappers.Count - 1; i >= 0; i--)
            {
                if (settingWrappers[i].SearchMatch && iStarting == -1) { iStarting = i; }
                if (!settingWrappers[i].IsSelected) { continue; }
                iStarting = i;
                break;
            }
            if (iStarting < 0) { return null; }
            if (!settingWrappers[iStarting].IsSelected && settingWrappers[iStarting].SearchMatch) { pResult = settingWrappers[iStarting]; }
            if (pResult == null)
            {
                for (int i = iStarting - 1; i >= 0; i--)
                {
                    if (!settingWrappers[i].SearchMatch)
                        continue;
                    pResult = settingWrappers[i];
                    break;
                }
            }
            return pResult;
        }
    }
}
