﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.Validation;
using urakawa;
using urakawa.command;
using urakawa.commands;
using urakawa.core;
using urakawa.daisy;
using urakawa.events.undo;
using urakawa.property.xml;
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

            Tobi.Common.Settings.Default.PropertyChanged += OnSettingsPropertyChanged;

            m_Logger.Log(@"MissingAudioValidator initialized", Category.Debug, Priority.Medium);
        }

        private void updateTreeNodeAudioStatus(bool forceRemove, TreeNode node)
        {
            foreach (var childTreeNode in node.Children.ContentsAs_Enumerable)
            {
                updateTreeNodeAudioStatus(forceRemove, childTreeNode);
            }

            if (!forceRemove && node.NeedsAudio() && !node.HasOrInheritsAudio()
                && (!Tobi.Common.Settings.Default.ValidMissingAudioElements_Enable || !isTreeNodeValidNoAudio(node)))
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


        public void OnUndoRedoManagerChanged(UndoRedoManagerEventArgs eventt, bool done, Command command, bool isTransactionEndEvent, bool isNoTransactionOrTrailingEdge)
        {
//            if (!Dispatcher.CheckAccess())
//            {
//#if DEBUG
//                Debugger.Break();
//#endif

//#if NET40x
//                TheDispatcher.Invoke(DispatcherPriority.Normal,
//                    (Action<UndoRedoManagerEventArgs, bool, Command, bool, bool>)OnUndoRedoManagerChanged,
//                    eventt, done, command, isTransactionEndEvent, isNoTransactionOrTrailingEdge);
//#else
//            TheDispatcher.Invoke(DispatcherPriority.Normal,
//                (Action)(() => OnUndoRedoManagerChanged(eventt, done, command, isTransactionEndEvent, isNoTransactionOrTrailingEdge))
//                );
//#endif
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

        private void OnSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == PropertyChangedNotifyBase.GetMemberName(() => Tobi.Common.Settings.Default.ValidMissingAudioElements))
            {
                m_ValidNoAudioElements = null;
            }
        }

        private List<string> m_ValidNoAudioElements;
        private bool isElementValidNoAudio(string name, string epubType)
        {
            if (m_ValidNoAudioElements == null)
            {
                string[] names = Tobi.Common.Settings.Default.ValidMissingAudioElements.Split(new char[] { ',', ' ', ';', '/' });

                //m_SkippableElements = new List<string>(names);
                m_ValidNoAudioElements = new List<string>(names.Length);

                foreach (string n in names)
                {
                    string n_ = n.Trim(); //.ToLower();
                    if (!string.IsNullOrEmpty(n_))
                    {
                        m_ValidNoAudioElements.Add(n_);
                    }
                }
            }

            foreach (var str in m_ValidNoAudioElements)
            {
                if (str.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (!string.IsNullOrEmpty(epubType) && str.Equals(epubType, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;

            //return m_ValidNoAudioElements.Contains(name.ToLower());
        }
        
        public bool isTreeNodeValidNoAudio(TreeNode node)
        {
            if (node.HasXmlProperty)
            {
                string epubType = null;
                XmlProperty xmlProp = node.GetXmlProperty();
                XmlAttribute attrEpubType = xmlProp.GetAttribute("epub:type", DiagramContentModelHelper.NS_URL_EPUB);
                if (attrEpubType != null && !string.IsNullOrEmpty(attrEpubType.Value))
                {
                    epubType = attrEpubType.Value;
                }
                
                if (isElementValidNoAudio(node.GetXmlElementLocalName(), epubType))
                {
                    return true;
                }
            }
            if (node.Parent == null)
            {
                return false;
            }
            return isTreeNodeValidNoAudio(node.Parent);
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

            if (m_UndoRedoManagerHooker != null) m_UndoRedoManagerHooker.UnHook();
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

            if (Tobi.Common.Settings.Default.ValidMissingAudioElements_Enable && isTreeNodeValidNoAudio(treeNode))
            {
                return;
            }

            foreach (var valItem in ValidationItems)
            {
                // ensure no duplicates
                if (valItem is MissingAudioValidationError && ((MissingAudioValidationError) valItem).Target == treeNode)
                    return;
            }

            var error = new MissingAudioValidationError(m_Session)
            {
                Target = treeNode,
                Validator = this
            };
            addValidationItem(error);
        }

        public override bool Validate()
        {
            if (m_Session.IsXukSpine)
            {
                return true;
            }

            return IsValid;
        }
    }
}
