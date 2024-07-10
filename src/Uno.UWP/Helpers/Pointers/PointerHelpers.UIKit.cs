using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation;

namespace Uno.UI.Xaml.Extensions;

internal static class PointerHelpers
{
	private static long? _bootTime;
	private static double? _firstTimestamp;

	internal static ulong ToTimeStamp(double timestamp)
	{
		_bootTime ??= DateTime.UtcNow.Ticks - (long)(TimeSpan.TicksPerSecond * new NSProcessInfo().SystemUptime);

		return (ulong)_bootTime.Value + (ulong)(TimeSpan.TicksPerSecond * timestamp);
	}

	internal static uint ToFrameId(double timestamp)
	{
		_firstTimestamp ??= timestamp;

		var relativeTimestamp = timestamp - _firstTimestamp;
		var frameId = relativeTimestamp * 120.0; // we allow a precision of 120Hz (8.333 ms per frame)

		// When we cast, we are not overflowing but instead capping to uint.MaxValue.
		// We use modulo to make sure to reset to 0 in that case (1.13 years of app run-time, but we prefer to be safe).
		return (uint)(frameId % uint.MaxValue);
	}
}
