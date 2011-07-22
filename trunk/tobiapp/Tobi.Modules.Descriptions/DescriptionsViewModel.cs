using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using urakawa;
using urakawa.commands;
using urakawa.core;
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



        private void OnProjectUnLoaded(Project obj)
        {
            OnProjectLoaded(null);
        }

        private void OnProjectLoaded(Project project)
        {
            m_Logger.Log("DescriptionsViewModel.OnProject(UN)Loaded" + (project == null ? "(null)" : ""),
                Category.Debug, Priority.Medium);

            //


            TreeNode node = null;
            //if (m_TextElementForEdit != null)
            //{
            //    Debug.Assert(m_TextElementForEdit.Tag is TreeNode);
            //    node = (TreeNode)m_TextElementForEdit.Tag;
            //    m_TextElementForEdit = null;
            //}
            //else
            {
                Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                node = selection.Item2 ?? selection.Item1;
            }
            if (node == null) return;

            AlternateContentProperty altProp = node.GetProperty<AlternateContentProperty>();

            //string oldTxt = null;
            //if (altProp != null && altProp.ShortDescription != null)
            //{
            //    oldTxt = altProp.ShortDescription.Text.Text;
            //}

            //string txt = showDialogTextEdit(oldTxt);

            //if (txt == oldTxt) return;

            m_EventAggregator.GetEvent<EscapeEvent>().Publish(null);

            //if (string.IsNullOrEmpty(txt))
            //{
            //    //remove ShortDescription
            //}
            //else
            //{
            //    //change ShortDescription text
            //}

            //var cmd = node.Presentation.CommandFactory.CreateTreeNodeAddAlternateContentCommand();
            //node.Presentation.UndoRedoManager.Execute(cmd);
        }
    }
}
