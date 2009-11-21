using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Events;
using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Common.Validation;
using urakawa;
using urakawa.events.undo;

namespace Tobi.Plugin.Validator
{
    [Export(typeof(Validator)), PartCreationPolicy(CreationPolicy.Shared)]
    public class Validator : AbstractValidator, IPartImportsSatisfiedNotification
    {
        public void OnImportsSatisfied()
        {
//#if DEBUG
//            Debugger.Break();
//#endif

            var session = Container.Resolve<IUrakawaSession>();
            if (session.DocumentProject != null)
            {
                OnProjectLoaded(session.DocumentProject);
            }
        }

        public readonly IEnumerable<IValidator> Validators;

        [ImportingConstructor]
        public Validator(
            IUnityContainer container,
            IEventAggregator eventAggregator,
            ILoggerFacade logger,

            [ImportMany(typeof(IValidator), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = false)]
            IEnumerable<IValidator> validators)
        {
            Container = container;
            EventAggregator = eventAggregator;
            Logger = logger;

            Validators = validators;

            IsValid = true;

            EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(OnProjectLoaded, ThreadOption.UIThread);
            EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(OnProjectUnLoaded, ThreadOption.UIThread);
        }

        //EventAggregator.GetEvent<TypeConstructedEvent>().Publish(GetType());
        //SubscriptionToken token = EventAggregator.GetEvent<TypeConstructedEvent>().Subscribe(OnTypeConstructed_IUrakawaSession, ThreadOption.UIThread, false, type => typeof(IUrakawaSession).IsAssignableFrom(type));

        protected IEventAggregator EventAggregator { get; private set; }
        protected ILoggerFacade Logger { get; private set; }
        protected IUnityContainer Container { get; private set; }

        private void OnProjectLoaded(Project project)
        {
            project.Presentations.Get(0).UndoRedoManager.CommandDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandReDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandUnDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.TransactionCancelled += OnUndoRedoManagerChanged;

            Validate();
        }

        private void OnProjectUnLoaded(Project project)
        {
            IsValid = true;

            project.Presentations.Get(0).UndoRedoManager.CommandDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandReDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandUnDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.TransactionCancelled -= OnUndoRedoManagerChanged;
        }

        private void OnUndoRedoManagerChanged(object sender, UndoRedoManagerEventArgs e)
        {
            Validate();
        }

        public override string Name
        {
            get { return "Aggregator Validator"; }
        }

        public override string Description
        {
            get { return "This is the main content validator"; }
        }

        public override IEnumerable<ValidationItem> ValidationItems
        {
            get
            {
                if (IsValid)
                {
                    yield return null;
                }

                foreach (IValidator validator in Validators)
                {
                    if (validator.IsValid)
                    {
                        continue;
                    }

                    foreach (ValidationItem item in validator.ValidationItems)
                    {
                        yield return item;
                    }
                }

                yield break;
            }
        }

        public override bool Validate()
        {
            bool m_IsValid = true;

            foreach (IValidator validator in Validators)
            {
                m_IsValid = m_IsValid && validator.Validate();
            }

            IsValid = m_IsValid;
            return IsValid;
        }
    }
}
