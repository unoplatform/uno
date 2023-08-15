using Android.Graphics;
using Rect = Windows.Foundation.Rect;
using Size = Windows.Foundation.Size;

namespace Windows.UI.Xaml.Media
{
	partial class GradientBrush
	{
		protected override void ApplyToPaintInner(Rect destinationRect, Paint paint)
		{
			paint.SetShader(GetShader(destinationRect.Size));
			paint.SetStyle(Paint.Style.Stroke);
		}

		protected internal abstract Shader GetShader(Size size);
	}
}
