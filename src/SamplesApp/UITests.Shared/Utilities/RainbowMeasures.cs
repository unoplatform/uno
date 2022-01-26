using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace SampleApps.Utilities
{
    public class RainbowMeasures : Panel
    {
        private static readonly IReadOnlyList<Color> _colors = new []{
            Colors.Blue,
            Colors.Green,
            Colors.Yellow,
            Colors.Orange,
            Colors.Red,
            Colors.Violet,
            Colors.Aqua,
            Colors.DarkMagenta,
            Colors.Gray,
            Colors.LightSeaGreen,
            Colors.MediumSpringGreen,
            Colors.DarkSlateGray,
            Colors.YellowGreen,
            Colors.Cornsilk,
        };

        private int _measureIndex = -1;

        public RainbowMeasures()
        {
            Background = new SolidColorBrush(Colors.Transparent);
            PointerPressed += PointerEvent;
        }

        private void PointerEvent(object sender, PointerRoutedEventArgs e)
        {
            if (e.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift))
            {
                InvalidateArrange();
            }
            else
            {
                InvalidateMeasure();
            }

            e.Handled = true;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Background = new SolidColorBrush(_colors[++_measureIndex % _colors.Count]);

            if (Children.Count > 0 && Children[0] is { } child)
            {
                child.Measure(availableSize);
                return child.DesiredSize;
            }

            return default;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Children.Count > 0 && Children[0] is { } child)
            {
                child.Arrange(new Rect(default, finalSize));
                return finalSize;
            }

            return default;
        }
    }
}
