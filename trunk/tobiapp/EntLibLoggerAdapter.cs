
using System;
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
        /// <summary>
        /// 
        /// </summary>
        public EntLibLoggerAdapter()
        {
            /*
              
            Console.SetWindowSize(Console.LargestWindowWidth, Console.LargestWindowHeight);
             
             */
            Console.SetWindowPosition(0, 0);
            Console.Title = "Tobi Log Window";
        }

        /// <summary>
        /// sdlkfhjdslkgjdfs
        /// </summary>
        /// <returns>the name of the given person firstname, is not null, but can be empty ""</returns>
        public string GetMyName(string firstName)
        {
            if (firstName == null) throw new ArgumentNullException("firstName");

            return "";
        }

        #region ILoggerFacade Members

        //<summary>
        // Simply delegates to the Logger from Microsoft.Practices.EnterpriseLibrary.Logging
        //</summary>
        public void Log(string message, Category category, Priority priority)
        {
            Logger.Write(message, category.ToString(), (int)priority);

            
            switch (category)
            {
                case Category.Info:
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("Info ");
                        consoleWritePriority(priority);
                        Console.WriteLine(" [" + message + "]");
                    }
                    break;
                case Category.Warn:
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write("Warn ");
                        consoleWritePriority(priority);
                        Console.WriteLine(" [" + message + "]");
                    }
                    break;

                case Category.Exception:
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("Exception ");
                        consoleWritePriority(priority);
                        Console.WriteLine(" [" + message + "]");
                    }
                    break;

                case Category.Debug:
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write("Debug ");
                        consoleWritePriority(priority);
                        Console.WriteLine(" [" + message + "]");
                    }
                    break;
            }
        }

        #endregion

        private void consoleWritePriority(Priority priority)
        {
            switch (priority)
            {
                case Priority.High:
                    {
                        Console.Write("(High)");
                    }
                    break;
                case Priority.Medium:
                    {
                        Console.Write("(Medium)");
                    }
                    break;

                case Priority.Low:
                    {
                        Console.Write("(Low)");
                    }
                    break;

                case Priority.None:
                    {
                        //Console.Write("(None)");
                    }
                    break;
            }
        }
    }
}
