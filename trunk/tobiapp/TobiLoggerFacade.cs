using System;
using System.ComponentModel.Composition.Diagnostics;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.UI;

#if (BITFACTORY) // We're not using BitFactory anymore ( http://dotnetlog.theobjectguy.com/ )
using BitFactory.Logging;
#endif

namespace Tobi
{
    public class TobiLoggerFacade : ILoggerFacade
    {
#if (BITFACTORY)
        private CompositeLogger m_Logger;
#else
        private TextWriter m_FileWriter;
#endif

        public TobiLoggerFacade()
        {
#if (DEBUG)
            //PresentationTraceSources.SetTraceLevel(obj, PresentationTraceLevel.High)
#endif
            PresentationTraceSources.ResourceDictionarySource.Listeners.Add(new LoggerFacadeTraceListener(this));
            PresentationTraceSources.ResourceDictionarySource.Switch.Level = SourceLevels.All;

            PresentationTraceSources.DataBindingSource.Listeners.Add(new LoggerFacadeTraceListener(this));
#if (DEBUG)
            PresentationTraceSources.DataBindingSource.Listeners.Add(new BindingErrorAdornerTraceListener());
#endif
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;

            PresentationTraceSources.DependencyPropertySource.Listeners.Add(new LoggerFacadeTraceListener(this));
            PresentationTraceSources.DependencyPropertySource.Switch.Level = SourceLevels.All;

            PresentationTraceSources.DocumentsSource.Listeners.Add(new LoggerFacadeTraceListener(this));
            PresentationTraceSources.DocumentsSource.Switch.Level = SourceLevels.All;

            PresentationTraceSources.MarkupSource.Listeners.Add(new LoggerFacadeTraceListener(this));
            PresentationTraceSources.MarkupSource.Switch.Level = SourceLevels.All;

            PresentationTraceSources.NameScopeSource.Listeners.Add(new LoggerFacadeTraceListener(this));
            PresentationTraceSources.NameScopeSource.Switch.Level = SourceLevels.All;

            //StreamWriter standardOutput = new StreamWriter(Console.OpenStandardOutput());
            //standardOutput.AutoFlush = true;
            //Console.SetOut(standardOutput);

            //StreamWriter standardErr = new StreamWriter(Console.OpenStandardError());
            //standardErr.AutoFlush = true;
            //Console.SetError(standardErr);

            if (File.Exists(UserInterfaceStrings.LOG_FILE_PATH))
            {
                // Remark: the following logging messages go to the System.Out, and that's it (not to any file target). We initialize file redirect later on (see below).

                Console.WriteLine("Deleting log file [" + UserInterfaceStrings.LOG_FILE_PATH + "]...");
                File.Delete(UserInterfaceStrings.LOG_FILE_PATH);
                Console.WriteLine("File deleted [" + UserInterfaceStrings.LOG_FILE_PATH + "].");

                Thread.Sleep(200);
            }

#if (BITFACTORY)
            
            Debug.Listeners.Add(new BitFactoryLoggerTraceListener(this));
            Trace.Listeners.Add(new BitFactoryLoggerTraceListener(this));

            m_Logger = new CompositeLogger();

            Logger consoleLogger = TextWriterLogger.NewConsoleLogger();
            m_Logger.AddLogger("console", consoleLogger);

            consoleLogger.Formatter = new BitFactoryLoggerLogEntryFormatter();
#endif


#if (BITFACTORY)
            Logger fileLogger = new FileLogger(UserInterfaceStrings.LOG_FILE_PATH);
            m_Logger.AddLogger("file", fileLogger);

            fileLogger.Formatter = new BitFactoryLoggerLogEntryFormatter();
#else

            // Remark: we could set DiagnosticsConfiguration.LogFileName
            // and benefit from DefaultTraceListener's built-in support for output to log file,
            // but unfortunately the WriteToLogFile() method opens and closes the file
            // for each Write operation, which obviously is a massive performance bottleneck.

            //m_FileWriter = File.CreateText(UserInterfaceStrings.LOG_FILE_PATH);
            FileStream fileStream = new FileStream(UserInterfaceStrings.LOG_FILE_PATH, FileMode.Create, FileAccess.Write, FileShare.Read);

#if (false && DEBUG) // We want clickable code line numbers in the debugger output window, but we don't want to spam the log file with this info.
            m_FileWriter = new CodeLocationTextWriter(new StreamWriter(fileStream));

#else
            m_FileWriter = new StreamWriter(fileStream);
#endif

            var listener = new TextWriterTraceListener(m_FileWriter)
                               {
                                   //                TraceOutputOptions = TraceOptions.DateTime
                                   //                                     | TraceOptions.LogicalOperationStack
                                   //                                     | TraceOptions.Timestamp
                                   //#if (DEBUG)
                                   // | TraceOptions.Callstack
                                   //#endif
                               };

            // Works for DEBUG too, no need for a second set of listeners
            Trace.Listeners.Add(listener);

            //TODO: this is a massive hack as we needed to change "internal" to "public" in the MEF source code !! (how else to do this though ?)
            TraceSourceTraceWriter.Source.Listeners.Add(listener);

#if (DEBUG)
            var systemOut = new CodeLocationTextWriter(Console.Out);

#else
            var systemOut = Console.Out;
#endif
            //Trace.Listeners.Add(new TextWriterTraceListener(systemOut));


#if (DEBUG)
            var systemErr = new CodeLocationTextWriter(Console.Error);

#else
            var systemErr = Console.Error;
#endif
            //Trace.Listeners.Add(new TextWriterTraceListener(systemErr));


            var compositeOut = new CompositeTextWriter(new[] { m_FileWriter, systemOut });
            Console.SetOut(compositeOut);

            var compositeErr = new CompositeTextWriter(new[] { m_FileWriter, systemErr });
            Console.SetError(compositeErr);
#endif
        }

        ~TobiLoggerFacade()
        {
            string msg = string.Format("Finalized: ({0})", GetType().Name);
            Console.WriteLine(msg);

            Trace.Flush();
            Debug.Flush();

            m_FileWriter.Flush();
            m_FileWriter.Close();
        }

        #region ILoggerFacade Members

        public void Log(string msg, Category category, Priority priority)
        {
            string message = msg;

#if (DEBUG)

            //StackTrace stackTrace = new StackTrace(2, true);
            //string str = CodeLocation.StackTraceToString(stackTrace, 0, stackTrace.FrameCount - 1);
            //message = message.Insert(0, str + " -- ");

            //CodeLocation codeLocation = CodeLocation.GetCallerLocation(2);
            //if (codeLocation != null)
            //{
            //    message = message.Insert(0, codeLocation.ToString() + " --- ");
            //}

            Console.WriteLine("");
#endif

            switch (category)
            {
                case Category.Info:
                    {
#if (BITFACTORY)
                        m_Logger.Log(LogSeverity.Info, message);
#endif

#if (false && DEBUG)
                        Console.ForegroundColor = ConsoleColor.Green;
#endif
                        Console.Write("[Info ");
                    }
                    break;
                case Category.Warn:
                    {
#if (BITFACTORY)
                        m_Logger.Log(LogSeverity.Warning, message);
#endif

#if (false && DEBUG)
                        Console.ForegroundColor = ConsoleColor.Blue;
#endif
                        Console.Write("[Warning ");
                    }
                    break;

                case Category.Exception:
                    {
#if (BITFACTORY)
                        m_Logger.Log(LogSeverity.Error, message);
#endif

#if (false && DEBUG)
                        Console.ForegroundColor = ConsoleColor.Red;
#endif
                        Console.Write("[Exception ");
                    }
                    break;

                case Category.Debug:
                    {
#if (BITFACTORY)
                        m_Logger.Log(LogSeverity.Debug, message);
#endif

#if (false && DEBUG)
                        Console.ForegroundColor = ConsoleColor.White;
#endif
                        Console.Write("[Debug ");
                    }
                    break;
            }
            consoleWritePriority(priority);
            Console.Write("] " + message);

            Console.Write(Console.Out.NewLine);

            //Console.Out.Flush();
            //Console.Error.Flush();
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
                        Console.Write("(No Priority)");
                    }
                    break;
            }
        }
    }

    public class LoggerFacadeTraceListener : TraceListener
    {
        protected ILoggerFacade Logger
        {
            get;
            private set;
        }

        public LoggerFacadeTraceListener(ILoggerFacade logger)
        {
            Logger = logger;
        }

        public override void Write(string message)
        {
            Logger.Log(message, Category.Debug, Priority.Medium);
        }

        public override void WriteLine(string message)
        {
            Write(message);
        }
    }


#if (BITFACTORY)
    public class BitFactoryLoggerLogEntryFormatter : LogEntryFormatter
    {
        protected override string AsString(LogEntry aLogEntry)
        {
            String appString = "";
            if (aLogEntry.Application != null)
            {
                appString = "[" + aLogEntry.Application + "] -- ";
            }
            if (aLogEntry.Category != null)
            {
                appString = appString + "{" + aLogEntry.Category + "} --";
            }
            return aLogEntry.Message + " -- " + appString + "<" + aLogEntry.SeverityString + "> -- " + DateString(aLogEntry);
        }
    }
#endif

    /// <summary>
    /// Code much inspired (some parts copied) from Daniel Vaughan's Clog logging framework
    /// </summary>
    public class CodeLocation
    {
        /// <summary>
        /// Gets the caller location from the <see cref="StackTrace"/>.
        /// </summary>
        /// <returns>The code location that the call to log originated.</returns>
        public static CodeLocation GetCallerLocation(int methodCallCount)
        {
            StackTrace trace;
            string className;
            string methodName;
            string fileName;
            int lineNumber;
            StackFrame frame = null;

            try
            {
                trace = new StackTrace(methodCallCount, true);

                frame = trace.GetFrame(0);
            }
            catch (MethodAccessException ex)
            {
                Debug.Fail("Unable to get stack trace." + ex);
            }

            if (frame != null)
            {
                className = frame.GetMethod().ReflectedType.FullName;
                methodName = frame.GetMethod().Name;
                fileName = frame.GetFileName();
                lineNumber = frame.GetFileLineNumber();
            }
            else
            {
                className = string.Empty;
                methodName = string.Empty;
                fileName = string.Empty;
                lineNumber = -1;
            }

            return new CodeLocation()
            {
                ClassName = className,
                MethodName = methodName,
                FileName = fileName,
                LineNumber = lineNumber
            };
        }

        public static string StackTraceToString(StackTrace trace, int startFrameIndex, int endFrameIndex)
        {
            StringBuilder sb = new StringBuilder(512);

            for (int i = startFrameIndex; i <= endFrameIndex; i++)
            {
                StackFrame frame = trace.GetFrame(i);
                MethodBase method = frame.GetMethod();
                sb.Append("\r\n    at ");
                if (method.ReflectedType != null)
                {
                    sb.Append(method.ReflectedType.Name);
                }
                else
                {
                    // This is for global methods and this is what shows up in windbg. 
                    sb.Append("<Module>");
                }
                sb.Append(".");
                sb.Append(method.Name);
                sb.Append("(");
                ParameterInfo[] parameters = method.GetParameters();
                for (int j = 0; j < parameters.Length; j++)
                {
                    ParameterInfo parameter = parameters[j];
                    if (j > 0)
                        sb.Append(", ");
                    sb.Append(parameter.ParameterType.Name);
                    sb.Append(" ");
                    sb.Append(parameter.Name);
                }
                sb.Append(")  ");
                sb.Append(frame.GetFileName());
                int line = frame.GetFileLineNumber();
                if (line > 0)
                {
                    sb.Append("(");
                    sb.Append(line.ToString(CultureInfo.InvariantCulture));
                    sb.Append(")");
                }
            }
            sb.Append("\r\n");

            return sb.ToString();
        }


        /// <summary>
        /// Gets or sets the name of the class.
        /// </summary>
        /// <value>The name of the class.</value>
        public string ClassName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the method.
        /// </summary>
        /// <value>The name of the method.</value>
        public string MethodName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        /// <value>The name of the file.</value>
        public string FileName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the line number of the location.
        /// </summary>
        /// <value>The line number.</value>
        public int LineNumber
        {
            get;
            set;
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents 
        /// the current <see cref="CodeLocation"/>.
        /// The format is &lt;FileName&gt;(&lt;LineNumber&gt;):&lt;ClassName&gt;.&lt;MethodName&gt;
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="CodeLocation"/>.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0}({1}): {2}.{3}",
                FileName, LineNumber, ClassName, MethodName);
        }
    }

    public class CompositeTextWriter : TextWriter
    {
        private TextWriter[] m_TextWriters;

        public CompositeTextWriter(TextWriter[] textWriters)
            : base(textWriters[0].FormatProvider)
        {
            m_TextWriters = textWriters;
        }

        public override Encoding Encoding
        {
            get { return m_TextWriters[0].Encoding; }
        }

        public override IFormatProvider FormatProvider
        {
            get { return m_TextWriters[0].FormatProvider; }
        }

        public override String NewLine
        {
            get { return m_TextWriters[0].NewLine; }
            set { foreach (var textWriter in m_TextWriters) textWriter.NewLine = value; }
        }

        public override void Close()
        {
            // So that any overriden Close() gets run 
            foreach (var textWriter in m_TextWriters) textWriter.Close();
        }

        protected override void Dispose(bool disposing)
        {
            // Explicitly pick up a potentially methodimpl'ed Dispose
            if (disposing)
            {
                foreach (var textWriter in m_TextWriters)
                    ((IDisposable)textWriter).Dispose();
            }
        }

        public override void Flush()
        {
            foreach (var textWriter in m_TextWriters) textWriter.Flush();
        }

        public override void Write(char value)
        {
            foreach (var textWriter in m_TextWriters) textWriter.Write(value);
        }

        public override void Write(char[] buffer)
        {
            foreach (var textWriter in m_TextWriters) textWriter.Write(buffer);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            foreach (var textWriter in m_TextWriters) textWriter.Write(buffer, index, count);
        }

        public override void Write(bool value)
        {
            foreach (var textWriter in m_TextWriters) textWriter.Write(value);
        }

        public override void Write(int value)
        {
            foreach (var textWriter in m_TextWriters) textWriter.Write(value);
        }

        public override void Write(uint value)
        {
            foreach (var textWriter in m_TextWriters) textWriter.Write(value);
        }

        public override void Write(long value)
        {
            foreach (var textWriter in m_TextWriters) textWriter.Write(value);
        }

        public override void Write(ulong value)
        {
            foreach (var textWriter in m_TextWriters) textWriter.Write(value);
        }

        public override void Write(float value)
        {
            foreach (var textWriter in m_TextWriters) textWriter.Write(value);
        }

        public override void Write(double value)
        {
            foreach (var textWriter in m_TextWriters) textWriter.Write(value);
        }

        public override void Write(Decimal value)
        {
            foreach (var textWriter in m_TextWriters) textWriter.Write(value);
        }

        public override void Write(String value)
        {
            foreach (var textWriter in m_TextWriters) textWriter.Write(value);
        }

        public override void Write(Object value)
        {
            foreach (var textWriter in m_TextWriters) textWriter.Write(value);
        }

        public override void Write(String format, Object arg0)
        {
            foreach (var textWriter in m_TextWriters) textWriter.Write(format, arg0);
        }

        public override void Write(String format, Object arg0, Object arg1)
        {
            foreach (var textWriter in m_TextWriters) textWriter.Write(format, arg0, arg1);
        }

        public override void Write(String format, Object arg0, Object arg1, Object arg2)
        {
            foreach (var textWriter in m_TextWriters) textWriter.Write(format, arg0, arg1, arg2);
        }

        public override void Write(String format, Object[] arg)
        {
            foreach (var textWriter in m_TextWriters) textWriter.Write(format, arg);
        }

        public override void WriteLine()
        {
            foreach (var textWriter in m_TextWriters) textWriter.WriteLine();
        }

        public override void WriteLine(char value)
        {
            foreach (var textWriter in m_TextWriters) textWriter.WriteLine(value);
        }

        public override void WriteLine(decimal value)
        {
            foreach (var textWriter in m_TextWriters) textWriter.WriteLine(value);
        }

        public override void WriteLine(char[] buffer)
        {
            foreach (var textWriter in m_TextWriters) textWriter.WriteLine(buffer);
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            foreach (var textWriter in m_TextWriters) textWriter.WriteLine(buffer, index, count);
        }

        public override void WriteLine(bool value)
        {
            foreach (var textWriter in m_TextWriters) textWriter.WriteLine(value);
        }

        public override void WriteLine(int value)
        {
            foreach (var textWriter in m_TextWriters) textWriter.WriteLine(value);
        }

        public override void WriteLine(uint value)
        {
            foreach (var textWriter in m_TextWriters) textWriter.WriteLine(value);
        }

        public override void WriteLine(long value)
        {
            foreach (var textWriter in m_TextWriters) textWriter.WriteLine(value);
        }

        public override void WriteLine(ulong value)
        {
            foreach (var textWriter in m_TextWriters) textWriter.WriteLine(value);
        }

        public override void WriteLine(float value)
        {
            foreach (var textWriter in m_TextWriters) textWriter.WriteLine(value);
        }

        public override void WriteLine(double value)
        {
            foreach (var textWriter in m_TextWriters) textWriter.WriteLine(value);
        }

        public override void WriteLine(String value)
        {
            foreach (var textWriter in m_TextWriters) textWriter.WriteLine(value);
        }

        public override void WriteLine(Object value)
        {
            foreach (var textWriter in m_TextWriters) textWriter.WriteLine(value);
        }

        public override void WriteLine(String format, Object arg0)
        {
            foreach (var textWriter in m_TextWriters) textWriter.WriteLine(format, arg0);
        }

        public override void WriteLine(String format, Object arg0, Object arg1)
        {
            foreach (var textWriter in m_TextWriters) textWriter.WriteLine(format, arg0, arg1);
        }

        public override void WriteLine(String format, Object arg0, Object arg1, Object arg2)
        {
            foreach (var textWriter in m_TextWriters) textWriter.WriteLine(format, arg0, arg1, arg2);
        }

        public override void WriteLine(String format, Object[] arg)
        {
            foreach (var textWriter in m_TextWriters) textWriter.WriteLine(format, arg);
        }
    }

    public class CodeLocationTextWriter : TextWriter
    {
        private TextWriter m_TextWriter;

        public CodeLocationTextWriter(TextWriter textWriter)
            : base(textWriter.FormatProvider)
        {
            m_TextWriter = textWriter;
        }

        public override Encoding Encoding
        {
            get { return m_TextWriter.Encoding; }
        }

        public override IFormatProvider FormatProvider
        {
            get { return m_TextWriter.FormatProvider; }
        }

        public override String NewLine
        {
            get { return m_TextWriter.NewLine; }
            set { m_TextWriter.NewLine = value; }
        }

        public override void Close()
        {
            // So that any overriden Close() gets run 
            m_TextWriter.Close();
        }

        protected override void Dispose(bool disposing)
        {
            // Explicitly pick up a potentially methodimpl'ed Dispose
            if (disposing)
            {
                    ((IDisposable)m_TextWriter).Dispose();
            }
        }

        public override void Flush()
        {
            m_TextWriter.Flush();
        }

        public override void Write(char value)
        {
            m_TextWriter.Write(value);
        }

        public override void Write(char[] buffer)
        {
            m_TextWriter.Write(buffer);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            m_TextWriter.Write(buffer, index, count);
        }

        public override void Write(bool value)
        {
            m_TextWriter.Write(value);
        }

        public override void Write(int value)
        {
            m_TextWriter.Write(value);
        }

        public override void Write(uint value)
        {
            m_TextWriter.Write(value);
        }

        public override void Write(long value)
        {
            m_TextWriter.Write(value);
        }

        public override void Write(ulong value)
        {
            m_TextWriter.Write(value);
        }

        public override void Write(float value)
        {
            m_TextWriter.Write(value);
        }

        public override void Write(double value)
        {
            m_TextWriter.Write(value);
        }

        public override void Write(Decimal value)
        {
            m_TextWriter.Write(value);
        }

        public override void Write(String value)
        {
            m_TextWriter.Write(value);
        }

        public override void Write(Object value)
        {
            m_TextWriter.Write(value);
        }

        public override void Write(String format, Object arg0)
        {
            m_TextWriter.Write(format, arg0);
        }

        public override void Write(String format, Object arg0, Object arg1)
        {
            m_TextWriter.Write(format, arg0, arg1);
        }

        public override void Write(String format, Object arg0, Object arg1, Object arg2)
        {
            m_TextWriter.Write(format, arg0, arg1, arg2);
        }

        public override void Write(String format, Object[] arg)
        {
            m_TextWriter.Write(format, arg);
        }

        public override void WriteLine()
        {
            m_TextWriter.WriteLine();
        }

        public override void WriteLine(char value)
        {
            m_TextWriter.WriteLine(value);
        }

        public override void WriteLine(decimal value)
        {
            m_TextWriter.WriteLine(value);
        }

        public override void WriteLine(char[] buffer)
        {
            m_TextWriter.WriteLine(buffer);
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            m_TextWriter.WriteLine(buffer, index, count);
        }

        public override void WriteLine(bool value)
        {
            m_TextWriter.WriteLine(value);
        }

        public override void WriteLine(int value)
        {
            m_TextWriter.WriteLine(value);
        }

        public override void WriteLine(uint value)
        {
            m_TextWriter.WriteLine(value);
        }

        public override void WriteLine(long value)
        {
            m_TextWriter.WriteLine(value);
        }

        public override void WriteLine(ulong value)
        {
            m_TextWriter.WriteLine(value);
        }

        public override void WriteLine(float value)
        {
            m_TextWriter.WriteLine(value);
        }

        public override void WriteLine(double value)
        {
            m_TextWriter.WriteLine(value);
        }

        public override void WriteLine(Object value)
        {
            m_TextWriter.WriteLine(value);
        }

        public override void WriteLine(String format, Object arg0)
        {
            m_TextWriter.WriteLine(format, arg0);
        }

        public override void WriteLine(String format, Object arg0, Object arg1)
        {
            m_TextWriter.WriteLine(format, arg0, arg1);
        }

        public override void WriteLine(String format, Object arg0, Object arg1, Object arg2)
        {
            m_TextWriter.WriteLine(format, arg0, arg1, arg2);
        }

        public override void WriteLine(String format, Object[] arg)
        {
            m_TextWriter.WriteLine(format, arg);
        }

        public override void WriteLine(String value)
        {
            if (value == null) return;

            //TODO: very hacky detection of stack level depth !! :(
            CodeLocation codeLocation = CodeLocation.GetCallerLocation(5 + (value == "" ? 1 : 0));
            if (codeLocation != null)
            {
                m_TextWriter.WriteLine(value.Insert(0, codeLocation.ToString() + " --- "));
            }
            else
            {
                m_TextWriter.WriteLine(value);
            }
        }
    }
}
