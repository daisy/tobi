using System.Windows;
using System.Windows.Input;

// See http://blogs.microsoft.co.il/blogs/tomershamam/archive/2009/04/14/wpf-commands-everywhere.aspx

namespace Tobi.Common._UnusedCode
{
    public abstract class CommandTrigger : Freezable, ICommandTrigger
    {
        public bool IsInitialized { get; set; }

        #region Dependency Properties

        #region Command Property

        /// <value>Identifies the Command dependency property</value>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(CommandTrigger),
                                        new FrameworkPropertyMetadata(null));

        /// <value>description for Command property</value>
        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        #endregion

        #region CustomParameter Property

        /// <value>Identifies the CustomParameterProperty dependency property</value>
        public static readonly DependencyProperty CustomParameterProperty =
            DependencyProperty.Register("CustomParameter", typeof(object), typeof(CommandTrigger),
                                        new FrameworkPropertyMetadata(null));

        /// <value>description for CustomParameter property</value>
        public object CustomParameter
        {
            get { return (object)GetValue(CustomParameterProperty); }
            set { SetValue(CustomParameterProperty, value); }
        }		

        #endregion

        #region CommandTarget Property

        /// <value>Identifies the CommandTarget dependency property</value>
        public static readonly DependencyProperty CommandTargetProperty =
            DependencyProperty.Register("CommandTarget", typeof(IInputElement), typeof(CommandTrigger),
                                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        /// <value>description for CommandTarget property</value>
        public IInputElement CommandTarget
        {
            get { return (IInputElement)GetValue(CommandTargetProperty); }
            set { SetValue(CommandTargetProperty, value); }
        }

        #endregion
		
        #endregion

        #region Internals

        void ICommandTrigger.Initialize(FrameworkElement source)
        {
            if (IsInitialized)
                return;
			
            InitializeCore(source);
            IsInitialized = true;
        }

        protected abstract void InitializeCore(FrameworkElement source);

        protected void ExecuteCommand(CommandParameter<object> parameter)
        {
            if (Command == null)
                return;

            RoutedCommand routedCommand = Command as RoutedCommand;
            if (routedCommand != null)
            {
                routedCommand.Execute(parameter, CommandTarget);
            }
            else
            {
                Command.Execute(parameter);
            }
        }

        #endregion
    }
}