using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Regions;
using Tobi.Common;
using Tobi.Common.UI;

namespace Tobi.Plugin.AudioPane
{
    ///<summary>
    /// The audio waveform display and editor
    ///</summary>
    [Export(typeof(ITobiPlugin)), PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class AudioPanePlugin : AbstractTobiPlugin, IPartImportsSatisfiedNotification
    {
#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied()
#pragma warning restore 1591
        {
            //#if DEBUG
            //            Debugger.Break();
            //#endif

            // If the menubar has been resolved, we can push our commands into it.
            tryMenubarCommands();
        }

#pragma warning disable 649 // non-initialized fields

        [Import(typeof(IMenuBarView), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = true, AllowDefault = true)]
        private IMenuBarView m_MenuBarView;

#pragma warning restore 649

        private readonly ILoggerFacade m_Logger;
        private readonly IRegionManager m_RegionManager;

        private readonly IShellView m_ShellView;
        private readonly AudioPaneView m_AudioPaneView;
        private readonly AudioPaneViewModel m_AudioPaneViewModel;


        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        ///<param name="regionManager">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        ///<param name="audioPaneView">normally obtained from the MEF composition container, it's a Tobi-specific service</param>
        ///<param name="audioPaneViewModel">normally obtained from the MEF composition container, it's a Tobi-specific service</param>
        ///<param name="shellView">normally obtained from the Unity container, it's a Tobi-specific entity</param>
        [ImportingConstructor]
        public AudioPanePlugin(
            ILoggerFacade logger,
            IRegionManager regionManager,
            [Import(typeof(IAudioPaneView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            AudioPaneView audioPaneView,
            [Import(typeof(AudioPaneViewModel), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            AudioPaneViewModel audioPaneViewModel,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView)
        {
            m_Logger = logger;
            m_RegionManager = regionManager;

            m_AudioPaneView = audioPaneView;
            m_AudioPaneViewModel = audioPaneViewModel;

            m_ShellView = shellView;

            m_RegionManager.RegisterNamedViewWithRegion(RegionNames.AudioPane,
                new PreferredPositionNamedView { m_viewInstance = m_AudioPaneView, m_viewName = @"ViewOf_" + RegionNames.AudioPane });

            //m_RegionManager.RegisterViewWithRegion(RegionNames.AudioPane, typeof(IAudioPaneView));

            //IRegion targetRegion = m_RegionManager.Regions[RegionNames.AudioPane];
            //targetRegion.Add(m_AudioPaneView);
            //targetRegion.Activate(m_AudioPaneView);

            m_Logger.Log(@"AudioPanePlugin is initializing...", Category.Debug, Priority.Medium);
        }

        private int m_MenuBarId_1;
        private int m_MenuBarId_2;
        private int m_MenuBarId_3;
        private int m_MenuBarId_4;
        private int m_MenuBarId_5;
        private int m_MenuBarId_6;
        private int m_MenuBarId_7;
        private bool m_MenuBarCommandsDone;
        private void tryMenubarCommands()
        {
            if (!m_MenuBarCommandsDone && m_MenuBarView != null)
            {
                var dataMonitor = new TwoStateMenuItemRichCommand_DataContextWrapper
                {
                    InputBindingManager = m_ShellView,
                    RichCommandActive = m_AudioPaneViewModel.IsMonitoring,
                    RichCommandOne = m_AudioPaneViewModel.CommandStopMonitor,
                    RichCommandTwo = m_AudioPaneViewModel.CommandStartMonitor,
                    RichCommandActive_BindingSource = m_AudioPaneViewModel,
                    RichCommandActive_BindingPropertyPathLambdaExpr = () => m_AudioPaneViewModel.IsMonitoring
                };
                var dataRecord = new TwoStateMenuItemRichCommand_DataContextWrapper
                {
                    InputBindingManager = m_ShellView,
                    RichCommandActive = m_AudioPaneViewModel.IsRecording,
                    RichCommandOne = m_AudioPaneViewModel.CommandStopRecord,
                    RichCommandTwo = m_AudioPaneViewModel.CommandStartRecord,
                    RichCommandActive_BindingSource = m_AudioPaneViewModel,
                    RichCommandActive_BindingPropertyPathLambdaExpr = () => m_AudioPaneViewModel.IsRecording
                };
                m_MenuBarId_1 = m_MenuBarView.AddMenuBarGroup(RegionNames.MenuBar_Audio, null, new[] { dataMonitor, dataRecord }, PreferredPosition.First, true);


                var dataPlay = new TwoStateMenuItemRichCommand_DataContextWrapper
                {
                    InputBindingManager = m_ShellView,
                    RichCommandActive = m_AudioPaneViewModel.IsPlaying,
                    RichCommandOne = m_AudioPaneViewModel.CommandPause,
                    RichCommandTwo = m_AudioPaneViewModel.CommandPlay,
                    RichCommandActive_BindingSource = m_AudioPaneViewModel,
                    RichCommandActive_BindingPropertyPathLambdaExpr = () => m_AudioPaneViewModel.IsPlaying
                };
                m_MenuBarId_2 = m_MenuBarView.AddMenuBarGroup(RegionNames.MenuBar_Audio, null, new object[] { m_AudioPaneViewModel.CommandPlayPreviewLeft, dataPlay, m_AudioPaneViewModel.CommandPlayPreviewRight }, PreferredPosition.First, true);

                m_MenuBarId_3 = m_MenuBarView.AddMenuBarGroup(RegionNames.MenuBar_Audio, RegionNames.MenuBar_AudioTransport, new[]
                                                                                             {
                                                                                                 m_AudioPaneViewModel.CommandGotoBegining,
                                                                                                 m_AudioPaneViewModel.CommandStepBack,
                                                                                                 m_AudioPaneViewModel.CommandRewind,
                                                                                                 m_AudioPaneViewModel.CommandFastForward,
                                                                                                 m_AudioPaneViewModel.CommandStepForward,
                                                                                                 m_AudioPaneViewModel.CommandGotoEnd
                                                                                             }, PreferredPosition.Last, true);

                m_MenuBarId_4 = m_MenuBarView.AddMenuBarGroup(RegionNames.MenuBar_Audio, RegionNames.MenuBar_AudioSelection, new[]
                                                                                             {
                                                                                                 m_AudioPaneViewModel.CommandSelectAll,
                                                                                                 m_AudioPaneViewModel.CommandSelectPreviousChunk,
                                                                                                 m_AudioPaneViewModel.CommandSelectNextChunk,
                                                                                                 m_AudioPaneViewModel.CommandBeginSelection,
                                                                                                 m_AudioPaneViewModel.CommandEndSelection,
                                                                                                 m_AudioPaneViewModel.CommandClearSelection
                                                                                             }, PreferredPosition.Last, true);

                m_MenuBarId_5 = m_MenuBarView.AddMenuBarGroup(RegionNames.MenuBar_Audio, RegionNames.MenuBar_AudioZoom, new[]
                                                                                             {
                                                                                                 m_AudioPaneViewModel.CommandZoomSelection,
                                                                                                 m_AudioPaneViewModel.CommandZoomFitFull
                                                                                             }, PreferredPosition.Last, true);


                m_MenuBarId_6 = m_MenuBarView.AddMenuBarGroup(RegionNames.MenuBar_View, RegionNames.MenuBar_Focus, new[] { m_AudioPaneViewModel.CommandFocus }, PreferredPosition.Last, false);
                m_MenuBarId_7 = m_MenuBarView.AddMenuBarGroup(RegionNames.MenuBar_View, RegionNames.MenuBar_Focus, new[] { m_AudioPaneViewModel.CommandFocusStatusBar }, PreferredPosition.Last, false);


                m_MenuBarCommandsDone = true;

                m_Logger.Log(@"Audio commands pushed to menubar", Category.Debug, Priority.Medium);
            }
        }

        public override void Dispose()
        {
            if (m_MenuBarCommandsDone)
            {
                m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_Audio, m_MenuBarId_1);
                m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_Audio, m_MenuBarId_2);
                m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_Audio, m_MenuBarId_3);
                m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_Audio, m_MenuBarId_4);
                m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_Audio, m_MenuBarId_5);

                m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_Focus, m_MenuBarId_6);
                m_MenuBarView.RemoveMenuBarGroup(RegionNames.MenuBar_Focus, m_MenuBarId_7);


                m_MenuBarCommandsDone = false;

                m_Logger.Log(@"Audio commands removed from menubar", Category.Debug, Priority.Medium);
            }

            m_RegionManager.Regions[RegionNames.AudioPane].Deactivate(m_AudioPaneView);
            m_RegionManager.Regions[RegionNames.AudioPane].Remove(m_AudioPaneView);

            m_Logger.Log(@"Audio view removed from region", Category.Debug, Priority.Medium);
        }

        public override string Name
        {
            get { return @"Audio waveform editor."; }
        }

        public override string Description
        {
            get { return @"The audio waveform editor and display."; }
        }
    }
}
