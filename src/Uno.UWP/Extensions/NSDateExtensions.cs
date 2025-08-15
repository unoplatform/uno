using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation;

namespace Windows.Extensions;

internal static class NSDateExtensions
{
	private static readonly DateTimeOffset NSDateConversionStart =
		new DateTimeOffset(2001, 1, 1, 0, 0, 0, TimeSpan.Zero);

	public static DateTimeOffset ToDateTimeOffset(this NSDate nsDate)
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

	public static NSDate ToNSDate(this DateTimeOffset dateTimeOffset)
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
