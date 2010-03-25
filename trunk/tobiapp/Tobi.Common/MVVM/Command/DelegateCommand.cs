using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;
using Tobi.Common.UI;

namespace Tobi.Common.MVVM.Command
{
    /// <summary>
    ///     This class allows delegating the commanding logic to methods passed as parameters,
    ///     and enables a View to bind commands to objects that are not part of the element tree.
    /// </summary>
    public class DelegateCommand : ActiveAware, ICommand
    {
        #region Constructors

        /// <summary>
        ///     Constructor
        /// </summary>
        public DelegateCommand(Action executeMethod)
            : this(executeMethod, null, false)
        {
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        public DelegateCommand(Action executeMethod, Func<bool> canExecuteMethod)
            : this(executeMethod, canExecuteMethod, false)
        {
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        public DelegateCommand(Action executeMethod, Func<bool> canExecuteMethod, bool isAutomaticRequeryDisabled)
        {
            _executeMethod = executeMethod;
            _canExecuteMethod = canExecuteMethod;
            _isAutomaticRequeryDisabled = isAutomaticRequeryDisabled;
        }

        #endregion

        protected override void OnIsActiveChanged()
        {
            base.OnIsActiveChanged();
            if (true || IsAutomaticRequeryDisabled) // we enforce the Requery, we don't want to rely on the AutomaticRequery in the case of IActiveAware
            {
                RaiseCanExecuteChanged();
            }
        }

        #region Public Methods

        /// <summary>
        ///     Method to determine if the command can be executed
        /// </summary>
        public virtual bool CanExecute()
        {
            if (!IsActive)
            {
                return false;
            }

            if (_canExecuteMethod != null)
            {
                return _canExecuteMethod();
            }
            return true;
        }

        /// <summary>
        ///     Execution of the command
        /// </summary>
        public virtual void Execute()
        {
            if (CanExecute() && _executeMethod != null)
            {
                try
                {
                    _executeMethod();
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debugger.Break();
#endif //DEBUG
                    ExceptionHandler.Handle(ex, false, null);
                }
            }
        }

        /// <summary>
        ///     Property to enable or disable CommandManager's automatic requery on this command
        /// </summary>
        public bool IsAutomaticRequeryDisabled
        {
            get
            {
                return _isAutomaticRequeryDisabled;
            }
            set
            {
                if (_isAutomaticRequeryDisabled != value)
                {
                    if (value)
                    {
                        CommandManagerHelper.RemoveHandlersFromRequerySuggested(_canExecuteChangedHandlers);
                    }
                    else
                    {
                        CommandManagerHelper.AddHandlersToRequerySuggested(_canExecuteChangedHandlers);
                    }
                    _isAutomaticRequeryDisabled = value;
                }
            }
        }

        /// <summary>
        ///     Raises the CanExecuteChaged event
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            OnCanExecuteChanged();
        }

        /// <summary>
        ///     Protected virtual method to raise CanExecuteChanged event
        /// </summary>
        protected virtual void OnCanExecuteChanged()
        {
            WeakReferencedEventHandlerHelper.CallWeakReferenceHandlers_WithDispatchCheck(_canExecuteChangedHandlers, this, EventArgs.Empty);
        }

        #endregion

        #region ICommand Members

        /// <summary>
        ///     ICommand.CanExecuteChanged implementation
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (!_isAutomaticRequeryDisabled)
                {
                    CommandManager.RequerySuggested += value;
                }

                WeakReferencedEventHandlerHelper.AddWeakReferenceHandler(ref _canExecuteChangedHandlers, value, 2);
            }
            remove
            {
                if (!_isAutomaticRequeryDisabled)
                {
                    CommandManager.RequerySuggested -= value;
                }

                WeakReferencedEventHandlerHelper.RemoveWeakReferenceHandler(_canExecuteChangedHandlers, value);
            }
        }

        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute();
        }

        void ICommand.Execute(object parameter)
        {
            Execute();
        }

        #endregion

        #region Data

        private readonly Action _executeMethod = null;
        private readonly Func<bool> _canExecuteMethod = null;
        private bool _isAutomaticRequeryDisabled = false;
        private List<WeakReference<EventHandler>> _canExecuteChangedHandlers;

        #endregion
    }

    /// <summary>
    ///     This class contains methods for the CommandManager that help avoid memory leaks by
    ///     using weak references.
    /// </summary>
    public static class CommandManagerHelper
    {
        public static void AddHandlersToRequerySuggested(List<WeakReference<EventHandler>> handlers)
        {
            if (handlers != null)
            {
                foreach (WeakReference<EventHandler> handlerRef in handlers)
                {
                    EventHandler handler = handlerRef.Target; // as EventHandler;
                    if (handler != null)
                    {
                        CommandManager.RequerySuggested += handler;
                    }
                }
            }
        }

        public static void RemoveHandlersFromRequerySuggested(List<WeakReference<EventHandler>> handlers)
        {
            if (handlers != null)
            {
                foreach (WeakReference<EventHandler> handlerRef in handlers)
                {
                    EventHandler handler = handlerRef.Target; // as EventHandler;
                    if (handler != null)
                    {
                        CommandManager.RequerySuggested -= handler;
                    }
                }
            }
        }
    }


    ///// <summary>
    /////     This class allows delegating the commanding logic to methods passed as parameters,
    /////     and enables a View to bind commands to objects that are not part of the element tree.
    ///// </summary>
    ///// <typeparam name="T">Type of the parameter passed to the delegates</typeparam>
    //public class DelegateCommand<T> : ActiveAware, ICommand
    //{
    //    #region Constructors
    //    /// <summary>
    //    ///     Constructor
    //    /// </summary>
    //    public DelegateCommand(Action<T> executeMethod)
    //        : this(executeMethod, null, false)
    //    {
    //    }

    //    /// <summary>
    //    ///     Constructor
    //    /// </summary>
    //    public DelegateCommand(Action<T> executeMethod, Predicate<T> canExecuteMethod)
    //        : this(executeMethod, canExecuteMethod, false)
    //    {
    //    }

    //    /// <summary>
    //    ///     Constructor
    //    /// </summary>
    //    public DelegateCommand(Action<T> executeMethod, Predicate<T> canExecuteMethod, bool isAutomaticRequeryDisabled)
    //    {
    //        _executeMethod = executeMethod;
    //        _canExecuteMethod = canExecuteMethod;
    //        _isAutomaticRequeryDisabled = isAutomaticRequeryDisabled;
    //    }

    //    #endregion

    //    protected override void OnIsActiveChanged()
    //    {
    //        base.OnIsActiveChanged();
    //        if (true || IsAutomaticRequeryDisabled) // we enforce the Requery, we don't want to rely on the AutomaticRequery in the case of IActiveAware
    //        {
    //            RaiseCanExecuteChanged();
    //        }
    //    }

    //    #region Public Methods

    //    /// <summary>
    //    ///     Method to determine if the command can be executed
    //    /// </summary>
    //    public bool CanExecute(T parameter)
    //    {
    //        if (!IsActive)
    //        {
    //            return false;
    //        }

    //        if (_canExecuteMethod != null)
    //        {
    //            return _canExecuteMethod(parameter);
    //        }
    //        return true;
    //    }

    //    /// <summary>
    //    ///     Execution of the command
    //    /// </summary>
    //    public void Execute(T parameter)
    //    {
    //        if (CanExecute(parameter) && _executeMethod != null)
    //        {
    //            _executeMethod(parameter);
    //        }
    //    }

    //    /// <summary>
    //    ///     Raises the CanExecuteChaged event
    //    /// </summary>
    //    public void RaiseCanExecuteChanged()
    //    {
    //        OnCanExecuteChanged();
    //    }

    //    /// <summary>
    //    ///     Protected virtual method to raise CanExecuteChanged event
    //    /// </summary>
    //    protected virtual void OnCanExecuteChanged()
    //    {
    //        WeakReferencedEventHandlerHelper.CallWeakReferenceHandlers_WithDispatchCheck(_canExecuteChangedHandlers);
    //    }

    //    /// <summary>
    //    ///     Property to enable or disable CommandManager's automatic requery on this command
    //    /// </summary>
    //    public bool IsAutomaticRequeryDisabled
    //    {
    //        get
    //        {
    //            return _isAutomaticRequeryDisabled;
    //        }
    //        set
    //        {
    //            if (_isAutomaticRequeryDisabled != value)
    //            {
    //                if (value)
    //                {
    //                    CommandManagerHelper.RemoveHandlersFromRequerySuggested(_canExecuteChangedHandlers);
    //                }
    //                else
    //                {
    //                    CommandManagerHelper.AddHandlersToRequerySuggested(_canExecuteChangedHandlers);
    //                }
    //                _isAutomaticRequeryDisabled = value;
    //            }
    //        }
    //    }

    //    #endregion

    //    #region ICommand Members

    //    /// <summary>
    //    ///     ICommand.CanExecuteChanged implementation
    //    /// </summary>
    //    public event EventHandler CanExecuteChanged
    //    {
    //        add
    //        {
    //            if (!_isAutomaticRequeryDisabled)
    //            {
    //                CommandManager.RequerySuggested += value;
    //            }
    //            WeakReferencedEventHandlerHelper.AddWeakReferenceHandler(ref _canExecuteChangedHandlers, value, 2);
    //        }
    //        remove
    //        {
    //            if (!_isAutomaticRequeryDisabled)
    //            {
    //                CommandManager.RequerySuggested -= value;
    //            }
    //            WeakReferencedEventHandlerHelper.RemoveWeakReferenceHandler(_canExecuteChangedHandlers, value);
    //        }
    //    }

    //    bool ICommand.CanExecute(object parameter)
    //    {
    //        // if T is of value type and the parameter is not
    //        // set yet, then return false if CanExecute delegate
    //        // exists, else return true
    //        if (parameter == null &&
    //            typeof(T).IsValueType)
    //        {
    //            return (_canExecuteMethod == null);
    //        }
    //        return CanExecute((T)parameter);
    //    }

    //    void ICommand.Execute(object parameter)
    //    {
    //        Execute((T)parameter);
    //    }

    //    #endregion

    //    #region Data

    //    private readonly Action<T> _executeMethod = null;
    //    private readonly Predicate<T> _canExecuteMethod = null;
    //    private bool _isAutomaticRequeryDisabled = false;
    //    private List<WeakReference> _canExecuteChangedHandlers;

    //    #endregion
    //}

}