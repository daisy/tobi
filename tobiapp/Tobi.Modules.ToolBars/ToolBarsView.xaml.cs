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
                UserInterfaceStrings.Toolbar_Focus,
                null,
                UserInterfaceStrings.Toolbar_Focus_KEYS,
                null,
                () => FocusHelper.Focus(this, FocusStart),
                () => true);
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


        public int AddToolBarGroup(RichDelegateCommand[] commands)
        {
            m_Logger.Log(@"AddToolBarGroup", Category.Debug, Priority.Medium);
#if DEBUG
            if (!Dispatcher.CheckAccess())
            {
                Debugger.Break();
            }
#endif
            int uid = getNewUid();

            IRegion targetRegion = m_RegionManager.Regions[RegionNames.MainToolbar];

            int count = 0;

            foreach (var command in commands)
            {
                //targetRegion.Add(new ButtonRichCommand(){RichCommand = command}, uid + "_" + count++);
                targetRegion.Add(command, uid + @"_" + count++);
                targetRegion.Activate(command);
                //command.IconProvider.IconDrawScale
            }
#if DEBUG
            if (count == 0)
            {
                Debugger.Break();
            }
#endif
            var sep = new Separator();
            targetRegion.Add(sep, uid + @"_" + count++);
            targetRegion.Activate(sep);

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
    }
}
