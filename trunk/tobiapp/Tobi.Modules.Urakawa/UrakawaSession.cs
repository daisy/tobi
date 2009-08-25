using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa;
using urakawa.events;

namespace Tobi.Modules.Urakawa
{
    ///<summary>
    ///</summary>
    public partial class UrakawaSession : PropertyChangedNotifyBase, IUrakawaSession
    {
        protected ILoggerFacade Logger { get; private set; }
        protected IRegionManager RegionManager { get; private set; }

        protected IUnityContainer Container { get; private set; }
        protected IEventAggregator EventAggregator { get; private set; }

        public RichDelegateCommand<object> SaveAsCommand { get; private set; }
        public RichDelegateCommand<object> SaveCommand { get; private set; }

        public RichDelegateCommand<object> NewCommand { get; private set; }
        public RichDelegateCommand<object> OpenCommand { get; private set; }
        public RichDelegateCommand<object> CloseCommand { get; private set; }

        public RichDelegateCommand<object> UndoCommand { get; private set; }
        public RichDelegateCommand<object> RedoCommand { get; private set; }

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

        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        public UrakawaSession(IUnityContainer container,
                            ILoggerFacade logger,
                            IRegionManager regionManager,
                            IEventAggregator eventAggregator)
        {
            Container = container;
            Logger = logger;
            RegionManager = regionManager;
            EventAggregator = eventAggregator;

            IsDirty = false;

            initCommands();
        }

        private void initCommands()
        {
            initCommands_Open();
            initCommands_Save();

            var shellPresenter = Container.Resolve<IShellPresenter>();

            //
            UndoCommand = new RichDelegateCommand<object>(
                UserInterfaceStrings.Undo,
                UserInterfaceStrings.Undo_,
                UserInterfaceStrings.Undo_KEYS,
                shellPresenter.LoadTangoIcon("edit-undo"),
                obj => DocumentProject.Presentations.Get(0).UndoRedoManager.Undo(),
                obj => DocumentProject != null && DocumentProject.Presentations.Get(0).UndoRedoManager.CanUndo);

            shellPresenter.RegisterRichCommand(UndoCommand);
            //
            RedoCommand = new RichDelegateCommand<object>(
                UserInterfaceStrings.Redo,
                UserInterfaceStrings.Redo_,
                UserInterfaceStrings.Redo_KEYS,
                shellPresenter.LoadTangoIcon("edit-redo"),
                obj => DocumentProject.Presentations.Get(0).UndoRedoManager.Redo(),
                obj => DocumentProject != null && DocumentProject.Presentations.Get(0).UndoRedoManager.CanRedo);

            shellPresenter.RegisterRichCommand(RedoCommand);
            //
            CloseCommand = new RichDelegateCommand<object>(
                UserInterfaceStrings.Close,
                UserInterfaceStrings.Close_,
                UserInterfaceStrings.Close_KEYS,
                shellPresenter.LoadTangoIcon("go-jump"),
                obj => Close(),
                obj => DocumentProject != null);

            shellPresenter.RegisterRichCommand(CloseCommand);
        }

        public bool Close()
        {
            if (DocumentProject == null)
            {
                return true;
            }

            if (IsDirty)
            {
                Logger.Log("UrakawaSession.askUserSave", Category.Debug, Priority.Medium);

                var shellPresenter = Container.Resolve<IShellPresenter>();
                //var window = shellPresenter.View as Window;

                var label = new TextBlock
                {
                    Text = UserInterfaceStrings.UnsavedChangesConfirm,
                    Margin = new Thickness(8, 0, 8, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Focusable = false,
                    TextWrapping = TextWrapping.Wrap
                };

                var iconProvider = new ScalableGreyableImageProvider(shellPresenter.LoadTangoIcon("help-browser"))
                                       {
                                           IconDrawScale = shellPresenter.View.MagnificationLevel
                                       };
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

                var details = new TextBox
                {
                    Text = UserInterfaceStrings.UnsavedChangesDetails,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    TextWrapping = TextWrapping.Wrap,
                    Background = SystemColors.ControlLightLightBrush,
                    BorderBrush = SystemColors.ControlDarkDarkBrush,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(6),
                    SnapsToDevicePixels = true
                };

                FocusHelper.ConfigureReadOnlyTextBoxHack(details, details.Text, new FocusHelper.TextBoxSelection());

                var windowPopup = new PopupModalWindow(shellPresenter,
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

            Logger.Log("-- PublishEvent [ProjectUnLoadedEvent] UrakawaSession.close", Category.Debug, Priority.Medium);

            EventAggregator.GetEvent<ProjectUnLoadedEvent>().Publish(DocumentProject);

            DocumentFilePath = null;
            DocumentProject = null;

            return true;
        }
    }
}
