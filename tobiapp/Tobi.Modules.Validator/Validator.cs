using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.Validation;
using urakawa;
using urakawa.events.undo;

namespace Tobi.Plugin.Validator
{
    [Export(typeof(ValidatorAggregator)), PartCreationPolicy(CreationPolicy.Shared)]
    public class ValidatorAggregator : AbstractValidator, IPartImportsSatisfiedNotification
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
        
        //public readonly IEnumerable<IValidator> Validators;
        public List<ObservableValidatorWrapper> ObsValidators{ get; private set;}
        
        private bool m_RanOnce;

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
            m_Logger = logger;
            m_RanOnce = false;
            //Validators = validators;
            ObsValidators = new List<ObservableValidatorWrapper>();
            foreach (IValidator v in validators)
            {
                ObsValidators.Add(new ObservableValidatorWrapper(v));
            }
            
            m_UrakawaSession = session;
            
            IsValid = true;

            if (m_UrakawaSession.DocumentProject != null)
            {
                OnProjectLoaded(m_UrakawaSession.DocumentProject);
            }

            m_EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(OnProjectLoaded, ProjectLoadedEvent.THREAD_OPTION);
            m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(OnProjectUnLoaded, ProjectUnLoadedEvent.THREAD_OPTION);
        }

        //EventAggregator.GetEvent<TypeConstructedEvent>().Publish(GetType());
        //SubscriptionToken token = EventAggregator.GetEvent<TypeConstructedEvent>().Subscribe(OnTypeConstructed_IUrakawaSession, ThreadOption.UIThread, false, type => typeof(IUrakawaSession).IsAssignableFrom(type));

        private void OnProjectLoaded(Project project)
        {
            project.Presentations.Get(0).UndoRedoManager.CommandDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandReDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandUnDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.TransactionCancelled += OnUndoRedoManagerChanged;
            m_RanOnce = false;
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
            get { return Tobi_Plugin_Validator_Lang.Validator_Name; }    // TODO LOCALIZE Validator_Name
        }

        public override string Description
        {
            get { return Tobi_Plugin_Validator_Lang.Validator_Description; }    // TODO LOCALIZE Validator_Description
        }

        public override IEnumerable<ValidationItem> ValidationItems
        {
            get
            {
                if (IsValid)
                {
                    yield return null;
                }

                foreach (ObservableValidatorWrapper ov in ObsValidators)
                {
                    if (ov.Validator.IsValid)
                    {
                        continue;
                    }

                    foreach (ValidationItem item in ov.ValidationItems)
                    {
                        yield return item;
                    }
                }

                yield break;
            }
        }
        
        public override bool Validate()
        {
            
            bool isValid = true;
            
            foreach (ObservableValidatorWrapper ov in ObsValidators)
            {
                bool result = true;
                if (!ov.Validator.ShouldRunOnlyOnce || 
                    (ov.Validator.ShouldRunOnlyOnce && !m_RanOnce))
                {
                    result = ov.Validator.Validate();
                    isValid = isValid && result;
                }
                
            }
             
            /*
            foreach (IValidator v in Validators)
            {
                bool result = true;
                if (!v.ShouldRunOnlyOnce ||
                    (v.ShouldRunOnlyOnce && !m_RanOnce))
                {
                    result = v.Validate();
                    isValid = isValid && result;
                }

            }
            */
            IsValid = isValid;

            m_RanOnce = true;

            return IsValid;
        }
    }

    //give each validator an observablecollection of items
    public class ObservableValidatorWrapper
    {
        public IValidator Validator { get; private set; }
        public ObservableCollection<ValidationItem> ValidationItems { get; private set; }
       
        public ObservableValidatorWrapper(IValidator validator)
        {
            Validator = validator;
            ValidationItems = new ObservableCollection<ValidationItem>(Validator.ValidationItems);
            Validator.ValidatorStateRefreshed += new EventHandler<ValidatorStateRefreshedEventArgs>(Validator_ValidatorStateRefreshed);
            refreshValidationItems();
        }
        ~ObservableValidatorWrapper()
        {
            Validator.ValidatorStateRefreshed -= new EventHandler<ValidatorStateRefreshedEventArgs>(Validator_ValidatorStateRefreshed);
        }
        void Validator_ValidatorStateRefreshed(object sender, ValidatorStateRefreshedEventArgs e)
        {
            refreshValidationItems();
        }
        private void refreshValidationItems()
        {
            ValidationItems.Clear();
            foreach (ValidationItem v in Validator.ValidationItems)
            {
                ValidationItems.Add(v);
            }
            
        }
        
    }
}
