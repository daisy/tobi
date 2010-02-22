using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Practices.Composite.Presentation.Commands;
using Tobi.Common.UI;
namespace Tobi.Common.MVVM.Command
{
    public class RichCompositeCommand:RichDelegateCommand
    {
        private CompositeCommand m_compCommand;

        public RichCompositeCommand(String shortDescription, String longDescription,
                                   KeyGesture keyGesture,
                                   VisualBrush icon)
        : base(shortDescription, longDescription, keyGesture, icon,null, null)
        {
            m_compCommand = new CompositeCommand();
            //m_compCommand.CanExecuteChanged;  
        }

        public void RegisterCommand(RichDelegateCommand command)
        {
            m_compCommand.RegisterCommand(command);
        }

        public void UnregisterCommand(RichDelegateCommand command)
        {
            m_compCommand.UnregisterCommand(command);
        }

        public IList<RichDelegateCommand> RegisteredCommands
        {
            get
            {
                IList<RichDelegateCommand> ilCommands = new List<RichDelegateCommand>(m_compCommand.RegisteredCommands.Count);
                foreach (ICommand registeredCommand in m_compCommand.RegisteredCommands) { ilCommands.Add((RichDelegateCommand)registeredCommand); }
                return ilCommands;
            }
        }

        public bool CanExecute(object parameter)
        {
            return m_compCommand.CanExecute(parameter);
        }

        public void Execute(object parameter)
        {
            m_compCommand.Execute(parameter);
        }
    }
}
