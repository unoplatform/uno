using Windows.UI;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.Xaml.Media;

internal static class DefaultBrushes
{
	public static SolidColorBrush SelectionHighlightColor { get; } = new SolidColorBrush(Color.FromArgb(255, 0, 120, 212));
}
