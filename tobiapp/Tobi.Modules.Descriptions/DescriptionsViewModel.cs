using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows.Threading;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Common.MVVM;
using urakawa;
using urakawa.command;
using urakawa.core;
using urakawa.daisy;
using urakawa.events.undo;
using urakawa.xuk;

namespace Tobi.Plugin.Descriptions
{/// <summary>
    /// ViewModel for the MetadataPane
    /// </summary>
    [Export(typeof(DescriptionsViewModel)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class DescriptionsViewModel : ViewModelBase, IPartImportsSatisfiedNotification
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

            ShowAdvancedEditor = false;

            m_EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(OnProjectLoaded, ProjectLoadedEvent.THREAD_OPTION);
            m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(OnProjectUnLoaded, ProjectUnLoadedEvent.THREAD_OPTION);

            m_EventAggregator.GetEvent<TreeNodeSelectionChangedEvent>().Subscribe(OnTreeNodeSelectionChanged, TreeNodeSelectionChangedEvent.THREAD_OPTION);
        }

        private bool m_ShowAdvancedEditor = false;
        public bool ShowAdvancedEditor
        {
            get { return m_ShowAdvancedEditor; }
            set
            {
                m_ShowAdvancedEditor = value;
                RaisePropertyChanged(() => ShowAdvancedEditor);
            }
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



        private int m_XmlIDCounter = -1;
        public string GetNewXmlID(string prefix)
        {
            string id = null;
            do
            {
                id = prefix + '_' + (++m_XmlIDCounter);

            } while (XmlIDAlreadyExists(id, true, true));

            return id;
        }

        public bool XmlIDAlreadyExists(string xmlid, bool inHeadMetadata, bool inBodyContent)
        {
            foreach (var id in GetExistingXmlIDs(inHeadMetadata, inBodyContent))
            {
                if (xmlid == id)
                {
                    return true;
                }
            }

            return false;
        }

        public IEnumerable<string> GetExistingXmlIDs(bool inHeadMetadata, bool inBodyContent)
        {
            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) yield break;

            var altProp = node.GetAlternateContentProperty();
            if (altProp == null) yield break;


            if (inHeadMetadata && altProp.Metadatas != null)
            {
                foreach (var metadata in altProp.Metadatas.ContentsAs_Enumerable)
                {
                    if (metadata.OtherAttributes != null)
                    {
                        foreach (var metadataAttr in metadata.OtherAttributes.ContentsAs_Enumerable)
                        {
                            if (metadataAttr.Name.Equals(XmlReaderWriterHelper.XmlId))
                            {
                                yield return metadataAttr.Value;
                            }
                        }
                    }
                }
            }

            if (inBodyContent && altProp.AlternateContents != null)
            {
                foreach (var altContent in altProp.AlternateContents.ContentsAs_Enumerable)
                {
                    if (altContent.Metadatas != null)
                    {
                        foreach (var metadata in altContent.Metadatas.ContentsAs_Enumerable)
                        {
                            if (metadata.NameContentAttribute.Name.Equals(XmlReaderWriterHelper.XmlId))
                            {
                                yield return metadata.NameContentAttribute.Value;
                            }
                        }
                    }
                }
            }
        }


        public bool XmlIDIsReferredTo(string xmlid, bool inHeadMetadata, bool inBodyContent)
        {
            Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return false;

            var altProp = node.GetAlternateContentProperty();
            if (altProp == null) return false;


            if (inHeadMetadata && altProp.Metadatas != null)
            {
                foreach (var metadata in altProp.Metadatas.ContentsAs_Enumerable)
                {
                    if (metadata.OtherAttributes != null)
                    {
                        foreach (var metadataAttr in metadata.OtherAttributes.ContentsAs_Enumerable)
                        {
                            if (metadataAttr.Name.Equals(DiagramContentModelHelper.About))
                            {
                                string idref = metadataAttr.Value;
                                if (idref.StartsWith("#") && idref.Length > 1)
                                {
                                    idref = idref.Substring(1, idref.Length - 1);
                                }
                                if (idref == xmlid)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            if (inBodyContent && altProp.AlternateContents != null)
            {
                foreach (var altContent in altProp.AlternateContents.ContentsAs_Enumerable)
                {
                    if (altContent.Metadatas != null)
                    {
                        foreach (var metadata in altContent.Metadatas.ContentsAs_Enumerable)
                        {
                            if (metadata.NameContentAttribute.Name.Equals(DiagramContentModelHelper.Ref))
                            {
                                string idref = metadata.NameContentAttribute.Value;
                                if (idref.StartsWith("#") && idref.Length > 1)
                                {
                                    idref = idref.Substring(1, idref.Length - 1);
                                }
                                if (idref == xmlid)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        public string ValidationText
        {
            get { return "NOT IMPLEMENTED YET."; }
        }
    }
}
