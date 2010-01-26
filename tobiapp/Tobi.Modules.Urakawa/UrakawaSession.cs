using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa;
using urakawa.events;

namespace Tobi.Plugin.Urakawa
{
    ///<summary>
    /// Single shared instance (singleton) of a session to host the Urakawa SDK aurthoring data model.
    ///</summary>
    [Export(typeof(IUrakawaSession)), PartCreationPolicy(CreationPolicy.Shared)]
    public sealed partial class UrakawaSession : PropertyChangedNotifyBase, IUrakawaSession, IPartImportsSatisfiedNotification
    {
#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied()
#pragma warning restore 1591
        {
            //#if DEBUG
            //            Debugger.Break();
            //#endif
        }

        private readonly ILoggerFacade m_Logger;
        private readonly IEventAggregator m_EventAggregator;
        private readonly IShellView m_ShellView;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// No document is open and IsDirty is initialized to false.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity container, it's a built-in CAG service</param>
        ///<param name="eventAggregator">normally obtained from the Unity container, it's a built-in CAG service</param>
        ///<param name="shellView">normally obtained from the Unity container, it's a Tobi-specific entity</param>
        [ImportingConstructor]
        public UrakawaSession(
            ILoggerFacade logger,
            IEventAggregator eventAggregator,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView)
        {
            m_Logger = logger;
            m_EventAggregator = eventAggregator;
            m_ShellView = shellView;

            IsDirty = false;

            InitializeCommands();
        }

#pragma warning disable 1591 // missing comments
        //public RichDelegateCommand NewCommand { get; private set; }

        public RichDelegateCommand OpenCommand { get; private set; }

        public RichDelegateCommand SaveCommand { get; private set; }
        public RichDelegateCommand SaveAsCommand { get; private set; }

        public RichDelegateCommand ExportCommand { get; private set; }

        public RichDelegateCommand CloseCommand { get; private set; }

        public RichDelegateCommand UndoCommand { get; private set; }
        public RichDelegateCommand RedoCommand { get; private set; }
#pragma warning restore 1591

        private Project m_DocumentProject;
        public Project DocumentProject
        {
            get { return m_DocumentProject; }
            set
            {
                if (m_DocumentProject == value)
                {
                    return;
                }
                if (m_DocumentProject != null)
                {
                    m_DocumentProject.Changed -= OnDocumentProjectChanged;
                    //m_DocumentProject.Presentations.Get(0).UndoRedoManager.Changed -= OnUndoRedoManagerChanged;
                }

                IsDirty = false;
                m_DocumentProject = value;
                if (m_DocumentProject != null)
                {
                    m_DocumentProject.Changed += OnDocumentProjectChanged;
                    //m_DocumentProject.Presentations.Get(0).UndoRedoManager.Changed += OnUndoRedoManagerChanged;
                }
                RaisePropertyChanged(() => DocumentProject);
            }
        }

        //private void OnUndoRedoManagerChanged(object sender, DataModelChangedEventArgs e)
        //{
        //    IsDirty = m_DocumentProject.Presentations.Get(0).UndoRedoManager.CanUndo;
        //}

        private void OnDocumentProjectChanged(object sender, DataModelChangedEventArgs e)
        {
            IsDirty = true;
        }

        private string m_DocumentFilePath;
        [NotifyDependsOn("DocumentProject")]
        public string DocumentFilePath
        {
            get { return m_DocumentFilePath; }
            set
            {
                if (m_DocumentFilePath == value)
                {
                    return;
                }
                m_DocumentFilePath = value;
                RaisePropertyChanged(() => DocumentFilePath);
            }
        }

        private bool m_IsDirty;
        public bool IsDirty
        {
            get { return m_IsDirty; }
            set
            {
                if (m_IsDirty == value)
                {
                    return;
                }
                m_IsDirty = value;
                RaisePropertyChanged(() => IsDirty);
            }
        }

        internal void InitializeCommands()
        {
            initCommands_Open();
            initCommands_Save();

            //
            UndoCommand = new RichDelegateCommand(
                UserInterfaceStrings.Undo,
                UserInterfaceStrings.Undo_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon(@"edit-undo"),
                () => DocumentProject.Presentations.Get(0).UndoRedoManager.Undo(),
                () => DocumentProject != null && DocumentProject.Presentations.Get(0).UndoRedoManager.CanUndo,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Undo));

            m_ShellView.RegisterRichCommand(UndoCommand);
            //
            RedoCommand = new RichDelegateCommand(
                UserInterfaceStrings.Redo,
                UserInterfaceStrings.Redo_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon(@"edit-redo"),
                () => DocumentProject.Presentations.Get(0).UndoRedoManager.Redo(),
                () => DocumentProject != null && DocumentProject.Presentations.Get(0).UndoRedoManager.CanRedo,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Redo));

            m_ShellView.RegisterRichCommand(RedoCommand);
            //
            CloseCommand = new RichDelegateCommand(
                UserInterfaceStrings.Close,
                UserInterfaceStrings.Close_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon(@"go-jump"),
                () => Close(),
                () => DocumentProject != null,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Close));

            m_ShellView.RegisterRichCommand(CloseCommand);
        }

        public bool Close()
        {
            if (DocumentProject == null)
            {
                return true;
            }

            if (IsDirty)
            {
                m_Logger.Log(@"UrakawaSession.askUserSave", Category.Debug, Priority.Medium);

                var label = new TextBlock
                {
                    Text = UserInterfaceStrings.UnsavedChangesConfirm,
                    Margin = new Thickness(8, 0, 8, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Focusable = true,
                    TextWrapping = TextWrapping.Wrap
                };

                var iconProvider = new ScalableGreyableImageProvider(m_ShellView.LoadTangoIcon(@"help-browser"),
                                                                     m_ShellView.MagnificationLevel);
                //var zoom = (Double)Resources["MagnificationLevel"]; //Application.Current.

                var panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Stretch,
                };
                panel.Children.Add(iconProvider.IconLarge);
                panel.Children.Add(label);
                //panel.Margin = new Thickness(8, 8, 8, 0);

                var details = new TextBoxReadOnlyCaretVisible(UserInterfaceStrings.UnsavedChangesDetails);

                var windowPopup = new PopupModalWindow(m_ShellView,
                                                       UserInterfaceStrings.EscapeMnemonic(
                                                           UserInterfaceStrings.UnsavedChanges),
                                                       panel,
                                                       PopupModalWindow.DialogButtonsSet.YesNoCancel,
                                                       PopupModalWindow.DialogButton.Cancel,
                                                       false, 300, 160, details, 40);

                windowPopup.ShowModal();

                if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Cancel)
                {
                    return false;
                }

                if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Yes)
                {
                    if (!save())
                    {
                        return false;
                    }
                }
            }

            m_Logger.Log(@"-- PublishEvent [ProjectUnLoadedEvent] UrakawaSession.close", Category.Debug, Priority.Medium);

            m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Publish(DocumentProject);

            DocumentFilePath = null;
            DocumentProject = null;

            return true;
        }
    }
}
