using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;
using Microsoft.Win32;
using Tobi.Infrastructure;
using Tobi.Infrastructure.Commanding;
using urakawa;

namespace Tobi.Modules.Urakawa
{
    ///<summary>
    ///</summary>
    public class UrakawaSession : IUrakawaSession
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

        public Project DocumentProject
        {
            get;
            set;
        }

        public string DocumentFilePath
        {
            get;
            set;
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
            Logger = logger;
            Container = container;
            RegionManager = regionManager;
            EventAggregator = eventAggregator;

            initCommands();
        }

        private void initCommands()
        {
            var shellPresenter = Container.Resolve<IShellPresenter>();

            SaveAsCommand = new RichDelegateCommand<object>(UserInterfaceStrings.SaveAs,
                UserInterfaceStrings.SaveAs_,
                UserInterfaceStrings.SaveAs_KEYS,
                (VisualBrush)Application.Current.FindResource("document-save"),
                //RichDelegateCommand<object>.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Save_As")),
                obj => { throw new NotImplementedException("Functionality not implemented, sorry :("); }, obj => true);

            shellPresenter.RegisterRichCommand(SaveAsCommand);
            //
            SaveCommand = new RichDelegateCommand<object>(
                UserInterfaceStrings.Save,
                UserInterfaceStrings.Save_,
                UserInterfaceStrings.Save_KEYS,
                (VisualBrush)Application.Current.FindResource("media-floppy"),
                //RichDelegateCommand<object>.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Save")),
                obj =>
                {
                    throw new NotImplementedException("Functionality not implemented, sorry :(",
                        new NotImplementedException("Just trying nested expections",
                        new NotImplementedException("The last inner exception ! :)")));
                }, obj => true);

            shellPresenter.RegisterRichCommand(SaveCommand);
            //
            NewCommand = new RichDelegateCommand<object>(UserInterfaceStrings.New,
                UserInterfaceStrings.New_,
                UserInterfaceStrings.New_KEYS,
                (VisualBrush)Application.Current.FindResource("document-new"),
                obj => openDefaultTemplate(), obj => true);

            shellPresenter.RegisterRichCommand(NewCommand);
            //
            OpenCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Open,
                UserInterfaceStrings.Open_,
                UserInterfaceStrings.Open_KEYS,
                (VisualBrush)Application.Current.FindResource("document-open"),
                obj => openFile(), obj => true);

            shellPresenter.RegisterRichCommand(OpenCommand);
            //
            CloseCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Close,
                UserInterfaceStrings.Close_,
                UserInterfaceStrings.Close_KEYS,
                (VisualBrush)Application.Current.FindResource("go-jump"),
                obj => closeProject(), obj => IsProjectLoaded);

            shellPresenter.RegisterRichCommand(CloseCommand);
        }

        public bool IsDirty
        {
            get
            {
                return false;
            }
        }

        private bool IsProjectLoaded
        {
            get
            {
                return DocumentProject != null;
            }
        }

        private void closeProject()
        {
            Logger.Log("-- PublishEvent [ProjectUnLoadedEvent] UrakawaSession.closeProject", Category.Debug, Priority.Medium);

            EventAggregator.GetEvent<ProjectUnLoadedEvent>().Publish(DocumentProject);
        }

        private void openDefaultTemplate()
        {
            string currentAssemblyDirectoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            openFile(currentAssemblyDirectoryName + @"\empty-dtbook-z3986-2005.xml");
        }

        private void openFile()
        {
            var dlg = new OpenFileDialog();
            dlg.FileName = "dtbook"; // Default file name
            dlg.DefaultExt = ".xml"; // Default file extension
            dlg.Filter = "DTBook, OPF, EPUB or XUK (.xml, *.opf, *.xuk, *.epub)|*.xml;*.opf;*.xuk;*.epub";
            bool? result = dlg.ShowDialog();
            if (result == false)
            {
                return;
            }
            openFile(dlg.FileName);
        }

        private void openFile(string filename)
        {
            DocumentFilePath = filename;
            if (Path.GetExtension(DocumentFilePath) == ".xuk")
            {
                DocumentProject = new Project();

                Uri uri = new Uri(DocumentFilePath, UriKind.Absolute);
                DocumentProject.OpenXuk(uri);
            }
            else
            {
                var converter = new XukImport.DaisyToXuk(DocumentFilePath);
                DocumentProject = converter.Project;
            }

            Logger.Log("-- PublishEvent [ProjectLoadedEvent] UrakawaSession.OpenFile", Category.Debug, Priority.Medium);

            EventAggregator.GetEvent<ProjectLoadedEvent>().Publish(DocumentProject);
        }
    }
}
