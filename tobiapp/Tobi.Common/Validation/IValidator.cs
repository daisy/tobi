using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Composite.Presentation.Events;
using Microsoft.Practices.Composite.UnityExtensions;
using Microsoft.Practices.Unity;
using Microsoft.Win32;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.Onyx.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace Tobi.Common
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
    }
}
