using Android.Graphics;

namespace Windows.UI.Xaml.Media
{
	// Android partial for SolidColorBrush
	public partial class SolidColorBrush : Brush
	{

		protected override Paint GetPaintInner(Windows.Foundation.Rect destinationRect)
		{
			return new Paint() { Color = this.ColorWithOpacity, AntiAlias = true };
		}
	}
}
