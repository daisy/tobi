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
        private static readonly string SPLIT_MERGE = "splitMerge";
        private static readonly string SPLIT_MERGE_ID = "splitMergeId";
        private static readonly string SPLIT_MERGE_SUB_ID = "splitMergeSubId";
        private static readonly string MASTER_SUFFIX = "__MASTER";

        // This tree-walk method ensures nested marked nodes are ignored / bypassed.
        // Also discards text-only nodes (to avoid introducing a distruptive XML wrapper element to support the splitMerge attribute)
        protected static TreeNode getNextSplitMergeMark(TreeNode context)
        {
            TreeNode elem = context.Parent == null ? context.GetFirstDescendantWithMark() : context.GetNextSiblingWithMark();

            while (elem != null && !elem.HasXmlProperty) //string.IsNullOrEmpty(elem.GetXmlElementLocalName())
            {
#if DEBUG
                Debugger.Break();
#endif
                elem = elem.GetNextSiblingWithMark();
            }

            return elem;
        }

        protected static TreeNode findSubSplitMergeAnchor(TreeNode root, int counter, int subcounter)
        {
            TreeNode hd = root.GetFirstDescendantWithXmlAttribute(SPLIT_MERGE_SUB_ID);

            while (hd != null)
            {
                XmlAttribute attrCheck = hd.GetXmlProperty().GetAttribute(SPLIT_MERGE_SUB_ID);
                string val = attrCheck.Value;
                string val1 = "-1";
                string val2 = "-1";
                int isep = val.IndexOf('~');
                if (isep >= 0 && isep < val.Length - 1)
                {
                    val1 = val.Substring(0, isep);
                    val2 = val.Substring(isep + 1);
                }
                if (counter == Int32.Parse(val1) && subcounter == Int32.Parse(val2))
                {
                    return hd;
                }

                hd = hd.GetNextSiblingWithXmlAttribute(SPLIT_MERGE_SUB_ID);
            }

            return null;
        }

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
                    throw new Exception("Merge", ex);
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

                    TreeNode hd = root.GetFirstDescendantWithXmlAttribute(SPLIT_MERGE_ID);
                    while (hd != null)
                    {
                        counter++;
#if DEBUG
                        XmlAttribute xmlAttr = hd.GetXmlProperty().GetAttribute(SPLIT_MERGE_ID);
                        DebugFix.Assert(counter == Int32.Parse(xmlAttr.Value));
#endif
                        hd = hd.GetNextSiblingWithXmlAttribute(SPLIT_MERGE_ID);
                    }
                    int total = counter + 1;
                    counter = -1;
                    hd = root.GetFirstDescendantWithXmlAttribute(SPLIT_MERGE_ID);
                    while (hd != null)
                    {
                        if (RequestCancellation)
                        {
                            return;
                        }

                        counter++;
#if DEBUG
                        XmlAttribute xmlAttr = hd.GetXmlProperty().GetAttribute(SPLIT_MERGE_ID);
                        DebugFix.Assert(counter == Int32.Parse(xmlAttr.Value));
#endif

                        int i = counter + 1;
                        reportProgress(100 * i / total, i + " / " + total);

                        //Thread.Sleep(500);

                        string xukFolder = Path.Combine(splitDirectory, fileNameWithoutExtension + "_" + counter);
                        string xukPath = Path.Combine(xukFolder, counter + extension);

                        //try
                        //{
                        //}
                        //catch (Exception ex)
                        //{
                        //    //messageBoxAlert("PROBLEM:\n " + xukPath, null);
                        //    //m_session.messageBoxText("MERGE PROBLEM", xukPath, ex.Message);

                        //    throw ex;
                        //}

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

                        XmlAttribute attrCheck = subroot.GetXmlProperty().GetAttribute(SPLIT_MERGE);
                        DebugFix.Assert(attrCheck.Value == counter.ToString());

                        TreeNode mark = subroot.GetFirstDescendantWithXmlAttribute(SPLIT_MERGE_ID);

                        TreeNode nextMark = null;
                        TreeNode scan = mark.GetNextSiblingWithXmlAttribute(SPLIT_MERGE_SUB_ID);
                        while (scan != null)
                        {
                            nextMark = scan;
                            scan = scan.GetNextSiblingWithXmlAttribute(SPLIT_MERGE_SUB_ID);
                        }

                        //DebugFix.Assert(nextMark != null);

                        attrCheck = mark.GetXmlProperty().GetAttribute(SPLIT_MERGE_ID);

#if DEBUG
                        DebugFix.Assert(counter == Int32.Parse(attrCheck.Value));
#endif
                        mark.GetXmlProperty().RemoveAttribute(attrCheck);

                        TreeNode importedLevel = mark.Export(presentation);

                        TreeNode parent = hd.Parent;
                        int index = parent.Children.IndexOf(hd);
                        parent.RemoveChild(index);
                        parent.Insert(importedLevel, index);
                        hd = importedLevel;

                        int subcounter = -1;

                        TreeNode anchorNode = mark;
                        while (anchorNode != null)
                        {
                            TreeNode nextCandidateToSubmark = anchorNode.NextSibling;
                            if (nextCandidateToSubmark != null)
                            {
                                if (!nextCandidateToSubmark.HasXmlProperty)
                                {
#if DEBUG
                                    Debugger.Break();
#endif
                                    anchorNode = nextCandidateToSubmark;
                                    continue;
                                }

                                if (nextMark == null ||
                                    //nextMark != nextCandidateToSubmark &&
                                !nextMark.IsDescendantOf(nextCandidateToSubmark))
                                {
                                    XmlProperty xProp = nextCandidateToSubmark.GetXmlProperty();
                                    attrCheck = xProp == null ? null : xProp.GetAttribute(SPLIT_MERGE_SUB_ID);
                                    if (attrCheck != null)
                                    {
                                        subcounter++;
#if DEBUG
                                        string val = attrCheck.Value;
                                        string val1 = "-1";
                                        string val2 = "-1";
                                        int isep = val.IndexOf('~');
                                        if (isep >= 0 && isep < val.Length - 1)
                                        {
                                            val1 = val.Substring(0, isep);
                                            val2 = val.Substring(isep + 1);
                                        }
                                        DebugFix.Assert(counter == Int32.Parse(val1));
                                        DebugFix.Assert(subcounter == Int32.Parse(val2));
#endif
                                        xProp.RemoveAttribute(attrCheck);

                                        importedLevel = nextCandidateToSubmark.Export(presentation);

                                        //hd = hd.GetNextSiblingWithXmlAttribute(SPLIT_MERGE_SUB_ID);
                                        hd = UrakawaSession.findSubSplitMergeAnchor(root, counter, subcounter);
                                        DebugFix.Assert(hd != null);

                                        attrCheck = hd.GetXmlProperty().GetAttribute(SPLIT_MERGE_SUB_ID);

#if DEBUG
                                        val = attrCheck.Value;
                                        val1 = "-1";
                                        val2 = "-1";
                                        isep = val.IndexOf('~');
                                        if (isep >= 0 && isep < val.Length - 1)
                                        {
                                            val1 = val.Substring(0, isep);
                                            val2 = val.Substring(isep + 1);
                                        }
                                        DebugFix.Assert(counter == Int32.Parse(val1));
                                        DebugFix.Assert(subcounter == Int32.Parse(val2));
#endif
                                        if (hd != null)
                                        {
                                            parent = hd.Parent;
                                            index = parent.Children.IndexOf(hd);
                                            parent.RemoveChild(index);
                                            parent.Insert(importedLevel, index);
                                            hd = importedLevel;
                                        }
                                    }

                                    anchorNode = nextCandidateToSubmark;
                                    continue;
                                }

                                //if (nextMark == nextCandidateToSubmark)
                                //{
                                //    anchorNode = null; //break higher while
                                //    break;
                                //}

                                //assert nextMark.IsDescendantOf(nextCandidateToSubmark)
                                TreeNode topChild = nextMark;
                                while (topChild != null && topChild.Parent != null && topChild.Parent != nextCandidateToSubmark.Parent)
                                {
                                    TreeNode child = topChild.Parent.Children.Get(0);

                                    while (child != null)
                                    {
                                        if (!child.HasXmlProperty)
                                        {
#if DEBUG
                                            Debugger.Break();
#endif
                                            anchorNode = child;
                                            child = anchorNode.NextSibling;
                                            continue;
                                        }


                                        attrCheck = child.GetXmlProperty().GetAttribute(SPLIT_MERGE_SUB_ID);
                                        if (attrCheck != null)
                                        {
                                            subcounter++;
#if DEBUG
                                            string val = attrCheck.Value;
                                            string val1 = "-1";
                                            string val2 = "-1";
                                            int isep = val.IndexOf('~');
                                            if (isep >= 0 && isep < val.Length - 1)
                                            {
                                                val1 = val.Substring(0, isep);
                                                val2 = val.Substring(isep + 1);
                                            }
                                            DebugFix.Assert(counter == Int32.Parse(val1));
                                            DebugFix.Assert(subcounter == Int32.Parse(val2));
#endif
                                            child.GetXmlProperty().RemoveAttribute(attrCheck);

                                            importedLevel = child.Export(presentation);

                                            //hd = hd.GetNextSiblingWithXmlAttribute(SPLIT_MERGE_SUB_ID);
                                            hd = UrakawaSession.findSubSplitMergeAnchor(root, counter, subcounter);
                                            DebugFix.Assert(hd != null);

                                            attrCheck = hd.GetXmlProperty().GetAttribute(SPLIT_MERGE_SUB_ID);

#if DEBUG
                                            val = attrCheck.Value;
                                            val1 = "-1";
                                            val2 = "-1";
                                            isep = val.IndexOf('~');
                                            if (isep >= 0 && isep < val.Length - 1)
                                            {
                                                val1 = val.Substring(0, isep);
                                                val2 = val.Substring(isep + 1);
                                            }
                                            DebugFix.Assert(counter == Int32.Parse(val1));
                                            DebugFix.Assert(subcounter == Int32.Parse(val2));
#endif
                                            if (hd != null)
                                            {
                                                parent = hd.Parent;
                                                index = parent.Children.IndexOf(hd);
                                                parent.RemoveChild(index);
                                                parent.Insert(importedLevel, index);
                                                hd = importedLevel;
                                            }
                                        }

                                        if (child == topChild)
                                        {
                                            topChild = topChild.Parent;
                                            break;
                                        }

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

                        hd = hd.GetNextSiblingWithXmlAttribute(SPLIT_MERGE_ID);
                    }

#if DEBUG
                    //Debugger.Break();

                    TreeNode check = root.GetFirstDescendantWithXmlAttribute(SPLIT_MERGE_ID);
                    DebugFix.Assert(check == null);

                    check = root.GetFirstDescendantWithXmlAttribute(SPLIT_MERGE_SUB_ID);
                    DebugFix.Assert(check == null);
#endif

                    //int total = counter + 1;

                    string deletedDataFolderPath = m_session.DataCleanup(false);

                    if (!string.IsNullOrEmpty(deletedDataFolderPath) && Directory.Exists(deletedDataFolderPath))
                    {
                        FileDataProvider.DeleteDirectory(deletedDataFolderPath);

                        //if (Directory.GetFiles(deletedDataFolderPath).Length != 0 ||
                        //    Directory.GetDirectories(deletedDataFolderPath).Length != 0)
                        //{
                        //    m_session.m_ShellView.ExecuteShellProcess(deletedDataFolderPath);
                        //}
                    }


                    //XmlAttribute attrSplitMerge = root.GetXmlProperty().GetAttribute(SPLIT_MERGE);
                    //root.GetXmlProperty().RemoveAttribute(attrSplitMerge);
                    root.GetXmlProperty().RemoveAttribute(SPLIT_MERGE, "");

#if DEBUG
                    root.GetXmlProperty().SetAttribute(SPLIT_MERGE, "", "-1");
#endif

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
                    throw new Exception("Split", ex);
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
                    root.GetXmlProperty().SetAttribute(SPLIT_MERGE, "", i.ToString());

                    int counter = -1;

                    TreeNode mark = UrakawaSession.getNextSplitMergeMark(root);

                    TreeNode topText = TreeNode.NavigateInsideSignificantText(root);
                    if (topText.IsBefore(mark))
                    {
                        topText.IsMarked = true;
                        mark = UrakawaSession.getNextSplitMergeMark(root);
                    }

                    while (mark != null)
                    {
                        counter++;

                        TreeNode nextMark = UrakawaSession.getNextSplitMergeMark(mark);

                        if (counter == i)
                        {
                            XmlProperty xmlProp = mark.GetXmlProperty();
                            xmlProp.SetAttribute(SPLIT_MERGE_ID, "", i.ToString());

                            // purge node content before mark:
                            {
                                TreeNode topChild = mark;
                                while (topChild != null && topChild.Parent != null)
                                {
                                    TreeNode parent = topChild.Parent;
                                    TreeNode child = topChild;

                                    while (child != null)
                                    {
                                        TreeNode prevChild = child.PreviousSibling;

                                        if (child != mark && (child != topChild || child.Children.Count == 0))
                                        {
                                            parent.RemoveChild(child);
                                        }

                                        child = prevChild;
                                    }

                                    topChild = parent;
                                }
                            }

                            int subcounter = -1;

                            TreeNode anchorNode = mark;
                            while (anchorNode != null)
                            {
                                TreeNode nextCandidateToSubmark = anchorNode.NextSibling;
                                if (nextCandidateToSubmark != null)
                                {
                                    if (!nextCandidateToSubmark.HasXmlProperty)
                                    {
#if DEBUG
                                        Debugger.Break();
#endif
                                        anchorNode = nextCandidateToSubmark;
                                        continue;
                                    }

                                    if (nextMark == null
                                        || nextMark != nextCandidateToSubmark && !nextMark.IsDescendantOf(nextCandidateToSubmark))
                                    {
                                        subcounter++;

                                        xmlProp = nextCandidateToSubmark.GetXmlProperty();
                                        xmlProp.SetAttribute(SPLIT_MERGE_SUB_ID, "", counter.ToString() + '~' + subcounter.ToString());

                                        anchorNode = nextCandidateToSubmark;
                                        continue;
                                    }

                                    if (nextMark == nextCandidateToSubmark)
                                    {
                                        anchorNode = null; //break higher while
                                        break;
                                    }

                                    //assert nextMark.IsDescendantOf(nextCandidateToSubmark)
                                    TreeNode topChild = nextMark;
                                    while (topChild != null && topChild.Parent != null && topChild.Parent != nextCandidateToSubmark.Parent)
                                    {
                                        TreeNode child = topChild.Parent.Children.Get(0);

                                        while (child != null)
                                        {
                                            if (!child.HasXmlProperty)
                                            {
#if DEBUG
                                                Debugger.Break();
#endif
                                                anchorNode = child;
                                                child = anchorNode.NextSibling;
                                                continue;
                                            }

                                            if (child == topChild)
                                            {
                                                topChild = topChild.Parent;
                                                break;
                                            }

                                            subcounter++;

                                            xmlProp = child.GetXmlProperty();
                                            xmlProp.SetAttribute(SPLIT_MERGE_SUB_ID, "", counter.ToString() + '~' + subcounter.ToString());

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

                            // purge node content after following mark (including mark itself):
                            if (nextMark != null)
                            {
                                TreeNode topChild = nextMark;
                                while (topChild != null && topChild.Parent != null)
                                {
                                    TreeNode parent = topChild.Parent;
                                    TreeNode child = topChild;

                                    while (child != null)
                                    {
                                        TreeNode nextChild = child.NextSibling;

                                        if (child == nextMark || child != topChild || child.Children.Count == 0)
                                        {
                                            parent.RemoveChild(child);
                                        }

                                        child = nextChild;
                                    }

                                    topChild = parent;
                                }
                            }

                            // we're done with this sub project trimming and marking
                            break;
                        }

                        mark = nextMark;
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

        private TreeNode handleSplitFragmentAnchorInMaster(TreeNode level, String headingText, Presentation presentation, String headingAttributeName, String headingAttributeValue)
        {
#if DEBUG
            if (level.GetXmlProperty() == null)
            {
                Debugger.Break(); // text?
            }
#endif


#if true
            XmlProperty xmlProp = level.GetXmlProperty();
            xmlProp.SetAttribute(headingAttributeName, "", headingAttributeValue);

            return level;
#else
            TreeNode parent = level.Parent;

            int index = parent.Children.IndexOf(level);
            parent.RemoveChild(index);

            TreeNode anchorNode = presentation.TreeNodeFactory.Create();
            parent.Insert(anchorNode, index);

            XmlProperty xmlProp = anchorNode.GetXmlProperty();
            xmlProp.SetQName(UrakawaSession.SPLIT_MERGE_ANCHOR_ELEMENT, "");
            xmlProp.SetAttribute(headingAttributeName, "", headingAttributeValue);

            TextMedia textMedia = presentation.MediaFactory.CreateTextMedia();
            textMedia.Text = headingText;

            ChannelsProperty chProp = anchorNode.GetOrCreateChannelsProperty();
            chProp.SetMedia(presentation.ChannelsManager.GetOrCreateTextChannel(), textMedia);
            
            return anchorNode;
#endif
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

                    //DataCleanupCommand.Execute();
                    //Thread.Sleep(1000);
                    ////m_ShellView.PumpDispatcherFrames();

                    // Backup before close.
                    string docPath = DocumentFilePath;
                    Project project = DocumentProject;

                    Presentation presentation = project.Presentations.Get(0);
                    TreeNode root = presentation.RootNode;


                    bool hasAudio = false;
                    //hasAudio = project.Presentations.Get(0).RootNode.GetDurationOfManagedAudioMediaFlattened() != null;
                    
                    TreeNode nodeTestAudio = UrakawaSession.getNextSplitMergeMark(root);

                    TreeNode topText = TreeNode.NavigateInsideSignificantText(root);

                    if (nodeTestAudio == null || topText == null)
                    {
                        messageBoxAlert(Tobi_Plugin_Urakawa_Lang.SplitNothing, null);
                        return;
                    }

                    if (topText.IsBefore(nodeTestAudio))
                    {
                        topText.IsMarked = true;
                        nodeTestAudio = UrakawaSession.getNextSplitMergeMark(root);
                    }

                    while (nodeTestAudio != null)
                    {
                        if (nodeTestAudio.GetFirstAncestorWithManagedAudio() != null)
                        {
#if DEBUG
                            Debugger.Break();
#endif //DEBUG
                            hasAudio = true;
                            break;
                        }

                        nodeTestAudio = UrakawaSession.getNextSplitMergeMark(nodeTestAudio);
                    }
                    if (hasAudio)
                    {
                        messageBoxAlert(Tobi_Plugin_Urakawa_Lang.SplitMasterNoAudio, null);
                        return;
                    }


                    string parentDirectory = Path.GetDirectoryName(docPath);
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(docPath);
                    string extension = Path.GetExtension(docPath);

                    string splitDirectory = Path.Combine(parentDirectory, "_SPLIT");

                    string masterDirectory = Path.Combine(splitDirectory, fileNameWithoutExtension + MASTER_SUFFIX);

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


                    // Closing is REQUIRED ! 
                    PopupModalWindow.DialogButton button = CheckSaveDirtyAndClose(
                        PopupModalWindow.DialogButtonsSet.OkCancel, Tobi_Plugin_Urakawa_Lang.Menu_SplitMergeProject);
                    if (!PopupModalWindow.IsButtonOkYesApply(button))
                    {
                        return;
                    }

                    root.GetXmlProperty().SetAttribute(SPLIT_MERGE, "", "MASTER");

                    int counter = -1;

                    TreeNode mark = UrakawaSession.getNextSplitMergeMark(root);

                    while (mark != null)
                    {
                        counter++;
                        TreeNode heading = handleSplitFragmentAnchorInMaster(mark, "PART " + counter, presentation, SPLIT_MERGE_ID, counter.ToString());

                        mark = UrakawaSession.getNextSplitMergeMark(heading);

                        int subcounter = -1;

                        TreeNode anchorNode = heading;
                        while (anchorNode != null)
                        {
                            TreeNode nextCandidateSubMark = anchorNode.NextSibling;
                            if (nextCandidateSubMark != null)
                            {
                                if (!nextCandidateSubMark.HasXmlProperty)
                                {
#if DEBUG
                                    Debugger.Break();
#endif
                                    anchorNode = nextCandidateSubMark;
                                    continue;
                                }

                                if (mark == null || mark != nextCandidateSubMark && !mark.IsDescendantOf(nextCandidateSubMark))
                                {
                                    subcounter++;
                                    anchorNode = handleSplitFragmentAnchorInMaster(nextCandidateSubMark, "PART " + counter + " - " + subcounter, presentation, SPLIT_MERGE_SUB_ID, counter.ToString() + '~' + subcounter.ToString());
                                    continue;
                                }

                                if (mark == nextCandidateSubMark)
                                {
                                    anchorNode = null; //break higher while
                                    break;
                                }

                                //assert mark.IsDescendantOf(nextToRemove)
                                TreeNode topChild = mark;
                                while (topChild != null && topChild.Parent != null && topChild.Parent != nextCandidateSubMark.Parent) //heading anchorNode
                                {
                                    TreeNode child = topChild.Parent.Children.Get(0);

                                    while (child != null)
                                    {
                                        if (!child.HasXmlProperty)
                                        {
#if DEBUG
                                            Debugger.Break();
#endif
                                            anchorNode = child;
                                            child = anchorNode.NextSibling;
                                            continue;
                                        }

                                        if (child == topChild)
                                        {
                                            topChild = topChild.Parent;
                                            break;
                                        }

                                        subcounter++;
                                        anchorNode = handleSplitFragmentAnchorInMaster(child,
                                            "PART " + counter + " - " + subcounter, presentation, SPLIT_MERGE_SUB_ID,
                                            counter.ToString() + '~' + subcounter.ToString());

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
                    nodeTestAudio = nodeTestAudio.GetFirstDescendantWithXmlAttribute(SPLIT_MERGE_ID);
                    while (nodeTestAudio != null)
                    {
                        if (nodeTestAudio.GetFirstAncestorWithManagedAudio() != null
                            //|| nodeTestAudio.GetManagedAudioMedia() != null
                            )
                        {
#if DEBUG
                            Debugger.Break();
#endif //DEBUG
                            hasAudio = true;
                            break;
                        }

                        nodeTestAudio = nodeTestAudio.GetNextSiblingWithXmlAttribute(SPLIT_MERGE_ID);
                    }

                    if (!hasAudio)
                    {
                        nodeTestAudio = DocumentProject.Presentations.Get(0).RootNode;
                        nodeTestAudio = nodeTestAudio.GetFirstDescendantWithXmlAttribute(SPLIT_MERGE_SUB_ID);
                        while (nodeTestAudio != null)
                        {
                            if (nodeTestAudio.GetFirstAncestorWithManagedAudio() != null
                                //|| nodeTestAudio.GetManagedAudioMedia() != null
                             )
                            {
#if DEBUG
                                Debugger.Break();
#endif //DEBUG
                                hasAudio = true;
                                break;
                            }

                            nodeTestAudio = nodeTestAudio.GetNextSiblingWithXmlAttribute(SPLIT_MERGE_SUB_ID);
                        }
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


                    string parentDirectory = Path.GetDirectoryName(docPath);
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(parentDirectory);
                    if (fileNameWithoutExtension.EndsWith(MASTER_SUFFIX))
                    {
                        fileNameWithoutExtension = fileNameWithoutExtension.Substring(0,
                            fileNameWithoutExtension.Length - MASTER_SUFFIX.Length);
                    }
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

                    // Closing is REQUIRED ! 
                    PopupModalWindow.DialogButton button = CheckSaveDirtyAndClose(
                        PopupModalWindow.DialogButtonsSet.OkCancel, Tobi_Plugin_Urakawa_Lang.Menu_SplitMergeProject);
                    if (!PopupModalWindow.IsButtonOkYesApply(button))
                    {
                        return;
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
