using Foundation;
#if __IOS__
using System;

namespace Windows.Devices.Sensors.Extensions
{
	internal static class DoubleExtensions
	{
		public static DateTimeOffset SensorTimestampToDateTimeOffset(this double timestamp)
		{
			var bootTime = NSDate.FromTimeIntervalSinceNow(-NSProcessInfo.ProcessInfo.SystemUptime);
			var date = (DateTime)bootTime.AddSeconds(timestamp);
			return new DateTimeOffset(date);
		}
	}
}
#endif
