using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Practices.Composite;

namespace Tobi.Common.MVVM.Command
{
    public class RichDispatcherCommand : RichDelegateCommand
    {
        private readonly List<ICommand> m_RegisteredCommands = new List<ICommand>();

        public RichDispatcherCommand(String shortDescription, String longDescription,
                                   KeyGesture keyGesture,
                                   VisualBrush icon,
                                   ApplicationSettingsBase settingContainer, string settingName)
            : base(shortDescription, longDescription, keyGesture, icon,
                    null, null,
                    settingContainer, settingName)
        {
        }

        public void RegisterCommand(RichDelegateCommand command)
        {
            lock (m_RegisteredCommands)
            {
                if (!m_RegisteredCommands.Contains(command))
                {
                    m_RegisteredCommands.Add(command);
                }
            }
        }

        public void UnregisterCommand(RichDelegateCommand command)
        {
            lock (m_RegisteredCommands)
            {
                if (m_RegisteredCommands.Contains(command))
                {
                    m_RegisteredCommands.Remove(command);
                }
            }
        }

        public override bool CanExecute()
        {
            return true;
        }

        public override void Execute()
        {
            foreach (var command in m_RegisteredCommands)
            {
                if (command is IActiveAware && !((IActiveAware)command).IsActive) continue;
                if (command.CanExecute(null))
                {
                    command.Execute(null);
                }
            }
        }
    }
}
