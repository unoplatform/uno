namespace Windows.Foundation;

internal static class RectExtensions
{
	internal static double GetMidX(this Rect rect)
		=> rect.Left + ((rect.Right - rect.Left) / 2);

	internal static double GetMidY(this Rect rect)
		=> rect.Top + ((rect.Bottom - rect.Top) / 2);
}
