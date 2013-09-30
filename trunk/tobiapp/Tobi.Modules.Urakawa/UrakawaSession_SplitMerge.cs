using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using AudioLib;
using Microsoft.Internal;
using Tobi.Common.MVVM;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa.data;
using urakawa.progress;
using urakawa.property.channel;
using urakawa.xuk;
using System.Collections.Generic;
using urakawa;
using urakawa.core;
using urakawa.media;
using urakawa.property.xml;
using XmlAttribute = urakawa.property.xml.XmlAttribute;

namespace Tobi.Plugin.Urakawa
{
    public partial class UrakawaSession
    {
        public class MergeAction : DualCancellableProgressReporter
        {
            private readonly UrakawaSession m_session;
            private readonly string docPath;
            private readonly Project project;
            private readonly string destinationFilePath;
            private readonly string topDirectory;
            private readonly string fileNameWithoutExtension;
            private readonly string extension;

            public MergeAction(UrakawaSession session, string docPath_, Project project_, string destinationFilePath_, string topDirectory_, string fileNameWithoutExtension_, string extension_)
            {
                m_session = session;
                docPath = docPath_;
                project = project_;
                destinationFilePath = destinationFilePath_;
                topDirectory = topDirectory_;
                fileNameWithoutExtension = fileNameWithoutExtension_;
                extension = extension_;
            }

            public override void DoWork()
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                try
                {
                    Merge();
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debugger.Break();
#endif
                    throw ex;
                }
                finally
                {
                    stopWatch.Stop();
                    Console.WriteLine(@"......MERGE milliseconds: " + stopWatch.ElapsedMilliseconds);
                }
            }

            public void Merge()
            {
                m_session.DocumentFilePath = docPath;
                m_session.DocumentProject = project;

                bool saved = false;
                try
                {
                    saved = m_session.saveAsCommand(destinationFilePath, true);
                    //SaveAsCommand.Execute();
                }
                finally
                {
                    m_session.DocumentFilePath = null;
                    m_session.DocumentProject = null;
                }

                if (!saved)
                {
#if DEBUG
                    Debugger.Break();
#endif //DEBUG
                    RequestCancellation = true;
                    return;
                }

                m_session.DocumentFilePath = destinationFilePath;
                m_session.DocumentProject = project;

                try
                {

                    //Uri oldUri = DocumentProject.Presentations.Get(0).RootUri;
                    //string oldDataDir = DocumentProject.Presentations.Get(0).DataProviderManager.DataFileDirectory;

                    string dirPath = Path.GetDirectoryName(destinationFilePath);
                    string prefix = Path.GetFileNameWithoutExtension(destinationFilePath);

                    m_session.DocumentProject.Presentations.Get(0).DataProviderManager.SetCustomDataFileDirectory(prefix);
                    m_session.DocumentProject.Presentations.Get(0).RootUri = new Uri(dirPath + Path.DirectorySeparatorChar, UriKind.Absolute);



                    Presentation presentation = project.Presentations.Get(0);
                    TreeNode root = presentation.RootNode;

                    int counter = -1;

                    TreeNode hd = root.GetFirstDescendantWithXmlElement("h1");
                    while (hd != null)
                    {
                        XmlAttribute xmlAttr = hd.GetXmlProperty().GetAttribute("splitMergeId");
                        if (xmlAttr != null)
                        {
                            counter++;
                            DebugFix.Assert(counter == Int32.Parse(xmlAttr.Value));
                        }

                        hd = hd.GetNextSiblingWithXmlElement("h1");
                    }
                    int total = counter + 1;
                    counter = -1;
                    hd = root.GetFirstDescendantWithXmlElement("h1");
                    while (hd != null)
                    {
                        if (RequestCancellation)
                        {
                            return;
                        }

                        XmlAttribute xmlAttr = hd.GetXmlProperty().GetAttribute("splitMergeId");
                        if (xmlAttr != null)
                        {
                            counter++;
                            //DebugFix.Assert(counter == Int32.Parse(xmlAttr.Value));
                            
                            int i = counter + 1;
                            reportProgress(100 * i / total, i + " / " + total);

                            //Thread.Sleep(500);

                            string xukFolder = Path.Combine(topDirectory, fileNameWithoutExtension + "_" + counter);
                            string xukPath = Path.Combine(xukFolder, counter + extension);

                            try
                            {
                                Uri uri = new Uri(xukPath, UriKind.Absolute);
                                bool pretty = project.PrettyFormat;
                                Project subproject = new Project();
                                subproject.PrettyFormat = pretty;
                                OpenXukAction action = new OpenXukAction(subproject, uri);
                                action.ShortDescription = "...";
                                action.LongDescription = "...";
                                action.Execute();

                                Presentation subpresentation = subproject.Presentations.Get(0);
                                TreeNode subroot = subpresentation.RootNode;
                                XmlAttribute attrCheck = subroot.GetXmlProperty().GetAttribute("splitMerge");
                                DebugFix.Assert(attrCheck.Value == counter.ToString());


                                TreeNode level = subroot.GetFirstDescendantWithXmlElement("level1");
                                while (level != null)
                                {
                                    attrCheck = level.GetXmlProperty().GetAttribute("splitMergeId");
                                    if (attrCheck != null)
                                    {
                                        DebugFix.Assert(counter == Int32.Parse(attrCheck.Value));
                                        level.GetXmlProperty().RemoveAttribute(attrCheck);

                                        //TextMedia txtMedia = (TextMedia)hd.GetChannelsProperty().GetMedia(presentation.ChannelsManager.GetOrCreateTextChannel());
                                        //txtMedia.Text = "MERGED_OK_" + counter;

                                        TreeNode importedLevel = level.Export(presentation);

                                        TreeNode parent = hd.Parent;
                                        int index = parent.Children.IndexOf(hd);
                                        parent.RemoveChild(index);
                                        parent.Insert(importedLevel, index);
                                        hd = importedLevel;

                                        break;
                                    }

                                    level = level.GetNextSiblingWithXmlElement("level1");
                                }
                            }
                            catch (Exception ex)
                            {
                                //messageBoxAlert("PROBLEM:\n " + xukPath, null);
                                //m_session.messageBoxText("MERGE PROBLEM", xukPath, ex.Message);

                                throw ex;
                            }
                        }

                        hd = hd.GetNextSiblingWithXmlElement("h1");
                    }

                    //int total = counter + 1;

                    string deletedDataFolderPath = m_session.DataCleanup(false);

                    if (!string.IsNullOrEmpty(deletedDataFolderPath) && Directory.Exists(deletedDataFolderPath))
                    {
                        //FileDataProvider.DeleteDirectory(deletedDataFolderPath);
                        if (Directory.GetFiles(deletedDataFolderPath).Length != 0 ||
                            Directory.GetDirectories(deletedDataFolderPath).Length != 0)
                        {
                            m_session.m_ShellView.ExecuteShellProcess(deletedDataFolderPath);
                        }
                    }

                    root.GetXmlProperty().RemoveAttribute("splitMerge", "");

                    saved = m_session.save(true);
                }
                finally
                {
                    m_session.DocumentFilePath = null;
                    m_session.DocumentProject = null;
                }

                if (!saved)
                {
#if DEBUG
                    Debugger.Break();
#endif //DEBUG
                    RequestCancellation = true;
                    return;
                }
            }
        }

        public class SplitAction : DualCancellableProgressReporter
        {
            private readonly UrakawaSession m_session;
            private readonly int m_total;
            private readonly string m_docPath;
            private readonly bool m_pretty;
            private readonly List<string> m_cleanupFolders;
            private readonly string m_splitDirectory;
            private readonly string m_fileNameWithoutExtension;
            private readonly string m_extension;

            public SplitAction(UrakawaSession session, int total, string docPath, bool pretty, List<string> cleanupFolders, string splitDirectory, string fileNameWithoutExtension, string extension)
            {
                m_session = session;
                m_total = total;
                m_docPath = docPath;
                m_pretty = pretty;
                m_cleanupFolders = cleanupFolders;
                m_splitDirectory = splitDirectory;
                m_fileNameWithoutExtension = fileNameWithoutExtension;
                m_extension = extension;
            }

            public override void DoWork()
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                try
                {
                    Split();
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debugger.Break();
#endif
                    throw ex;
                }
                finally
                {
                    stopWatch.Stop();
                    Console.WriteLine(@"......SPLIT milliseconds: " + stopWatch.ElapsedMilliseconds);
                }
            }

            public void Split()
            {
                for (int i = 0; i < m_total; i++)
                {
                    int j = i + 1;

                    reportProgress(100 * j / m_total, j + " / " + m_total);

                    //Thread.Sleep(1000);

                    if (RequestCancellation)
                    {
                        return;
                    }

                    //// Open original (untouched)

                    Uri uri = new Uri(m_docPath, UriKind.Absolute);
                    Project project = new Project();
                    project.PrettyFormat = m_pretty;
                    OpenXukAction action = new OpenXukAction(project, uri);
                    action.ShortDescription = "...";
                    action.LongDescription = "...";
                    action.Execute();

                    if (project.Presentations.Count <= 0)
                    {
#if DEBUG
                        Debugger.Break();
#endif //DEBUG
                        RequestCancellation = true;
                        return;
                    }

                    Presentation presentation = project.Presentations.Get(0);
                    TreeNode root = presentation.RootNode;
                    root.GetXmlProperty().SetAttribute("splitMerge", "", i.ToString());

                    int counter = -1;

                    TreeNode level = root.GetFirstDescendantWithXmlElement("level1");
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
                                        parent.RemoveChild(child);
                                    }
                                }

                                toKeep = parent;
                                parent = parent.Parent;
                            }

                            break;
                        }

                        level = level.GetNextSiblingWithXmlElement("level1");
                    }


                    string subDirectory = Path.Combine(m_splitDirectory, m_fileNameWithoutExtension + "_" + i);
                    string destinationFilePath = Path.Combine(subDirectory, i + m_extension);

                    m_session.DocumentFilePath = m_docPath;
                    m_session.DocumentProject = project;

                    bool saved = false;
                    try
                    {
                        saved = m_session.saveAsCommand(destinationFilePath, true);
                        //SaveAsCommand.Execute();
                    }
                    finally
                    {
                        m_session.DocumentFilePath = null;
                        m_session.DocumentProject = null;
                    }

                    if (!saved)
                    {
#if DEBUG
                        Debugger.Break();
#endif //DEBUG

                        RequestCancellation = true;
                        return;
                    }

                    m_session.DocumentFilePath = destinationFilePath;
                    m_session.DocumentProject = project;

                    //Uri oldUri = DocumentProject.Presentations.Get(0).RootUri;
                    //string oldDataDir = DocumentProject.Presentations.Get(0).DataProviderManager.DataFileDirectory;

                    string dirPath_ = Path.GetDirectoryName(destinationFilePath);
                    string prefix_ = Path.GetFileNameWithoutExtension(destinationFilePath);

                    m_session.DocumentProject.Presentations.Get(0).DataProviderManager.SetCustomDataFileDirectory(prefix_);
                    m_session.DocumentProject.Presentations.Get(0).RootUri = new Uri(dirPath_ + Path.DirectorySeparatorChar, UriKind.Absolute);

                    try
                    {
                        string deletedDataFolderPath_ = m_session.DataCleanup(false);

                        if (!string.IsNullOrEmpty(deletedDataFolderPath_) && Directory.Exists(deletedDataFolderPath_))
                        {
                            m_cleanupFolders.Add(deletedDataFolderPath_);
                            //FileDataProvider.DeleteDirectory(deletedDataFolderPath_);
                        }

                        saved = m_session.save(true);
                    }
                    finally
                    {
                        m_session.DocumentFilePath = null;
                        m_session.DocumentProject = null;
                    }

                    if (!saved)
                    {
#if DEBUG
                        Debugger.Break();
#endif //DEBUG
                        RequestCancellation = true;
                        return;
                    }
                }
            }
        }

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

                    //TODO clone project here instead of OpenXukAction() for every subproject

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

                    string splitDirectory = Path.Combine(parentDirectory, "SPLIT_MERGE");

                    string masterDirectory = Path.Combine(splitDirectory, fileNameWithoutExtension);

                    string destinationFilePath = Path.Combine(masterDirectory, "master" + extension);

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

                        TreeNode parent = level.Parent;
                        int index = parent.Children.IndexOf(level);
                        parent.RemoveChild(index);

                        TreeNode anchorNode = presentation.TreeNodeFactory.Create();
                        parent.Insert(anchorNode, index);

                        XmlProperty xmlProp = anchorNode.GetOrCreateXmlProperty();
                        xmlProp.SetQName("h1", "");
                        xmlProp.SetAttribute("splitMergeId", "", counter.ToString());

                        TextMedia textMedia = presentation.MediaFactory.CreateTextMedia();
                        textMedia.Text = "PART " + counter;

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
                        saved = saveAsCommand(destinationFilePath, true);
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

                        saved = save(true);
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


                    bool cancelled = false;
                    bool error = false;

                    var action = new SplitAction(this, total, docPath, project.PrettyFormat, cleanupFolders, splitDirectory, fileNameWithoutExtension, extension);

                    Action cancelledCallback =
                        () =>
                        {
                            cancelled = true;

                        };

                    Action finishedCallback =
                        () =>
                        {
                            cancelled = false;

                        };

                    error = m_ShellView.RunModalCancellableProgressTask(true,
                        Tobi_Plugin_Urakawa_Lang.CmdSplitProject_ShortDesc, action,
                        cancelledCallback,
                        finishedCallback
                        );

                    DocumentFilePath = null;
                    DocumentProject = null;

                    if (!cancelled && !error)
                    {
                        foreach (string cleanupFolder in cleanupFolders)
                        {
                            FileDataProvider.DeleteDirectory(cleanupFolder);
                        }

                        // Conclude: open master project, show folder with sub projects

                        try
                        {
                            OpenFile(masterFilePath);
                        }
                        catch (Exception ex)
                        {
                            ExceptionHandler.Handle(ex, false, m_ShellView);
                        }
                    }

                    m_ShellView.ExecuteShellProcess(splitDirectory);
                },
                () => DocumentProject != null && !IsXukSpine && !HasXukSpine && !IsSplitMaster && !IsSplitSub,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_ProjectSplit));

            m_ShellView.RegisterRichCommand(SplitProjectCommand);

            //























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
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(parentDirectory);
                    string extension = Path.GetExtension(docPath);

                    string topDirectory = Path.GetDirectoryName(parentDirectory);
                    string mergeDirectory = Path.Combine(topDirectory, "_MERGE");

                    string destinationFilePath = Path.Combine(mergeDirectory, fileNameWithoutExtension + extension);

                    if (checkWarningFilePathLength(destinationFilePath))
                    {
                        return;
                    }

                    if (
                        //File.Exists(destinationFilePath)
                        Directory.Exists(mergeDirectory)
                        )
                    {
                        if (
                            //!askUserConfirmOverwriteFileFolder(destinationFilePath, false, null)
                            !askUserConfirmOverwriteFileFolder(mergeDirectory, true, null)
                            )
                        {
                            return;
                        }

                        FileDataProvider.DeleteDirectory(mergeDirectory);
                    }



                    bool cancelled = false;
                    bool error = false;

                    var action = new MergeAction(this, docPath, project, destinationFilePath, topDirectory, fileNameWithoutExtension, extension);

                    Action cancelledCallback =
                        () =>
                        {
                            cancelled = true;

                        };

                    Action finishedCallback =
                        () =>
                        {
                            cancelled = false;

                        };

                    error = m_ShellView.RunModalCancellableProgressTask(true,
                        Tobi_Plugin_Urakawa_Lang.CmdMergeProject_ShortDesc, action,
                        cancelledCallback,
                        finishedCallback
                        );



                    // Conclude: open master project, show folder with sub projects

                    DocumentFilePath = null;
                    DocumentProject = null;
                    if (!cancelled && !error)
                    {
                        try
                        {
                            OpenFile(destinationFilePath);
                        }
                        catch (Exception ex)
                        {
                            DocumentFilePath = null;
                            DocumentProject = null;

                            ExceptionHandler.Handle(ex, false, m_ShellView);
                        }
                    }
                    //m_ShellView.ExecuteShellProcess(mergeDirectory);
                },
                () => DocumentProject != null && !IsXukSpine && !HasXukSpine && IsSplitMaster,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_ProjectMerge));

            m_ShellView.RegisterRichCommand(MergeProjectCommand);
            //
        }
    }
}
