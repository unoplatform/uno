using Windows.Foundation;

namespace Windows.Graphics;

internal static class PointExtensions
{
	internal static PointInt32 ToPointInt32(this Point point) => new PointInt32((int)point.X, (int)point.Y);
}
