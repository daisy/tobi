using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Unity;
using Tobi.Infrastructure;

namespace Tobi.Modules.ToolBars
{
    /// <summary>
    /// Interaction logic for ToolBarsView.xaml
    /// </summary>
    public partial class ToolBarsView : INotifyPropertyChanged
    {
        public RichDelegateCommand<object> MagnifyUiIncreaseCommand { get; private set; }
        public RichDelegateCommand<object> MagnifyUiDecreaseCommand { get; private set; }
        public RichDelegateCommand<object> ManageShortcutsCommand { get; private set; }
        public RichDelegateCommand<object> SaveCommand { get; private set; }
        public RichDelegateCommand<object> SaveAsCommand { get; private set; }

        public RichDelegateCommand<object> NewCommand { get; private set; }
        public RichDelegateCommand<object> OpenCommand { get; private set; }

        public RichDelegateCommand<object> UndoCommand { get; private set; }
        public RichDelegateCommand<object> RedoCommand { get; private set; }

        public RichDelegateCommand<object> CopyCommand { get; private set; }
        public RichDelegateCommand<object> CutCommand { get; private set; }
        public RichDelegateCommand<object> PasteCommand { get; private set; }

        public RichDelegateCommand<object> HelpCommand { get; private set; }
        public RichDelegateCommand<object> PreferencesCommand { get; private set; }
        public RichDelegateCommand<object> WebHomeCommand { get; private set; }

        public RichDelegateCommand<object> NavNextCommand { get; private set; }
        public RichDelegateCommand<object> NavPreviousCommand { get; private set; }

        protected IUnityContainer Container { get; private set; }
        public ILoggerFacade Logger { get; private set; }

        ///<summary>
        /// Default constructor
        ///</summary>
        public ToolBarsView(IUnityContainer container, ILoggerFacade logger)
        {
            Container = container;
            Logger = logger;

            var shellPresenter = Container.Resolve<IShellPresenter>();
            if (shellPresenter != null)
            {
                MagnifyUiIncreaseCommand = shellPresenter.MagnifyUiIncreaseCommand;
                MagnifyUiDecreaseCommand = shellPresenter.MagnifyUiDecreaseCommand;
                ManageShortcutsCommand = shellPresenter.ManageShortcutsCommand;

                SaveCommand = shellPresenter.SaveCommand;
                SaveAsCommand = shellPresenter.SaveAsCommand;

                UndoCommand = shellPresenter.UndoCommand;
                RedoCommand = shellPresenter.RedoCommand;

                OpenCommand = shellPresenter.OpenCommand;
                NewCommand = shellPresenter.NewCommand;

                CopyCommand = shellPresenter.CopyCommand;
                CutCommand = shellPresenter.CutCommand;
                PasteCommand = shellPresenter.PasteCommand;

                HelpCommand = shellPresenter.HelpCommand;
                PreferencesCommand = shellPresenter.PreferencesCommand;
                WebHomeCommand = shellPresenter.WebHomeCommand;

                NavNextCommand = shellPresenter.NavNextCommand;
                NavPreviousCommand = shellPresenter.NavPreviousCommand;
            }

            InitializeComponent();
        }


        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            //PropertyChanged.Invoke(this, e);

            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged

        // ReSharper disable RedundantDefaultFieldInitializer
        private readonly List<Double> m_IconHeights = new List<double>
                                               {
                                                   20,30,40,50,60,70,80,90,100,150,200,250,300
                                               };
        // ReSharper restore RedundantDefaultFieldInitializer
        public List<Double> IconHeights
        {
            get
            {
                return m_IconHeights;
            }
        }
        
        public List<Double> IconWidths
        {
            get
            {
                return m_IconHeights;
            }
        }

        private double m_IconHeight = 32;
        public Double IconHeight
        {
            get
            {
                return m_IconHeight;
            }
            set
            {
                if (m_IconHeight == value) return;
                m_IconHeight = value;
                OnPropertyChanged("IconHeight");
            }
        }

        private double m_IconWidth = 32;
        public Double IconWidth
        {
            get
            {
                return m_IconWidth;
            }
            set
            {
                if (m_IconWidth == value) return;
                m_IconWidth = value;
                OnPropertyChanged("IconWidth");
            }
        }
    }
}
