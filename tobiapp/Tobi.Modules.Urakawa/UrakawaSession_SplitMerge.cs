using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using AudioLib;
using Microsoft.Internal;
using Tobi.Common;
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
#if SPLIT_MERGE_FRAGMENT
        protected static TreeNode getNextSplitMergeFragment(TreeNode context)
        {
            if (context.Parent == null)
            {
                return context.GetFirstDescendantWithXmlElement("level2");
            }
            return context.GetNextSiblingWithXmlElement("level2");
        }
#else
        protected static TreeNode getNextSplitMergeMark(TreeNode context)
        {
            if (context.Parent == null)
            {
                return context.GetFirstDescendantWithMark();
            }
            return context.GetNextSiblingWithMark();
        }
#endif //SPLIT_FRAGMENT

        public class MergeAction : DualCancellableProgressReporter
        {
            private readonly UrakawaSession m_session;
            private readonly string docPath;
            private readonly Project project;
            private readonly string destinationFilePath;
            private readonly string splitDirectory;
            private readonly string fileNameWithoutExtension;
            private readonly string extension;

            public MergeAction(UrakawaSession session, string docPath_, Project project_, string destinationFilePath_, string splitDirectory_, string fileNameWithoutExtension_, string extension_)
            {
                m_session = session;
                docPath = docPath_;
                project = project_;
                destinationFilePath = destinationFilePath_;
                splitDirectory = splitDirectory_;
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

                    TreeNode hd = root.GetFirstDescendantWithXmlElement("hd");
                    while (hd != null)
                    {
                        XmlAttribute xmlAttr = hd.GetXmlProperty().GetAttribute("splitMergeId");
                        if (xmlAttr != null)
                        {
                            counter++;
                            DebugFix.Assert(counter == Int32.Parse(xmlAttr.Value));
                        }

                        hd = hd.GetNextSiblingWithXmlElement("hd");
                    }
                    int total = counter + 1;
                    counter = -1;
                    hd = root.GetFirstDescendantWithXmlElement("hd");
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

                            string xukFolder = Path.Combine(splitDirectory, fileNameWithoutExtension + "_" + counter);
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

#if SPLIT_MERGE_FRAGMENT
                                TreeNode level = UrakawaSession.getNextSplitMergeFragment(subroot);
#else
                                TreeNode level = UrakawaSession.getNextSplitMergeMark(subroot);
#endif //SPLIT_FRAGMENT
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

#if SPLIT_MERGE_FRAGMENT
                                    level = UrakawaSession.getNextSplitMergeFragment(level);
#else
                                    level = UrakawaSession.getNextSplitMergeMark(level);
#endif //SPLIT_FRAGMENT
                                }
                            }
                            catch (Exception ex)
                            {
                                //messageBoxAlert("PROBLEM:\n " + xukPath, null);
                                //m_session.messageBoxText("MERGE PROBLEM", xukPath, ex.Message);

                                throw ex;
                            }
                        }

                        hd = hd.GetNextSiblingWithXmlElement("hd");
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
            //private readonly List<string> m_cleanupFolders;
            private readonly string m_splitDirectory;
            private readonly string m_fileNameWithoutExtension;
            private readonly string m_extension;

            //List<string> cleanupFolders
            public SplitAction(UrakawaSession session, int total, string docPath, bool pretty, string splitDirectory, string fileNameWithoutExtension, string extension)
            {
                m_session = session;
                m_total = total;
                m_docPath = docPath;
                m_pretty = pretty;
                //m_cleanupFolders = cleanupFolders;
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

#if SPLIT_MERGE_FRAGMENT
                    TreeNode mark = UrakawaSession.getNextSplitMergeFragment(root);
                    
                    while (mark != null)
                    {
                        counter++;

                        if (counter == i)
                        {
                            XmlProperty xmlProp = mark.GetOrCreateXmlProperty();
                            xmlProp.SetAttribute("splitMergeId", "", i.ToString());

                            TreeNode parent = mark.Parent;
                            TreeNode toKeep = mark;
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

                        mark = UrakawaSession.getNextSplitMergeFragment(mark);
                    }
#else
                    TreeNode mark = UrakawaSession.getNextSplitMergeMark(root);

                    while (mark != null)
                    {
                        counter++;

                        TreeNode nextMark = UrakawaSession.getNextSplitMergeMark(mark);

                        if (counter == i)
                        {
                            XmlProperty xmlProp = mark.GetOrCreateXmlProperty();
                            xmlProp.SetAttribute("splitMergeId", "", i.ToString());
                            // TODO: merge check that XmlProp element name == null then txt node! (remove attr AND XmlProp)

                            // purge node content before mark:
                            {
                                TreeNode topChild = mark;
                                while (topChild != null && topChild.Parent != null)
                                {
                                    TreeNode child = topChild.Parent.Children.Count > 0
                                        ? topChild.Parent.Children.Get(0)
                                        : null;

                                    //foreach (TreeNode child in topChild.Parent.Children.ContentsAs_Enumerable)
                                    while (child != null)
                                    {
                                        if (child == topChild)
                                        {
                                            topChild = topChild.Parent;
                                            break;
                                        }

                                        TreeNode nextChild = child.NextSibling;

                                        topChild.Parent.RemoveChild(child);

                                        child = nextChild;
                                    }
                                }
                            }

                            // purge node content after following mark (including mark itself):

                            if (nextMark != null)
                            {
                                TreeNode topChild = nextMark;
                                while (topChild != null && topChild.Parent != null)
                                {
                                    TreeNode child = topChild.Parent.Children.Count > 0
                                        ? topChild.Parent.Children.Get(topChild.Parent.Children.Count - 1)
                                        : null;

                                    //foreach (TreeNode child in topChild.Parent.Children.ContentsAs_Enumerable)
                                    while (child != null)
                                    {
                                        if (child == topChild)
                                        {
                                            topChild = topChild.Parent;
                                            break;
                                        }

                                        TreeNode prevChild = child.PreviousSibling;

                                        topChild.Parent.RemoveChild(child);

                                        child = prevChild;
                                    }
                                }

                                nextMark.Parent.RemoveChild(nextMark);
                            }

                            int subcounter = -1;

                            TreeNode anchorNode = mark;
                            while (anchorNode != null)
                            {
                                TreeNode nextToRemove = anchorNode.NextSibling;
                                if (nextToRemove != null)
                                {
                                    if (nextMark == null || nextMark != nextToRemove && !nextMark.IsDescendantOf(nextToRemove))
                                    {
                                        subcounter++;

                                        xmlProp = nextToRemove.GetOrCreateXmlProperty();
                                        xmlProp.SetAttribute("splitMergeSubId", "", subcounter.ToString());
                                        // TODO: merge check that XmlProp element name == null then txt node! (remove attr AND XmlProp)

                                        anchorNode = nextToRemove;
                                        continue;
                                    }

                                    if (nextMark == nextToRemove)
                                    {
                                        anchorNode = null; //break higher while
                                        break;
                                    }

                                    //assert nextMark.IsDescendantOf(nextToRemove)
                                    TreeNode topChild = nextMark;
                                    while (topChild != null && topChild.Parent != null && topChild.Parent != nextToRemove.Parent) //heading anchorNode
                                    {
                                        TreeNode child = topChild.Parent.Children.Count > 0
                                            ? topChild.Parent.Children.Get(0)
                                            : null;

                                        //foreach (TreeNode child in topChild.Parent.Children.ContentsAs_Enumerable)
                                        while (child != null)
                                        {
                                            if (child == topChild)
                                            {
                                                topChild = topChild.Parent;
                                                break;
                                            }

                                            subcounter++;

                                            xmlProp = child.GetOrCreateXmlProperty();
                                            xmlProp.SetAttribute("splitMergeSubId", "", subcounter.ToString());
                                            // TODO: merge check that XmlProp element name == null then txt node! (remove attr AND XmlProp)

                                            anchorNode = child;
                                            child = anchorNode.NextSibling;
                                        }
                                    }

                                    anchorNode = null; //break higher while
                                    break;
                                }
                                else
                                {
                                    anchorNode = anchorNode.Parent;
                                }
                            }

                            break;
                        }

                        mark = nextMark;
                    }

#endif //SPLIT_FRAGMENT


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
                            //m_cleanupFolders.Add(deletedDataFolderPath_);
                            FileDataProvider.DeleteDirectory(deletedDataFolderPath_);
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

        private TreeNode replaceFragmentWithAnchor(TreeNode level, String headingText, Presentation presentation, String headingAttributeName, String headingAttributeValue)
        {
#if DEBUG
            if (level.GetXmlProperty() == null)
            {
                Debugger.Break(); // text?
            }
#endif
            TreeNode parent = level.Parent;

            int index = parent.Children.IndexOf(level);
            parent.RemoveChild(index);

            TreeNode anchorNode = presentation.TreeNodeFactory.Create();
            parent.Insert(anchorNode, index);

            XmlProperty xmlProp = anchorNode.GetOrCreateXmlProperty();
            xmlProp.SetQName("hd", "");
            xmlProp.SetAttribute(headingAttributeName, "", headingAttributeValue);

            TextMedia textMedia = presentation.MediaFactory.CreateTextMedia();
            textMedia.Text = headingText;

            ChannelsProperty chProp = anchorNode.GetOrCreateChannelsProperty();
            chProp.SetMedia(presentation.ChannelsManager.GetOrCreateTextChannel(), textMedia);

            return anchorNode;
        }

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

                    bool hasAudio = false;
                    //hasAudio = DocumentProject.Presentations.Get(0).RootNode.GetDurationOfManagedAudioMediaFlattened() != null;
                    TreeNode nodeTestAudio = DocumentProject.Presentations.Get(0).RootNode;
#if SPLIT_MERGE_FRAGMENT
                    nodeTestAudio = UrakawaSession.getNextSplitMergeFragment(nodeTestAudio);
#else
                    nodeTestAudio = UrakawaSession.getNextSplitMergeMark(nodeTestAudio);
#endif //SPLIT_FRAGMENT

                    if (nodeTestAudio == null)
                    {
                        messageBoxAlert(Tobi_Plugin_Urakawa_Lang.SplitNothing, null);
                        return;
                    }

                    while (nodeTestAudio != null)
                    {
                        if (nodeTestAudio.GetFirstAncestorWithManagedAudio() != null)
                        {
                            hasAudio = true;
                            break;
                        }
#if SPLIT_MERGE_FRAGMENT
                        nodeTestAudio = UrakawaSession.getNextSplitMergeFragment(nodeTestAudio);
#else
                        nodeTestAudio = UrakawaSession.getNextSplitMergeMark(nodeTestAudio);
#endif //SPLIT_FRAGMENT
                    }
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

                    string splitDirectory = Path.Combine(parentDirectory, "_SPLIT");

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

#if SPLIT_MERGE_FRAGMENT
                    TreeNode mark = UrakawaSession.getNextSplitMergeFragment(root);
                    
                    while (mark != null)
                    {
                        counter++;
                        TreeNode heading = replaceFragmentWithAnchor(mark, "PART " + counter, presentation, "splitMergeId", counter.ToString());

                        mark = UrakawaSession.getNextSplitMergeFragment(heading);
                    }
#else
                    TreeNode mark = UrakawaSession.getNextSplitMergeMark(root);

                    while (mark != null)
                    {
                        counter++;
                        TreeNode heading = replaceFragmentWithAnchor(mark, "PART " + counter, presentation, "splitMergeId", counter.ToString());

                        mark = UrakawaSession.getNextSplitMergeMark(heading);

                        int subcounter = -1;

                        TreeNode anchorNode = heading;
                        while (anchorNode != null)
                        {
                            TreeNode nextToRemove = anchorNode.NextSibling;
                            if (nextToRemove != null)
                            {
                                if (mark == null || mark != nextToRemove && !mark.IsDescendantOf(nextToRemove))
                                {
                                    subcounter++;
                                    anchorNode = replaceFragmentWithAnchor(nextToRemove, "PART " + counter + " - " + subcounter, presentation, "splitMergeSubId", subcounter.ToString());
                                    continue;
                                }

                                if (mark == nextToRemove)
                                {
                                    anchorNode = null; //break higher while
                                    break;
                                }

                                //assert mark.IsDescendantOf(nextToRemove)
                                TreeNode topChild = mark;
                                while (topChild != null && topChild.Parent != null && topChild.Parent != nextToRemove.Parent) //heading anchorNode
                                {
                                    TreeNode child = topChild.Parent.Children.Count > 0
                                        ? topChild.Parent.Children.Get(0)
                                        : null;

                                    //foreach (TreeNode child in topChild.Parent.Children.ContentsAs_Enumerable)
                                    while (child != null)
                                    {
                                        if (child == topChild)
                                        {
                                            topChild = topChild.Parent;
                                            break;
                                        }

                                        subcounter++;
                                        anchorNode = replaceFragmentWithAnchor(child,
                                            "PART " + counter + " - " + subcounter, presentation, "splitMergeSubId",
                                            subcounter.ToString());

                                        child = anchorNode.NextSibling;
                                    }
                                }

                                anchorNode = null; //break higher while
                                break;
                            }
                            else
                            {
                                anchorNode = anchorNode.Parent;
                            }
                        }
                    }
#endif //SPLIT_FRAGMENT

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

                    //List<string> cleanupFolders = new List<string>(total + 1);

                    try
                    {
                        string deletedDataFolderPath = DataCleanup(false);

                        if (!string.IsNullOrEmpty(deletedDataFolderPath) && Directory.Exists(deletedDataFolderPath))
                        {
                            //cleanupFolders.Add(deletedDataFolderPath);
                            FileDataProvider.DeleteDirectory(deletedDataFolderPath);
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

                    //cleanupFolders
                    var action = new SplitAction(this, total, docPath, project.PrettyFormat, splitDirectory, fileNameWithoutExtension, extension);

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
                        UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Urakawa_Lang.CmdSplitProject_ShortDesc), action,
                        cancelledCallback,
                        finishedCallback
                        );

                    DocumentFilePath = null;
                    DocumentProject = null;

                    if (!cancelled && !error)
                    {
                        //foreach (string cleanupFolder in cleanupFolders)
                        //{
                        //    FileDataProvider.DeleteDirectory(cleanupFolder);
                        //}

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

                    bool hasAudio = false;
                    //hasAudio = DocumentProject.Presentations.Get(0).RootNode.GetDurationOfManagedAudioMediaFlattened() != null;
                    TreeNode nodeTestAudio = DocumentProject.Presentations.Get(0).RootNode;
                    nodeTestAudio = nodeTestAudio.GetFirstDescendantWithXmlElement("hd");
                    while (nodeTestAudio != null)
                    {
                        XmlAttribute xmlAttr = nodeTestAudio.GetXmlProperty().GetAttribute("splitMergeId");
                        if (xmlAttr != null)
                        {
                            if (nodeTestAudio.GetFirstAncestorWithManagedAudio() != null || nodeTestAudio.GetManagedAudioMedia() != null)
                            {
                                hasAudio = true;
                                break;
                            }
                        }
                        nodeTestAudio = nodeTestAudio.GetNextSiblingWithXmlElement("hd");
                    }
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
                    string splitDirectory = Path.GetDirectoryName(parentDirectory);
                    string containerFolder = Path.GetDirectoryName(splitDirectory);
                    string mergeDirectory = Path.Combine(containerFolder, "_MERGE");

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

                    var action = new MergeAction(this, docPath, project, destinationFilePath, splitDirectory, fileNameWithoutExtension, extension);

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
                        UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Urakawa_Lang.CmdMergeProject_ShortDesc), action,
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

                    //UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Urakawa_Lang.CmdDataCleanup_ShortDesc)
                    if (askUser(Tobi_Plugin_Urakawa_Lang.DeleteSplitFiles, splitDirectory)
                        &&
                        askUser(Tobi_Plugin_Urakawa_Lang.DeleteSplitFilesConfirm, splitDirectory))
                    {
                        FileDataProvider.DeleteDirectory(splitDirectory);

//                        try
//                        {
//                            FileDataProvider.TryDeleteDirectory(topDirectory, false);
//                        }
//                        catch (Exception ex)
//                        {
//#if DEBUG
//                            Debugger.Break();
//#endif // DEBUG
//                            Console.WriteLine(ex.Message);
//                            Console.WriteLine(ex.StackTrace);
//                        }
                    }
                    else
                    {
                        m_ShellView.ExecuteShellProcess(containerFolder);
                    }
                },
                () => DocumentProject != null && !IsXukSpine && !HasXukSpine && IsSplitMaster,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_ProjectMerge));

            m_ShellView.RegisterRichCommand(MergeProjectCommand);
            //
        }
    }
}
