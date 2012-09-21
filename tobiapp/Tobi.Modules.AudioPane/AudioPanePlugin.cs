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
    public sealed class AudioPanePlugin : AbstractTobiPlugin
    {
        private readonly IRegionManager m_RegionManager;

        private readonly IShellView m_ShellView;
        private readonly AudioPaneView m_AudioPaneView;
        private readonly AudioPaneViewModel m_AudioPaneViewModel;

        private readonly ILoggerFacade m_Logger;

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

            //m_Logger.Log(@"AudioPanePlugin is initializing...", Category.Debug, Priority.Medium);
        }
        private int m_ToolBarId_1;
        protected override void OnToolBarReady()
        {
            m_ToolBarId_1 = m_ToolBarsView.AddToolBarGroup(
                new[] { m_AudioPaneViewModel.CopyCommand, m_AudioPaneViewModel.CutCommand, m_AudioPaneViewModel.PasteCommand },
                PreferredPosition.Any);
        }

        private int m_MenuBarId_1;
        private int m_MenuBarId_2;
        private int m_MenuBarId_3;
        private int m_MenuBarId_4, m_MenuBarId_41, m_MenuBarId_42, m_MenuBarId_43, m_MenuBarId_44;
        private int m_MenuBarId_5;
        private int m_MenuBarId_6;
        private int m_MenuBarId_7;
        private int m_MenuBarId_8;
        private int m_MenuBarId_9;
        private int m_MenuBarId_10;
        private int m_MenuBarId_11;
        private int m_MenuBarId_12;
        private int m_MenuBarId_13;
        private int m_MenuBarId_14;
        private int m_MenuBarId_15;
        //private int m_MenuBarId_16;
        protected override void OnMenuBarReady()
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

            var dataPlay = new TwoStateMenuItemRichCommand_DataContextWrapper
            {
                InputBindingManager = m_ShellView,
                RichCommandActive = m_AudioPaneViewModel.IsPlaying,
                RichCommandOne = m_AudioPaneViewModel.CommandPause,
                RichCommandTwo = m_AudioPaneViewModel.CommandPlay,
                RichCommandActive_BindingSource = m_AudioPaneViewModel,
                RichCommandActive_BindingPropertyPathLambdaExpr = () => m_AudioPaneViewModel.IsPlaying
            };

            m_MenuBarId_5 = m_MenuBarView.AddMenuBarGroup(
                Tobi_Common_Lang.Menu_Audio, PreferredPosition.Last, true,
                Tobi_Common_Lang.Menu_AudioZoom, PreferredPosition.First, true,
                new[]
                 {
                     m_AudioPaneViewModel.CommandZoomSelection,
                     m_AudioPaneViewModel.CommandZoomFitFull
                 });
            m_MenuBarId_14 = m_MenuBarView.AddMenuBarGroup(
                Tobi_Common_Lang.Menu_Audio, PreferredPosition.Last, true,
                Tobi_Common_Lang.Menu_AudioZoom, PreferredPosition.Last, true,
                new[]
                 {
                     m_AudioPaneViewModel.CommandRefresh
                 });

            m_MenuBarId_13 = m_MenuBarView.AddMenuBarGroup(
                 Tobi_Common_Lang.Menu_Edit, PreferredPosition.First, true,
                null, PreferredPosition.Any, true,
                new[]
                 {
                     m_AudioPaneViewModel.CopyCommand,m_AudioPaneViewModel.CutCommand,m_AudioPaneViewModel.PasteCommand
                 });

            m_MenuBarId_11 = m_MenuBarView.AddMenuBarGroup(
                Tobi_Common_Lang.Menu_Audio, PreferredPosition.Last, true,
                null, PreferredPosition.Any, true,
                new[]
                 {
                     m_AudioPaneViewModel.CommandDeleteAudioSelection,
                     m_AudioPaneViewModel.CommandSplitShift
                 });

            m_MenuBarId_10 = m_MenuBarView.AddMenuBarGroup(
                Tobi_Common_Lang.Menu_Audio, PreferredPosition.Last, true,
                null, PreferredPosition.Any, true,
                new[]
                 {
                     m_AudioPaneViewModel.CommandAudioSettings,
#if false && DEBUG
                     m_AudioPaneViewModel.CommandShowOptionsDialog
#endif //DEBUG
                 });

            m_MenuBarId_1 = m_MenuBarView.AddMenuBarGroup(
                Tobi_Common_Lang.Menu_Audio, PreferredPosition.First, false,
                Tobi_Common_Lang.Menu_AudioRecording, PreferredPosition.First, true,
                new[]
                 {
                     dataMonitor
                 });

            m_MenuBarId_15 = m_MenuBarView.AddMenuBarGroup(
                Tobi_Common_Lang.Menu_Audio, PreferredPosition.First, false,
                Tobi_Common_Lang.Menu_AudioRecording, PreferredPosition.Last, true,
                new object[]
                 {
                     dataRecord,
                     m_AudioPaneViewModel.CommandStopRecordAndContinue,
                     m_AudioPaneViewModel.CommandTogglePlayPreviewBeforeRecord
                 });

            m_MenuBarId_12 = m_MenuBarView.AddMenuBarGroup(
                Tobi_Common_Lang.Menu_Audio, PreferredPosition.First, false,
                null, PreferredPosition.Any, true,
                new[]
                 {
                     m_AudioPaneViewModel.CommandInsertFile,
                     m_AudioPaneViewModel.CommandGenTTS,
                     m_AudioPaneViewModel.CommandResetSessionCounter
                 });

            m_MenuBarId_2 = m_MenuBarView.AddMenuBarGroup(
                Tobi_Common_Lang.Menu_Audio, PreferredPosition.First, true,
                Tobi_Common_Lang.Menu_AudioPlayback, PreferredPosition.First, true,
                new object[]
                {
                    m_AudioPaneViewModel.CommandPlayPreviewLeft,
                    dataPlay,
                    m_AudioPaneViewModel.CommandPlayPreviewRight
                });

            m_MenuBarId_9 = m_MenuBarView.AddMenuBarGroup(
                Tobi_Common_Lang.Menu_Audio, PreferredPosition.First, true,
                Tobi_Common_Lang.Menu_AudioPlayback, PreferredPosition.Last, true,
                new object[]
                    {
                        m_AudioPaneViewModel.CommandPlayAutoAdvance,
                        //m_AudioPaneViewModel.CommandAutoPlay
                    });

            m_MenuBarId_8 = m_MenuBarView.AddMenuBarGroup(
                Tobi_Common_Lang.Menu_Audio, PreferredPosition.First, true,
                Tobi_Common_Lang.Menu_AudioPlayback, PreferredPosition.Any, true,
                new object[]
                    {
                        m_AudioPaneViewModel.CommandPlaybackRateUp,
                        m_AudioPaneViewModel.CommandPlaybackRateReset,
                        m_AudioPaneViewModel.CommandPlaybackRateDown
                    });

            m_MenuBarId_3 = m_MenuBarView.AddMenuBarGroup(
                Tobi_Common_Lang.Menu_Audio, PreferredPosition.First, true,
                Tobi_Common_Lang.Menu_AudioTransport, PreferredPosition.First, true,
                new[]
                 {
                     m_AudioPaneViewModel.CommandGotoBegining,
                     m_AudioPaneViewModel.CommandStepBack,
                     m_AudioPaneViewModel.CommandRewind,
                     m_AudioPaneViewModel.CommandFastForward,
                     m_AudioPaneViewModel.CommandStepForward,
                     m_AudioPaneViewModel.CommandGotoEnd
                 });

            //m_MenuBarId_4 = m_MenuBarView.AddMenuBarGroup(
            //    Tobi_Common_Lang.Menu_Audio, PreferredPosition.First, true,
            //    Tobi_Common_Lang.Menu_AudioSelection, PreferredPosition.First, true,
            //    new[]
            //     {
            //         m_AudioPaneViewModel.CommandSelectAll,
            //         m_AudioPaneViewModel.CommandSelectLeft,
            //         m_AudioPaneViewModel.CommandSelectRight,
            //         m_AudioPaneViewModel.CommandSelectPreviousChunk,
            //         m_AudioPaneViewModel.CommandSelectNextChunk,
            //         m_AudioPaneViewModel.CommandBeginSelection,
            //         m_AudioPaneViewModel.CommandEndSelection,
            //         m_AudioPaneViewModel.CommandClearSelection
            //     });

            m_MenuBarId_4 = m_MenuBarView.AddMenuBarGroup(
                Tobi_Common_Lang.Menu_Audio, PreferredPosition.First, true,
                Tobi_Common_Lang.Menu_AudioSelection, PreferredPosition.First, true,
                new[]
                 {
                     m_AudioPaneViewModel.CommandSelectAll
                 });

            m_MenuBarId_41 = m_MenuBarView.AddMenuBarGroup(
                Tobi_Common_Lang.Menu_Audio, PreferredPosition.First, true,
                Tobi_Common_Lang.Menu_AudioSelection, PreferredPosition.Any, true,
                new[]
                 {
                     m_AudioPaneViewModel.CommandSelectLeft,
                     m_AudioPaneViewModel.CommandSelectRight
                 });
            m_MenuBarId_42 = m_MenuBarView.AddMenuBarGroup(
                Tobi_Common_Lang.Menu_Audio, PreferredPosition.First, true,
                Tobi_Common_Lang.Menu_AudioSelection, PreferredPosition.Any, true,
                new[]
                 {
                     m_AudioPaneViewModel.CommandSelectPreviousChunk,
                     m_AudioPaneViewModel.CommandSelectNextChunk
                 });
            m_MenuBarId_43 = m_MenuBarView.AddMenuBarGroup(
                Tobi_Common_Lang.Menu_Audio, PreferredPosition.First, true,
                Tobi_Common_Lang.Menu_AudioSelection, PreferredPosition.Any, true,
                new[]
                 {
                     m_AudioPaneViewModel.CommandBeginSelection,
                     m_AudioPaneViewModel.CommandEndSelection
                 });
            m_MenuBarId_44 = m_MenuBarView.AddMenuBarGroup(
                Tobi_Common_Lang.Menu_Audio, PreferredPosition.First, true,
                Tobi_Common_Lang.Menu_AudioSelection, PreferredPosition.Last, true,
                new[]
                 {
                     m_AudioPaneViewModel.CommandClearSelection
                 });
            m_MenuBarId_6 = m_MenuBarView.AddMenuBarGroup(
                Tobi_Common_Lang.Menu_View, PreferredPosition.First, true,
                Tobi_Common_Lang.Menu_Focus, PreferredPosition.First, false,
                new[] { m_AudioPaneViewModel.CommandFocus });

            m_MenuBarId_7 = m_MenuBarView.AddMenuBarGroup(
                Tobi_Common_Lang.Menu_View, PreferredPosition.First, true,
                Tobi_Common_Lang.Menu_Focus, PreferredPosition.Last, false,
                new[] { m_AudioPaneViewModel.CommandFocusStatusBar });


            m_Logger.Log(@"Audio commands pushed to menubar", Category.Debug, Priority.Medium);
        }

        public override void Dispose()
        {
            if (m_ToolBarsView != null)
            {
                m_ToolBarsView.RemoveToolBarGroup(m_ToolBarId_1);
            }

            if (m_MenuBarView != null)
            {
                m_MenuBarView.RemoveMenuBarGroup(Tobi_Common_Lang.Menu_Audio, m_MenuBarId_1);
                m_MenuBarView.RemoveMenuBarGroup(Tobi_Common_Lang.Menu_Audio, m_MenuBarId_15);
                m_MenuBarView.RemoveMenuBarGroup(Tobi_Common_Lang.Menu_Audio, m_MenuBarId_2);
                m_MenuBarView.RemoveMenuBarGroup(Tobi_Common_Lang.Menu_Audio, m_MenuBarId_3);
                m_MenuBarView.RemoveMenuBarGroup(Tobi_Common_Lang.Menu_Audio, m_MenuBarId_4);
                m_MenuBarView.RemoveMenuBarGroup(Tobi_Common_Lang.Menu_Audio, m_MenuBarId_42);
                m_MenuBarView.RemoveMenuBarGroup(Tobi_Common_Lang.Menu_Audio, m_MenuBarId_43);
                m_MenuBarView.RemoveMenuBarGroup(Tobi_Common_Lang.Menu_Audio, m_MenuBarId_44);
                m_MenuBarView.RemoveMenuBarGroup(Tobi_Common_Lang.Menu_Audio, m_MenuBarId_5);
                m_MenuBarView.RemoveMenuBarGroup(Tobi_Common_Lang.Menu_Audio, m_MenuBarId_14);

                m_MenuBarView.RemoveMenuBarGroup(Tobi_Common_Lang.Menu_Focus, m_MenuBarId_6);
                m_MenuBarView.RemoveMenuBarGroup(Tobi_Common_Lang.Menu_Focus, m_MenuBarId_7);

                m_MenuBarView.RemoveMenuBarGroup(Tobi_Common_Lang.Menu_AudioPlayback, m_MenuBarId_8);
                m_MenuBarView.RemoveMenuBarGroup(Tobi_Common_Lang.Menu_AudioPlayback, m_MenuBarId_9);

                m_MenuBarView.RemoveMenuBarGroup(Tobi_Common_Lang.Menu_Audio, m_MenuBarId_10);
                m_MenuBarView.RemoveMenuBarGroup(Tobi_Common_Lang.Menu_Audio, m_MenuBarId_11);
                m_MenuBarView.RemoveMenuBarGroup(Tobi_Common_Lang.Menu_Audio, m_MenuBarId_12);

                m_MenuBarView.RemoveMenuBarGroup(Tobi_Common_Lang.Menu_Edit, m_MenuBarId_13);

                m_Logger.Log(@"Audio commands removed from menubar", Category.Debug, Priority.Medium);
            }

            m_RegionManager.Regions[RegionNames.AudioPane].Deactivate(m_AudioPaneView);
            m_RegionManager.Regions[RegionNames.AudioPane].Remove(m_AudioPaneView);

            m_Logger.Log(@"Audio view removed from region", Category.Debug, Priority.Medium);
        }

        public override string Name
        {
            get { return Tobi_Plugin_AudioPane_Lang.AudioPanePlugin_Name; } 
        }

        public override string Description
        {
            get { return Tobi_Plugin_AudioPane_Lang.AudioPanePlugin_Description; } 
        }
    }
}
