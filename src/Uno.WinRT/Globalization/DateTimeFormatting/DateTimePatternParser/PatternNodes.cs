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

	internal string Format(DateTimeOffset dateTime, CultureInfo culture, bool isTwentyFourHours)
	{
		var builder = new StringBuilder();
		Format(builder, dateTime, culture, isTwentyFourHours);
		return builder.ToString();
	}

	internal void Format(StringBuilder builder, DateTimeOffset dateTime, CultureInfo culture, bool isTwentyFourHours)
	{
		OptionalPrefixLiteralText?.Format(builder, dateTime, culture, isTwentyFourHours);
		DateTimeNode.Format(builder, dateTime, culture, isTwentyFourHours);
		OptionalSuffixLiteralText?.Format(builder, dateTime, culture, isTwentyFourHours);
		OptionalSuffixPattern?.Format(builder, dateTime, culture, isTwentyFourHours);
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

	internal void Format(StringBuilder builder, DateTimeOffset dateTime, CultureInfo culture, bool isTwentyFourHours)
		=> builder.Append(Text);
}

// <datetime-pattern> ::= <era> | <year> | <month> | <day> | <dayofweek> |
//                        <period> | <hour> | <minute> | <second> | <timezone>
internal abstract class PatternDateTimeNode
{
	internal abstract void Format(StringBuilder builder, DateTimeOffset dateTime, CultureInfo culture, bool isTwentyFourHours);
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

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, CultureInfo culture, bool isTwentyFourHours)
	{
		// C# can't represent negative dates (i.e, before christ).
		// So, looks like Era will always be "AD" (Anno Domini).
		builder.Append(IdealLength.HasValue && IdealLength.Value < 4 ? "AD" : "A.D.");
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

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, CultureInfo culture, bool isTwentyFourHours)
	{
		if (Kind == YearKind.Full)
		{
			if (IdealLength.HasValue)
			{
				builder.Append(dateTime.Year.ToString(new string('0', IdealLength.Value), culture));
			}
			else
			{
				builder.Append(dateTime.Year);
			}
		}
		else if (Kind == YearKind.Abbreviated)
		{
			if (IdealLength.HasValue)
			{
				// TODO: This might not always be the right approach for all calendars.
				// This implementation assumes gregorian calendar. The "correct" approach isn't yet known.
				builder.Append((dateTime.Year % (10 * IdealLength.Value)).ToString(new string('0', IdealLength.Value), culture));
			}
			else
			{
				builder.Append((dateTime.Year % 100).ToString("00", culture));
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

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, CultureInfo culture, bool isTwentyFourHours)
	{
		if (Kind == MonthKind.Full)
		{
			builder.Append(dateTime.ToString("MMMM", culture));
		}
		else if (Kind == MonthKind.SoloFull)
		{
			builder.Append(dateTime.ToString("MMMM", culture));
		}
		else if (Kind == MonthKind.Abbreviated)
		{
			builder.Append(dateTime.ToString("MMM", culture));
		}
		else if (Kind == MonthKind.SoloAbbreviated)
		{
			builder.Append(dateTime.ToString("MMM", culture));
		}
		else if (Kind == MonthKind.Integer)
		{
			if (IdealLength.HasValue)
			{
				var idealLength = IdealLength.Value;
				if (idealLength >= 2)
				{
					builder.Append(dateTime.ToString("MM", culture));
				}
				else
				{
					builder.Append(dateTime.Month);
				}
			}
			else
			{
				builder.Append(dateTime.Month);
			}
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

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, CultureInfo culture, bool isTwentyFourHours)
	{
		if (Kind == DayOfWeekKind.Full)
		{
			builder.Append(dateTime.ToString("dddd", culture));
		}
		else if (Kind == DayOfWeekKind.SoloFull)
		{
			builder.Append(dateTime.ToString("dddd", culture));
		}
		else if (Kind == DayOfWeekKind.Abbreviated)
		{
			builder.Append(dateTime.ToString("ddd", culture));
		}
		else if (Kind == DayOfWeekKind.SoloAbbreviated)
		{
			builder.Append(dateTime.ToString("ddd", culture));
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

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, CultureInfo culture, bool isTwentyFourHours)
	{
		if (IdealLength.HasValue)
		{
			builder.Append(dateTime.Day.ToString(new string('0', IdealLength.Value), culture));
		}
		else
		{
			builder.Append(dateTime.Day);
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

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, CultureInfo culture, bool isTwentyFourHours)
	{
		if (isTwentyFourHours)
		{
			return;
		}

		string period = dateTime.ToString("tt", culture);
		if (IdealLength.HasValue && IdealLength.Value < period.Length)
		{
			builder.Append(period.Substring(0, IdealLength.Value));
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

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, CultureInfo culture, bool isTwentyFourHours)
	{
		int hour = dateTime.Hour;
		if (!isTwentyFourHours && hour > 12)
		{
			hour = hour - 12;
		}

		if (IdealLength.HasValue)
		{
			builder.Append(hour.ToString(new string('0', IdealLength.Value), culture));
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

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, CultureInfo culture, bool isTwentyFourHours)
	{
		if (IdealLength.HasValue)
		{
			builder.Append(dateTime.Minute.ToString(new string('0', IdealLength.Value), culture));
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

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, CultureInfo culture, bool isTwentyFourHours)
	{
		if (IdealLength.HasValue)
		{
			builder.Append(dateTime.Second.ToString(new string('0', IdealLength.Value), culture));
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

	internal override void Format(StringBuilder builder, DateTimeOffset dateTime, CultureInfo culture, bool isTwentyFourHours)
	{
		// Important: dateTime parameter shouldn't be used here.
		// WinUI uses the local time zone info.
		// The offset part of DateTimeOffset will actually be lost when marshalling to WinUI.
		// The marshalling of DateTimeOffset to C++ is just a simple "UniversalTime" that doesn't have
		// information about time zones. It's simply UtcTicks - ManagedUtcTicksAtNativeZero (which is 504911232000000000)
		if (Kind == TimeZoneKind.Full)
		{
			builder.Append(TimeZoneInfo.Local.StandardName);
		}
		else if (Kind == TimeZoneKind.Abbreviated)
		{
			// Note: Couldn't notice any behavior difference on WinUI using different values of ideal length.
			// So it's unused for now until it's known how the value should be used and how it affects WinUI behavior.
			builder.Append("GMT+");
			builder.Append(TimeZoneInfo.Local.BaseUtcOffset.TotalHours);
		}
	}
}

// <ideal-length> ::= "(" <non-zero-digit> ")"
// <non-zero-digit> ::= "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9"
