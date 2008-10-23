using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using Tobi.Infrastructure;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Wpf.Commands;

namespace Tobi.Modules.StatusBar.PresentationModels
{
    public class StatusBarPresentationModel
    {
        public event EventHandler<DataEventArgs<StatusBarData>> StatusBarDataChanged = delegate { };

        public DelegateCommand<String> StatusTextCommand;

        private const string displayFormat = "[{0}] / {1}";

        public StatusBarData CurrentStatusBarData { get; set; }
        public ArrayList KeyBindings = new ArrayList();

        public StatusBarPresentationModel()
        {
            StatusTextCommand = new DelegateCommand<String>(OnStatusTextCommandExecute, OnStatusTextCommandCanExecute);
            
            KeyBinding kb = new KeyBinding(StatusTextCommand, Key.D, ModifierKeys.Shift | ModifierKeys.Control);
            kb.CommandParameter = "CMD from KeyBinding";
            KeyBindings.Add(kb);
        }

        public string DisplayString
        {
            get
            {
                if (CurrentStatusBarData != null)
                {
                    return string.Format(CultureInfo.InvariantCulture, displayFormat, CurrentStatusBarData.Str1, CurrentStatusBarData.Str2);
                }

                return string.Empty + "?";
            }
        }
        private void OnStatusTextCommandExecute(String parameter)
        {
            MessageBox.Show(parameter);
            if (CurrentStatusBarData == null)
            {
                CurrentStatusBarData = new StatusBarData();
            }

            CurrentStatusBarData.Str1 = parameter;

            EventHandler<DataEventArgs<StatusBarData>> eventHandler = StatusBarDataChanged;
            if (eventHandler != null)
                eventHandler(this, new DataEventArgs<StatusBarData>(CurrentStatusBarData));
        }
        private bool OnStatusTextCommandCanExecute(String parameter)
        {
            return true;
        }




    }
}
