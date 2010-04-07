using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Events;
using urakawa;

namespace Tobi.Common.Validation
{
    [InheritedExport(typeof(IValidator)), PartCreationPolicy(CreationPolicy.Shared)]
    public abstract class AbstractValidator : IValidator
    {
        public abstract string Name { get; }
        public abstract string Description { get; }

        public abstract bool Validate();

        public event EventHandler<ValidatorStateRefreshedEventArgs> ValidatorStateRefreshed;
        
        private ObservableCollection<ValidationItem> m_ValidationItems = new ObservableCollection<ValidationItem>();
        public ObservableCollection<ValidationItem> ValidationItems
        {
            get
            {
                return m_ValidationItems;
            }
        }

        protected void resetToValid()
        {
            if (m_ValidationItems.Count > 0)
            {
                m_ValidationItems.Clear();
                notifyValidationStateChanged();
            }
        }

        protected void addValidationItem(ValidationItem valItem)
        {
            m_ValidationItems.Add(valItem);
            notifyValidationStateChanged();
        }

        protected void addValidationItems(IEnumerable<ValidationItem> valItems)
        {
            foreach (var valItem in valItems)
            {
                m_ValidationItems.Add(valItem);
            }
            notifyValidationStateChanged();
        }

        protected void removeValidationItem(ValidationItem valItem)
        {
            m_ValidationItems.Remove(valItem);
            notifyValidationStateChanged();
        }

        protected void removeValidationItems(IEnumerable<ValidationItem> valItems)
        {
            foreach (var valItem in valItems)
            {
                m_ValidationItems.Remove(valItem);
            }
            notifyValidationStateChanged();
        }

        protected void notifyValidationStateChanged()
        {
            EventHandler<ValidatorStateRefreshedEventArgs> eventHandler = ValidatorStateRefreshed;
            if (eventHandler != null)
            {
                eventHandler(this, new ValidatorStateRefreshedEventArgs(this));
            }
        }

        protected readonly IEventAggregator m_EventAggregator;

        protected AbstractValidator(IEventAggregator eventAggregator)
        {
            m_EventAggregator = eventAggregator;

            m_EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(OnProjectLoaded, ProjectLoadedEvent.THREAD_OPTION);
            m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(OnProjectUnLoaded, ProjectUnLoadedEvent.THREAD_OPTION);

            resetToValid();
        }

        protected virtual void OnProjectLoaded(Project project)
        {
            resetToValid();
        }

        protected virtual void OnProjectUnLoaded(Project project)
        {
            resetToValid();
        }

        public bool IsValid
        {
            get
            {
                return m_ValidationItems.Count == 0;
            }
        }
    }

    public class ValidatorStateRefreshedEventArgs : EventArgs
    {
        public readonly IValidator Validator;

        public ValidatorStateRefreshedEventArgs(IValidator validator)
        {
            Validator = validator;
        }
    }

    public interface IValidator
    {
        event EventHandler<ValidatorStateRefreshedEventArgs> ValidatorStateRefreshed;

        bool Validate();
        bool IsValid { get; }
        ObservableCollection<ValidationItem> ValidationItems { get; }
        
        string Name { get; }
        string Description { get; }
    }

    public enum ValidationSeverity
    {
        Error,
        Warning
    } 
    
    public abstract class ValidationItem
    {
        public abstract string Message { get; }
        public ValidationSeverity Severity { get; set;}
        public IValidator Validator { get; set; }
        //this is a longer string representing everything about the error
        //it will be used by the validator-aggregator to copy the error detail to the clipboard
        public abstract string CompleteSummary { get;}
        //this function launches something that shows the error in context and maybe
        //allows the user to fix it
        public abstract void TakeAction();
    }

    public static class ValidationDataTemplateProperties
    {
        public const string TypeIdentifier = "ValidationItemDataTemplate";
    }
}