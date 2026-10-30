#nullable enable

using System;
using System.Globalization;
using System.Text;
using _Calendar = global::System.Globalization.Calendar;

namespace Windows.Globalization.DateTimeFormatting;

// What a formatter's pattern reads, built once per formatter.
internal sealed class PatternFormatContext
{
	public PatternFormatContext(_Calendar calendar, CultureInfo dateCulture, CultureInfo timeCulture, bool isTwentyFourHours)
	{
		Calendar = calendar;
		DateCulture = dateCulture;
		TimeCulture = timeCulture;
		IsTwentyFourHours = isTwentyFourHours;
	}

	// Numbers the date parts.
	public _Calendar Calendar { get; }

	// Names the date parts in the calendar in use (Calendar.ResolveFormatCulture).
	public CultureInfo DateCulture { get; }

	// The requested language, which formats the time parts and names the period.
	public CultureInfo TimeCulture { get; }

	public bool IsTwentyFourHours { get; }
}

// <pattern> ::= [<literal-text>] <datetime-pattern> [<literal-text>] |
//               [<literal-text>] <datetime-pattern> <pattern>
internal sealed class PatternRootNode
{
	public PatternRootNode(
		PatternLiteralTextNode? optionalPrefixLiteralText,
		PatternDateTimeNode dateTimeNode,
		PatternLiteralTextNode? optionalSuffixLiteralText)
	{
		OptionalPrefixLiteralText = optionalPrefixLiteralText;
		DateTimeNode = dateTimeNode;
		OptionalSuffixLiteralText = optionalSuffixLiteralText;
	}

	public PatternRootNode(
		PatternLiteralTextNode? optionalPrefixLiteralText,
		PatternDateTimeNode dateTimeNode,
		PatternRootNode? optionalSuffixPattern)
	{
		OptionalPrefixLiteralText = optionalPrefixLiteralText;
		DateTimeNode = dateTimeNode;
		OptionalSuffixPattern = optionalSuffixPattern;
	}

	public PatternLiteralTextNode? OptionalPrefixLiteralText { get; }
	public PatternDateTimeNode DateTimeNode { get; }

	// Either both are null, or only one is null.
	// The two cannot be non-null at the same time
	public PatternLiteralTextNode? OptionalSuffixLiteralText { get; }
	public PatternRootNode? OptionalSuffixPattern { get; }

	internal string Format(DateTimeOffset dateTime, PatternFormatContext context)
	{
		var builder = new StringBuilder();
		Format(builder, dateTime, context);
		return builder.ToString();
	}

	internal void Format(StringBuilder builder, DateTimeOffset dateTime, PatternFormatContext context)
	{
		OptionalPrefixLiteralText?.Format(builder, dateTime, context);
		DateTimeNode.Format(builder, dateTime, context);
		OptionalSuffixLiteralText?.Format(builder, dateTime, context);
		OptionalSuffixPattern?.Format(builder, dateTime, context);
	}
}

// <literal-text> ::= <literal-character>+
// <literal-character> ::= [^{}] | "{openbrace}" | "{closebrace}"
internal sealed class PatternLiteralTextNode
{
	public PatternLiteralTextNode(string text)
	{
		Text = text;
	}

	public string Text { get; }

	internal void Format(StringBuilder builder, DateTimeOffset dateTime, PatternFormatContext context)
		=> builder.Append(Text);
}

// <datetime-pattern> ::= <era> | <year> | <month> | <day> | <dayofweek> |
//                        <period> | <hour> | <minute> | <second> | <timezone>
internal abstract class PatternDateTimeNode
{
	internal abstract void Format(StringBuilder builder, DateTimeOffset dateTime, PatternFormatContext context);
}

// <era> ::= "{era.abbreviated" [<ideal-length>] "}"
internal sealed class PatternEraNode : PatternDateTimeNode
{
	public PatternEraNode(int? idealLength)
	{
		IdealLength = idealLength;
	}

	// Era is always "era.abbreviated", so no need to distinguish.
	public int? IdealLength { get; }

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, PatternFormatContext context)
	{
		// The era of the calendar in use, named by the culture that names its dates. As in WinRT, nothing when that
		// culture has no names for the calendar (Julian, or Korean for en-US), and an ideal length shorter than the
		// name takes the abbreviated one (令 for 令和). .NET's abbreviated Gregorian eras are ICU's narrow ones ("A"),
		// which WinRT never shows.
		var format = context.DateCulture.DateTimeFormat;
		if (format.Calendar.GetType() != context.Calendar.GetType())
		{
			return;
		}

		var era = context.Calendar.GetEra(dateTime.DateTime);
		var name = format.GetEraName(era);
		builder.Append(IdealLength < name.Length && context.Calendar is not GregorianCalendar ? format.GetAbbreviatedEraName(era) : name);
	}
}

// <year> ::= "{year.full" [<ideal-length>] "}" |
//           "{year.abbreviated" [<ideal-length>] "}" |
internal sealed class PatternYearNode : PatternDateTimeNode
{
	internal enum YearKind
	{
		Full,
		Abbreviated
	}

	public PatternYearNode(YearKind kind, int? idealLength)
	{
		Kind = kind;
		IdealLength = idealLength;
	}

	public YearKind Kind { get; }
	public int? IdealLength { get; }

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, PatternFormatContext context)
	{
		var year = context.Calendar.GetYear(dateTime.DateTime);
		if (Kind == YearKind.Full)
		{
			builder.Append(IdealLength is { } idealLength
				? year.ToString(new string('0', idealLength), context.DateCulture)
				: year.ToString(context.DateCulture));
		}
		else if (Kind == YearKind.Abbreviated)
		{
			if (IdealLength is { } idealLength)
			{
				builder.Append(Calendar.GetTruncatedYear(year, idealLength, context.DateCulture));
			}
			else
			{
				// As in WinRT, the last two digits, unpadded: "24", and "6" for Reiwa 6.
				var digits = year.ToString(context.DateCulture);
				builder.Append(digits, Math.Max(0, digits.Length - 2), Math.Min(2, digits.Length));
			}
		}
	}
}

// <month> ::= "{month.full}" |
//             "{month.solo.full}" |
//             "{month.abbreviated" [<ideal-length>] "}"
//             "{month.solo.abbreviated" [<ideal-length>] "}"
//             "{month.integer" [<ideal-length>] "}"
internal sealed class PatternMonthNode : PatternDateTimeNode
{
	internal enum MonthKind
	{
		Full,
		SoloFull,
		Abbreviated,
		SoloAbbreviated,
		Integer,
	}

	public PatternMonthNode(MonthKind kind, int? idealLength = null)
	{
		Kind = kind;
		IdealLength = idealLength;
	}

	public MonthKind Kind { get; }

	// Always null for full and solo.full
	public int? IdealLength { get; }

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, PatternFormatContext context)
	{
		if (Kind is MonthKind.Full or MonthKind.SoloFull)
		{
			builder.Append(Calendar.GetMonthName(dateTime.DateTime, context.Calendar, context.DateCulture, abbreviated: false, inDate: Kind == MonthKind.Full));
		}
		else if (Kind is MonthKind.Abbreviated or MonthKind.SoloAbbreviated)
		{
			builder.Append(Calendar.GetMonthName(dateTime.DateTime, context.Calendar, context.DateCulture, abbreviated: true, inDate: Kind == MonthKind.Abbreviated));
		}
		else if (Kind == MonthKind.Integer)
		{
			var month = context.Calendar.GetMonth(dateTime.DateTime);
			builder.Append(IdealLength is { } idealLength
				? month.ToString(new string('0', idealLength), context.DateCulture)
				: month.ToString(context.DateCulture));
		}
	}
}

// <dayofweek> ::= "{dayofweek.full}" |
//                 "{dayofweek.solo.full}" |
//                 "{dayofweek.abbreviated" [<ideal-length>] "}"
//                 "{dayofweek.solo.abbreviated" [<ideal-length>] "}"
internal sealed class PatternDayOfWeekNode : PatternDateTimeNode
{
	internal enum DayOfWeekKind
	{
		Full,
		SoloFull,
		Abbreviated,
		SoloAbbreviated,
	}

	public PatternDayOfWeekNode(DayOfWeekKind kind, int? idealLength = null)
	{
		Kind = kind;
		IdealLength = idealLength;
	}

	public DayOfWeekKind Kind { get; }

	// Always null for full and solo.full
	public int? IdealLength { get; }

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, PatternFormatContext context)
	{
		var format = context.DateCulture.DateTimeFormat;
		builder.Append(Kind is DayOfWeekKind.Full or DayOfWeekKind.SoloFull
			? format.GetDayName(dateTime.DayOfWeek)
			: format.GetAbbreviatedDayName(dateTime.DayOfWeek));
	}
}

// <day> ::= "{day.integer" [<ideal-length>] "}"
internal sealed class PatternDayNode : PatternDateTimeNode
{
	public PatternDayNode(int? idealLength)
	{
		IdealLength = idealLength;
	}

	public int? IdealLength { get; }

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, PatternFormatContext context)
	{
		var day = context.Calendar.GetDayOfMonth(dateTime.DateTime);
		builder.Append(IdealLength is { } idealLength
			? day.ToString(new string('0', idealLength), context.DateCulture)
			: day.ToString(context.DateCulture));
	}
}

// <period> ::= "{period.abbreviated" [<ideal-length>] "}"
internal sealed class PatternPeriodNode : PatternDateTimeNode
{
	public PatternPeriodNode(int? idealLength)
	{
		IdealLength = idealLength;
	}

	public int? IdealLength { get; }

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, PatternFormatContext context)
	{
		if (context.IsTwentyFourHours)
		{
			return;
		}

		// The ideal length is a hint, so designators like "p.m." or "μ.μ." stay whole. Only the narrow
		// form, which .NET doesn't provide, is approximated by the first character.
		var format = context.TimeCulture.DateTimeFormat;
		string period = dateTime.Hour < 12 ? format.AMDesignator : format.PMDesignator;
		if (IdealLength == 1 && period.Length > 1)
		{
			builder.Append(period[0]);
		}
		else
		{
			builder.Append(period);
		}
	}
}

// <hour> ::= "{hour.integer" [<ideal-length>] "}"
internal sealed class PatternHourNode : PatternDateTimeNode
{
	public PatternHourNode(int? idealLength)
	{
		IdealLength = idealLength;
	}

	public int? IdealLength { get; }

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, PatternFormatContext context)
	{
		int hour = dateTime.Hour;
		if (!context.IsTwentyFourHours)
		{
			// Midnight and noon are 12 on a 12-hour clock.
			hour = hour % 12 == 0 ? 12 : hour % 12;
		}

		if (IdealLength.HasValue)
		{
			builder.Append(hour.ToString(new string('0', IdealLength.Value), context.TimeCulture));
		}
		else
		{
			builder.Append(hour);
		}
	}
}

// <minute> ::= "{minute.integer" [<ideal-length>] "}"
internal sealed class PatternMinuteNode : PatternDateTimeNode
{
	public PatternMinuteNode(int? idealLength)
	{
		IdealLength = idealLength;
	}

	public int? IdealLength { get; }

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, PatternFormatContext context)
	{
		if (IdealLength.HasValue)
		{
			builder.Append(dateTime.Minute.ToString(new string('0', IdealLength.Value), context.TimeCulture));
		}
		else
		{
			builder.Append(dateTime.Minute);
		}
	}
}

// <second> ::= "{second.integer" [<ideal-length>] "}"
internal sealed class PatternSecondNode : PatternDateTimeNode
{
	public PatternSecondNode(int? idealLength)
	{
		IdealLength = idealLength;
	}

	public int? IdealLength { get; }

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, PatternFormatContext context)
	{
		if (IdealLength.HasValue)
		{
			builder.Append(dateTime.Second.ToString(new string('0', IdealLength.Value), context.TimeCulture));
		}
		else
		{
			builder.Append(dateTime.Second);
		}
	}
}

// <timezone> ::= "{timezone.full}" |
//                 "{timezone.abbreviated" [<ideal-length>] "}"
internal sealed class PatternTimeZoneNode : PatternDateTimeNode
{
	internal enum TimeZoneKind
	{
		Full,
		Abbreviated,
	}

	public PatternTimeZoneNode(TimeZoneKind kind, int? idealLength = null)
	{
		Kind = kind;
		IdealLength = idealLength;
	}

	public TimeZoneKind Kind { get; }

	// Always null for full
	public int? IdealLength { get; }

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, PatternFormatContext context)
	{
		// The local time zone at that instant, as the value comes in local time: its standard or daylight name
		// ("Eastern Daylight Time" in July). .NET has only those names, so the abbreviated one is the GMT offset WinRT
		// writes when the language has no short name for the zone ("GMT-4", "GMT+5:30"), whatever the ideal length.
		var zone = TimeZoneInfo.Local;
		if (Kind == TimeZoneKind.Full)
		{
			builder.Append(zone.IsDaylightSavingTime(dateTime) ? zone.DaylightName : zone.StandardName);
			return;
		}

		builder.Append("GMT");
		var offset = zone.GetUtcOffset(dateTime);
		if (offset != TimeSpan.Zero)
		{
			builder.Append(offset < TimeSpan.Zero ? '-' : '+');
			offset = offset.Duration();
			builder.Append(offset.Hours.ToString(CultureInfo.InvariantCulture));
			if (offset.Minutes != 0)
			{
				builder.Append(':');
				builder.Append(offset.Minutes.ToString("00", CultureInfo.InvariantCulture));
			}
		}
	}
}

// <ideal-length> ::= "(" <non-zero-digit> ")"
// <non-zero-digit> ::= "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9"
