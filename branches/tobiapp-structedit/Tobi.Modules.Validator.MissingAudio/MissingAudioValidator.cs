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
using urakawa.daisy;
using urakawa.events.undo;
using urakawa.undo;
using urakawa.xuk;

#if USE_ISOLATED_STORAGE
using System.IO.IsolatedStorage;
#endif //USE_ISOLATED_STORAGE

namespace Tobi.Plugin.Validator.MissingAudio
{
    /// <summary>
    /// The main validator class
    /// </summary>
    public class MissingAudioValidator : AbstractValidator, IPartImportsSatisfiedNotification, UndoRedoManager.Hooker.Host
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

        private void updateTreeNodeAudioStatus(bool forceRemove, TreeNode node)
        {
            foreach (var childTreeNode in node.Children.ContentsAs_Enumerable)
            {
                updateTreeNodeAudioStatus(forceRemove, childTreeNode);
            }

            if (!forceRemove && node.NeedsAudio() && !node.HasOrInheritsAudio())
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


        public void OnUndoRedoManagerChanged(UndoRedoManagerEventArgs eventt, bool isTransactionActive, bool done, Command command)
        {
//            if (!Dispatcher.CheckAccess())
//            {
//#if DEBUG
//                Debugger.Break();
//#endif
//                Dispatcher.Invoke(DispatcherPriority.Normal, (Action<UndoRedoManagerEventArgs, bool, bool, Command>)OnUndoRedoManagerChanged, eventt, isTransactionActive, done, command);
//                return;
//            }

            if (command is CompositeCommand)
            {
#if DEBUG
                Debugger.Break();
#endif
            }

            //if (!command.IsTransaction()
            //    || done && command.IsTransactionLast()
            //    || !done && command.IsTransactionFirst()
            //    )
            //{
            //}

            TreeNode node = null;
            bool forceRemove = false;

            if (command is AudioEditCommand)
            {
                var cmd = (AudioEditCommand)command;
                node = cmd.TreeNode;
            }
            else if (command is TreeNodeChangeTextCommand)
            {
                var cmd = (TreeNodeChangeTextCommand)command;
                node = cmd.TreeNode;
            }
            else if (command is TextNodeStructureEditCommand)
            {
                var cmd = (TextNodeStructureEditCommand)command;
                node = cmd.TreeNode;

                forceRemove = (command is TreeNodeInsertCommand && !done) || (command is TreeNodeRemoveCommand && done);
            }

            if (node != null)
            {
                updateTreeNodeAudioStatus(forceRemove, node);
            }
        }

        private UndoRedoManager.Hooker m_UndoRedoManagerHooker = null;

        protected override void OnProjectLoaded(Project project)
        {
            if (m_Session.IsXukSpine)
            {
                return;
            }

            // WE MUST PREVENT THE BASE CLASS TO RESET THE VALIDATION ITEMS (WHICH WE JUST RECEIVED FROM THE FLOWDOC PARSER)
            //base.OnProjectLoaded(project);

            m_UndoRedoManagerHooker = project.Presentations.Get(0).UndoRedoManager.Hook(this);
        }

        protected override void OnProjectUnLoaded(Project project)
        {
            base.OnProjectUnLoaded(project);

            m_UndoRedoManagerHooker.UnHook();
            m_UndoRedoManagerHooker = null;
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
            DebugFix.Assert(treeNode.NeedsAudio());
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
    }
}
