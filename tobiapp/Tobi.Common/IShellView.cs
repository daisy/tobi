using System.Windows;
using Tobi.Common.MVVM;
using System;
using System.Windows.Media;
using Tobi.Common.MVVM.Command;

namespace Tobi.Common
{
    public interface IShellView : INotifyPropertyChangedEx, IInputBindingManager
    {
        void Show();

        bool SplitterDrag { get; }

        double MagnificationLevel { get; set; }

        //RichDelegateCommand ExitCommand { get; }
        //RichDelegateCommand MagnifyUiIncreaseCommand { get; }
        //RichDelegateCommand MagnifyUiDecreaseCommand { get; }
        //RichDelegateCommand ManageShortcutsCommand { get; }

        //RichDelegateCommand CopyCommand { get; }
        //RichDelegateCommand CutCommand { get; }
        //RichDelegateCommand PasteCommand { get; }

        //RichDelegateCommand HelpCommand { get; }
        //RichDelegateCommand PreferencesCommand { get; }
        //RichDelegateCommand WebHomeCommand { get; }

        //RichDelegateCommand NavNextCommand { get; }
        //RichDelegateCommand NavPreviousCommand { get; }

        void RegisterRichCommand(RichDelegateCommand command);

        VisualBrush LoadTangoIcon(string resourceKey);
        VisualBrush LoadGnomeNeuIcon(string resourceKey);
        VisualBrush LoadGnomeGionIcon(string resourceKey);
        VisualBrush LoadGnomeFoxtrotIcon(string resourceKey);

        void DimBackgroundWhile(Action action);
    }
}