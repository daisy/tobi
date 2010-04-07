using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.Validation;
using urakawa;
using urakawa.commands;
using urakawa.core;
using urakawa.events.undo;
using urakawa.media.data;

#if USE_ISOLATED_STORAGE
using System.IO.IsolatedStorage;
#endif //USE_ISOLATED_STORAGE

namespace Tobi.Plugin.Validator.AudioContent
{
    /// <summary>
    /// The main validator class
    /// </summary>
    [Export(typeof(IValidator)), PartCreationPolicy(CreationPolicy.Shared)]
    public class AudioContentValidator : AbstractValidator, IPartImportsSatisfiedNotification
    {
#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied()
#pragma warning restore 1591
        {
            //#if DEBUG
            //            Debugger.Break();
            //#endif
        }

        private readonly ILoggerFacade m_Logger;
        protected readonly IUrakawaSession m_Session;
        private readonly IEventAggregator m_EventAggregator;
        
        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        ///<param name="session">normally obtained from the MEF composition container, it's a Tobi-specific service</param>
        [ImportingConstructor]
        public AudioContentValidator(
            ILoggerFacade logger,
            IEventAggregator eventAggregator,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = false, AllowDefault = false)]
            IUrakawaSession session)
        {
            m_Logger = logger;
            m_Session = session;
            m_EventAggregator = eventAggregator;

            m_EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(OnProjectLoaded, ProjectLoadedEvent.THREAD_OPTION);
            m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(OnProjectUnLoaded, ProjectUnLoadedEvent.THREAD_OPTION);

            m_ValidationItems = new List<ValidationItem>();
            m_Logger.Log(@"AudioContentValidator initialized", Category.Debug, Priority.Medium);
        }
        
        private void OnProjectLoaded(Project project)
        {
            project.Presentations.Get(0).UndoRedoManager.CommandDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandReDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandUnDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.TransactionCancelled += OnUndoRedoManagerChanged;
        }

        private void OnProjectUnLoaded(Project project)
        {
            project.Presentations.Get(0).UndoRedoManager.CommandDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandReDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandUnDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.TransactionCancelled -= OnUndoRedoManagerChanged;
        }

        private void OnUndoRedoManagerChanged(object sender, UndoRedoManagerEventArgs e)
        {
            TreeNode node = null;

            //see if there was a change in the audio media for the affected nodes
            if (e.Command is ManagedAudioMediaInsertDataCommand)
            {
                node = (e.Command as ManagedAudioMediaInsertDataCommand).TreeNode;
                AudioContentValidationError error =
                    (AudioContentValidationError)m_ValidationItems.Find(v => (v as AudioContentValidationError).Target == node);
                if (error != null)
                {
                    m_ValidationItems.Remove(error);
                    //TODO: raise validator refreshed event
                }

            }
            else if (e.Command is TreeNodeAudioStreamDeleteCommand)
            {
                //is CurrentTreeNode correct?  want to know the tree node that had its audio deleted.
                node = (e.Command as TreeNodeAudioStreamDeleteCommand).CurrentTreeNode;
                WalkTreeAndFlagMissingAudio(node);
            }
            else if (e.Command is TreeNodeSetManagedAudioMediaCommand)
            {
                node = (e.Command as TreeNodeSetManagedAudioMediaCommand).TreeNode;
                AudioContentValidationError error =
                   (AudioContentValidationError)m_ValidationItems.Find(v => (v as AudioContentValidationError).Target == node);
                if (error != null)
                {
                    m_ValidationItems.Remove(error);
                    //TODO: raise validator refreshed event
                }
            }

        }

        public override string Name
        {
            get { return "Audio Content Validator"; }
        }

        public override string Description
        {
            get { return "A validator that shows which text nodes are missing audio content"; }
        }

        public override bool ShouldRunOnlyOnce
        {
            get { return true; }
        }

        public override IEnumerable<ValidationItem> ValidationItems
        {
            get { return m_ValidationItems; }
        }

        private List<ValidationItem> m_ValidationItems;

        public override bool Validate()
        {  
            if (m_Session.DocumentProject != null &&
                m_Session.DocumentProject.Presentations.Count > 0)
            {
                m_ValidationItems = new List<ValidationItem>();

                //this expensive operation could be replaced by creating and hooking into events from the XukToFlowDocument process
                WalkTreeAndFlagMissingAudio(m_Session.DocumentProject.Presentations.Get(0).RootNode);
                
            }

            IsValid = m_ValidationItems.Count <= 0;
            return IsValid;
        }

        private void WalkTreeAndFlagMissingAudio(TreeNode node)
        {
            if (node.Children.Count > 0)
            {
                foreach (TreeNode child in node.Children.ContentsAs_YieldEnumerable)
                {
                    WalkTreeAndFlagMissingAudio(child);
                }
            }
            else
            {
                if (node.GetFirstAncestorWithManagedAudio() == null)
                {
                    AudioContentValidationError error = new AudioContentValidationError();
                    error.Target = node;
                    error.Validator = this;
                    m_ValidationItems.Add(error);

                    //TODO: raise validator refreshed event
                }

            }
        }


        
    }
}
