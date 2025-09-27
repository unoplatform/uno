using Foundation;
using System;

namespace Uno.Devices.Sensors.Helpers;

internal static class SensorHelpers
{
	public static DateTimeOffset TimestampToDateTimeOffset(double timestamp)
	{
		var bootTime = NSDate.FromTimeIntervalSinceNow(-NSProcessInfo.ProcessInfo.SystemUptime);
		var date = (DateTime)bootTime.AddSeconds(timestamp);
		return new DateTimeOffset(date);
	}
}
