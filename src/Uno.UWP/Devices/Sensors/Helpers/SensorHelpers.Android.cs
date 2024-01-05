using System;
using System.Collections.Generic;
using System.Text;
using Android.App;
using Android.Content;
using Android.Hardware;
using Android.OS;
using Java.Lang;

namespace Uno.Devices.Sensors.Helpers
{
	internal static class SensorHelpers
	{
		public const uint UiReportingInterval = 60;

		public static DateTimeOffset SystemBootDateTimeOffset =>
			DateTimeOffset.Now.AddMilliseconds(-SystemClock.ElapsedRealtime());

		public static DateTimeOffset TimestampToDateTimeOffset(long timestamp)
		{
			return DateTimeOffset.Now
				.AddMilliseconds(-SystemClock.ElapsedRealtime())
				.AddMilliseconds(timestamp / 1000000.0);
		}

		public static SensorManager GetSensorManager() =>
			(Application.Context.GetSystemService(Context.SensorService) as SensorManager)!;
	}
}
