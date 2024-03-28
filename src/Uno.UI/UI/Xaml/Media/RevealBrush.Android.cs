using Android.Graphics;
using Rect = Windows.Foundation.Rect;

namespace Windows.UI.Xaml.Media;

partial class RevealBrush
{
	private protected override void ApplyToPaintInner(Rect destinationRect, Paint paint)
	{
		var color = this.IsDependencyPropertySet(FallbackColorProperty) ?
			GetColorWithOpacity(FallbackColor) :
			GetColorWithOpacity(Color);
		paint.Color = color;
	}
}
