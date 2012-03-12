using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using AudioLib;
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
using urakawa.data;
using urakawa.events.undo;
using urakawa.exception;
using urakawa.media;
using urakawa.media.data.audio;
using urakawa.media.data.image;
using urakawa.media.data.image.codec;
using urakawa.media.timing;
using urakawa.metadata;
using urakawa.metadata.daisy;
using urakawa.property.alt;
using urakawa.xuk;

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
            RaisePropertyChanged(() => Descriptions);
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
            RaisePropertyChanged(() => Descriptions);
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
                AddMetadata(null, altContent, XmlReaderWriterHelper.XmlId, uid);
            }
            if (!string.IsNullOrEmpty(descriptionName))
            {
                AddMetadata(null, altContent, DiagramContentModelHelper.DiagramElementName, descriptionName);
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

                ImageMediaData imgData1 = node.Presentation.MediaDataFactory.CreateImageMediaData(ext);
                if (imgData1 == null)
                {
                    return;
                }

                imgData1.InitializeImage(fullPath, Path.GetFileName(fullPath));
                img1.ImageMediaData = imgData1;

                AlternateContentSetManagedMediaCommand cmd22 =
                    node.Presentation.CommandFactory.CreateAlternateContentSetManagedMediaCommand(node, altContent, img1);
                node.Presentation.UndoRedoManager.Execute(cmd22);
            }

            RaisePropertyChanged(() => Descriptions);
        }

        public void SetDescriptionAudio(AlternateContent altContent, ManagedAudioMedia manMedia)
        {
            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            if (altProp.AlternateContents.IndexOf(altContent) < 0) return;

            if (manMedia == null
                || manMedia.HasActualAudioMediaData && !manMedia.Duration.IsGreaterThan(Time.Zero))
            {
                if (altContent.Audio != null)
                {
                    AlternateContentRemoveManagedMediaCommand cmd1 =
                        node.Presentation.CommandFactory.CreateAlternateContentRemoveManagedMediaCommand(node, altContent,
                                                                                                         altContent.Audio);
                    node.Presentation.UndoRedoManager.Execute(cmd1);
                }
            }
            else
            {
                ManagedAudioMedia audio1 = node.Presentation.MediaFactory.CreateManagedAudioMedia();
                AudioMediaData audioData1 = node.Presentation.MediaDataFactory.CreateAudioMediaData();
                audio1.AudioMediaData = audioData1;

                // WARNING: WavAudioMediaData implementation differs from AudioMediaData:
                // the latter is naive and performs a stream binary copy, the latter is optimized and re-uses existing WavClips. 
                //  WARNING 2: The audio data from the given parameter gets emptied !
                //audio1.AudioMediaData.MergeWith(manMedia.AudioMediaData);

                if (!audio1.AudioMediaData.PCMFormat.Data.IsCompatibleWith(manMedia.AudioMediaData.PCMFormat.Data))
                {
                    throw new InvalidDataFormatException(
                        "Can not merge description audio with a AudioMediaData with incompatible audio data");
                }
                Stream stream = manMedia.AudioMediaData.OpenPcmInputStream();
                try
                {
                    audio1.AudioMediaData.AppendPcmData(stream, null); //manMedia.AudioMediaData.AudioDuration
                }
                finally
                {
                    stream.Close();
                }

                AlternateContentSetManagedMediaCommand cmd22 =
                    node.Presentation.CommandFactory.CreateAlternateContentSetManagedMediaCommand(node, altContent, audio1);
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

            if (mdAttr == null)
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
            RaisePropertyChanged(() => Descriptions);
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
            //    DebugFix.Assert(eventt is DoneEventArgs || eventt is TransactionEndedEventArgs);
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
        public bool HasDescriptionAudio
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

                return m_SelectedAlternateContent.Audio != null;
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



        public void ImportDiagramXML(string xmlFilePath)
        {
            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode treeNode = selection.Item2 ?? selection.Item1;
            var altProp = treeNode.GetProperty<AlternateContentProperty>();



            XmlDocument diagramXML = XmlReaderWriterHelper.ParseXmlDocument(xmlFilePath, false);

            XmlNode description = XmlDocumentHelper.GetFirstChildElementOrSelfWithName(diagramXML, false, "description", DiagramContentModelHelper.NS_URL_DIAGRAM);
            if (description == null)
            {
                return;
            }

            XmlAttributeCollection descrAttrs = description.Attributes;
            if (descrAttrs != null)
            {
                for (int i = 0; i < descrAttrs.Count; i++)
                {
                    XmlAttribute attr = descrAttrs[i];

                    if (!attr.Name.StartsWith(XmlReaderWriterHelper.NS_PREFIX_XML + ":"))
                    {
                        continue;
                    }

                    Metadata altContMetadata = treeNode.Presentation.MetadataFactory.CreateMetadata();
                    altContMetadata.NameContentAttribute = new MetadataAttribute();
                    altContMetadata.NameContentAttribute.Name = attr.Name;
                    altContMetadata.NameContentAttribute.NamespaceUri = XmlReaderWriterHelper.NS_URL_XML;
                    altContMetadata.NameContentAttribute.Value = attr.Value;
                    AlternateContentMetadataAddCommand cmd_AltPropMetadata_XML =
                        treeNode.Presentation.CommandFactory.CreateAlternateContentMetadataAddCommand(
                            treeNode,
                            altProp,
                            null,
                            altContMetadata,
                            null
                            );
                    treeNode.Presentation.UndoRedoManager.Execute(cmd_AltPropMetadata_XML);
                }
            }

            XmlNode head = XmlDocumentHelper.GetFirstChildElementOrSelfWithName(description, false, "head", DiagramContentModelHelper.NS_URL_DIAGRAM);
            if (head != null)
            {
                foreach (XmlNode metaNode in XmlDocumentHelper.GetChildrenElementsOrSelfWithName(head, true, "meta", DiagramContentModelHelper.NS_URL_ZAI, false))
                {
                    if (metaNode.NodeType != XmlNodeType.Element || metaNode.LocalName != "meta")
                    {
#if DEBUG
                        Debugger.Break();
#endif // DEBUG
                        continue;
                    }



                    //XmlNode childMetadata = XmlDocumentHelper.GetFirstChildElementOrSelfWithName(node, false, "meta", DiagramContentModelHelper.NS_URL_ZAI);
                    //if (childMetadata != null)
                    //{
                    //    continue;
                    //}
                    bool foundAtLeastOneChildMeta = false;
                    foreach (XmlNode child in XmlDocumentHelper.GetChildrenElementsOrSelfWithName(metaNode, false, "meta", DiagramContentModelHelper.NS_URL_ZAI, false))
                    {
                        if (child == metaNode) continue;

                        foundAtLeastOneChildMeta = true;
                        break;
                    }
                    if (foundAtLeastOneChildMeta)
                    {
                        continue;
                    }



                    XmlAttributeCollection mdAttributes = metaNode.Attributes;
                    if (mdAttributes == null || mdAttributes.Count <= 0)
                    {
                        continue;
                    }

                    XmlNode attrName = mdAttributes.GetNamedItem("name");
                    XmlNode attrProperty = mdAttributes.GetNamedItem("property");

                    string property = (attrName != null && !String.IsNullOrEmpty(attrName.Value))
                                          ? attrName.Value
                                          : (attrProperty != null && !String.IsNullOrEmpty(attrProperty.Value)
                                                 ? attrProperty.Value
                                                 : null);

                    XmlNode attrContent = mdAttributes.GetNamedItem("content");

                    string content = (attrContent != null && !String.IsNullOrEmpty(attrContent.Value))
                                         ? attrContent.Value
                                         : metaNode.InnerText;

                    if (!(
                             String.IsNullOrEmpty(property) && String.IsNullOrEmpty(content)
                             ||
                             !String.IsNullOrEmpty(property) && !String.IsNullOrEmpty(content)
                         ))
                    {
                        continue;
                    }

                    Metadata altContMetadata = treeNode.Presentation.MetadataFactory.CreateMetadata();
                    altContMetadata.NameContentAttribute = new MetadataAttribute();
                    altContMetadata.NameContentAttribute.Name = String.IsNullOrEmpty(property)
                                                                    ? DiagramContentModelHelper.NA
                                                                    : property;
                    altContMetadata.NameContentAttribute.NamespaceUri =
                        String.IsNullOrEmpty(property)
                            ? null
                            : (
                                  property.StartsWith(DiagramContentModelHelper.NS_PREFIX_DIAGRAM_METADATA + ":")
                                      ? DiagramContentModelHelper.NS_URL_DIAGRAM
                                      : (property.StartsWith(SupportedMetadata_Z39862005.NS_PREFIX_DUBLIN_CORE + ":")
                                             ? SupportedMetadata_Z39862005.NS_URL_DUBLIN_CORE
                                             : null)
                              )
                        ;
                    altContMetadata.NameContentAttribute.Value = String.IsNullOrEmpty(content)
                                                                     ? DiagramContentModelHelper.NA
                                                                     : content;
                    AlternateContentMetadataAddCommand cmd_AltPropMetadata =
                        treeNode.Presentation.CommandFactory.CreateAlternateContentMetadataAddCommand(
                            treeNode,
                            altProp,
                            null,
                            altContMetadata,
                            null
                            );
                    treeNode.Presentation.UndoRedoManager.Execute(cmd_AltPropMetadata);

                    bool parentIsMeta = metaNode.ParentNode.LocalName == "meta";

                    var listAttrs = new List<XmlAttribute>(mdAttributes.Count +
                        (parentIsMeta && metaNode.ParentNode.Attributes != null ? metaNode.ParentNode.Attributes.Count : 0)
                        );

                    for (int i = 0; i < mdAttributes.Count; i++)
                    {
                        XmlAttribute attribute = mdAttributes[i];
                        listAttrs.Add(attribute);
                    }

                    if (parentIsMeta && metaNode.ParentNode.Attributes != null)
                    {
                        for (int i = 0; i < metaNode.ParentNode.Attributes.Count; i++)
                        {
                            XmlAttribute attribute = metaNode.ParentNode.Attributes[i];
                            if (mdAttributes.GetNamedItem(attribute.LocalName, attribute.NamespaceURI) == null)
                            {
                                listAttrs.Add(attribute);
                            }
                        }
                    }

                    foreach (var attribute in listAttrs)
                    {
                        if (attribute.LocalName == DiagramContentModelHelper.Name
                            || attribute.LocalName == DiagramContentModelHelper.Property
                            || attribute.LocalName == DiagramContentModelHelper.Content)
                        {
                            continue;
                        }


                        if (attribute.Name.StartsWith("xmlns:"))
                        {
                            //
                        }
                        else if (attribute.Name == "xmlns")
                        {
                            //
                        }
                        else
                        {
                            MetadataAttribute metadatattribute = new MetadataAttribute();
                            metadatattribute.Name = attribute.Name;
                            metadatattribute.NamespaceUri = attribute.Name.Contains(":") ? attribute.NamespaceURI : null;
                            metadatattribute.Value = attribute.Value;
                            AlternateContentMetadataAddCommand cmd_AltPropMetadataAttr =
                                treeNode.Presentation.CommandFactory.CreateAlternateContentMetadataAddCommand(
                                    treeNode,
                                    altProp,
                                    null,
                                    altContMetadata,
                                    metadatattribute
                                    );
                            treeNode.Presentation.UndoRedoManager.Execute(cmd_AltPropMetadataAttr);
                        }
                    }
                }
            }

            XmlNode body = XmlDocumentHelper.GetFirstChildElementOrSelfWithName(description, false, "body", DiagramContentModelHelper.NS_URL_DIAGRAM);
            if (body != null)
            {
                diagramXmlParseBodySpecific(xmlFilePath, treeNode, body, DiagramContentModelHelper.D_Summary);
                diagramXmlParseBodySpecific(xmlFilePath, treeNode, body, DiagramContentModelHelper.D_LondDesc);
                diagramXmlParseBodySpecific(xmlFilePath, treeNode, body, DiagramContentModelHelper.D_SimplifiedLanguageDescription);

                diagramXmlParseBodySpecific(xmlFilePath, treeNode, body, DiagramContentModelHelper.D_Tactile);
                diagramXmlParseBodySpecific(xmlFilePath, treeNode, body, DiagramContentModelHelper.D_SimplifiedImage);

//#if true || SUPPORT_ANNOTATION_ELEMENT
//                diagramXmlParseBodySpecific(xmlFilePath, treeNode, body, DiagramContentModelHelper.Annotation);
//#endif //SUPPORT_ANNOTATION_ELEMENT



                diagramXmlParseBody(xmlFilePath, treeNode, body);
            }

            OnPanelLoaded();

        }


        private void diagramXmlParseBody(string xmlFilePath, TreeNode treeNode, XmlNode body)
        {
            IEnumerator enumerator = body.GetEnumerator();
            while (enumerator.MoveNext())
            {
                XmlNode node = (XmlNode)enumerator.Current;

                if (node.NodeType != XmlNodeType.Element)
                {
                    continue;
                }


                string name = node.Name;
                if (name == DiagramContentModelHelper.D_Summary
                    || name == DiagramContentModelHelper.D_LondDesc
                    || name == DiagramContentModelHelper.D_SimplifiedLanguageDescription
                    || name == DiagramContentModelHelper.D_Tactile
                    || name == DiagramContentModelHelper.D_SimplifiedImage
//#if true || SUPPORT_ANNOTATION_ELEMENT
// || name == DiagramContentModelHelper.Annotation
//#endif //SUPPORT_ANNOTATION_ELEMENT
)
                {
                    continue;
                }

                diagramXmlParseBody_(node, xmlFilePath, treeNode, 0);
            }
        }

        private void diagramXmlParseBodySpecific(string xmlFilePath, TreeNode treeNode, XmlNode body, string diagramElementName)
        {
            string localName = DiagramContentModelHelper.StripNSPrefix(diagramElementName);
            foreach (XmlNode diagramElementNode in XmlDocumentHelper.GetChildrenElementsOrSelfWithName(body, false, localName, DiagramContentModelHelper.NS_URL_DIAGRAM, false))
            {
                if (diagramElementNode.NodeType != XmlNodeType.Element || diagramElementNode.LocalName != localName)
                {
#if DEBUG
                    Debugger.Break();
#endif
                    // DEBUG
                    continue;
                }

                diagramXmlParseBody_(diagramElementNode, xmlFilePath, treeNode, 0);
            }
        }

        private void diagramXmlParseBody_(XmlNode diagramElementNode, string xmlFilePath, TreeNode treeNode, int objectIndex)
        {
            string diagramElementName = diagramElementNode.Name;

            AlternateContent altContent = treeNode.Presentation.AlternateContentFactory.CreateAlternateContent();
            AlternateContentAddCommand cmd_AltContent =
                treeNode.Presentation.CommandFactory.CreateAlternateContentAddCommand(treeNode, altContent);
            treeNode.Presentation.UndoRedoManager.Execute(cmd_AltContent);



            Metadata diagramElementName_Metadata = new Metadata();
            diagramElementName_Metadata.NameContentAttribute = new MetadataAttribute();
            diagramElementName_Metadata.NameContentAttribute.Name = DiagramContentModelHelper.DiagramElementName;
            diagramElementName_Metadata.NameContentAttribute.NamespaceUri = null;
            diagramElementName_Metadata.NameContentAttribute.Value = diagramElementName;
            AlternateContentMetadataAddCommand cmd_AltContent_diagramElementName_Metadata =
                treeNode.Presentation.CommandFactory.CreateAlternateContentMetadataAddCommand(
                    treeNode,
                    null,
                    altContent,
                    diagramElementName_Metadata,
                    null
                    );
            treeNode.Presentation.UndoRedoManager.Execute(cmd_AltContent_diagramElementName_Metadata);


            if (diagramElementNode.Attributes != null)
            {
                for (int i = 0; i < diagramElementNode.Attributes.Count; i++)
                {
                    XmlAttribute attribute = diagramElementNode.Attributes[i];


                    if (attribute.Name.StartsWith("xmlns:"))
                    {
                        //
                    }
                    else if (attribute.Name == "xmlns")
                    {
                        //
                    }
                    else
                    {
                        Metadata diagramElementAttribute_Metadata = new Metadata();
                        diagramElementAttribute_Metadata.NameContentAttribute = new MetadataAttribute();
                        diagramElementAttribute_Metadata.NameContentAttribute.Name = attribute.Name;
                        diagramElementAttribute_Metadata.NameContentAttribute.NamespaceUri = attribute.NamespaceURI;
                        diagramElementAttribute_Metadata.NameContentAttribute.Value = attribute.Value;
                        AlternateContentMetadataAddCommand cmd_AltContent_diagramElementAttribute_Metadata =
                            treeNode.Presentation.CommandFactory.CreateAlternateContentMetadataAddCommand(
                                treeNode,
                                null,
                                altContent,
                                diagramElementAttribute_Metadata,
                                null
                                );
                        treeNode.Presentation.UndoRedoManager.Execute(
                            cmd_AltContent_diagramElementAttribute_Metadata);
                    }
                }
            }

            int nObjects = -1;

            XmlNode textNode = diagramElementNode;

            if (diagramElementName == DiagramContentModelHelper.D_SimplifiedImage
                || diagramElementName == DiagramContentModelHelper.D_Tactile)
            {
                string localTourName = DiagramContentModelHelper.StripNSPrefix(DiagramContentModelHelper.D_Tour);
                XmlNode tour =
                    XmlDocumentHelper.GetFirstChildElementOrSelfWithName(diagramElementNode, false,
                                                                         localTourName,
                                                                         DiagramContentModelHelper.NS_URL_DIAGRAM);
                textNode = tour;

                IEnumerable<XmlNode> objects = XmlDocumentHelper.GetChildrenElementsOrSelfWithName(diagramElementNode, false,
                                                                                          DiagramContentModelHelper.
                                                                                              Object,
                                                                                          DiagramContentModelHelper.
                                                                                              NS_URL_ZAI, false);
                nObjects = 0;
                foreach (XmlNode obj in objects)
                {
                    nObjects++;
                }

                int i = -1;
                foreach (XmlNode obj in objects)
                {
                    i++;
                    if (i != objectIndex)
                    {
                        continue;
                    }

                    if (obj.Attributes == null || obj.Attributes.Count <= 0)
                    {
                        break;
                    }

                    for (int j = 0; j < obj.Attributes.Count; j++)
                    {
                        XmlAttribute attribute = obj.Attributes[j];


                        if (attribute.Name.StartsWith("xmlns:"))
                        {
                            //
                        }
                        else if (attribute.Name == "xmlns")
                        {
                            //
                        }
                        else if (attribute.Name == DiagramContentModelHelper.Src)
                        {
                            //
                        }
                        else if (attribute.Name == DiagramContentModelHelper.SrcType)
                        {
                            //
                        }
                        else
                        {
                            Metadata diagramElementAttribute_Metadata = new Metadata();
                            diagramElementAttribute_Metadata.NameContentAttribute = new MetadataAttribute();
                            diagramElementAttribute_Metadata.NameContentAttribute.Name = attribute.Name;
                            diagramElementAttribute_Metadata.NameContentAttribute.NamespaceUri = attribute.NamespaceURI;
                            diagramElementAttribute_Metadata.NameContentAttribute.Value = attribute.Value;
                            AlternateContentMetadataAddCommand cmd_AltContent_diagramElementAttribute_Metadata =
                                treeNode.Presentation.CommandFactory.CreateAlternateContentMetadataAddCommand(
                                    treeNode,
                                    null,
                                    altContent,
                                    diagramElementAttribute_Metadata,
                                    null
                                    );
                            treeNode.Presentation.UndoRedoManager.Execute(
                                cmd_AltContent_diagramElementAttribute_Metadata);
                        }
                    }

                    XmlAttribute srcAttr = (XmlAttribute)obj.Attributes.GetNamedItem(DiagramContentModelHelper.Src);
                    if (srcAttr != null)
                    {
                        XmlAttribute srcType =
                            (XmlAttribute)obj.Attributes.GetNamedItem(DiagramContentModelHelper.SrcType);

                        ManagedImageMedia img = treeNode.Presentation.MediaFactory.CreateManagedImageMedia();

                        string imgFullPath = null;
                        if (FileDataProvider.isHTTPFile(srcAttr.Value))
                        {
                            imgFullPath = FileDataProvider.EnsureLocalFilePathDownloadTempDirectory(srcAttr.Value);
                        }
                        else
                        {
                            imgFullPath = Path.Combine(Path.GetDirectoryName(xmlFilePath), srcAttr.Value);
                        }
                        if (imgFullPath != null && File.Exists(imgFullPath))
                        {
                            string ext = Path.GetExtension(imgFullPath);
                            ext = ext == null ? null : ext.ToLower();

                            ImageMediaData imgData = treeNode.Presentation.MediaDataFactory.CreateImageMediaData(ext);
                            if (imgData != null)
                            {
                                imgData.InitializeImage(imgFullPath, Path.GetFileName(imgFullPath));
                                img.ImageMediaData = imgData;

                                AlternateContentSetManagedMediaCommand cmd_AltContent_Image =
                                    treeNode.Presentation.CommandFactory.CreateAlternateContentSetManagedMediaCommand(
                                        treeNode, altContent, img);
                                treeNode.Presentation.UndoRedoManager.Execute(cmd_AltContent_Image);
                            }
                        }
                    }
                }
            }

            if (textNode != null)
            {
                string strText = textNode.InnerXml;
                TextMedia txtMedia = treeNode.Presentation.MediaFactory.CreateTextMedia();
                txtMedia.Text = strText;
                AlternateContentSetManagedMediaCommand cmd_AltContent_Text =
                    treeNode.Presentation.CommandFactory.CreateAlternateContentSetManagedMediaCommand(treeNode,
                                                                                                      altContent,
                                                                                                      txtMedia);
                treeNode.Presentation.UndoRedoManager.Execute(cmd_AltContent_Text);
            }

            if (nObjects > 0 && ++objectIndex <= nObjects - 1)
            {
                diagramXmlParseBody_(diagramElementNode, xmlFilePath, treeNode, objectIndex);
            }
        }
    }
}
