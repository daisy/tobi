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
using urakawa.property.alt;
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

        public List<string> GetDuplicatedIDs(bool inHeadMetadata, bool inBodyContent)
        {
            var listOfIDs = new List<string>();
            var listOfDuplicatedIDs = new List<string>();
            foreach (var id in GetExistingXmlIDs(inHeadMetadata, inBodyContent))
            {
                if (!listOfIDs.Contains(id))
                {
                    listOfIDs.Add(id);
                }
                else if (!listOfDuplicatedIDs.Contains(id))
                {
                    listOfDuplicatedIDs.Add(id);
                }
            }
            return listOfDuplicatedIDs;
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


        private bool getValidationText_Descriptions(ref string message)
        {
            bool first = true;

            string strDupIDS = "";
            foreach (var id in GetDuplicatedIDs(false, true))
            {
                strDupIDS += "[";
                strDupIDS += id;
                strDupIDS += "]";
            }

            if (!string.IsNullOrEmpty(strDupIDS))
            {
                if (!first)
                {
                    if (message != null)
                    {
                        message += "\n";
                    }
                }
                first = false;
                if (message != null)
                {
                    message += "- Some identifiers are duplicated (may be valid if used for grouping image objects): ";
                    message += strDupIDS;
                }
            }

            bool hasMessages = !first;
            return hasMessages;
        }

        [NotifyDependsOn("ValidationText_Descriptions")]
        public bool HasValidationWarning_Descriptions
        {
            get
            {
                string str = null;
                return getValidationText_Descriptions(ref str);
            }
        }

        [NotifyDependsOn("Descriptions")]
        public string ValidationText_Descriptions
        {
            get
            {
                string str = "";
                if (HasValidationWarning_Descriptions)
                {
                    getValidationText_Descriptions(ref str);
                }
                return str;
            }
        }

        private bool getValidationText_Metadata(ref string message)
        {
            bool first = true;

            string strDupIDS = "";
            foreach (var id in GetDuplicatedIDs(true, false))
            {
                strDupIDS += "[";
                strDupIDS += id;
                strDupIDS += "]";
            }

            if (!string.IsNullOrEmpty(strDupIDS))
            {
                if (!first)
                {
                    if (message != null)
                    {
                        message += "\n";
                    }
                }
                first = false;
                if (message != null)
                {
                    message += "- Some identifiers are duplicated (may be valid if used for grouping metadata): ";
                    message += strDupIDS;
                }
            }

            bool hasMessages = !first;
            return hasMessages;
        }

        [NotifyDependsOn("ValidationText_Metadata")]
        public bool HasValidationWarning_Metadata
        {
            get
            {
                string str = null;
                return getValidationText_Metadata(ref str);
            }
        }

        [NotifyDependsOn("Metadatas")]
        public string ValidationText_Metadata
        {
            get
            {
                string str = "";
                if (HasValidationWarning_Metadata)
                {
                    getValidationText_Metadata(ref str);
                }
                return str;
            }
        }


        private bool getValidationText_BasicImage(ref string message)
        {
            bool first = true;

            AlternateContent altContent = GetAltContent(DiagramContentModelHelper.D_SimplifiedImage);
            if (altContent != null)
            {
                if (altContent.Image != null && altContent.Text == null)
                {
                    if (!first)
                    {
                        if (message != null)
                        {
                            message += "\n";
                        }
                    }
                    first = false;
                    if (message != null)
                    {
                        message += "- It is recommended to specify a tour for the simplified image.";
                    }
                }
                if (altContent.Image == null && altContent.Text != null)
                {
                    if (!first)
                    {
                        if (message != null)
                        {
                            message += "\n";
                        }
                    }
                    first = false;
                    if (message != null)
                    {
                        message += "- A tour is specified without its associated simplified image.";
                    }
                }
            }
            altContent = GetAltContent(DiagramContentModelHelper.D_Tactile);
            if (altContent != null)
            {
                if (altContent.Image != null && altContent.Text == null)
                {
                    if (!first)
                    {
                        if (message != null)
                        {
                            message += "\n";
                        }
                    }
                    first = false;
                    if (message != null)
                    {
                        message += "- It is recommended to specify a tour for the tactile image.";
                    }
                }
                if (altContent.Image == null && altContent.Text != null)
                {
                    if (!first)
                    {
                        if (message != null)
                        {
                            message += "\n";
                        }
                    }
                    first = false;
                    if (message != null)
                    {
                        message += "- A tour is specified without its associated tactile image.";
                    }
                }
            }

            bool hasMessages = !first;
            return hasMessages;
        }

        [NotifyDependsOn("ValidationText_BasicImage")]
        public bool HasValidationWarning_BasicImage
        {
            get
            {
                string str = null;
                return getValidationText_BasicImage(ref str);
            }
        }

        [NotifyDependsOn("Descriptions")]
        public string ValidationText_BasicImage
        {
            get
            {
                string str = "";
                if (HasValidationWarning_BasicImage)
                {
                    getValidationText_BasicImage(ref str);
                }
                return str;
            }
        }

        private bool getValidationText_BasicText(ref string message)
        {
            bool first = true;

            AlternateContent altContent = GetAltContent(DiagramContentModelHelper.D_LondDesc);
            if (altContent == null || altContent.Text == null || string.IsNullOrEmpty(altContent.Text.Text))
            {
                first = false;
                if (message != null)
                {
                    message += "- A long description must be specified.";
                }
            }

            altContent = GetAltContent(DiagramContentModelHelper.D_Summary);
            if (altContent == null || altContent.Text == null || string.IsNullOrEmpty(altContent.Text.Text))
            {
                if (!first)
                {
                    if (message != null)
                    {
                        message += "\n";
                    }
                }
                first = false;
                if (message != null)
                {
                    message += "- Specifying a summary is recommended.";
                }
            }

            bool hasMessages = !first;
            return hasMessages;
        }

        [NotifyDependsOn("ValidationText_BasicText")]
        public bool HasValidationWarning_BasicText
        {
            get
            {
                string str = null;
                return getValidationText_BasicText(ref str);
            }
        }

        [NotifyDependsOn("Descriptions")]
        public string ValidationText_BasicText
        {
            get
            {
                string str = "";
                if (HasValidationWarning_BasicText)
                {
                    getValidationText_BasicText(ref str);
                }
                return str;
            }
        }
    }
}
