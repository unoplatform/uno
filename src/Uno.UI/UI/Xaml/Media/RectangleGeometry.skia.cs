using System.Numerics;
using SkiaSharp;
using Windows.UI.Composition;

namespace Windows.UI.Xaml.Media
{
	partial class RectangleGeometry
	{
		internal override SKPath GetSKPath() =>
			CompositionGeometry.BuildRectangleGeometry(offset: new Vector2((float)Rect.X, (float)Rect.Y), size: new Vector2((float)Rect.Width, (float)Rect.Height));
	}
}
