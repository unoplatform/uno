using Android.Graphics;
using Rect = Windows.Foundation.Rect;

namespace Windows.UI.Xaml.Media;

public partial class XamlCompositionBrushBase : Brush
{
	protected override Paint GetPaintInner(Rect destinationRect)
	{
		// By default fallback to FallbackColor, unless overridden by a derived class.
		var color = GetColorWithOpacity(FallbackColor);
		return new Paint() { Color = color, AntiAlias = true };
	}
}
