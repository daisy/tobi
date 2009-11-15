using Microsoft.Practices.Composite.Presentation.Events;

namespace Tobi.Common.Validation
{
    public class ValidationEvents
    {
        public class ValidationItemSelectedEvent : CompositePresentationEvent<ValidationItem>
        {
        }

        public class ValidationItemsEvent : CompositePresentationEvent<IValidator>
        {
        }
    }
}
