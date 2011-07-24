using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows.Threading;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using urakawa;
using urakawa.command;
using urakawa.commands;
using urakawa.core;
using urakawa.events.undo;
using urakawa.metadata;
using urakawa.property;
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

        [ImportingConstructor]
        public DescriptionsViewModel(
            IEventAggregator eventAggregator,
            ILoggerFacade logger,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession session
            )
        {
            m_EventAggregator = eventAggregator;
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
            if (altProp == null)
            {
                AlternateContent altContent = node.Presentation.AlternateContentFactory.CreateAlternateContent();
                AlternateContentAddCommand cmd = node.Presentation.CommandFactory.CreateAlternateContentAddCommand(node, altContent);
                node.Presentation.UndoRedoManager.Execute(cmd);

                //AlternateContentProperty altContentProp = node.Presentation.PropertyFactory.CreateAlternateContentProperty();
                altProp = node.GetProperty<AlternateContentProperty>();
                Debug.Assert(altProp != null);

                Metadata meta = node.Presentation.MetadataFactory.CreateMetadata();
                meta.NameContentAttribute = new MetadataAttribute();
                meta.NameContentAttribute.Name = node.ToString() + "... dummy property";
                meta.NameContentAttribute.NamespaceUri = "dummy namespace";
                meta.NameContentAttribute.Value = "dummy content";
                var mdAttr = new MetadataAttribute();
                mdAttr.Name = "dummy name 1";
                mdAttr.Value = "dummy value 1";
                meta.OtherAttributes.Insert(meta.OtherAttributes.Count, mdAttr);
                mdAttr = new MetadataAttribute();
                mdAttr.Name = "dummy name 2";
                mdAttr.Value = "dummy value 2";
                meta.OtherAttributes.Insert(meta.OtherAttributes.Count, mdAttr);
                AlternateContentMetadataAddCommand cmd2 = node.Presentation.CommandFactory.CreateAlternateContentMetadataAddCommand(altProp, null, meta);
                node.Presentation.UndoRedoManager.Execute(cmd2);

                meta = node.Presentation.MetadataFactory.CreateMetadata();
                meta.NameContentAttribute = new MetadataAttribute();
                meta.NameContentAttribute.Name = "dummy property 2";
                meta.NameContentAttribute.NamespaceUri = "dummy namespace 2";
                meta.NameContentAttribute.Value = "dummy content 2";
                mdAttr = new MetadataAttribute();
                mdAttr.Name = "2 dummy name 1";
                mdAttr.Value = "2 dummy value 1";
                meta.OtherAttributes.Insert(meta.OtherAttributes.Count, mdAttr);
                mdAttr = new MetadataAttribute();
                mdAttr.Name = "2 dummy name 2";
                mdAttr.Value = " 2dummy value 2";
                meta.OtherAttributes.Insert(meta.OtherAttributes.Count, mdAttr);
                cmd2 = node.Presentation.CommandFactory.CreateAlternateContentMetadataAddCommand(altProp, null, meta);
                node.Presentation.UndoRedoManager.Execute(cmd2);
            }

            m_SelectedMedatadata = -1;
            RaisePropertyChanged(() => Metadatas);
            RaisePropertyChanged(() => MetadataAttributes);
        }

        private void OnTreeNodeSelectionChanged(Tuple<Tuple<TreeNode, TreeNode>, Tuple<TreeNode, TreeNode>> oldAndNewTreeNodeSelection)
        {
            Tuple<TreeNode, TreeNode> oldTreeNodeSelection = oldAndNewTreeNodeSelection.Item1;
            Tuple<TreeNode, TreeNode> newTreeNodeSelection = oldAndNewTreeNodeSelection.Item2;

            //Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = newTreeNodeSelection.Item2 ?? newTreeNodeSelection.Item1;
            if (node == null) return;

            m_SelectedMedatadata = -1;
            RaisePropertyChanged(() => Metadatas);
            RaisePropertyChanged(() => MetadataAttributes);
        }


        //public ObservableCollection<Description> Descriptions { get; set; }

        //private void reset()
        //{
        //    Descriptions.Clear();

        //    foreach (var description in thing.Descriptions)
        //    {
        //        Descriptions.Add(validationItem);
        //    }

        //    RaisePropertyChanged(() => Descriptions);
        //}



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

            m_SelectedMedatadata = -1;
            RaisePropertyChanged(() => Metadatas);
            RaisePropertyChanged(() => MetadataAttributes);

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

            RaisePropertyChanged(() => Metadatas);
        }

        public IEnumerable<MetadataAttribute> MetadataAttributes
        {
            get
            {
                if (m_SelectedMedatadata == -1) return null;

                if (m_UrakawaSession.DocumentProject == null) return null;

                Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                TreeNode node = selection.Item2 ?? selection.Item1;
                if (node == null) return null;

                AlternateContentProperty altProp = node.GetProperty<AlternateContentProperty>();
                if (altProp == null) return null;

                return altProp.Metadatas.Get(m_SelectedMedatadata).OtherAttributes.ContentsAs_Enumerable;
            }
        }

        private int m_SelectedMedatadata = -1;
        public void SetSelectedMetadata(int selectedIndex)
        {
            m_SelectedMedatadata = selectedIndex;
            RaisePropertyChanged(() => MetadataAttributes);
        }
        public IEnumerable<Metadata> Metadatas
        {
            get
            {
                if (m_UrakawaSession.DocumentProject == null) return null;

                Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                TreeNode node = selection.Item2 ?? selection.Item1;
                if (node == null) return null;

                AlternateContentProperty altProp = node.GetProperty<AlternateContentProperty>();
                if (altProp == null) return null;

                return altProp.Metadatas.ContentsAs_Enumerable;
            }
        }
    }
}
