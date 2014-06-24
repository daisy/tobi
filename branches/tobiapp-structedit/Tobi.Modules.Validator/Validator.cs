using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.Validation;

namespace Tobi.Plugin.Validator
{
    [Export(typeof(ValidatorAggregator)), PartCreationPolicy(CreationPolicy.Shared)]
    public class ValidatorAggregator : IPartImportsSatisfiedNotification
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
        private readonly IUrakawaSession m_UrakawaSession;
        protected readonly IEventAggregator m_EventAggregator;

        public readonly List<IValidator> m_Validators = new List<IValidator>();
        public IEnumerable<IValidator> Validators
        {
            get
            {
                return m_Validators;
            }
        }

        [ImportingConstructor]
        public ValidatorAggregator(
            ILoggerFacade logger,
            IEventAggregator eventAggregator,
            [ImportMany(typeof(IValidator), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = false)]
            IEnumerable<IValidator> validators,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession session)
        {
            m_EventAggregator = eventAggregator;
            m_UrakawaSession = session;
            m_Logger = logger;

            foreach (IValidator validator in validators)
            {
                m_Validators.Add(validator);
                validator.ValidatorStateRefreshed += OnValidatorStateRefreshed;
            }

            //m_EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(OnProjectLoaded, ProjectLoadedEvent.THREAD_OPTION);
            //m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(OnProjectUnLoaded, ProjectUnLoadedEvent.THREAD_OPTION);
        }

        private void OnValidatorStateRefreshed(object sender, ValidatorStateRefreshedEventArgs e)
        {
            var validator = e.Validator.ValidationItems;
        }

//        protected void OnProjectLoaded(Project project)
//        {
//            project.Presentations.Get(0).UndoRedoManager.CommandDone += OnUndoRedoManagerChanged;
//            project.Presentations.Get(0).UndoRedoManager.CommandReDone += OnUndoRedoManagerChanged;
//            project.Presentations.Get(0).UndoRedoManager.CommandUnDone += OnUndoRedoManagerChanged;
//            //project.Presentations.Get(0).UndoRedoManager.TransactionEnded += OnUndoRedoManagerChanged;
//        }

//        protected void OnProjectUnLoaded(Project project)
//        {
//            project.Presentations.Get(0).UndoRedoManager.CommandDone -= OnUndoRedoManagerChanged;
//            project.Presentations.Get(0).UndoRedoManager.CommandReDone -= OnUndoRedoManagerChanged;
//            project.Presentations.Get(0).UndoRedoManager.CommandUnDone -= OnUndoRedoManagerChanged;
//            //project.Presentations.Get(0).UndoRedoManager.TransactionEnded -= OnUndoRedoManagerChanged;
//        }

//        private void OnUndoRedoManagerChanged(object sender, UndoRedoManagerEventArgs e)
//        {
//            if (!Dispatcher.CurrentDispatcher.CheckAccess())
//            {
//#if DEBUG
//                Debugger.Break();
//#endif
//                Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Normal, (Action<object, UndoRedoManagerEventArgs>)OnUndoRedoManagerChanged, sender, e);
//                return;
//            }
//        }
    }
}
