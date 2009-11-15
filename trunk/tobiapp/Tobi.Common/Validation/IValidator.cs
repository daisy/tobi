using System.Collections.Generic;

namespace Tobi.Common.Validation
{
    public interface IValidator
    {
        string Name { get; }
        string Description { get; }
        bool Validate();
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
        public object Target { get; set; }
    }
}