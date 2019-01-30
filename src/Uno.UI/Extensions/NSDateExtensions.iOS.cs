using Foundation;
using System;

namespace Uno.UI.Extensions
{
	public static class NSDateExtensions
	{
		//http://stackoverflow.com/questions/27925179/converting-nullable-datetime-to-nsdate-after-switching-to-unfied-api
		public static DateTime ToDateTime(this NSDate date)
		{
			var reference = new DateTime(2001, 1, 1, 0, 0, 0);
			var result = reference.AddSeconds(date.SecondsSinceReferenceDate);
			return result;
		}

		public static TimeSpan ToTimeSpan(this NSDate date)
		{
			return date.ToDateTime().TimeOfDay;
		}

		public static NSDate ToNSDate(this DateTime date)
		{
			var reference = new DateTime(2001, 1, 1, 0, 0, 0);
			var result = NSDate.FromTimeIntervalSinceReferenceDate(
				(date - reference).TotalSeconds);
			return result;
		}

		public static NSDate ToNSDate(this TimeSpan time)
		{
			return DateTime.Today.Add(time).ToNSDate();
		}

		internal static TimeSpan ToTimeSpan(this NSDate date, nint offsetInSecondsFromGMT)
		{
			var offset = TimeSpan.FromSeconds(offsetInSecondsFromGMT);
			return date.ToTimeSpan().Add(offset);
		}
	}
}
