using SkiaSharp;

namespace Microsoft.UI.Xaml;

partial record struct NonUniformCornerRadius
{
	internal void GetRadii(SKPoint[] radiiStore)
	{
		radiiStore[0] = new(TopLeft.X, TopLeft.Y);
		radiiStore[1] = new(TopRight.X, TopRight.Y);
		radiiStore[2] = new(BottomRight.X, BottomRight.Y);
		radiiStore[3] = new(BottomLeft.X, BottomLeft.Y);
	}
}
