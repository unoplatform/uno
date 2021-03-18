using Android.Graphics;
using Rect = Windows.Foundation.Rect;

namespace Windows.UI.Xaml.Media
{
	partial class GradientBrush
	{
		protected override Paint GetPaintInner(Rect destinationRect)
		{
			var paint = new Paint();
			paint.SetShader(GetShader(destinationRect));
			paint.SetStyle(Paint.Style.Stroke);
			return paint;
		}

		protected internal abstract Shader GetShader(Rect destinationRect);
	}
}
