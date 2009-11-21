using System;
using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Regions;
using Tobi.Common;

namespace Tobi.Modules.ToolBars
{
    ///<summary>
    /// The tool bar contains groups of buttons (commands actually) from various parts of the application.
    /// (i.e. it is a host service, it doesn't own command data directly)
    ///</summary>
    [Export(typeof(ITobiModule)), PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class ToolBarsModule : ITobiModule, IPartImportsSatisfiedNotification
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
        private readonly IRegionManager m_RegionManager;

        private readonly ToolBarsView m_ToolBarsView;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        ///<param name="regionManager">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        ///<param name="toolBarsView">normally obtained from the MEF composition container, it's a Tobi-specific service</param>
        [ImportingConstructor]
        public ToolBarsModule(
            ILoggerFacade logger,
            IRegionManager regionManager,
            [Import(typeof(IToolBarsView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            ToolBarsView toolBarsView)
        {
            m_Logger = logger;
            m_RegionManager = regionManager;

            m_ToolBarsView = toolBarsView;

            m_RegionManager.RegisterViewWithRegion(RegionNames.ToolBars, typeof(IToolBarsView));

            //IRegion targetRegion = m_RegionManager.Regions[RegionNames.ToolBars];
            //targetRegion.Add(m_ToolBarsView);
            //targetRegion.Activate(m_ToolBarsView);

            m_Logger.Log(@"Toolbar pushed to region", Category.Debug, Priority.Medium);
        }

        public void Dispose()
        {
            m_RegionManager.Regions[RegionNames.ToolBars].Deactivate(m_ToolBarsView);
            m_RegionManager.Regions[RegionNames.ToolBars].Remove(m_ToolBarsView);

            m_Logger.Log(@"Toolbar removed from region", Category.Debug, Priority.Medium);
        }

        public string Name
        {
            get { return @"Application toolbar."; }
        }

        public string Description
        {
            get { return @"The visual host for command buttons."; }
        }

        public Uri Home
        {
            get { return UserInterfaceStrings.TobiHomeUri; }
        }
    }
}
