using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace Tobi.Infrastructure.UI
{
    public class WrapPanelLastChildFill : WrapPanel
    {
        public static readonly DependencyProperty LastChildFillProperty =
            DependencyProperty.Register("LastChildFill",
                                        typeof(Boolean),
                                        typeof(WrapPanelLastChildFill),
                                        new PropertyMetadata(new PropertyChangedCallback(OnLastChildFillChanged)));

        internal static void OnLastChildFillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        public Boolean LastChildFill
        {
            get
            {
                return (Boolean)GetValue(LastChildFillProperty);
            }
            set
            {
                SetValue(LastChildFillProperty, value);
            }
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            int start = 0;
            double itemWidth = this.ItemWidth;
            double itemHeight = this.ItemHeight;
            double v = 0.0;
            double itemU = (this.Orientation == Orientation.Horizontal) ? itemWidth : itemHeight;
            UVSize size = new UVSize(this.Orientation);
            UVSize size2 = new UVSize(this.Orientation, finalSize.Width, finalSize.Height);
            bool flag = !Double.IsNaN(itemWidth);
            bool flag2 = !Double.IsNaN(itemHeight);
            bool useItemU = (this.Orientation == Orientation.Horizontal) ? flag : flag2;
            UIElementCollection internalChildren = base.InternalChildren;
            int end = 0;
            int count = internalChildren.Count;
            UVSize size3 = new UVSize(this.Orientation);
            while (end < count)
            {
                UIElement element = internalChildren[end];
                if (element != null)
                {
                    size3 = new UVSize(this.Orientation, flag ? itemWidth : element.DesiredSize.Width, flag2 ? itemHeight : element.DesiredSize.Height);
                    if (LastChildFill && end == count - 1)
                    {
                        size3 = new UVSize(this.Orientation, flag ? itemWidth : size2.U - size.U, flag2 ? itemHeight : size2.V - size.V);
                    }
                    if (GreaterThan(size.U + size3.U, size2.U))
                    {
                        this.arrangeLine(v, size.V, start, end, useItemU, itemU, size3);
                        v += size.V;
                        size = size3;
                        if (GreaterThan(size3.U, size2.U))
                        {
                            this.arrangeLine(v, size3.V, end, ++end, useItemU, itemU, new UVSize(this.Orientation));
                            v += size3.V;
                            size = new UVSize(this.Orientation);
                        }
                        start = end;
                    }
                    else
                    {
                        size.U += size3.U;
                        size.V = Math.Max(size3.V, size.V);
                    }
                }
                end++;
            }
            if (start < internalChildren.Count)
            {
                this.arrangeLine(v, size.V, start, internalChildren.Count, useItemU, itemU, size3);
            }
            return finalSize;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            UVSize size = new UVSize(this.Orientation);
            UVSize size2 = new UVSize(this.Orientation);
            UVSize size3 = new UVSize(this.Orientation, constraint.Width, constraint.Height);
            double itemWidth = this.ItemWidth;
            double itemHeight = this.ItemHeight;
            bool flag = !Double.IsNaN(itemWidth);
            bool flag2 = !Double.IsNaN(itemHeight);
            Size availableSize = new Size(flag ? itemWidth : constraint.Width, flag2 ? itemHeight : constraint.Height);
            UIElementCollection internalChildren = base.InternalChildren;
            int num3 = 0;
            int count = internalChildren.Count;
            while (num3 < count)
            {
                UIElement element = internalChildren[num3];
                if (element != null)
                {
                    element.Measure(availableSize);
                    UVSize size5 = new UVSize(this.Orientation, flag ? itemWidth : element.DesiredSize.Width, flag2 ? itemHeight : element.DesiredSize.Height);

                    if (LastChildFill && num3 == count - 1)
                    {
                        double valU = size3.U - size.U;
                        double valV = size3.V - size.V;
                        if (valU == Double.PositiveInfinity || valU == Double.NegativeInfinity)
                        {
                            valU = element.DesiredSize.Width;
                        }
                        if (valV == Double.PositiveInfinity || valV == Double.NegativeInfinity)
                        {
                            valV = element.DesiredSize.Height;
                        }

                        size5 = new UVSize(this.Orientation, flag ? itemWidth : valU, flag2 ? itemHeight : valV);
                    }

                    if (GreaterThan(size.U + size5.U, size3.U))
                    {
                        size2.U = Math.Max(size.U, size2.U);
                        size2.V += size.V;
                        size = size5;
                        if (GreaterThan(size5.U, size3.U))
                        {
                            size2.U = Math.Max(size5.U, size2.U);
                            size2.V += size5.V;
                            size = new UVSize(this.Orientation);
                        }
                    }
                    else
                    {
                        size.U += size5.U;
                        size.V = Math.Max(size5.V, size.V);
                    }
                }
                num3++;
            }
            size2.U = Math.Max(size.U, size2.U);
            size2.V += size.V;
            return new Size(size2.Width, size2.Height);
        }


        private void arrangeLine(double v, double lineV, int start, int end, bool useItemU, double itemU, UVSize desiredOverride)
        {
            double num = 0.0;
            bool flag = this.Orientation == Orientation.Horizontal;
            UIElementCollection internalChildren = base.InternalChildren;
            for (int i = start; i < end; i++)
            {
                int count = internalChildren.Count;
                UIElement element = internalChildren[i];
                if (element != null)
                {
                    UVSize size = new UVSize(this.Orientation,
                                                    element.DesiredSize.Width,
                                                    element.DesiredSize.Height);

                    if (LastChildFill && i == count - 1)
                    {
                        size = new UVSize(this.Orientation, desiredOverride.U, desiredOverride.V);
                    }

                    double num3 = useItemU ? itemU : size.U;
                    element.Arrange(new Rect(flag ? num : v,
                                                flag ? v : num,
                                                flag ? num3 : lineV,
                                                flag ? lineV : num3));
                    num += num3;
                }
            }
        }

        public static bool AreClose(double value1, double value2)
        {
            if (value1 == value2)
            {
                return true;
            }
            double num = ((Math.Abs(value1) + Math.Abs(value2)) + 10.0) * 2.2204460492503131E-16;
            double num2 = value1 - value2;
            return ((-num < num2) && (num > num2));
        }
        public static bool GreaterThan(double value1, double value2)
        {
            return ((value1 > value2) && !AreClose(value1, value2));
        }

        private static bool IsWidthHeightValid(object value)
        {
            double num = (double)value;
            return (Double.IsNaN(num) || ((num >= 0.0) && !double.IsPositiveInfinity(num)));
        }

        // Nested Types
        [StructLayout(LayoutKind.Sequential)]
        private struct UVSize
        {
            internal double U;
            internal double V;
            private Orientation _orientation;
            internal UVSize(Orientation orientation, double width, double height)
            {
                this.U = this.V = 0.0;
                this._orientation = orientation;
                this.Width = width;
                this.Height = height;
            }

            internal UVSize(Orientation orientation)
            {
                this.U = this.V = 0.0;
                this._orientation = orientation;
            }

            internal double Width
            {
                get
                {
                    if (this._orientation != Orientation.Horizontal)
                    {
                        return this.V;
                    }
                    return this.U;
                }
                set
                {
                    if (this._orientation == Orientation.Horizontal)
                    {
                        this.U = value;
                    }
                    else
                    {
                        this.V = value;
                    }
                }
            }
            internal double Height
            {
                get
                {
                    if (this._orientation != Orientation.Horizontal)
                    {
                        return this.U;
                    }
                    return this.V;
                }
                set
                {
                    if (this._orientation == Orientation.Horizontal)
                    {
                        this.V = value;
                    }
                    else
                    {
                        this.U = value;
                    }
                }
            }
        }
    }
}
