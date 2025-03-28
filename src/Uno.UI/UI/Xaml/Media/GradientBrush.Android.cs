using Android.Graphics;
using Rect = Windows.Foundation.Rect;
using Size = Windows.Foundation.Size;

namespace Windows.UI.Xaml.Media
{
	partial class GradientBrush
	{
		private protected override void ApplyToPaintInner(Rect destinationRect, Paint paint)
		{
			paint.SetShader(GetShader(destinationRect.Size));
		}

		protected internal abstract Shader GetShader(Size size);
	}
}
