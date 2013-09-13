using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AudioLib;
using Tobi.Common.MVVM;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa.data;
using urakawa.events.progress;
using urakawa.xuk;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Orientation = System.Windows.Controls.Orientation;
using ProgressBar = System.Windows.Controls.ProgressBar;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;
using AudioLib;
using PipelineWSClient;
using Saxon.Api;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Win32;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa;
using urakawa.core;
using urakawa.daisy.export;
using urakawa.daisy.import;
using urakawa.data;
using urakawa.exception;
using urakawa.media;
using urakawa.property.xml;
using urakawa.xuk;

namespace Tobi.Plugin.Urakawa
{
    public partial class UrakawaSession
    {
        public RichDelegateCommand SplitProjectCommand { get; private set; }
        public RichDelegateCommand MergeProjectCommand { get; private set; }

        private void initCommands_SplitMerge()
        {
            SplitProjectCommand = new RichDelegateCommand(
                Tobi_Plugin_Urakawa_Lang.CmdSplitProject_ShortDesc,
                Tobi_Plugin_Urakawa_Lang.CmdSplitProject_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon(@"network-workgroup"),
                //ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Save_As")),
                () =>
                {
                    if (DocumentProject == null)
                    {
                        return;
                    }

                    m_Logger.Log(@"UrakawaSession.splitProject", Category.Debug, Priority.Medium);

                },
                () => DocumentProject != null && !IsXukSpine && !HasXukSpine, // && !isMasterMergeProject && !isSubSplitProject
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_SplitProject));

            m_ShellView.RegisterRichCommand(SplitProjectCommand);
            //
            MergeProjectCommand = new RichDelegateCommand(
                Tobi_Plugin_Urakawa_Lang.CmdMergeProject_ShortDesc,
                Tobi_Plugin_Urakawa_Lang.CmdMergeProject_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon(@"network-server"),
                //ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Save_As")),
                () =>
                {
                    if (DocumentProject == null)
                    {
                        return;
                    }

                    m_Logger.Log(@"UrakawaSession.mergeProject", Category.Debug, Priority.Medium);

                },
                () => DocumentProject != null && !IsXukSpine && !HasXukSpine, // && isMasterMergeProject
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_MergeProject));

            m_ShellView.RegisterRichCommand(MergeProjectCommand);
            //
        }
    }
}
