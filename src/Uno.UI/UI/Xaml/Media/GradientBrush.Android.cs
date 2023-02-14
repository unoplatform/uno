using Android.Graphics;
using Rect = Windows.Foundation.Rect;
using Size = Windows.Foundation.Size;

namespace Microsoft.UI.Xaml.Media
{
	partial class GradientBrush
	{
		protected override Paint GetPaintInner(Rect destinationRect)
		{
			var paint = new Paint();
			paint.SetShader(GetShader(destinationRect.Size));
			paint.SetStyle(Paint.Style.Stroke);
			return paint;
		}

		protected internal abstract Shader GetShader(Size size);
	}
}
