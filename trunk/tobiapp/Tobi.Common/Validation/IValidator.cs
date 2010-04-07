using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Windows;

namespace Tobi.Common.Validation
{
    public abstract class AbstractValidator : IValidator
    {
        public event EventHandler<ValidatorStateRefreshedEventArgs> ValidatorStateRefreshed;
        
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract bool Validate();
        public abstract IEnumerable<ValidationItem> ValidationItems { get; }

        public virtual bool ShouldRunOnlyOnce
        {
            get { return false; }
        }

        private bool m_IsValid;
        public bool IsValid
        {
            get { return m_IsValid; }
            protected set
            {
                if (value == m_IsValid)
                {
                    return;
                }

                EventHandler<ValidatorStateRefreshedEventArgs> ev = ValidatorStateRefreshed;
                if (ev != null) ev(this, new ValidatorStateRefreshedEventArgs(this));

                m_IsValid = value;
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

        string Name { get; }
        string Description { get; }
        bool Validate();
        bool IsValid { get; }
        IEnumerable<ValidationItem> ValidationItems { get; }
        bool ShouldRunOnlyOnce { get;}
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