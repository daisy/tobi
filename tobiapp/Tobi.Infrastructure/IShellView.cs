﻿using System.Windows;
using System.Windows.Data;

namespace Tobi
{
    public interface IShellView
    {
        ///<summary>
        /// Shows the Shell window
        ///</summary>
        void ShowView();
        Window Window { get; }

        bool SplitterDrag { get; }

        double MagnificationLevel { get; set; }

        /*
        void DecreaseZoom(double? step);
        void IncreaseZoom(double? step);
        Binding GetZoomBinding();
        */
    }
}
