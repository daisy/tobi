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


            //var metadata = Container.Resolve<MetadataPaneViewModel>();
            //if (metadata != null)
            //{
            //    CommandShowMetadataPane = metadata.CommandShowMetadataPane;
            //}

            //var audioModule = Container.Resolve<AudioPaneViewModel>();
            //if (audioModule != null)
            //{

            //    AudioCommandInsertFile = audioModule.CommandInsertFile;
            //    AudioCommandGotoBegining = audioModule.CommandGotoBegining;
            //    AudioCommandGotoEnd = audioModule.CommandGotoEnd;
            //    AudioCommandStepBack = audioModule.CommandStepBack;
            //    AudioCommandStepForward = audioModule.CommandStepForward;
            //    AudioCommandRewind = audioModule.CommandRewind;
            //    AudioCommandFastForward = audioModule.CommandFastForward;
            //    AudioCommandSelectAll = audioModule.CommandSelectAll;
            //    AudioCommandClearSelection = audioModule.CommandClearSelection;
            //    AudioCommandZoomSelection = audioModule.CommandZoomSelection;
            //    AudioCommandZoomFitFull = audioModule.CommandZoomFitFull;
            //    AudioCommandPlay = audioModule.CommandPlay;
            //    AudioCommandPlayPreviewLeft = audioModule.CommandPlayPreviewLeft;
            //    AudioCommandPlayPreviewRight = audioModule.CommandPlayPreviewRight;
            //    AudioCommandPause = audioModule.CommandPause;
            //    AudioCommandStartRecord = audioModule.CommandStartRecord;
            //    AudioCommandStopRecord = audioModule.CommandStopRecord;
            //    AudioCommandStartMonitor = audioModule.CommandStartMonitor;
            //    AudioCommandStopMonitor = audioModule.CommandStopMonitor;
            //    AudioCommandBeginSelection = audioModule.CommandBeginSelection;
            //    AudioCommandEndSelection = audioModule.CommandEndSelection;
            //    AudioCommandSelectNextChunk = audioModule.CommandSelectNextChunk;
            //    AudioCommandSelectPreviousChunk = audioModule.CommandSelectPreviousChunk;
            //    AudioCommandDeleteAudioSelection = audioModule.CommandDeleteAudioSelection;
            //}
        }


        public int AddMenuBarGroup(string region, object[] commands, string rootHeader)
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
                return -1;
            }

            if (targetRegion == null)
            {
                return -1;
            }

            int count = 0;

            MenuItemRichCommand menuRoot = null;
            if (!string.IsNullOrEmpty(rootHeader))
            {
                var subRegionName = @"SubMenuRegion_" + uid;

                menuRoot = new MenuItemRichCommand { Header = rootHeader };

                //RegionManager.SetRegionManager(menuRoot, m_RegionManager);
                RegionManager.SetRegionName(menuRoot, subRegionName);
                //RegionManager.UpdateRegions();

                targetRegion.Add(menuRoot, uid + @"_" + count++);
                targetRegion.Activate(menuRoot);

                targetRegion = m_RegionManager.Regions[subRegionName];
            }
            else
            {
                if (targetRegion.Views.Count() > 0)
                {
                    var sep = new Separator();
                    targetRegion.Add(sep, uid + @"_" + count++);
                    targetRegion.Activate(sep);
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

            if (targetRegion == null)
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
                targetRegion.Remove(obj);
            }
        }

        private int m_Uid;
        private int getNewUid()
        {
            return m_Uid++;
        }

        //public AudioPaneViewModel AudioPaneViewModel
        //{
        //    get
        //    {
        //        var viewModel = Container.Resolve<AudioPaneViewModel>();

        //        return viewModel;
        //    }
        //}

        //public IInputBindingManager InputBindingManager
        //{
        //    get
        //    {
        //        var shellView = Container.Resolve<IShellView>();

        //        return shellView;
        //    }
        //}
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
