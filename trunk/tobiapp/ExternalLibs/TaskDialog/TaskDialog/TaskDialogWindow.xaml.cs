using System;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sid.Windows.Controls
{
    /// <summary>
    /// Interaction logic for TaskDialogWindow.xaml
    /// </summary>
    public partial class TaskDialogWindow : InteropWindow
    {
        internal TaskDialogWindow()
        {
            InitializeComponent();
        }

        /*
        private void OnGotKeyboardFocusTextBox(Object sender, KeyboardFocusChangedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }*/

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            TaskDialog td = Content as TaskDialog;
            if (td != null)
            {
                // Too soon (see OnShowWindow)
                //td.OnWindowLoaded();
            }
        }
        protected override void OnShowWindow()
        {
            // Plays a system sound when the window is initialized (shown)
            TaskDialog td = Content as TaskDialog;
            if (td != null)
            {

                td.OnWindowLoaded();

                td.OnWindowInitialized();

                switch (td.SystemSound)
                {
                    case TaskDialogSound.Beep:
                        SystemSounds.Beep.Play();
                        break;

                    case TaskDialogSound.Exclamation:
                        SystemSounds.Exclamation.Play();
                        break;

                    case TaskDialogSound.Hand:
                        SystemSounds.Hand.Play();
                        break;

                    case TaskDialogSound.Question:
                        SystemSounds.Question.Play();
                        break;

                    case TaskDialogSound.Asterisk:
                        SystemSounds.Asterisk.Play();
                        break;
                }
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            TaskDialog td = Content as TaskDialog;
            if (td != null)
            {
                td.OnClosing();
            }
            Content = null;
            base.OnClosed(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                TaskDialog td = Content as TaskDialog;
                if (td != null)
                {
                    if (td.OnEscape())
                    {
                        // Closing.
                        return;
                    }
                }
            }
            base.OnKeyUp(e);
        }
    }
}
