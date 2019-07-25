#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Text;
using Android.OS;
using Java.Lang;

namespace Uno.Devices.Sensors.Helpers
{
	internal static class SensorHelpers
	{
		public static DateTimeOffset TimestampToDateTimeOffset(long timestamp)
		{
			return DateTimeOffset.Now
				.AddMilliseconds(-SystemClock.ElapsedRealtime())
				.AddMilliseconds(timestamp / 1000000.0);
		}
	}
}
#endif
