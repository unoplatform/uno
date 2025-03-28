using Android.Graphics;
using Rect = Windows.Foundation.Rect;

namespace Windows.UI.Xaml.Media;

public partial class XamlCompositionBrushBase : Brush
{
	private protected override void ApplyToPaintInner(Rect destinationRect, Paint paint)
	{
		// By default fallback to FallbackColor, unless overridden by a derived class.
		paint.Color = FallbackColorWithOpacity;
	}
}
