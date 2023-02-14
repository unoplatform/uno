using Android.Graphics;
using Rect = Windows.Foundation.Rect;

namespace Microsoft.UI.Xaml.Media
{
	// Android partial for SolidColorBrush
	public partial class SolidColorBrush : Brush
	{

		protected override Paint GetPaintInner(Rect destinationRect)
		{
			return new Paint() { Color = this.ColorWithOpacity, AntiAlias = true };
		}
	}
}
