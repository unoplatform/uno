using SkiaSharp;

namespace Windows.UI.Xaml;

partial record struct FullCornerRadius
{
	internal void GetRadii(SKPoint[] radiiStore)
	{
		radiiStore[0] = new((float)TopLeft.X, (float)TopLeft.Y);
		radiiStore[1] = new((float)TopRight.X, (float)TopRight.Y);
		radiiStore[2] = new((float)BottomRight.X, (float)BottomRight.Y);
		radiiStore[3] = new((float)BottomLeft.X, (float)BottomLeft.Y);
	}
}
