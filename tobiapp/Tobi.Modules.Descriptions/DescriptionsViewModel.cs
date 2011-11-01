using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Common.MVVM;
using urakawa;
using urakawa.command;
using urakawa.commands;
using urakawa.core;
using urakawa.daisy;
using urakawa.events.undo;
using urakawa.media;
using urakawa.media.data.image;
using urakawa.media.data.image.codec;
using urakawa.metadata;
using urakawa.property.alt;

namespace Tobi.Plugin.Descriptions
{/// <summary>
    /// ViewModel for the MetadataPane
    /// </summary>
    [Export(typeof(DescriptionsViewModel)), PartCreationPolicy(CreationPolicy.Shared)]
    public class DescriptionsViewModel : ViewModelBase, IPartImportsSatisfiedNotification
    {

#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied()
#pragma warning restore 1591
        {
            //#if DEBUG
            //            Debugger.Break();
            //#endif

        }

        private readonly IEventAggregator m_EventAggregator;
        private readonly ILoggerFacade m_Logger;

        private readonly IUrakawaSession m_UrakawaSession;
        private readonly IUnityContainer m_Container;

        [ImportingConstructor]
        public DescriptionsViewModel(
            IEventAggregator eventAggregator,
            IUnityContainer container,
            ILoggerFacade logger,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession session
            )
        {
            m_EventAggregator = eventAggregator;
            m_Container = container;
            m_Logger = logger;

            m_UrakawaSession = session;

            m_EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(OnProjectLoaded, ProjectLoadedEvent.THREAD_OPTION);
            m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(OnProjectUnLoaded, ProjectUnLoadedEvent.THREAD_OPTION);

            m_EventAggregator.GetEvent<TreeNodeSelectionChangedEvent>().Subscribe(OnTreeNodeSelectionChanged, TreeNodeSelectionChangedEvent.THREAD_OPTION);
        }

        public void OnPanelLoaded()
        {
            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();

#if true || !DEBUG
            if (altProp == null)
            {
                altProp = node.GetOrCreateAlternateContentProperty();
                Debug.Assert(altProp != null);
            }
#else //DEBUG
            if (altProp == null)
            {
                {
                    AlternateContent altContent1 = node.Presentation.AlternateContentFactory.CreateAlternateContent();
                    TextMedia txt1 = node.Presentation.MediaFactory.CreateTextMedia();
                    txt1.Text = "<p>This is a textual description</p>";
                    altContent1.Text = txt1;

                    AlternateContentAddCommand cmd11 =
                        node.Presentation.CommandFactory.CreateAlternateContentAddCommand(node, altContent1);
                    node.Presentation.UndoRedoManager.Execute(cmd11);

                    {
                        Metadata altContMeta1 = node.Presentation.MetadataFactory.CreateMetadata();
                        altContMeta1.NameContentAttribute = new MetadataAttribute();
                        altContMeta1.NameContentAttribute.Name = "attr1";
                        altContMeta1.NameContentAttribute.NamespaceUri = "http://purl/dc";
                        altContMeta1.NameContentAttribute.Value = "val1";
                        AlternateContentMetadataAddCommand cmd12 =
                            node.Presentation.CommandFactory.CreateAlternateContentMetadataAddCommand(null,
                                                                                                      altContent1,
                                                                                                      altContMeta1);
                        node.Presentation.UndoRedoManager.Execute(cmd12);
                    }

                    {
                        Metadata altContMeta2 = node.Presentation.MetadataFactory.CreateMetadata();
                        altContMeta2.NameContentAttribute = new MetadataAttribute();
                        altContMeta2.NameContentAttribute.Name = "attr2";
                        //altContMeta2.NameContentAttribute.NamespaceUri = "http://purl/dc";
                        altContMeta2.NameContentAttribute.Value = "val2";
                        AlternateContentMetadataAddCommand cmd13 =
                            node.Presentation.CommandFactory.CreateAlternateContentMetadataAddCommand(null, altContent1, altContMeta2);
                        node.Presentation.UndoRedoManager.Execute(cmd13);
                    }
                }

                {
                    AlternateContent altContent2 = node.Presentation.AlternateContentFactory.CreateAlternateContent();

                    AlternateContentAddCommand cmd21 =
                        node.Presentation.CommandFactory.CreateAlternateContentAddCommand(node, altContent2);
                    node.Presentation.UndoRedoManager.Execute(cmd21);

                    TextMedia txt2 = node.Presentation.MediaFactory.CreateTextMedia();
                    txt2.Text = "<p>This is another textual description</p>";

                    //altContent2.Text = txt2;
                    AlternateContentSetManagedMediaCommand cmd22 =
                        node.Presentation.CommandFactory.CreateAlternateContentSetManagedMediaCommand(altContent2, txt2);
                    node.Presentation.UndoRedoManager.Execute(cmd22);


                    ManagedImageMedia img1 = node.Presentation.MediaFactory.CreateManagedImageMedia();
                    string img1DirPath = Path.GetDirectoryName(ApplicationConstants.LOG_FILE_PATH);
                    string img1FullPath = Path.Combine(img1DirPath, "daisy_01.png");
                    //ImageMediaData imgData1 = node.Presentation.MediaDataFactory.CreateImageMediaData();
                    ImageMediaData imgData1 = node.Presentation.MediaDataFactory.Create<PngImageMediaData>();
                    imgData1.InitializeImage(img1FullPath, Path.GetFileName(img1FullPath));
                    img1.ImageMediaData = imgData1;

                    AlternateContentSetManagedMediaCommand cmd23 =
                        node.Presentation.CommandFactory.CreateAlternateContentSetManagedMediaCommand(altContent2, img1);
                    node.Presentation.UndoRedoManager.Execute(cmd23);
                }

                //altProp = node.Presentation.PropertyFactory.CreateAlternateContentProperty();
                //altProp = node.GetOrCreateAlternateContentProperty();
                altProp = node.GetProperty<AlternateContentProperty>();
                Debug.Assert(altProp != null);

                {
                    Metadata meta1 = node.Presentation.MetadataFactory.CreateMetadata();
                    meta1.NameContentAttribute = new MetadataAttribute();
                    meta1.NameContentAttribute.Name = "author";
                    meta1.NameContentAttribute.NamespaceUri = "http://purl/dc";
                    meta1.NameContentAttribute.Value = "John Doe";
                    var metaAttr1 = new MetadataAttribute();
                    metaAttr1.Name = "about";
                    metaAttr1.Value = "something";
                    meta1.OtherAttributes.Insert(meta1.OtherAttributes.Count, metaAttr1);
                    var metaAttr2 = new MetadataAttribute();
                    metaAttr2.Name = "rel";
                    metaAttr2.Value = "authorship";
                    meta1.OtherAttributes.Insert(meta1.OtherAttributes.Count, metaAttr2);
                    AlternateContentMetadataAddCommand cmd31 =
                        node.Presentation.CommandFactory.CreateAlternateContentMetadataAddCommand(altProp, null, meta1);
                    node.Presentation.UndoRedoManager.Execute(cmd31);
                }
                {
                    Metadata meta2 = node.Presentation.MetadataFactory.CreateMetadata();
                    meta2.NameContentAttribute = new MetadataAttribute();
                    meta2.NameContentAttribute.Name = "publisher";
                    meta2.NameContentAttribute.NamespaceUri = "http://purl/dc";
                    meta2.NameContentAttribute.Value = "DAISY";
                    var metaAttr1 = new MetadataAttribute();
                    metaAttr1.Name = "priority";
                    metaAttr1.Value = "none";
                    meta2.OtherAttributes.Insert(meta2.OtherAttributes.Count, metaAttr1);
                    var metaAttr2 = new MetadataAttribute();
                    metaAttr2.Name = "rel";
                    metaAttr2.Value = "business";
                    meta2.OtherAttributes.Insert(meta2.OtherAttributes.Count, metaAttr2);
                    AlternateContentMetadataAddCommand cmd32 =
                        node.Presentation.CommandFactory.CreateAlternateContentMetadataAddCommand(altProp, null, meta2);
                    node.Presentation.UndoRedoManager.Execute(cmd32);
                }
            }

#endif //DEBUG

            //m_SelectedMedatadata = null;
            //m_SelectedAlternateContent = null;
            RaisePropertyChanged(() => Metadatas);
            RaisePropertyChanged(() => Descriptions);

            RaisePropertyChanged(() => DescribableImage);
            RaisePropertyChanged(() => DescribableImageInfo);
        }


        public void RemoveMetadata(AlternateContentProperty altProp, AlternateContent altContent,
            Metadata md)
        {
            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp_ = node.GetProperty<AlternateContentProperty>();
            if (altProp_ == null) return;

            if (altProp != null && altProp_ != altProp) return;

            if (altContent != null && altProp_.AlternateContents.IndexOf(altContent) < 0) return;

            AlternateContentMetadataRemoveCommand cmd = node.Presentation.CommandFactory.CreateAlternateContentMetadataRemoveCommand(node, altProp, altContent, md, null);
            node.Presentation.UndoRedoManager.Execute(cmd);

            RaisePropertyChanged(() => Metadatas);
        }

        public void AddMetadata(AlternateContentProperty altProp, AlternateContent altContent,
            string newName, string newValue)
        {
            if (string.IsNullOrEmpty(newName) || string.IsNullOrEmpty(newValue)) return;

            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp_ = node.GetOrCreateAlternateContentProperty();

            if (altProp != null && altProp_ != altProp) return;

            if (altContent != null && altProp_.AlternateContents.IndexOf(altContent) < 0) return;

            Metadata meta = node.Presentation.MetadataFactory.CreateMetadata();
            meta.NameContentAttribute = new MetadataAttribute();
            meta.NameContentAttribute.Name = newName;
            //meta.NameContentAttribute.NamespaceUri = "dummy namespace";
            meta.NameContentAttribute.Value = newValue;
            AlternateContentMetadataAddCommand cmd = node.Presentation.CommandFactory.CreateAlternateContentMetadataAddCommand(node, altProp, altContent, meta, null);
            node.Presentation.UndoRedoManager.Execute(cmd);

            RaisePropertyChanged(() => Metadatas);
        }

        public void AddDescription(string uid, string descriptionName)
        {
            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            //var altProp = node.GetOrCreateAlternateContentProperty();
            //if (altProp == null) return;

            AlternateContent altContent = node.Presentation.AlternateContentFactory.CreateAlternateContent();

            AlternateContentAddCommand cmd1 =
                node.Presentation.CommandFactory.CreateAlternateContentAddCommand(node, altContent);
            node.Presentation.UndoRedoManager.Execute(cmd1);

            RaisePropertyChanged(() => Descriptions);

            if (!string.IsNullOrEmpty(uid))
            {
                AddMetadata(null, altContent, DiagramContentModelStrings.XmlId, uid);
            }
            if (!string.IsNullOrEmpty(descriptionName))
            {
                AddMetadata(null, altContent, DiagramContentModelStrings.DescriptionName, descriptionName);
            }
        }

        public void RemoveDescription(AlternateContent altContent)
        {
            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            if (altProp.AlternateContents.IndexOf(altContent) < 0) return;

            AlternateContentRemoveCommand cmd1 =
                node.Presentation.CommandFactory.CreateAlternateContentRemoveCommand(node, altContent);
            node.Presentation.UndoRedoManager.Execute(cmd1);

            RaisePropertyChanged(() => Descriptions);
        }

        public void SetDescriptionText(AlternateContent altContent, string txt)
        {
            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            if (altProp.AlternateContents.IndexOf(altContent) < 0) return;

            if (string.IsNullOrEmpty(txt))
            {
                if (altContent.Text != null)
                {
                    AlternateContentRemoveManagedMediaCommand cmd1 =
                        node.Presentation.CommandFactory.CreateAlternateContentRemoveManagedMediaCommand(node, altContent,
                                                                                                         altContent.Text);
                    node.Presentation.UndoRedoManager.Execute(cmd1);
                }
            }
            else
            {
                TextMedia txt2 = node.Presentation.MediaFactory.CreateTextMedia();
                txt2.Text = txt;

                AlternateContentSetManagedMediaCommand cmd22 =
                    node.Presentation.CommandFactory.CreateAlternateContentSetManagedMediaCommand(node, altContent, txt2);
                node.Presentation.UndoRedoManager.Execute(cmd22);
            }

            RaisePropertyChanged(() => Descriptions);
        }

        public void SetDescriptionImage(AlternateContent altContent, string fullPath)
        {
            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            if (altProp.AlternateContents.IndexOf(altContent) < 0) return;

            if (string.IsNullOrEmpty(fullPath))
            {
                if (altContent.Image != null)
                {
                    AlternateContentRemoveManagedMediaCommand cmd1 =
                        node.Presentation.CommandFactory.CreateAlternateContentRemoveManagedMediaCommand(node, altContent,
                                                                                                         altContent.Image);
                    node.Presentation.UndoRedoManager.Execute(cmd1);
                }
            }
            else if (File.Exists(fullPath))
            {
                string ext = Path.GetExtension(fullPath);
                if (string.IsNullOrEmpty(ext)) return;
                ext = ext.ToLower();

                ManagedImageMedia img1 = node.Presentation.MediaFactory.CreateManagedImageMedia();

                //ImageMediaData imgData1 = node.Presentation.MediaDataFactory.CreateImageMediaData();
                ImageMediaData imgData1 = null;
                switch (ext)
                {
                    case ".jpg":
                        imgData1 = node.Presentation.MediaDataFactory.Create<JpgImageMediaData>();
                        break;

                    case ".bmp":
                        imgData1 = node.Presentation.MediaDataFactory.Create<BmpImageMediaData>();
                        break;

                    case ".png":
                        imgData1 = node.Presentation.MediaDataFactory.Create<PngImageMediaData>();
                        break;

                    default:
                        {
                            return;
                            break;
                        }
                }

                imgData1.InitializeImage(fullPath, Path.GetFileName(fullPath));
                img1.ImageMediaData = imgData1;

                AlternateContentSetManagedMediaCommand cmd22 =
                    node.Presentation.CommandFactory.CreateAlternateContentSetManagedMediaCommand(node, altContent, img1);
                node.Presentation.UndoRedoManager.Execute(cmd22);
            }

            RaisePropertyChanged(() => Descriptions);
        }


        public void RemoveMetadataAttr(Metadata md, MetadataAttribute mdAttr)
        {
            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            int index = altProp.Metadatas.IndexOf(md);
            if (index < 0) return;

            index = altProp.Metadatas.Get(index).OtherAttributes.IndexOf(mdAttr);
            if (index < 0) return;

            AlternateContentMetadataRemoveCommand cmd = node.Presentation.CommandFactory.CreateAlternateContentMetadataRemoveCommand(node, altProp, null, md, mdAttr);
            node.Presentation.UndoRedoManager.Execute(cmd);

            RaisePropertyChanged(() => Metadatas);
        }

        public void AddMetadataAttr(Metadata md, string newName, string newValue)
        {
            if (string.IsNullOrEmpty(newName) || string.IsNullOrEmpty(newValue)) return;

            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            int index = altProp.Metadatas.IndexOf(md);
            if (index < 0) return;

            var metaAttr = new MetadataAttribute();
            metaAttr.Name = newName;
            //metaAttr.NamespaceUri = "dummy namespace";
            metaAttr.Value = newValue;
            
            AlternateContentMetadataAddCommand cmd = node.Presentation.CommandFactory.CreateAlternateContentMetadataAddCommand(node, altProp, null, md, metaAttr);
            node.Presentation.UndoRedoManager.Execute(cmd);

            RaisePropertyChanged(() => Metadatas);
        }

        public void SetMetadataAttr(AlternateContentProperty altProp, AlternateContent altContent,
            Metadata md, MetadataAttribute mdAttr, string newName, string newValue)
        {
            if (string.IsNullOrEmpty(newName) || string.IsNullOrEmpty(newValue)) return;

            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp_ = node.GetProperty<AlternateContentProperty>();
            if (altProp_ == null) return;

            if (altProp != null && altProp_ != altProp) return;

            if (altContent != null && altProp_.AlternateContents.IndexOf(altContent) < 0) return;

            if (mdAttr==null)
            {
                MetadataAttribute attr = md.NameContentAttribute;

                if (attr.Name != newName)
                {
                    AlternateContentMetadataSetNameCommand cmd1 =
                        node.Presentation.CommandFactory.CreateAlternateContentMetadataSetNameCommand(
                            altProp,
                            altContent,
                            attr,
                            newName
                            );
                    node.Presentation.UndoRedoManager.Execute(cmd1);
                }
                if (attr.Value != newValue)
                {
                    AlternateContentMetadataSetContentCommand cmd2 =
                        node.Presentation.CommandFactory.CreateAlternateContentMetadataSetContentCommand(
                            altProp,
                            altContent,
                            attr,
                            newValue
                            );
                    node.Presentation.UndoRedoManager.Execute(cmd2);
                }
            }
            else
            {
                if (mdAttr.Name != newName)
                {
                    AlternateContentMetadataSetNameCommand cmd1 =
                        node.Presentation.CommandFactory.CreateAlternateContentMetadataSetNameCommand(
                            altProp,
                            altContent,
                            mdAttr,
                            newName
                            );
                    node.Presentation.UndoRedoManager.Execute(cmd1);
                }
                if (mdAttr.Value != newValue)
                {
                    AlternateContentMetadataSetContentCommand cmd2 =
                        node.Presentation.CommandFactory.CreateAlternateContentMetadataSetContentCommand(
                            altProp,
                            altContent,
                            mdAttr,
                            newValue
                            );
                    node.Presentation.UndoRedoManager.Execute(cmd2);
                }
            }


            RaisePropertyChanged(() => Metadatas);
        }

        private void OnTreeNodeSelectionChanged(Tuple<Tuple<TreeNode, TreeNode>, Tuple<TreeNode, TreeNode>> oldAndNewTreeNodeSelection)
        {
            Tuple<TreeNode, TreeNode> oldTreeNodeSelection = oldAndNewTreeNodeSelection.Item1;
            Tuple<TreeNode, TreeNode> newTreeNodeSelection = oldAndNewTreeNodeSelection.Item2;

            //Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = newTreeNodeSelection.Item2 ?? newTreeNodeSelection.Item1;
            if (node == null) return;

            RaisePropertyChanged(() => Metadatas);
            RaisePropertyChanged(() => Descriptions);
        }


        private void OnProjectUnLoaded(Project project)
        {
            project.Presentations.Get(0).UndoRedoManager.CommandDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandReDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandUnDone -= OnUndoRedoManagerChanged;
            //project.Presentations.Get(0).UndoRedoManager.TransactionEnded -= OnUndoRedoManagerChanged;

            OnProjectLoaded(null);
        }

        private void OnProjectLoaded(Project project)
        {
            m_Logger.Log("DescriptionsViewModel.OnProject(UN)Loaded" + (project == null ? "(null)" : ""),
                Category.Debug, Priority.Medium);

            RaisePropertyChanged(() => Metadatas);
            RaisePropertyChanged(() => Descriptions);

            if (project == null) return;

            project.Presentations.Get(0).UndoRedoManager.CommandDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandReDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandUnDone += OnUndoRedoManagerChanged;
            //project.Presentations.Get(0).UndoRedoManager.TransactionEnded += OnUndoRedoManagerChanged;
        }

        private void OnUndoRedoManagerChanged(object sender, UndoRedoManagerEventArgs eventt)
        {
            if (!Dispatcher.CurrentDispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Normal, (Action<object, UndoRedoManagerEventArgs>)OnUndoRedoManagerChanged, sender, eventt);
                return;
            }

            //m_Logger.Log("MissingAudioValidator.OnUndoRedoManagerChanged", Category.Debug, Priority.Medium);

            if (!(eventt is DoneEventArgs
                           || eventt is UnDoneEventArgs
                           || eventt is ReDoneEventArgs
                //|| eventt is TransactionEndedEventArgs
                           ))
            {
                Debug.Fail("This should never happen !!");
                return;
            }

            //if (m_Session.DocumentProject.Presentations.Get(0).UndoRedoManager.IsTransactionActive)
            //{
            //    Debug.Assert(eventt is DoneEventArgs || eventt is TransactionEndedEventArgs);
            //    m_Logger.Log("AudioContentValidator.OnUndoRedoManagerChanged (exit: ongoing TRANSACTION...)", Category.Debug, Priority.Medium);
            //    return;
            //}

            bool done = eventt is DoneEventArgs || eventt is ReDoneEventArgs; // || eventt is TransactionEndedEventArgs;

            Command cmd = eventt.Command;

            //RaisePropertyChanged(() => Metadatas);
        }

        //public IEnumerable<MetadataAttribute> MetadataAttributes
        //{
        //    get
        //    {
        //        if (m_SelectedMedatadata == -1) return null;

        //        if (m_UrakawaSession.DocumentProject == null) return null;

        //        Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
        //        TreeNode node = selection.Item2 ?? selection.Item1;
        //        if (node == null) return null;

        //        AlternateContentProperty altProp = node.GetProperty<AlternateContentProperty>();
        //        if (altProp == null) return null;

        //        return altProp.Metadatas.Get(m_SelectedMedatadata).OtherAttributes.ContentsAs_Enumerable;
        //    }
        //}
        private Metadata m_SelectedMedatadata;
        public void SetSelectedMetadata(Metadata md)
        {
            m_SelectedMedatadata = md;
            RaisePropertyChanged(() => Metadatas);
        }
        private AlternateContent m_SelectedAlternateContent;
        public void SetSelectedAlternateContent(AlternateContent altContent)
        {
            m_SelectedAlternateContent = altContent;
            RaisePropertyChanged(() => Descriptions);
        }

        [NotifyDependsOn("Descriptions")]
        public bool HasDescriptionMetadata
        {
            get
            {
                if (m_UrakawaSession.DocumentProject == null) return false;

                Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                TreeNode node = selection.Item2 ?? selection.Item1;
                if (node == null) return false;

                AlternateContentProperty altProp = node.GetProperty<AlternateContentProperty>();
                if (altProp == null) return false;

                if (altProp.AlternateContents.Count <= 0) return false;

                if (m_SelectedAlternateContent == null) return false;

                if (altProp.AlternateContents.IndexOf(m_SelectedAlternateContent) < 0) return false;

                return m_SelectedAlternateContent.Metadatas.Count > 0;
            }
        }

        [NotifyDependsOn("Descriptions")]
        public bool HasDescriptionImage
        {
            get
            {
                if (m_UrakawaSession.DocumentProject == null) return false;

                Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                TreeNode node = selection.Item2 ?? selection.Item1;
                if (node == null) return false;

                AlternateContentProperty altProp = node.GetProperty<AlternateContentProperty>();
                if (altProp == null) return false;

                if (altProp.AlternateContents.Count <= 0) return false;

                if (m_SelectedAlternateContent == null) return false;

                if (altProp.AlternateContents.IndexOf(m_SelectedAlternateContent) < 0) return false;

                return m_SelectedAlternateContent.Image != null;
            }
        }

        [NotifyDependsOn("Descriptions")]
        public bool HasDescriptionText
        {
            get
            {
                if (m_UrakawaSession.DocumentProject == null) return false;

                Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                TreeNode node = selection.Item2 ?? selection.Item1;
                if (node == null) return false;

                AlternateContentProperty altProp = node.GetProperty<AlternateContentProperty>();
                if (altProp == null) return false;

                if (altProp.AlternateContents.Count <= 0) return false;

                if (m_SelectedAlternateContent == null) return false;

                if (altProp.AlternateContents.IndexOf(m_SelectedAlternateContent) < 0) return false;

                return m_SelectedAlternateContent.Text != null;
            }
        }

        [NotifyDependsOn("Metadatas")]
        public bool HasMetadataAttrs
        {
            get
            {
                if (m_UrakawaSession.DocumentProject == null) return false;

                Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                TreeNode node = selection.Item2 ?? selection.Item1;
                if (node == null) return false;

                AlternateContentProperty altProp = node.GetProperty<AlternateContentProperty>();
                if (altProp == null) return false;

                if (altProp.Metadatas.Count <= 0) return false;

                if (m_SelectedMedatadata == null) return false;

                if (altProp.Metadatas.IndexOf(m_SelectedMedatadata) < 0) return false;

                return m_SelectedMedatadata.OtherAttributes.Count > 0;
            }
        }

        [NotifyDependsOn("Metadatas")]
        public bool HasMetadata
        {
            get
            {
                if (m_UrakawaSession.DocumentProject == null) return false;

                Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                TreeNode node = selection.Item2 ?? selection.Item1;
                if (node == null) return false;

                AlternateContentProperty altProp = node.GetProperty<AlternateContentProperty>();
                if (altProp == null) return false;

                return altProp.Metadatas.Count > 0;
            }
        }

        public IEnumerable<Metadata> Metadatas //ObservableCollection
        {
            get
            {
                if (m_UrakawaSession.DocumentProject == null) return null;

                Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                TreeNode node = selection.Item2 ?? selection.Item1;
                if (node == null) return null;

                AlternateContentProperty altProp = node.GetProperty<AlternateContentProperty>();
                if (altProp == null) return null;

                //return new ObservableCollection<Metadata>(altProp.Metadatas.ContentsAs_Enumerable);
                return altProp.Metadatas.ContentsAs_Enumerable;
            }
        }

        [NotifyDependsOn("Descriptions")]
        public bool HasDescriptions
        {
            get
            {
                if (m_UrakawaSession.DocumentProject == null) return false;

                Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                TreeNode node = selection.Item2 ?? selection.Item1;
                if (node == null) return false;

                AlternateContentProperty altProp = node.GetProperty<AlternateContentProperty>();
                if (altProp == null) return false;

                //return new ObservableCollection<Metadata>(altProp.Metadatas.ContentsAs_Enumerable);
                return altProp.AlternateContents.Count > 0;
            }
        }

        public IEnumerable<AlternateContent> Descriptions //ObservableCollection
        {
            get
            {
                if (m_UrakawaSession.DocumentProject == null) return null;

                Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                TreeNode node = selection.Item2 ?? selection.Item1;
                if (node == null) return null;

                AlternateContentProperty altProp = node.GetProperty<AlternateContentProperty>();
                if (altProp == null) return null;

                //return new ObservableCollection<Metadata>(altProp.Metadatas.ContentsAs_Enumerable);
                return altProp.AlternateContents.ContentsAs_Enumerable;
            }
        }

        public ImageSource DescribableImage
        {
            get
            {
                TreeNode checkedNode = getCheckTreeNode();
                if (checkedNode == null) return null;

                return DescribableTreeNode.GetDescribableImage(checkedNode);
            }
        }

        public string DescribableImageInfo
        {
            get
            {
                TreeNode checkedNode = getCheckTreeNode();
                if (checkedNode == null) return null;

                return DescribableTreeNode.GetDescriptionLabel(checkedNode);
            }
        }

        private TreeNode getCheckTreeNode()
        {
            if (m_UrakawaSession.DocumentProject == null) return null;

            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return null;

            var navModel = m_Container.Resolve<DescriptionsNavigationViewModel>();
            if (navModel.DescriptionsNavigator == null) return null;

            bool found = false;
            foreach (DescribableTreeNode dnode in navModel.DescriptionsNavigator.DescribableTreeNodes)
            {
                found = dnode.TreeNode == node;
                if (found) break;
            }
            if (!found) return null;

            return node;
        }
    }
}
