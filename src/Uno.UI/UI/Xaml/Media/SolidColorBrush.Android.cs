using System;
using Android.Graphics;
using Rect = Windows.Foundation.Rect;

namespace Microsoft.UI.Xaml.Media
{
	// Android partial for SolidColorBrush
	public partial class SolidColorBrush : Brush
	{
		private protected override void ApplyToPaintInner(Rect destinationRect, Paint paint)
		{
			paint.Color = ColorWithOpacity;
		}
	}
}
