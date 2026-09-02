using Windows.Foundation;

namespace Windows.Graphics;

internal static class SizeExtensions
{
	internal static SizeInt32 ToSizeInt32(this Size size) => new SizeInt32((int)size.Width, (int)size.Height);
}
