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
using urakawa.property.channel;
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

                    bool hasAudio = DocumentProject.Presentations.Get(0).RootNode.GetDurationOfManagedAudioMediaFlattened() != null;
                    if (hasAudio)
                    {
                        messageBoxAlert(Tobi_Plugin_Urakawa_Lang.SplitMasterNoAudio, null);
                        return;
                    }

                    //DataCleanupCommand.Execute();
                    //Thread.Sleep(1000);
                    ////m_ShellView.PumpDispatcherFrames();

                    // Backup before close.
                    string docPath = DocumentFilePath;
                    Project project = DocumentProject;

                    // Closing is REQUIRED ! 
                    PopupModalWindow.DialogButton button = CheckSaveDirtyAndClose(
                        PopupModalWindow.DialogButtonsSet.OkCancel, Tobi_Plugin_Urakawa_Lang.Menu_SplitMergeProject);
                    if (!PopupModalWindow.IsButtonOkYesApply(button))
                    {
                        return;
                    }

                    string parentDirectory = Path.GetDirectoryName(docPath);
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(docPath);
                    string extension = Path.GetExtension(docPath);

                    string splitDirectory = Path.Combine(
                        parentDirectory,
                        //fileNameWithoutExtension + "_SPLIT"
                        "SPLIT_MERGE"
                        );

                    string masterDirectory = Path.Combine(
                        splitDirectory,
                        fileNameWithoutExtension
                        );

                    string destinationFilePath = Path.Combine(
                        masterDirectory,
                        "master"
                        + extension
                        ); // "0001", "0002", etc.

                    if (checkWarningFilePathLength(destinationFilePath))
                    {
                        return;
                    }

                    if (
                        //File.Exists(destinationFilePath)
                        Directory.Exists(splitDirectory)
                        )
                    {
                        if (
                            //!askUserConfirmOverwriteFileFolder(destinationFilePath, false, null)
                            !askUserConfirmOverwriteFileFolder(splitDirectory, true, null)
                            )
                        {
                            return;
                        }

                        FileDataProvider.DeleteDirectory(splitDirectory);
                    }


                    Presentation presentation = project.Presentations.Get(0);
                    TreeNode root = presentation.RootNode;
                    root.GetXmlProperty().SetAttribute("splitMerge", "", "MASTER");

                    int counter = -1;

                    TreeNode level = root.GetFirstDescendantWithXmlElement("level1");
                    while (level != null)
                    {
                        counter++;

                        int index = level.Parent.Children.IndexOf(level);
                        level.Parent.Children.Remove(level);

                        TreeNode anchorNode = presentation.TreeNodeFactory.Create();

                        //level.Parent.Children.Insert(index, anchorNode); // handles append() at index == list.count
                        level.Parent.Insert(anchorNode, index);

                        //level.Parent.InsertAfter(anchorNode, level);

                        XmlProperty xmlProp = anchorNode.GetOrCreateXmlProperty();
                        xmlProp.SetQName("h1", "");
                        xmlProp.SetAttribute("splitMergeId", "", counter.ToString());

                        TextMedia textMedia = presentation.MediaFactory.CreateTextMedia();
                        textMedia.Text = "SPLIT-MERGE: " + counter;

                        ChannelsProperty chProp = anchorNode.GetOrCreateChannelsProperty();
                        chProp.SetMedia(presentation.ChannelsManager.GetOrCreateTextChannel(), textMedia);

                        level = anchorNode.GetNextSiblingWithXmlElement("level1");
                    }

                    int total = counter + 1;

                    DocumentFilePath = docPath;
                    DocumentProject = project;

                    bool saved = false;
                    try
                    {
                        saved = saveAsCommand(destinationFilePath);
                        //SaveAsCommand.Execute();
                    }
                    finally
                    {
                        DocumentFilePath = null;
                        DocumentProject = null;
                    }

                    if (!saved)
                    {
#if DEBUG
                        Debugger.Break();
#endif //DEBUG
                        return;
                    }

                    DocumentFilePath = destinationFilePath;
                    DocumentProject = project;

                    //Uri oldUri = DocumentProject.Presentations.Get(0).RootUri;
                    //string oldDataDir = DocumentProject.Presentations.Get(0).DataProviderManager.DataFileDirectory;

                    string dirPath = Path.GetDirectoryName(destinationFilePath);
                    string prefix = Path.GetFileNameWithoutExtension(destinationFilePath);

                    DocumentProject.Presentations.Get(0).DataProviderManager.SetCustomDataFileDirectory(prefix);
                    DocumentProject.Presentations.Get(0).RootUri = new Uri(dirPath + Path.DirectorySeparatorChar, UriKind.Absolute);

                    List<string> cleanupFolders = new List<string>(total + 1);

                    try
                    {
                        string deletedDataFolderPath = DataCleanup(false);

                        if (!string.IsNullOrEmpty(deletedDataFolderPath) && Directory.Exists(deletedDataFolderPath))
                        {
                            cleanupFolders.Add(deletedDataFolderPath);
                            //FileDataProvider.DeleteDirectory(deletedDataFolderPath);
                        }

                        saved = save();
                    }
                    finally
                    {
                        DocumentFilePath = null;
                        DocumentProject = null;
                    }

                    if (!saved)
                    {
#if DEBUG
                        Debugger.Break();
#endif //DEBUG
                        return;
                    }

                    string masterFilePath = destinationFilePath;

                    for (int i = 0; i < total; i++)
                    {
                        //// Open original (untouched)

                        Uri uri = new Uri(docPath, UriKind.Absolute);
                        bool pretty = project.PrettyFormat;
                        project = new Project();
                        project.PrettyFormat = pretty;
                        OpenXukAction action = new OpenXukAction(project, uri);
                        action.ShortDescription = "...";
                        action.LongDescription = "...";
                        action.Execute();

                        if (project.Presentations.Count <= 0)
                        {
#if DEBUG
                            Debugger.Break();
#endif //DEBUG
                            continue;
                        }

                        presentation = project.Presentations.Get(0);
                        root = presentation.RootNode;
                        root.GetXmlProperty().SetAttribute("splitMerge", "", i.ToString());

                        counter = -1;

                        level = root.GetFirstDescendantWithXmlElement("level1");
                        while (level != null)
                        {
                            counter++;

                            if (counter == i)
                            {
                                XmlProperty xmlProp = level.GetOrCreateXmlProperty();
                                xmlProp.SetAttribute("splitMergeId", "", i.ToString());

                                TreeNode parent = level.Parent;
                                TreeNode toKeep = level;
                                while (parent != null)
                                {
                                    foreach (TreeNode child in parent.Children.ContentsAs_ListCopy)
                                    {
                                        if (child != toKeep)
                                        {
                                            parent.Children.Remove(child);
                                        }
                                    }

                                    toKeep = parent;
                                    parent = parent.Parent;
                                }

                                break;
                            }

                            level = level.GetNextSiblingWithXmlElement("level1");
                        }



                        string subDirectory = Path.Combine(splitDirectory, fileNameWithoutExtension + "_" + i);
                        destinationFilePath = Path.Combine(
                            subDirectory,
                            i + extension
                            );

                        DocumentFilePath = docPath;
                        DocumentProject = project;

                        saved = false;
                        try
                        {
                            saved = saveAsCommand(destinationFilePath);
                            //SaveAsCommand.Execute();
                        }
                        finally
                        {
                            DocumentFilePath = null;
                            DocumentProject = null;
                        }

                        if (!saved)
                        {
#if DEBUG
                            Debugger.Break();
#endif //DEBUG

                            continue;
                        }

                        DocumentFilePath = destinationFilePath;
                        DocumentProject = project;

                        //Uri oldUri = DocumentProject.Presentations.Get(0).RootUri;
                        //string oldDataDir = DocumentProject.Presentations.Get(0).DataProviderManager.DataFileDirectory;

                        string dirPath_ = Path.GetDirectoryName(destinationFilePath);
                        string prefix_ = Path.GetFileNameWithoutExtension(destinationFilePath);

                        DocumentProject.Presentations.Get(0).DataProviderManager.SetCustomDataFileDirectory(prefix_);
                        DocumentProject.Presentations.Get(0).RootUri = new Uri(dirPath_ + Path.DirectorySeparatorChar, UriKind.Absolute);

                        try
                        {
                            string deletedDataFolderPath_ = DataCleanup(false);

                            if (!string.IsNullOrEmpty(deletedDataFolderPath_) && Directory.Exists(deletedDataFolderPath_))
                            {
                                cleanupFolders.Add(deletedDataFolderPath_);
                                //FileDataProvider.DeleteDirectory(deletedDataFolderPath_);
                            }

                            saved = save();
                        }
                        finally
                        {
                            DocumentFilePath = null;
                            DocumentProject = null;
                        }

                        if (!saved)
                        {
#if DEBUG
                            Debugger.Break();
#endif //DEBUG
                            continue;
                        }
                    }

                    foreach (string cleanupFolder in cleanupFolders)
                    {
                        FileDataProvider.DeleteDirectory(cleanupFolder);
                    }

                    // Conclude: open master project, show folder with sub projects

                    DocumentFilePath = null;
                    DocumentProject = null;
                    try
                    {
                        OpenFile(masterFilePath);
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandler.Handle(ex, false, m_ShellView);
                    }

                    m_ShellView.ExecuteShellProcess(splitDirectory);
                },
                () => DocumentProject != null && !IsXukSpine && !HasXukSpine && !IsSplitMaster && !IsSplitSub,
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

                    bool hasAudio = DocumentProject.Presentations.Get(0).RootNode.GetDurationOfManagedAudioMediaFlattened() != null;
                    if (hasAudio)
                    {
                        messageBoxAlert(Tobi_Plugin_Urakawa_Lang.SplitMasterNoAudio, null);
                        return;
                    }

                    m_Logger.Log(@"UrakawaSession.mergeProject", Category.Debug, Priority.Medium);

                },
                () => DocumentProject != null && !IsXukSpine && !HasXukSpine && IsSplitMaster,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_MergeProject));

            m_ShellView.RegisterRichCommand(MergeProjectCommand);
            //
        }
    }
}
