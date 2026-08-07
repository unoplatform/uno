
#nullable enable

using Uno;
using Windows.Foundation;

#if !HAS_UNO
using System.Runtime.InteropServices;
#endif

#if HAS_UNO_WINUI || WINAPPSDK
#else
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif

namespace Uno.UI.Toolkit.DevTools.Input;

internal static class PointExtensions
{
	public static Point OffsetLinear(this Point point, double xAndY)
		=> new(point.X + xAndY, point.Y + xAndY);

	public static Point Offset(this Point point, double x = 0, double y = 0)
		=> new(point.X + x, point.Y + y);
}
