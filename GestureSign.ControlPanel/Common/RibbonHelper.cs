using System.Windows;
using System.Windows.Media;

namespace GestureSign.ControlPanel.Common
{
    public static class RibbonHelper
    {
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.RegisterAttached("Icon", typeof(Geometry), typeof(RibbonHelper), new FrameworkPropertyMetadata(null));

        public static Geometry GetIcon(DependencyObject element)
        {
            return (Geometry)element.GetValue(IconProperty);
        }

        public static void SetIcon(DependencyObject element, Geometry value)
        {
            element.SetValue(IconProperty, value);
        }

        public static readonly DependencyProperty IconBrushProperty =
            DependencyProperty.RegisterAttached("IconBrush", typeof(Brush), typeof(RibbonHelper), new FrameworkPropertyMetadata(Brushes.Black));

        public static Brush GetIconBrush(DependencyObject element)
        {
            return (Brush)element.GetValue(IconBrushProperty);
        }

        public static void SetIconBrush(DependencyObject element, Brush value)
        {
            element.SetValue(IconBrushProperty, value);
        }
    }
}
