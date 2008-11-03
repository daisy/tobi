
using System.Windows;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging;

namespace Tobi
{
    ///<summary>
    /// Our own logger, based on the Logger from Microsoft.Practices.EnterpriseLibrary.Logging
    ///</summary>
    public class EntLibLoggerAdapter : ILoggerFacade
    {
        #region ILoggerFacade Members

        ///<summary>
        /// Simply delegates to the Logger from Microsoft.Practices.EnterpriseLibrary.Logging
        ///</summary>
        ///<param name="message"></param>
        ///<param name="category"></param>
        ///<param name="priority"></param>
        public void Log(string message, Category category, Priority priority)
        {
            Logger.Write(message, category.ToString(), (int)priority);

            switch (category)
            {
                case Category.Info:
                    {
                        ;
                    }
                    break;
                case Category.Warn:
                    {
                        ;
                    }
                    break;

                case Category.Exception:
                    {
                        ;
                    }
                    break;

                case Category.Debug:
                    {
                        ;
                    }
                    break;
            }

            switch (priority)
            {
                case Priority.High:
                    {
                        ;
                    }
                    break;
                case Priority.Medium:
                    {
                        ;
                    }
                    break;

                case Priority.Low:
                    {
                        ;
                    }
                    break;

                case Priority.None:
                    {
                        ;
                    }
                    break;
            }
        }

        #endregion
    }
}
