using System;
using System.Diagnostics;
using System.IO;
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
using urakawa.property.channel;
using urakawa.publish;
using urakawa.xuk;

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

        public RichDelegateCommand SaveAsCommand { get; private set; }
        public RichDelegateCommand SaveCommand { get; private set; }

        public RichDelegateCommand ExportCommand { get; private set; }

        public RichDelegateCommand NewCommand { get; private set; }
        public RichDelegateCommand OpenCommand { get; private set; }
        public RichDelegateCommand CloseCommand { get; private set; }

        public RichDelegateCommand UndoCommand { get; private set; }
        public RichDelegateCommand RedoCommand { get; private set; }

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

                    //testExport(); // TODO REMOVE THIS !!!!! THIS IS FOR TEST PURPOSES ONLY !!!!
                }
                RaisePropertyChanged(() => DocumentProject);
            }
        }

        public void testExport()
        {
            var publishVisitor = new PublishFlattenedManagedAudioVisitor(
            node =>
            {
                var qName = node.GetXmlElementQName();
                return qName != null && qName.LocalName == "level1";
            },
            n => false);

            //Directory.GetParent(DocumentFilePath) + Path.DirectorySeparatorChar + Path.GetFileName(DocumentFilePath)

            var dirPath = DocumentFilePath + ".EXPORT_DATA";
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            publishVisitor.DestinationDirectory = new Uri(dirPath, UriKind.Absolute);
            publishVisitor.SourceChannel =
                DocumentProject.Presentations.Get(0).ChannelsManager.GetOrCreateAudioChannel();
            Channel publishChannel = DocumentProject.Presentations.Get(0).ChannelFactory.CreateAudioChannel();
            publishChannel.Name = "Temporary External Audio Medias (Publish Visitor)";
            publishVisitor.DestinationChannel = publishChannel;

            Debugger.Break();
            DocumentProject.Presentations.Get(0).RootNode.AcceptDepthFirst(publishVisitor);

            Debugger.Break();
            publishVisitor.VerifyTree(DocumentProject.Presentations.Get(0).RootNode);

            Debugger.Break();
            DocumentProject.Presentations.Get(0).ChannelsManager.RemoveManagedObject(publishChannel);

            Debugger.Break();
            var project = new Project();
            var action = new OpenXukAction(project, new Uri(DocumentFilePath, UriKind.Absolute));
            action.Execute();

            Debugger.Break();
            DocumentProject.Presentations.Get(0).DataProviderManager.CompareByteStreamsDuringValueEqual = false;
            project.Presentations.Get(0).DataProviderManager.CompareByteStreamsDuringValueEqual = false;
            Debug.Assert(project.ValueEquals(DocumentProject));

            Debugger.Break();
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
            UndoCommand = new RichDelegateCommand(
                UserInterfaceStrings.Undo,
                UserInterfaceStrings.Undo_,
                UserInterfaceStrings.Undo_KEYS,
                shellPresenter.LoadTangoIcon("edit-undo"),
                ()=> DocumentProject.Presentations.Get(0).UndoRedoManager.Undo(),
                ()=> DocumentProject != null && DocumentProject.Presentations.Get(0).UndoRedoManager.CanUndo);

            shellPresenter.RegisterRichCommand(UndoCommand);
            //
            RedoCommand = new RichDelegateCommand(
                UserInterfaceStrings.Redo,
                UserInterfaceStrings.Redo_,
                UserInterfaceStrings.Redo_KEYS,
                shellPresenter.LoadTangoIcon("edit-redo"),
                ()=> DocumentProject.Presentations.Get(0).UndoRedoManager.Redo(),
                ()=> DocumentProject != null && DocumentProject.Presentations.Get(0).UndoRedoManager.CanRedo);

            shellPresenter.RegisterRichCommand(RedoCommand);
            //
            CloseCommand = new RichDelegateCommand(
                UserInterfaceStrings.Close,
                UserInterfaceStrings.Close_,
                UserInterfaceStrings.Close_KEYS,
                shellPresenter.LoadTangoIcon("go-jump"),
                ()=> Close(),
                ()=> DocumentProject != null);

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
                    Focusable = true,
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

                var details = new TextBoxReadOnlyCaretVisible(UserInterfaceStrings.UnsavedChangesDetails)
                {
                };

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
