using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Reflection;
using System.Diagnostics;
using Tobi.Common.UI.XAML;


namespace Tobi.Common.UI
{
    /// <summary>
    /// Originally written by Øystein Bjørke, March 2009. Code Project Open License (CPOL)
    /// </summary>
    public partial class ComboBoxColor
    {
        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            var key = (e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));

            if (key == Key.Escape)
            {
                SelectedColor = m_previousColor;
            }
        }

        private Color m_previousColor;

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            FontWeight = FontWeights.Normal;
        }

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            m_previousColor = SelectedColor;
            FontWeight = FontWeights.UltraBold;
        }

        #region Dependency properties
        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Color), typeof(ComboBoxColor),
                                        new FrameworkPropertyMetadata(OnSelectedColorChanged));

        private static void OnSelectedColorChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            ComboBoxColor cp = obj as ComboBoxColor;
            Debug.Assert(cp != null);

            Color newColor = (Color)args.NewValue;
            Color oldColor = (Color)args.OldValue;


            //OnColorChanged(oldColor, newColor);

            if (newColor == oldColor)
                return;

            // When the SelectedColor changes, set the selected value of the combo box
            ColorViewModel selectedColorViewModel = cp.ColorList1.SelectedValue as ColorViewModel;
            if (selectedColorViewModel != null && selectedColorViewModel.Color.Equals(newColor))
            {
                //cp.ColorList1.AutomationPropertiesName = selectedColorViewModel.Name;
            }

            if (selectedColorViewModel == null || !selectedColorViewModel.Color.Equals(newColor))
            {
                // Add the color if not found
                ColorViewModel cvm = cp.ListContains(newColor);
                if (cvm == null)
                {
                    cvm = AddColor(newColor, newColor.ToString());
                    //cp.ColorList1.Items.Add(cvm);
                    cp.ColorList1.Items.Refresh();
                    cp.ColorList1.SelectedIndex = cp.ColorList1.Items.Count - 1;
                }
                //cp.ColorList1.AutomationPropertiesName = cvm.Name;
            }

            // Also update the brush
            // cp.SelectedBrush = ColorBrushCache.Get(newColor);

            cp.SelectedColor = newColor;
        }

        private ColorViewModel ListContains(Color color)
        {
            foreach (object o in ColorList1.Items)
            {
                ColorViewModel vcm = o as ColorViewModel;
                if (vcm == null) continue;
                if (vcm.Color == color) return vcm;
            }
            return null;
        }

        //public Brush SelectedBrush
        //{
        //    get { return (Brush)GetValue(SelectedBrushProperty); }
        //    set { SetValue(SelectedBrushProperty, value); }
        //}

        //// Using a DependencyProperty as the backing store for SelectedBrush.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty SelectedBrushProperty =
        //    DependencyProperty.Register("SelectedBrush", typeof(Brush), typeof(ComboBoxColor),
        //                                new FrameworkPropertyMetadata(OnSelectedBrushChanged));

        //private static void OnSelectedBrushChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        //{
        //    // Debug.WriteLine("OnSelectedBrushChanged");
        //    ComboBoxColor cp = (ComboBoxColor)obj;
        //    SolidColorBrush newBrush = (SolidColorBrush)args.NewValue;
        //    // SolidColorBrush oldBrush = (SolidColorBrush)args.OldValue;

        //    if (cp.SelectedColor != newBrush.Color)
        //        cp.SelectedColor = newBrush.Color;
        //}
        #endregion

        //#region Events
        //public static readonly RoutedEvent ColorChangedEvent =
        //    EventManager.RegisterRoutedEvent("ColorChanged", RoutingStrategy.Bubble,
        //                                     typeof(RoutedPropertyChangedEventHandler<Color>), typeof(ComboBoxColor));

        //public event RoutedPropertyChangedEventHandler<Color> ColorChanged
        //{
        //    add { AddHandler(ColorChangedEvent, value); }
        //    remove { RemoveHandler(ColorChangedEvent, value); }
        //}

        //protected virtual void OnColorChanged(Color oldValue, Color newValue)
        //{
        //    RoutedPropertyChangedEventArgs<Color> args = new RoutedPropertyChangedEventArgs<Color>(oldValue, newValue);
        //    args.RoutedEvent = ComboBoxColor.ColorChangedEvent;
        //    RaiseEvent(args);
        //}
        //#endregion

        private static Brush _CheckerBrush = CreateCheckerBrush();
        public static Brush CheckerBrush { get { return _CheckerBrush; } }

        public IEnumerable ColorModels
        {
            get { return m_colors; }
        }

        private static List<ColorViewModel> m_colors = new List<ColorViewModel>();
        static ComboBoxColor()
        {
            /*

            // Add some common colors
            AddColor(Colors.Black, "Black");
            AddColor(Colors.Gray, "Gray");
            AddColor(Colors.LightGray, "LightGray");
            AddColor(Colors.White, "White");
            AddColor(Colors.Transparent, "Transparent");
            AddColor(Colors.Red, "Red");
            AddColor(Colors.Green, "Green");
            AddColor(Colors.Blue, "Blue");
            AddColor(Colors.Cyan, "Cyan");
            AddColor(Colors.Magenta, "Magenta");
            AddColor(Colors.Yellow, "Yellow");
            AddColor(Colors.Purple, "Purple");
            AddColor(Colors.Orange, "Orange");
            AddColor(Colors.Brown, "Brown");

            // And some colors with transparency
            AddColor(Color.FromArgb(128, 0, 0, 0), "Black 50%");
            AddColor(Color.FromArgb(128, 255, 255, 255), "White 50%");
            AddColor(Color.FromArgb(128, 255, 0, 0), "Red 50%");
            AddColor(Color.FromArgb(128, 0, 255, 0), "Green 50%");
            AddColor(Color.FromArgb(128, 0, 0, 255), "Blue 50%");
            ColorList1.Items.Add(new Separator());
             * */

            // Enumerate constant colors from the Colors class
            Type colorsType = typeof(System.Windows.Media.Colors);
            PropertyInfo[] pis = colorsType.GetProperties();
            foreach (PropertyInfo pi in pis)
            {
                AddColor((Color)pi.GetValue(null, null), pi.Name);
            }

        }

        private static ColorViewModel AddColor(Color color, string name)
        {
            if (!name.StartsWith("#", StringComparison.Ordinal))
                name = NiceName(name);
            var cvm = new ColorViewModel(color, name);
            m_colors.Add(cvm);

            return cvm;
        }

        private static string NiceName(string name)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]))
                    sb.Append(" ");
                sb.Append(name[i]);
            }
            return sb.ToString();
        }

        public ComboBoxColor()
        {
            InitializeComponent();

            //ColorList1.Items.Clear();
            //ColorList1.Items.Add(new Separator());
            //foreach (var color in m_colors)
            //{
            //    ColorList1.Items.Add(color);
            //}

            //            ColorList1.SelectedValuePath = "Color";
        }


        public static Brush CreateCheckerBrush()
        {
            // from http://msdn.microsoft.com/en-us/library/aa970904.aspx

            DrawingBrush checkerBrush = new DrawingBrush();

            GeometryDrawing backgroundSquare =
                new GeometryDrawing(
                    Brushes.White,
                    null,
                    new RectangleGeometry(new Rect(0, 0, 8, 8)));
            backgroundSquare.Freeze();

            GeometryGroup aGeometryGroup = new GeometryGroup();
            aGeometryGroup.Children.Add(new RectangleGeometry(new Rect(0, 0, 4, 4)));
            aGeometryGroup.Children.Add(new RectangleGeometry(new Rect(4, 4, 4, 4)));

            GeometryDrawing checkers = new GeometryDrawing(Brushes.Black, null, aGeometryGroup);
            checkers.Freeze();

            DrawingGroup checkersDrawingGroup = new DrawingGroup();
            checkersDrawingGroup.Children.Add(backgroundSquare);
            checkersDrawingGroup.Children.Add(checkers);

            checkerBrush.Drawing = checkersDrawingGroup;
            checkerBrush.Viewport = new Rect(0, 0, 0.5, 0.5);
            checkerBrush.TileMode = TileMode.Tile;

            checkerBrush.Freeze();
            return checkerBrush;
        }

    }

    public class ColorViewModel
    {
        public ColorViewModel(Color color, String name)
        {
            Color = color;
            Name = name;
            Brush = ColorBrushCache.Get(Color);
        }

        public Color Color { get; private set; }
        public Brush Brush { get; private set; }
        public string Name { get; private set; }
    }
}
