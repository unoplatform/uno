#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Text;
using Android.OS;
using Java.Lang;

namespace Windows.Devices.Sensors.Extensions
{
	public static class LongExtensions
	{
		public static DateTimeOffset SensorTimestampToDateTimeOffset(this long timestamp)
		{
			return DateTimeOffset.Now
				.AddMilliseconds(-SystemClock.ElapsedRealtime())
				.AddMilliseconds(timestamp / 1000000.0);
		}
	}
}
#endif
