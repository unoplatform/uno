using Android.Graphics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Rect = Windows.Foundation.Rect;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Media;

public partial class RevealBrush : XamlCompositionBrushBase
{
	private protected override void ApplyToPaintInner(Rect destinationRect, Paint paint)
	{
		var color = this.IsDependencyPropertySet(FallbackColorProperty) ?
			GetColorWithOpacity(FallbackColor) :
			GetColorWithOpacity(Color);
		paint.Color = color;
	}
}
