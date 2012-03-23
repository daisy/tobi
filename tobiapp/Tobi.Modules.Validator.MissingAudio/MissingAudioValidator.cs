using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.Validation;
using urakawa;
using urakawa.command;
using urakawa.commands;
using urakawa.core;
using urakawa.events.undo;
using urakawa.xuk;

#if USE_ISOLATED_STORAGE
using System.IO.IsolatedStorage;
#endif //USE_ISOLATED_STORAGE

namespace Tobi.Plugin.Validator.MissingAudio
{
    /// <summary>
    /// The main validator class
    /// </summary>
    public class MissingAudioValidator : AbstractValidator, IPartImportsSatisfiedNotification
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

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// The Initialize method is then normally called by the bootstrapper of the plugin framework.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity dependency injection container, it's a built-in CAG service</param>
        ///<param name="session">normally obtained from the MEF composition container, it's a Tobi-specific service</param>
        [ImportingConstructor]
        public MissingAudioValidator(
            ILoggerFacade logger,
            IEventAggregator eventAggregator,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = false, AllowDefault = false)]
            IUrakawaSession session)
            : base(eventAggregator)
        {
            m_Logger = logger;
            m_Session = session;

            m_EventAggregator.GetEvent<NoAudioContentFoundByFlowDocumentParserEvent>().Subscribe(OnNoAudioContentFoundByFlowDocumentParserEvent, NoAudioContentFoundByFlowDocumentParserEvent.THREAD_OPTION);

            m_Logger.Log(@"MissingAudioValidator initialized", Category.Debug, Priority.Medium);
        }

        protected override void OnProjectLoaded(Project project)
        {
            // WE MUST PREVENT THE BASE CLASS TO RESET THE VALIDATION ITEMS (WHICH WE JUST RECEIVED FROM THE FLOWDOC PARSER)
            //base.OnProjectLoaded(project);

            project.Presentations.Get(0).UndoRedoManager.CommandDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandReDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandUnDone += OnUndoRedoManagerChanged;
            //project.Presentations.Get(0).UndoRedoManager.TransactionEnded += OnUndoRedoManagerChanged;
        }

        protected override void OnProjectUnLoaded(Project project)
        {
            base.OnProjectUnLoaded(project);

            project.Presentations.Get(0).UndoRedoManager.CommandDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandReDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandUnDone -= OnUndoRedoManagerChanged;
            //project.Presentations.Get(0).UndoRedoManager.TransactionEnded -= OnUndoRedoManagerChanged;
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

            updateTreeNodeAudioStatus(cmd, done);
        }

        private void updateTreeNodeAudioStatus(TreeNode node)
        {
            foreach (var childTreeNode in node.Children.ContentsAs_Enumerable)
            {
                updateTreeNodeAudioStatus(childTreeNode);
            }

            if (!bTreeNodeNeedsAudio(node))
            {
                return;
            }

            if (!node.HasOrInheritsAudio())
            {
                bool alreadyInList = false;
                foreach (var vItem in ValidationItems)
                {
                    var valItem = vItem as MissingAudioValidationError;

                    DebugFix.Assert(valItem != null);
                    if (valItem == null) continue;

                    if (valItem.Target == node)
                    {
                        alreadyInList = true;
                        break;
                    }
                }
                if (!alreadyInList)
                {
                    var validationItem = new MissingAudioValidationError(m_Session)
                    {
                        Target = node,
                        Validator = this
                    };

                    bool inserted = false;
                    int i = -1;
                    foreach (var vItem in ValidationItems)
                    {
                        i++;
                        var valItem = vItem as MissingAudioValidationError;

                        DebugFix.Assert(valItem != null);
                        if (valItem == null) continue;

                        if (node.IsBefore(valItem.Target))
                        {
                            insertValidationItem(i, validationItem);
                            inserted = true;
                            break;
                        }
                    }
                    if (!inserted)
                    {
                        addValidationItem(validationItem);
                    }
                }
            }
            else
            {
                var toRemove = new List<ValidationItem>();

                foreach (var vItem in ValidationItems)
                {
                    var valItem = vItem as MissingAudioValidationError;

                    DebugFix.Assert(valItem != null);
                    if (valItem == null) continue;

                    if (valItem.Target == node)
                    {
                        toRemove.Add(vItem);
                    }
                }

                removeValidationItems(toRemove);
            }
        }

        private void updateTreeNodeAudioStatus(Command cmd, bool done)
        {
            if (cmd is ManagedAudioMediaInsertDataCommand)
            {
                var command = (ManagedAudioMediaInsertDataCommand)cmd;
                updateTreeNodeAudioStatus(command.TreeNode);
            }
            else if (cmd is TreeNodeSetManagedAudioMediaCommand)
            {
                var command = (TreeNodeSetManagedAudioMediaCommand)cmd;
                updateTreeNodeAudioStatus(command.TreeNode);
            }
            else if (cmd is TreeNodeAudioStreamDeleteCommand)
            {
                var command = (TreeNodeAudioStreamDeleteCommand)cmd;
                updateTreeNodeAudioStatus(command.SelectionData.m_TreeNode);
            }
            else if (cmd is CompositeCommand)
            {
                foreach (var childCommand in ((CompositeCommand)cmd).ChildCommands.ContentsAs_Enumerable)
                {
                    updateTreeNodeAudioStatus(childCommand, done);
                }
            }
        }

        public override string Name
        {
            get { return Tobi_Plugin_Validator_MissingAudio_Lang.MissingAudioValidator_Name; }
        }

        public override string Description
        {
            get { return Tobi_Plugin_Validator_MissingAudio_Lang.MissingAudioValidator_Description; }
        }

        private void OnNoAudioContentFoundByFlowDocumentParserEvent(TreeNode treeNode)
        {
            DebugFix.Assert(bTreeNodeNeedsAudio(treeNode));
            DebugFix.Assert(!treeNode.HasOrInheritsAudio());

            var error = new MissingAudioValidationError(m_Session)
            {
                Target = treeNode,
                Validator = this
            };
            addValidationItem(error);
        }

        public override bool Validate()
        {
            return IsValid;
        }

        private static bool bTreeNodeNeedsAudio(TreeNode node)
        {
            if (node.GetTextMedia() != null)
            {
                DebugFix.Assert(node.Children.Count == 0);
                return true;
            }

            QualifiedName qname = node.GetXmlElementQName();
            if (qname != null &&
                (
                qname.LocalName.Equals("img", StringComparison.OrdinalIgnoreCase)
                || qname.LocalName.Equals("video", StringComparison.OrdinalIgnoreCase)
                ))
            {
                DebugFix.Assert(node.Children.Count == 0);
                return true;
            }

            return false;
        }
    }
}
