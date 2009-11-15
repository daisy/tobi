using System;
using System.Collections.Generic;

namespace Tobi.Common.Validation
{
    public abstract class AbstractValidator : IValidator
    {
        public event EventHandler<ValidatorStateChangedEventArgs> ValidatorStateChanged;
        
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract bool Validate();
        public abstract IEnumerable<ValidationItem> ValidationItems { get; }

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

                m_IsValid = value;

                EventHandler<ValidatorStateChangedEventArgs> ev = ValidatorStateChanged;
                if (ev != null) ev(this, new ValidatorStateChangedEventArgs(this));
            }
        }
    }

    public class ValidatorStateChangedEventArgs : EventArgs
    {
        public readonly IValidator Validator;

        public ValidatorStateChangedEventArgs(IValidator validator)
        {
            Validator = validator;
        }
    }

    public interface IValidator
    {
        event EventHandler<ValidatorStateChangedEventArgs> ValidatorStateChanged;

        string Name { get; }
        string Description { get; }
        bool Validate();
        bool IsValid { get; }
        IEnumerable<ValidationItem> ValidationItems { get; }
    }

    public enum ValidationSeverity
    {
        Error,
        Warning
    } 

    public class ValidationItem
    {
        public string Message { get; set; }
        public ValidationSeverity Severity { get; set;}
        public IValidator Validator { get; set; }
    }
}