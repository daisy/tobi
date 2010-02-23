using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Practices.Composite.Presentation.Commands;

namespace Tobi.Common.MVVM.Command
{
    public class RichCompositeCommand : RichDelegateCommand
    {
        private readonly CompositeCommand m_compCommand;

        public RichCompositeCommand(String shortDescription, String longDescription,
                                   KeyGesture keyGesture,
                                   VisualBrush icon,
                                   ApplicationSettingsBase settingContainer, string settingName)
            : base(shortDescription, longDescription, keyGesture, icon,
                    null, null,
                    settingContainer, settingName)
        {
            m_compCommand = new CompositeCommand(true);
            m_compCommand.CanExecuteChanged += OnCompCommandCanExecuteChanged;
        }

        private void OnCompCommandCanExecuteChanged(object sender, EventArgs e)
        {
            RaiseCanExecuteChanged();
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
                //return (IList<RichDelegateCommand>)m_compCommand.RegisteredCommands;

                IList<RichDelegateCommand> ilCommands = new List<RichDelegateCommand>(m_compCommand.RegisteredCommands.Count);
                foreach (ICommand registeredCommand in m_compCommand.RegisteredCommands) { ilCommands.Add((RichDelegateCommand)registeredCommand); }
                return ilCommands;
            }
        }

        public override bool CanExecute()
        {
            return m_compCommand.CanExecute(null);
        }

        public override void Execute()
        {
            m_compCommand.Execute(null);
        }
    }
}
