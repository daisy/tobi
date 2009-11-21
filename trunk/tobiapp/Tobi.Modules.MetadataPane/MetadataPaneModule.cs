using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Composite.UnityExtensions;
using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;

namespace Tobi.Plugin.MetadataPane
{
    /// <summary>
    /// The metadata pane offers a viewer / editor for the Urakawa SDK data model's metadata.
    /// </summary>
    public class MetadataPaneModule : IModule
    {
        private readonly IUnityContainer m_Container;

        public RichDelegateCommand CommandShowMetadataPane { get; private set; }

        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        public MetadataPaneModule(IUnityContainer container)
        {
            m_Container = container;
        }

        public void Initialize()
        {
            //m_Container.RegisterType<MetadataPaneViewModel>(new ContainerControlledLifetimeManager());
            m_Container.RegisterType<IMetadataPaneView, MetadataPaneView>(new ContainerControlledLifetimeManager());
            
            //Logger.Log("MetadataPaneViewModel.initializeCommands", Category.Debug, Priority.Medium);

            var shellView = m_Container.Resolve<IShellView>();

            CommandShowMetadataPane = new RichDelegateCommand(
                UserInterfaceStrings.ShowMetadata,
                UserInterfaceStrings.ShowMetadata_,
                UserInterfaceStrings.ShowMetadata_KEYS,
                shellView.LoadTangoIcon("accessories-text-editor"),
                ShowDialog,
                CanShowDialog);

            shellView.RegisterRichCommand(CommandShowMetadataPane);

            var toolbars = m_Container.TryResolve<IToolBarsView>();
            if (toolbars != null)
            {
                int uid = toolbars.AddToolBarGroup(new[] { CommandShowMetadataPane });
            }
            /*
             * The popup window (modal or not) that contains the metadata editor does not provide a region.
             * It could, but it's not necessary here.
             * 
            var regionManager = m_Container.Resolve<IRegionManager>();
            IRegion targetRegion = regionManager.Regions[RegionNames.MetadataPane];

            var view = m_Container.Resolve<MetadataPaneView>();
            targetRegion.Add(view);
            targetRegion.Activate(view);
             * */
        }
        bool CanShowDialog()
        {
            var session = m_Container.Resolve<IUrakawaSession>();
            return session.DocumentProject != null && session.DocumentProject.Presentations.Count > 0;
        }

        void ShowDialog()
        {
            //Logger.Log("MetadataPaneViewModel.showMetadata", Category.Debug, Priority.Medium);

            var view = m_Container.Resolve<IMetadataPaneView>();
            view.Popup();
        }
    }
}
