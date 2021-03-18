#if __IOS__
using Foundation;
using System;

namespace Uno.Devices.Sensors.Helpers
{
	internal static class SensorHelpers
	{
		private static readonly DateTimeOffset NSDateConversionStart =
			new DateTimeOffset(2001, 1, 1, 0, 0, 0, TimeSpan.Zero);

		public static DateTimeOffset TimestampToDateTimeOffset(double timestamp)
		{
			var bootTime = NSDate.FromTimeIntervalSinceNow(-NSProcessInfo.ProcessInfo.SystemUptime);
			var date = (DateTime)bootTime.AddSeconds(timestamp);
			return new DateTimeOffset(date);
		}

		public static DateTimeOffset NSDateToDateTimeOffset(NSDate nsDate)
		{
			if (nsDate == NSDate.DistantPast)
			{
				return DateTimeOffset.MinValue;
			}
			else if (nsDate == NSDate.DistantFuture)
			{
				return DateTimeOffset.MaxValue;
			}

			return NSDateConversionStart.AddSeconds(
				nsDate.SecondsSinceReferenceDate);
		}

		public static NSDate DateTimeOffsetToNSDate(DateTimeOffset dateTimeOffset)
		{
			if (dateTimeOffset == DateTimeOffset.MinValue)
			{
				return NSDate.DistantPast;
			}
			else if (dateTimeOffset == DateTimeOffset.MaxValue)
			{
				return NSDate.DistantFuture;
			}

			var dateInSecondsFromStart = dateTimeOffset
				.ToUniversalTime()
				.Subtract(NSDateConversionStart.UtcDateTime);

			return NSDate.FromTimeIntervalSinceReferenceDate(
				dateInSecondsFromStart.TotalSeconds);
		}
	}
}
#endif
