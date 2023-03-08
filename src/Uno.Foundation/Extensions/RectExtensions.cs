namespace Windows.Foundation;

internal static class RectExtensions
{
	internal static double GetMidX(this Rect rect)
		=> (rect.Left + rect.Right) / 2;

	internal static double GetMidY(this Rect rect)
		=> (rect.Top + rect.Bottom) / 2;
}
