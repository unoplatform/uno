#nullable enable

using System;
using System.Globalization;
using System.Text;

namespace Windows.Globalization.DateTimeFormatting;

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

	internal string Format(DateTimeOffset dateTime, DateTimeFormattingContext context)
	{
		var builder = new StringBuilder();
		Format(builder, dateTime, context);
		return builder.ToString();
	}

	internal void Format(StringBuilder builder, DateTimeOffset dateTime, DateTimeFormattingContext context)
	{
		OptionalPrefixLiteralText?.Format(builder, dateTime, context);
		DateTimeNode.Format(builder, dateTime, context);
		OptionalSuffixLiteralText?.Format(builder, dateTime, context);
		OptionalSuffixPattern?.Format(builder, dateTime, context);
	}

	// Legacy overloads for backward compatibility
	internal string Format(DateTimeOffset dateTime, CultureInfo culture, bool isTwentyFourHours)
	{
		var context = new DateTimeFormattingContext(
			culture,
			CalendarIdentifiers.Gregorian,
			isTwentyFourHours ? ClockIdentifiers.TwentyFourHour : ClockIdentifiers.TwelveHour);
		return Format(dateTime, context);
	}

	internal void Format(StringBuilder builder, DateTimeOffset dateTime, CultureInfo culture, bool isTwentyFourHours)
	{
		var context = new DateTimeFormattingContext(
			culture,
			CalendarIdentifiers.Gregorian,
			isTwentyFourHours ? ClockIdentifiers.TwentyFourHour : ClockIdentifiers.TwelveHour);
		Format(builder, dateTime, context);
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

	internal void Format(StringBuilder builder, DateTimeOffset dateTime, DateTimeFormattingContext context)
		=> builder.Append(Text);

	// Legacy overload for backward compatibility
	internal void Format(StringBuilder builder, DateTimeOffset dateTime, CultureInfo culture, bool isTwentyFourHours)
		=> builder.Append(Text);
}

// <datetime-pattern> ::= <era> | <year> | <month> | <day> | <dayofweek> |
//                        <period> | <hour> | <minute> | <second> | <timezone>
internal abstract class PatternDateTimeNode
{
	internal abstract void Format(StringBuilder builder, DateTimeOffset dateTime, DateTimeFormattingContext context);
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

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, DateTimeFormattingContext context)
	{
		context.Calendar.SetDateTime(dateTime);
		var eraString = IdealLength.HasValue
			? context.Calendar.EraAsString(IdealLength.Value)
			: context.Calendar.EraAsString();
		builder.Append(eraString);
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

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, DateTimeFormattingContext context)
	{
		context.Calendar.SetDateTime(dateTime);

		if (Kind == YearKind.Full)
		{
			if (IdealLength.HasValue)
			{
				builder.Append(context.Calendar.YearAsPaddedString(IdealLength.Value));
			}
			else
			{
				builder.Append(context.Calendar.YearAsString());
			}
		}
		else if (Kind == YearKind.Abbreviated)
		{
			if (IdealLength.HasValue)
			{
				builder.Append(context.Calendar.YearAsTruncatedString(IdealLength.Value));
			}
			else
			{
				builder.Append(context.Calendar.YearAsTruncatedString(2));
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

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, DateTimeFormattingContext context)
	{
		context.Calendar.SetDateTime(dateTime);

		switch (Kind)
		{
			case MonthKind.Full:
				builder.Append(context.Calendar.MonthAsString());
				break;

			case MonthKind.SoloFull:
				builder.Append(context.Calendar.MonthAsSoloString());
				break;

			case MonthKind.Abbreviated:
				builder.Append(IdealLength.HasValue
					? context.Calendar.MonthAsString(IdealLength.Value)
					: context.Calendar.MonthAsString(3));
				break;

			case MonthKind.SoloAbbreviated:
				builder.Append(IdealLength.HasValue
					? context.Calendar.MonthAsSoloString(IdealLength.Value)
					: context.Calendar.MonthAsSoloString(3));
				break;

			case MonthKind.Integer:
				builder.Append(IdealLength.HasValue
					? context.Calendar.MonthAsPaddedNumericString(IdealLength.Value)
					: context.Calendar.MonthAsNumericString());
				break;
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

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, DateTimeFormattingContext context)
	{
		context.Calendar.SetDateTime(dateTime);

		switch (Kind)
		{
			case DayOfWeekKind.Full:
				builder.Append(context.Calendar.DayOfWeekAsString());
				break;

			case DayOfWeekKind.SoloFull:
				builder.Append(context.Calendar.DayOfWeekAsSoloString());
				break;

			case DayOfWeekKind.Abbreviated:
				builder.Append(IdealLength.HasValue
					? context.Calendar.DayOfWeekAsString(IdealLength.Value)
					: context.Calendar.DayOfWeekAsString(3));
				break;

			case DayOfWeekKind.SoloAbbreviated:
				builder.Append(IdealLength.HasValue
					? context.Calendar.DayOfWeekAsSoloString(IdealLength.Value)
					: context.Calendar.DayOfWeekAsSoloString(3));
				break;
		}
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

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, DateTimeFormattingContext context)
	{
		context.Calendar.SetDateTime(dateTime);

		if (IdealLength.HasValue)
		{
			builder.Append(context.Calendar.DayAsPaddedString(IdealLength.Value));
		}
		else
		{
			builder.Append(context.Calendar.DayAsString());
		}
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

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, DateTimeFormattingContext context)
	{
		// For 24-hour clock, period should not be displayed
		if (!context.IsTwelveHourClock)
		{
			return;
		}

		context.Calendar.SetDateTime(dateTime);

		if (IdealLength.HasValue)
		{
			builder.Append(context.Calendar.PeriodAsString(IdealLength.Value));
		}
		else
		{
			builder.Append(context.Calendar.PeriodAsString());
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

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, DateTimeFormattingContext context)
	{
		context.Calendar.SetDateTime(dateTime);

		// The Calendar.Hour property already respects the clock setting (12/24 hour)
		if (IdealLength.HasValue)
		{
			builder.Append(context.Calendar.HourAsPaddedString(IdealLength.Value));
		}
		else
		{
			builder.Append(context.Calendar.HourAsString());
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

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, DateTimeFormattingContext context)
	{
		context.Calendar.SetDateTime(dateTime);

		if (IdealLength.HasValue)
		{
			builder.Append(context.Calendar.MinuteAsPaddedString(IdealLength.Value));
		}
		else
		{
			builder.Append(context.Calendar.MinuteAsString());
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

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, DateTimeFormattingContext context)
	{
		context.Calendar.SetDateTime(dateTime);

		if (IdealLength.HasValue)
		{
			builder.Append(context.Calendar.SecondAsPaddedString(IdealLength.Value));
		}
		else
		{
			builder.Append(context.Calendar.SecondAsString());
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

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, DateTimeFormattingContext context)
	{
		context.Calendar.SetDateTime(dateTime);

		if (Kind == TimeZoneKind.Full)
		{
			builder.Append(context.Calendar.TimeZoneAsString());
		}
		else if (Kind == TimeZoneKind.Abbreviated)
		{
			builder.Append(IdealLength.HasValue
				? context.Calendar.TimeZoneAsString(IdealLength.Value)
				: context.Calendar.TimeZoneAsString(3));
		}
	}
}

// <ideal-length> ::= "(" <non-zero-digit> ")"
// <non-zero-digit> ::= "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9"
