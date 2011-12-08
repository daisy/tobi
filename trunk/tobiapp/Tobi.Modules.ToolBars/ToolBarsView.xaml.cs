using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Regions;
using Microsoft.Practices.Composite.Regions;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;


namespace Tobi.Plugin.ToolBars
{
    ///<summary>
    /// Single shared instance (singleton) of the toolbar view
    ///</summary>
    [Export(typeof(IToolBarsView)), PartCreationPolicy(CreationPolicy.Shared)]
    public sealed partial class ToolBarsView : IToolBarsView, IPartImportsSatisfiedNotification //INotifyPropertyChangedEx
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
        private readonly IShellView m_ShellView;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity container, it's a built-in CAG service</param>
        ///<param name="regionManager">normally obtained from the Unity container, it's a built-in CAG service</param>
        ///<param name="shellView">normally obtained from the Unity container, it's a Tobi-specific entity</param>
        [ImportingConstructor]
        public ToolBarsView(
            ILoggerFacade logger,
            IRegionManager regionManager,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView)
        {
            m_Logger = logger;
            m_RegionManager = regionManager;
            m_ShellView = shellView;

            //m_PropertyChangeHandler = new PropertyChangedNotifyBase();
            //m_PropertyChangeHandler.InitializeDependentProperties(this);

            initCommands();

            InitializeComponent();

            RegionManager.SetRegionManager(this, m_RegionManager);
            RegionManager.UpdateRegions();
        }

        //private readonly PropertyChangedNotifyBase m_PropertyChangeHandler;

#pragma warning disable 1591 // missing comment
        public RichDelegateCommand CommandFocus { get; private set; }
#pragma warning restore 1591
        private void initCommands()
        {
            CommandFocus = new RichDelegateCommand(
                Tobi_Plugin_ToolBars_Lang.CmdToolbarFocus_ShortDesc,
                null,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("applications-accessories"),
                () =>
                {
                    if (FocusCollapsed.IsVisible)
                    {
                        FocusHelper.FocusBeginInvoke(FocusCollapsed);
                    }
                    else
                    {
                        FocusHelper.FocusBeginInvoke(FocusExpanded);
                    }
                },
                () => true,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Focus_Toolbar));
            m_ShellView.RegisterRichCommand(CommandFocus);
        }

        //public event PropertyChangedEventHandler PropertyChanged;
        //public void DispatchPropertyChangedEvent(PropertyChangedEventArgs e)
        //{
        //    var handler = PropertyChanged;

        //    if (handler != null)
        //    {
        //        handler(this, e);
        //    }
        //}


        public int AddToolBarGroup(RichDelegateCommand[] commands, PreferredPosition position)
        {
            //m_Logger.Log(@"AddToolBarGroup", Category.Debug, Priority.Medium);

#if DEBUG
            if (!Dispatcher.CheckAccess())
            {
                Debugger.Break();
            }
#endif
            int uid = getNewUid();

            //IRegion targetRegion = m_RegionManager.Regions[RegionNames.MainToolbar];

            int count = 0;

            foreach (var command in commands)
            {
                string viewname = uid + @"_" + count++;

                //m_Logger.Log(@"}}}}}}}}}}}}}> AddToolBar: " + position + " (" + viewname + ") " + command.ShortDescription, Category.Debug, Priority.Medium);

                m_RegionManager.RegisterNamedViewWithRegion(RegionNames.MainToolbar,
                    new PreferredPositionNamedView { m_viewName = viewname, m_viewInstance = command, m_viewPreferredPosition = position });
                //m_RegionManager.RegisterViewWithRegion(RegionNames.MainToolbar, () => cmd);
                //targetRegion.Add(command, viewname);
                //targetRegion.Activate(command);
            }
#if DEBUG
            if (count == 0)
            {
                Debugger.Break();
            }
#endif
            string name = uid + @"_" + count++;
            var sep = new Separator();
            m_RegionManager.RegisterNamedViewWithRegion(RegionNames.MainToolbar,
                new PreferredPositionNamedView { m_viewName = name, m_viewInstance = sep, m_viewPreferredPosition = position });
            //m_RegionManager.RegisterViewWithRegion(RegionNames.MainToolbar, () => sep);
            //targetRegion.Add(sep, name);
            //targetRegion.Activate(sep);

            return uid;
        }

        public void RemoveToolBarGroup(int uid)
        {
            m_Logger.Log(@"RemoveToolBarGroup", Category.Debug, Priority.Medium);

#if DEBUG
            if (!Dispatcher.CheckAccess())
            {
                Debugger.Break();
            }
#endif

            IRegion targetRegion = m_RegionManager.Regions[RegionNames.MainToolbar];

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

        //private static int count(IViewsCollection collection)
        //{
        //    int count = 0;
        //    foreach (var view in collection)
        //    {
        //        count++;
        //    }
        //    return count;
        //}
        private void OnToolbarToggleVisible(object sender, MouseButtonEventArgs e)
        {
            Settings.Default.ToolBarVisible = !Settings.Default.ToolBarVisible;
        }
        private void OnToolbarToggleVisibleKeyboard(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) // || e.Key == Key.Space)
            {
                Settings.Default.ToolBarVisible = !Settings.Default.ToolBarVisible;
                FocusHelper.FocusBeginInvoke(Settings.Default.ToolBarVisible ? FocusExpanded : FocusCollapsed);
            }
        }
    }
}
