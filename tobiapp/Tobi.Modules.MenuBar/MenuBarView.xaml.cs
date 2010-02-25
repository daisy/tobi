﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
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


        public int AddMenuBarGroup(string topLevelMenuItemId, PreferredPosition positionInTopLevel, bool addSeparatorTopLevel,
                                    string subMenuItemId, PreferredPosition positionInSubLevel, bool addSeparatorSubLevel,
                                    object[] commands)
        {
            m_Logger.Log(@"AddMenuBarGroup", Category.Debug, Priority.Medium);

#if DEBUG
            if (!Dispatcher.CheckAccess())
            {
                Debugger.Break();
            }
#endif

            IRegion targetRegion;
            try
            {
                targetRegion = m_RegionManager.Regions[topLevelMenuItemId];
            }
            catch
            {
                var menuRoot = new MenuItemRichCommand { Header = topLevelMenuItemId };

                //RegionManager.SetRegionManager(menuRoot, m_RegionManager);
                RegionManager.SetRegionName(menuRoot, topLevelMenuItemId);
                //RegionManager.UpdateRegions();

                MenuBarAnchor.Items.Add(menuRoot);

                targetRegion = m_RegionManager.Regions[topLevelMenuItemId];
            }

            int uid = generateNewUid();
            int count = 0;

            if (addSeparatorTopLevel && targetRegion.Views.Count() > 0)
            {
                var sep = new Separator();

                string viewname = (!string.IsNullOrEmpty(subMenuItemId) ? @"SUB_" : "")
                                   + uid + @"_" + count++;

                m_RegionManager.RegisterNamedViewWithRegion(targetRegion.Name,
                    new PreferredPositionNamedView { m_viewName = viewname, m_viewInstance = sep, m_viewPreferredPosition = positionInTopLevel });
                //m_RegionManager.RegisterViewWithRegion(targetRegion.Name, () => sep);
                //targetRegion.Add(sep, viewname);
                //targetRegion.Activate(sep);
            }

            if (!string.IsNullOrEmpty(subMenuItemId))
            {
                var subRegionName = subMenuItemId; // @"SubMenuRegion_" + uid;

                try
                {
                    IRegion targetRegionTry = m_RegionManager.Regions[subRegionName];
                    targetRegion = targetRegionTry;

                    //var concreteRegion = targetRegion as PreferredPositionRegion;
                    //if (concreteRegion != null)
                    //{
                    //    concreteRegion.
                    //}
                }
                catch
                {
                    var subMenuRoot = new MenuItemRichCommand { Header = subMenuItemId };

                    //RegionManager.SetRegionManager(menuRoot, m_RegionManager);
                    RegionManager.SetRegionName(subMenuRoot, subRegionName);
                    //RegionManager.UpdateRegions();

                    string viewname = @"SUB_" + uid + @"_" + count++;        // TODO LOCALIZE SUB

                    m_RegionManager.RegisterNamedViewWithRegion(targetRegion.Name,
                        new PreferredPositionNamedView { m_viewName = viewname, m_viewInstance = subMenuRoot, m_viewPreferredPosition = positionInTopLevel });
                    //m_RegionManager.RegisterViewWithRegion(targetRegion.Name, () => menuRoot);
                    //targetRegion.Add(menuRoot); //, viewname);
                    //targetRegion.Activate(menuRoot);

                    targetRegion = m_RegionManager.Regions[subRegionName];
                }
            }

            if (commands == null)
                return uid;


            if (!string.IsNullOrEmpty(subMenuItemId) && addSeparatorSubLevel && targetRegion.Views.Count() > 0)
            {
                var sep = new Separator();

                string viewname = uid + @"_" + count++;

                m_RegionManager.RegisterNamedViewWithRegion(targetRegion.Name,
                    new PreferredPositionNamedView { m_viewName = viewname, m_viewInstance = sep, m_viewPreferredPosition = positionInSubLevel });
                //m_RegionManager.RegisterViewWithRegion(targetRegion.Name, () => sep);
                //targetRegion.Add(sep, viewname);
                //targetRegion.Activate(sep);
            }

            foreach (var command in commands)
            {
                if (command is RichDelegateCommand)
                {
                    string viewname = uid + @"_" + count++;

                    m_RegionManager.RegisterNamedViewWithRegion(targetRegion.Name,
                        new PreferredPositionNamedView { m_viewName = viewname, m_viewInstance = command, m_viewPreferredPosition = (string.IsNullOrEmpty(subMenuItemId) ? positionInTopLevel : positionInSubLevel) });
                    //m_RegionManager.RegisterViewWithRegion(targetRegion.Name, () => command);
                    //targetRegion.Add(command, viewname);
                    //targetRegion.Activate(command);
                }
                else if (command is TwoStateMenuItemRichCommand_DataContextWrapper)
                {
                    string viewname = uid + @"_" + count++;

                    m_RegionManager.RegisterNamedViewWithRegion(targetRegion.Name,
                        new PreferredPositionNamedView { m_viewName = viewname, m_viewInstance = command, m_viewPreferredPosition = (string.IsNullOrEmpty(subMenuItemId) ? positionInTopLevel : positionInSubLevel) });
                    //m_RegionManager.RegisterViewWithRegion(targetRegion.Name, () => command);
                    //targetRegion.Add(command, viewname);
                    //targetRegion.Activate(command);
                }
            }

            return uid;
        }

        public void RemoveMenuBarGroup(string region, int uid)
        {
            //TODO: the removal logic is broken since we have introduced PreferredPosition !

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

            var concreteRegion = targetRegion as PreferredPositionRegion;

            var viewsToRemove = new List<object>();

            if (concreteRegion != null)
            {
                viewsToRemove.AddRange(concreteRegion.GetViewsWithNamePrefix(uid + @"_"));
            }
            else
            {
                object view;
                int count = 0;
                while ((view = targetRegion.GetView(uid + @"_" + count++)) != null)
                {
                    viewsToRemove.Add(view);
                }
            }

            if (viewsToRemove.Count == 0)
            {
                if (concreteRegion != null)
                {
                    foreach (var view in concreteRegion.GetViewsWithNamePrefix(@"SUB_")) // + uid + @"_"
                    {
                        if (view is MenuItem)
                        {
                            var menuItem = view as MenuItem;
                            if (menuItem.HasItems)
                            {
                                RemoveMenuBarGroup(RegionManager.GetRegionName(view as DependencyObject), uid);
                            }
                            //else
                            //    viewsToRemove.Add(view);
                        }
                        else
                            viewsToRemove.Add(view);
                    }
                }
                else
                {
                    object view;
                    int count = 0;
                    while ((view = targetRegion.GetView(@"SUB_" + uid + @"_" + count++)) != null)
                    {
                        if (view is MenuItemRichCommand)
                        {
                            MenuItem menuItem = view as MenuItem;
                            if (menuItem.HasItems)
                            {
                                RemoveMenuBarGroup(RegionManager.GetRegionName(view as DependencyObject), uid);
                            }
                            else
                                viewsToRemove.Add(view);
                        }
                        else
                            viewsToRemove.Add(view);
                    }
                }
            }

            foreach (var obj in viewsToRemove)
            {
                //targetRegion.Deactivate(obj);
                targetRegion.Remove(obj);
            }
        }

        private int m_Uid;
        private int generateNewUid()
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
