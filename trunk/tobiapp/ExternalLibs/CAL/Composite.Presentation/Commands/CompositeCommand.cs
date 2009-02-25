//===================================================================================
// Microsoft patterns & practices
// Composite Application Guidance for Windows Presentation Foundation and Silverlight
//===================================================================================
// Copyright (c) Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===================================================================================
// The example companies, organizations, products, domain names,
// e-mail addresses, logos, people, places, and events depicted
// herein are fictitious.  No association with any real company,
// organization, product, domain name, email address, logo, person,
// places, or events is intended or should be inferred.
//===================================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

using Microsoft.Practices.Composite.Presentation.Properties;

namespace Microsoft.Practices.Composite.Presentation.Commands
{
    /// <summary>
    /// The CompositeCommand composites one or more ICommands.
    /// </summary>
    public partial class CompositeCommand : ICommand
    {
        private readonly List<ICommand> registeredCommands = new List<ICommand>();
        private readonly bool monitorCommandActivity;

        /// <summary>
        /// Initializes a new instance of <see cref="CompositeCommand"/>.
        /// </summary>
        public CompositeCommand()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CompositeCommand"/>.
        /// </summary>
        /// <param name="monitorCommandActivity">Indicates when the command activity is going to be monitored.</param>
        public CompositeCommand(bool monitorCommandActivity)
            : this()
        {
            this.monitorCommandActivity = monitorCommandActivity;
        }

        /// <summary>
        /// Adds a command to the collection and signs up for the <see cref="ICommand.CanExecuteChanged"/> event of it.
        /// </summary>
        ///  <remarks>
        /// If this command is set to monitor command activity, and <paramref name="command"/> 
        /// implements the <see cref="IActiveAware"/> interface, this method will subscribe to its
        /// <see cref="IActiveAware.IsActiveChanged"/> event.
        /// </remarks>
        /// <param name="command">The command to register.</param>
        public virtual void RegisterCommand(ICommand command)
        {
            if (command == this)
            {
                throw new ArgumentException(Resources.CannotRegisterCompositeCommandInItself);
            }

            lock (this.registeredCommands)
            {
                if (this.registeredCommands.Contains(command))
                {
                    throw new InvalidOperationException(Resources.CannotRegisterSameCommandTwice);
                }
                this.registeredCommands.Add(command);
            }

            command.CanExecuteChanged += this.RegisteredCommand_CanExecuteChanged;
            this.OnCanExecuteChanged();

            if (this.monitorCommandActivity)
            {
                var activeAwareCommand = command as IActiveAware;
                if (activeAwareCommand != null)
                {
                    activeAwareCommand.IsActiveChanged += this.Command_IsActiveChanged;
                }
            }
        }

        /// <summary>
        /// Removes a command from the collection and removes itself from the <see cref="ICommand.CanExecuteChanged"/> event of it.
        /// </summary>
        /// <param name="command">The command to unregister.</param>
        public virtual void UnregisterCommand(ICommand command)
        {
            bool removed;
            lock (this.registeredCommands)
            {
                removed = this.registeredCommands.Remove(command);
            }

            if (removed)
            {
                command.CanExecuteChanged -= this.RegisteredCommand_CanExecuteChanged;
                this.OnCanExecuteChanged();

                if (this.monitorCommandActivity)
                {
                    var activeAwareCommand = command as IActiveAware;
                    if (activeAwareCommand != null)
                    {
                        activeAwareCommand.IsActiveChanged -= this.Command_IsActiveChanged;
                    }
                }
            }
        }

        private void RegisteredCommand_CanExecuteChanged(object sender, EventArgs e)
        {
            this.OnCanExecuteChanged();
        }

        /// <summary>
        /// Forwards <see cref="ICommand.CanExecute"/> to the registered commands and returns
        /// <see langword="true" /> if all of the commands return <see langword="true" />.
        /// </summary>
        /// <param name="parameter">Data used by the command.
        /// If the command does not require data to be passed, this object can be set to <see langword="null" />.
        /// </param>
        /// <returns><see langword="true" /> if all of the commands return <see langword="true" />; otherwise, <see langword="false" />.</returns>
        public virtual bool CanExecute(object parameter)
        {
            bool hasEnabledCommandsThatShouldBeExecuted = false;

            ICommand[] commandList;
            lock (this.registeredCommands)
            {
                commandList = this.registeredCommands.ToArray();
            }
            foreach (ICommand command in commandList)
            {
                if (this.ShouldExecute(command))
                {
                    if (!command.CanExecute(parameter))
                    {
                        return false;
                    }

                    hasEnabledCommandsThatShouldBeExecuted = true;
                }
            }

            return hasEnabledCommandsThatShouldBeExecuted;
        }

        /// <summary>
        /// Occurs when any of the registered commands raise <seealso cref="CanExecuteChanged"/>.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Forwards <see cref="ICommand.Execute"/> to the registered commands.
        /// </summary>
        /// <param name="parameter">Data used by the command.
        /// If the command does not require data to be passed, this object can be set to <see langword="null" />.
        /// </param>
        public virtual void Execute(object parameter)
        {
            Queue<ICommand> commands;
            lock (this.registeredCommands)
            {
                commands = new Queue<ICommand>(this.registeredCommands.Where(this.ShouldExecute).ToList());
            }

            while (commands.Count > 0)
            {
                ICommand command = commands.Dequeue();
                command.Execute(parameter);
            }
        }

        /// <summary>
        /// Evaluates if a command should execute.
        /// </summary>
        /// <param name="command">The command to evaluate.</param>
        /// <returns>A <see cref="bool"/> value indicating whether the command should be used 
        /// when evaluating <see cref="CompositeCommand.CanExecute"/> and <see cref="CompositeCommand.Execute"/>.</returns>
        /// <remarks>
        /// If this command is set to monitor command activity, and <paramref name="command"/>
        /// implements the <see cref="IActiveAware"/> interface, 
        /// this method will return <see langword="false" /> if the command's <see cref="IActiveAware.IsActive"/> 
        /// property is <see langword="false" />; otherwise it always returns <see langword="true" />.</remarks>
        protected virtual bool ShouldExecute(ICommand command)
        {
            var activeAwareCommand = command as IActiveAware;

            if (this.monitorCommandActivity && activeAwareCommand != null)
            {
                return activeAwareCommand.IsActive;
            }

            return true;
        }

        /// <summary>
        /// Gets the list of all the registered commands.
        /// </summary>
        /// <value>A list of registered commands.</value>
        /// <remarks>This returns a copy of the commands subscribed to the CompositeCommand.</remarks>
        public IList<ICommand> RegisteredCommands
        {
            get
            {
                IList<ICommand> commandList;
                lock (this.registeredCommands)
                {
                    commandList = this.registeredCommands.ToList();
                }

                return commandList;
            }
        }

        /// <summary>
        /// Handler for IsActiveChanged events of registered commands.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">EventArgs to pass to the event.</param>
        private void Command_IsActiveChanged(object sender, EventArgs e)
        {
            this.OnCanExecuteChanged();
        }
    }
}