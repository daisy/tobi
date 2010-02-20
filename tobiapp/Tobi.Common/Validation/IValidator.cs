using System;
using System.Collections.Generic;
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
        private bool m_IsValid;
        public bool IsValid
        {
            get { return m_IsValid; }
            protected set
            {
                EventHandler<ValidatorStateRefreshedEventArgs> ev = ValidatorStateRefreshed;
                if (ev != null) ev(this, new ValidatorStateRefreshedEventArgs(this));

                if (value == m_IsValid)
                {
                    return;
                }

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
    public static class ValidationDataTemplateProperties
    {
        public const string TypeIdentifier = "ValidationItemDataTemplate";

    }
}