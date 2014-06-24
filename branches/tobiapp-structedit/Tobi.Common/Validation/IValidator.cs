using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Events;
using Tobi.Common.MVVM;
using urakawa;
using urakawa.core;
using urakawa.metadata;

namespace Tobi.Common.Validation
{
    [InheritedExport(typeof(IValidator)), PartCreationPolicy(CreationPolicy.Shared)]
    public abstract class AbstractValidator : PropertyChangedNotifyBase, IValidator
    {
        public abstract string Name { get; }
        public abstract string Description { get; }

        public string NameAndNumberOfIssues
        {
            get { return Name + " (" + ValidationItems.Count + ")"; }
        }

        public abstract bool Validate();

        public event EventHandler<ValidatorStateRefreshedEventArgs> ValidatorStateRefreshed;
        
        private readonly ObservableCollection<ValidationItem> m_ValidationItems = new ObservableCollection<ValidationItem>();
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

        protected void insertValidationItem(int i, ValidationItem valItem)
        {
            m_ValidationItems.Insert(i, valItem);
            notifyValidationStateChanged();
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
            RaisePropertyChanged(() => NameAndNumberOfIssues);

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
        public abstract bool CanTakeAction { get; }
    }
    public abstract class ValidationItemWithTarget<T> : ValidationItem
    {
        public T Target { get; set; }
    }
   /* public abstract class ValidationItemWithTreeNodeTarget : ValidationItem
    {
        public TreeNode Target { get; set; }
    }
    public abstract class ValidationItemWithMetadataTarget : ValidationItem
    {
        public Metadata Target { get; set; }
    }
    */
    public static class ValidationDataTemplateProperties
    {
        public const string TypeIdentifier = "ValidationItemDataTemplate";
    }
}