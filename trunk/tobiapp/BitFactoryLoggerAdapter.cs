using System;
using System.IO;
using BitFactory.Logging;
using Microsoft.Practices.Composite.Logging;

namespace Tobi
{
    public class BitFactoryLoggerAdapter : ILoggerFacade
    {
        private CompositeLogger m_Logger;

        public BitFactoryLoggerAdapter()
        {
            m_Logger = new CompositeLogger();

            Logger consoleLogger = TextWriterLogger.NewConsoleLogger();

            string logPath = Directory.GetCurrentDirectory() + @"\Tobi.log";

            Logger fileLogger = new FileLogger(logPath);

            m_Logger.AddLogger("console", consoleLogger);
            m_Logger.AddLogger("file", fileLogger);
        }

        #region ILoggerFacade Members

        public void Log(string message, Category category, Priority priority)
        {
            switch (category)
            {
                case Category.Info:
                    {
                        m_Logger.Log(LogSeverity.Info, message);
#if (DEBUG)
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("Info ");
                        consoleWritePriority(priority);
                        Console.WriteLine(" [" + message + "]");
#endif
                    }
                    break;
                case Category.Warn:
                    {
                        m_Logger.Log(LogSeverity.Warning, message);
#if (DEBUG)
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write("Warn ");
                        consoleWritePriority(priority);
                        Console.WriteLine(" [" + message + "]");
#endif
                    }
                    break;

                case Category.Exception:
                    {
                        m_Logger.Log(LogSeverity.Error, message);
#if (DEBUG)
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("Exception ");
                        consoleWritePriority(priority);
                        Console.WriteLine(" [" + message + "]");
#endif
                    }
                    break;

                case Category.Debug:
                    {
                        m_Logger.Log(LogSeverity.Debug, message);
#if (DEBUG)
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write("Debug ");
                        consoleWritePriority(priority);
                        Console.WriteLine(" [" + message + "]");
#endif
                    }
                    break;
            }
        }

        #endregion

#if (DEBUG)

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
#endif
    }
}
