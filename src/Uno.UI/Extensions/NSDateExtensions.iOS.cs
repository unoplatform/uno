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
			var reference = DateTime.SpecifyKind(new DateTime(2001, 1, 1, 0, 0, 0), DateTimeKind.Utc);

			var result = NSDate.FromTimeIntervalSinceReferenceDate(
				(date - reference.ToReferenceLocalTime()).TotalSeconds);
			return result;
		}

		public static NSDate ToNSDate(this TimeSpan time)
		{
			return DateTime.Today.Add(time).ToNSDate();
		}

		internal static TimeSpan ToTimeSpanOfDay(this NSDate date, nint offsetInSecondsFromGMT)
		{
			var offset = TimeSpan.FromSeconds(offsetInSecondsFromGMT);
			return date.ToTimeSpan().Add(offset).NormalizeToDay();
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public static DateTime ToReferenceLocalTime(this DateTime time)
		{
			if (time.Kind == DateTimeKind.Local)
			{
				return time;
			}
			else if (time.Kind == DateTimeKind.Utc)
			{
				var returnTime = new DateTime(time.Ticks, DateTimeKind.Local);
				returnTime += TimeZone.CurrentTimeZone.GetUtcOffset(returnTime);

				// We need to know if the date to compare is saving daylight or not to adjust the reference date
				if (TimeZone.CurrentTimeZone.IsDaylightSavingTime(DateTime.Now))
				{
					returnTime += new TimeSpan(1, 0, 0);
				}

				return returnTime;
			}
			else
			{
				throw new ArgumentException("The source time zone cannot be determined.");
			}
		}
#pragma warning restore CS0618 // Type or member is obsolete
	}
}
