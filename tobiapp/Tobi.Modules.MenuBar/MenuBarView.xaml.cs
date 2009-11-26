using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows.Controls;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Regions;
using Microsoft.Practices.Composite.Regions;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using System.Linq;

namespace Tobi.Plugin.MenuBar
{
    ///<summary>
    /// Single shared instance (singleton) of the toolbar view
    ///</summary>
    [Export(typeof(IMenuBarView)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class MenuBarView : IMenuBarView, IPartImportsSatisfiedNotification
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


        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity container, it's a built-in CAG service</param>
        ///<param name="regionManager">normally obtained from the Unity container, it's a built-in CAG service</param>
        [ImportingConstructor]
        public MenuBarView(
            ILoggerFacade logger,
            IRegionManager regionManager)
        {
            m_Logger = logger;
            m_RegionManager = regionManager;

            m_Logger.Log(@"MenuBarView.ctor", Category.Debug, Priority.Medium);

            InitializeComponent();

            RegionManager.SetRegionManager(this, m_RegionManager);
            RegionManager.UpdateRegions();
        }


        public int AddMenuBarGroup(string region, object[] commands, string rootHeader, bool addSeparator)
        {
            m_Logger.Log(@"AddMenuBarGroup", Category.Debug, Priority.Medium);

#if DEBUG
            if (!Dispatcher.CheckAccess())
            {
                Debugger.Break();
            }
#endif
            int uid = getNewUid();

            IRegion targetRegion;
            try
            {
                targetRegion = m_RegionManager.Regions[region];
            }
            catch
            {
                var menuRoot = new MenuItemRichCommand { Header = region };

                //RegionManager.SetRegionManager(menuRoot, m_RegionManager);
                RegionManager.SetRegionName(menuRoot, region);
                //RegionManager.UpdateRegions();

                MenuBarAnchor.Items.Add(menuRoot);

                targetRegion = m_RegionManager.Regions[region];
            }

            int count = 0;
            
            if (addSeparator && targetRegion.Views.Count() > 0)
            {
                var sep = new Separator();
                targetRegion.Add(sep, uid + @"_" + count++);
                targetRegion.Activate(sep);
            }

            if (!string.IsNullOrEmpty(rootHeader))
            {
                var subRegionName = rootHeader; // @"SubMenuRegion_" + uid;

                try
                {
                    IRegion targetRegionTry = m_RegionManager.Regions[subRegionName];
                    targetRegion = targetRegionTry;
                }
                catch
                {
                    var menuRoot = new MenuItemRichCommand { Header = rootHeader };

                    //RegionManager.SetRegionManager(menuRoot, m_RegionManager);
                    RegionManager.SetRegionName(menuRoot, subRegionName);
                    //RegionManager.UpdateRegions();

                    targetRegion.Add(menuRoot); //, uid + @"_" + count++);
                    targetRegion.Activate(menuRoot);

                    targetRegion = m_RegionManager.Regions[subRegionName];
                }
            }

            foreach (var command in commands)
            {
                if (command is RichDelegateCommand)
                {
                    //var menuItem = new MenuItemRichCommand { RichCommand = (RichDelegateCommand)command };
                    targetRegion.Add(command, uid + @"_" + count++);
                    targetRegion.Activate(command);
                }
                else if (command is TwoStateMenuItemRichCommand_DataContextWrapper)
                {
                    //var cmdX = (TwoStateMenuItemRichCommand_DataContextWrapper)command;
                    //var menuItem = new TwoStateMenuItemRichCommand { InputBindingManager = m_ShellView, RichCommandOne = cmdX.RichCommandOne, RichCommandTwo = cmdX.RichCommandTwo, RichCommandActive = cmdX.RichCommandActive };
                    targetRegion.Add(command, uid + @"_" + count++);
                    targetRegion.Activate(command);
                }
            }

            return uid;
        }

        public void RemoveMenuBarGroup(string region, int uid)
        {
            m_Logger.Log(@"RemoveMenuBarGroup", Category.Debug, Priority.Medium);

#if DEBUG
            if (!Dispatcher.CheckAccess())
            {
                Debugger.Break();
            }
#endif
            IRegion targetRegion;
            try
            {
                targetRegion = m_RegionManager.Regions[region];
            }
            catch
            {
                return;
            }

            var viewsToRemove = new List<object>();

            int count = 0;
            object view;
            while ((view = targetRegion.GetView(uid + @"_" + count++)) != null)
            {
                viewsToRemove.Add(view);
            }

            foreach (var obj in viewsToRemove)
            {
                targetRegion.Deactivate(obj);
                targetRegion.Remove(obj);
            }
        }

        private int m_Uid;
        private int getNewUid()
        {
            return m_Uid++;
        }
    }

    ///// <summary>
    ///// Multiple choice of DataTemplate for the MenuItems based on the DataContext type
    ///// </summary>
    //public class MenuItemDataTemplateSelector : DataTemplateSelector
    //{
    //    public DataTemplate MenuItemSeparatorDataTemplate { get; set; }
    //    public DataTemplate MenuItemSimpleDataTemplate { get; set; }
    //    public DataTemplate MenuItemToggleDataTemplate { get; set; }

    //    public override DataTemplate SelectTemplate(object item,
    //               DependencyObject container)
    //    {
    //        if (item == null)
    //        {
    //            return MenuItemSeparatorDataTemplate;
    //        }

    //        if (item is RichDelegateCommand)
    //        {
    //            //RichDelegateCommand cmd = item as RichDelegateCommand;

    //            //Window window = Application.Current.MainWindow;
    //            //window.FindResource("MenuItemSimpleDataTemplate") as DataTemplate;

    //            return MenuItemSimpleDataTemplate;
    //        }

    //        if (item is TwoStateMenuItemRichCommand_DataContextWrapper)
    //        {
    //            return MenuItemToggleDataTemplate;
    //        }

    //        return MenuItemSeparatorDataTemplate;
    //    }
    //}
}
