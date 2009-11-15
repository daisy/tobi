using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tobi.Common.Validation;

namespace Tobi.Modules.Validator
{
    public class Validator
    {
        public bool Validate()
        {
            bool isValid = true;
            ValidationItems.Clear();
            foreach (IValidator v in Validators)
            {
                isValid = isValid && v.Validate();
                if (!isValid)
                {
                    foreach (ValidationItem item in v.ValidationItems)
                    {
                        ValidationItems.Add(item);
                    }
                }
            }
            return isValid;
        }
    
        public void OnProjectLoaded()
        {
            Validate();
        }

        public void OnUndoRedo()
        {
            Validate();
        }
    }
}
