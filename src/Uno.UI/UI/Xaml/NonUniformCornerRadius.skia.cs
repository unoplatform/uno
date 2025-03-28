using SkiaSharp;

#if IS_UNO_COMPOSITION
namespace Uno.UI.Composition;
#else
namespace Windows.UI.Xaml;
#endif

partial record struct NonUniformCornerRadius
{
	unsafe internal void GetRadii(SKPoint* radiiStore)
	{
		*(radiiStore++) = new(TopLeft.X, TopLeft.Y);
		*(radiiStore++) = new(TopRight.X, TopRight.Y);
		*(radiiStore++) = new(BottomRight.X, BottomRight.Y);
		*radiiStore = new(BottomLeft.X, BottomLeft.Y);
	}
}
