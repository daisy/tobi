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
