using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Tobi.Infrastructure;

namespace Tobi.Modules.ToolBars
{
    /// <summary>
    /// Interaction logic for ToolBarsView.xaml
    /// </summary>
    public partial class ToolBarsView : INotifyPropertyChanged
    {
        ///<summary>
        /// Default constructor
        ///</summary>
        public ToolBarsView()
        {
            InitializeComponent();
            //DataContext = this;
        }

        /*
        private void OnButtonImageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var image = sender as Image;
            if (image == null)
            {
                return;
            }
            BindingExpressionBase beb = image.GetBindingExpression(Image.SourceProperty);
            //BindingExpressionBase beb = BindingOperations.GetBindingExpressionBase(image, Image.SourceProperty);
            if (beb != null)
            {
                beb.UpdateTarget();
            }
        }
         * 
         */

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
