#if __IOS__
using System;
using System.Collections.Generic;
using System.Text;
using Foundation;

namespace Windows.Extensions
{
	internal static class DoubleExtensions
	{
		public static DateTimeOffset TimestampToDateTimeOffset(this double timestamp)
		{
			var bootTime = NSDate.FromTimeIntervalSinceNow(-NSProcessInfo.ProcessInfo.SystemUptime);
			var date = (DateTime)bootTime.AddSeconds(timestamp);
			return new DateTimeOffset(date);
		}
	}
}
#endif
