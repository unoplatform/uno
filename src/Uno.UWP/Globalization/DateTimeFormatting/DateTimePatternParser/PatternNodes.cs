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
		// Use Calendar.EraAsString() to get proper era names for different calendar systems
		var calendar = new Calendar([culture.Name]);
		calendar.SetDateTime(dateTime);
		var eraString = IdealLength.HasValue ? calendar.EraAsString(IdealLength.Value) : calendar.EraAsString();
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
			// Genitive form (used with day numbers, e.g., "5 января" in Russian)
			builder.Append(culture.DateTimeFormat.GetMonthName(dateTime.Month));
		}
		else if (Kind == MonthKind.SoloFull)
		{
			// Nominative/standalone form (used alone, e.g., "Январь" in Russian)
			// Use MonthGenitiveNames if available and different from MonthNames
			var soloName = GetSoloMonthName(culture, dateTime.Month, abbreviated: false);
			builder.Append(soloName);
		}
		else if (Kind == MonthKind.Abbreviated)
		{
			// Genitive abbreviated form
			builder.Append(culture.DateTimeFormat.GetAbbreviatedMonthName(dateTime.Month));
		}
		else if (Kind == MonthKind.SoloAbbreviated)
		{
			// Nominative/standalone abbreviated form
			var soloName = GetSoloMonthName(culture, dateTime.Month, abbreviated: true);
			if (IdealLength.HasValue && IdealLength.Value < soloName.Length)
			{
				builder.Append(soloName.Substring(0, IdealLength.Value));
			}
			else
			{
				builder.Append(soloName);
			}
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

	/// <summary>
	/// Gets the standalone/nominative month name for cultures that distinguish between
	/// genitive and nominative forms (like Slavic languages).
	/// </summary>
	private static string GetSoloMonthName(CultureInfo culture, int month, bool abbreviated)
	{
		// Some cultures have different standalone (nominative) month names
		// .NET provides MonthGenitiveNames which contains genitive forms
		// The regular MonthNames array contains nominative forms
		// However, the MMMM format uses genitive when followed by day

		// For standalone display, we want the nominative form
		// In .NET, AbbreviatedMonthNames and MonthNames are typically nominative
		// while AbbreviatedMonthGenitiveNames and MonthGenitiveNames are genitive

		if (abbreviated)
		{
			return culture.DateTimeFormat.AbbreviatedMonthNames[month - 1];
		}
		else
		{
			return culture.DateTimeFormat.MonthNames[month - 1];
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
			builder.Append(culture.DateTimeFormat.GetDayName(dateTime.DayOfWeek));
		}
		else if (Kind == DayOfWeekKind.SoloFull)
		{
			// Standalone/nominative form for day of week
			// In most languages, day names don't have genitive/nominative distinction
			// but we use DayNames array directly to be consistent
			builder.Append(culture.DateTimeFormat.DayNames[(int)dateTime.DayOfWeek]);
		}
		else if (Kind == DayOfWeekKind.Abbreviated)
		{
			builder.Append(culture.DateTimeFormat.GetAbbreviatedDayName(dateTime.DayOfWeek));
		}
		else if (Kind == DayOfWeekKind.SoloAbbreviated)
		{
			var name = culture.DateTimeFormat.AbbreviatedDayNames[(int)dateTime.DayOfWeek];
			if (IdealLength.HasValue && IdealLength.Value < name.Length)
			{
				builder.Append(name.Substring(0, IdealLength.Value));
			}
			else
			{
				builder.Append(name);
			}
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
		if (!isTwentyFourHours)
		{
			// Convert to 12-hour format
			// Midnight (0) -> 12, 1-11 stay same, 12 (noon) -> 12, 13-23 -> 1-11
			if (hour == 0)
			{
				hour = 12;
			}
			else if (hour > 12)
			{
				hour = hour - 12;
			}
			// hours 1-12 stay as is
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
		// Use the offset from the DateTimeOffset to determine the timezone display
		// This allows Format(datetime, timeZoneId) to work correctly
		var offset = dateTime.Offset;

		if (Kind == TimeZoneKind.Full)
		{
			// Try to get timezone name from the offset
			// First check if it matches local timezone
			if (offset == TimeZoneInfo.Local.BaseUtcOffset ||
				(TimeZoneInfo.Local.IsDaylightSavingTime(dateTime) && offset == TimeZoneInfo.Local.GetUtcOffset(dateTime)))
			{
				builder.Append(TimeZoneInfo.Local.IsDaylightSavingTime(dateTime)
					? TimeZoneInfo.Local.DaylightName
					: TimeZoneInfo.Local.StandardName);
			}
			else
			{
				// Fall back to GMT offset format for non-local timezones
				FormatGmtOffset(builder, offset);
			}
		}
		else if (Kind == TimeZoneKind.Abbreviated)
		{
			FormatGmtOffset(builder, offset);
		}
	}

	private static void FormatGmtOffset(StringBuilder builder, TimeSpan offset)
	{
		builder.Append("GMT");
		if (offset >= TimeSpan.Zero)
		{
			builder.Append('+');
		}

		var hours = (int)offset.TotalHours;
		var minutes = Math.Abs(offset.Minutes);

		builder.Append(hours);
		if (minutes > 0)
		{
			builder.Append(':');
			builder.Append(minutes.ToString("00"));
		}
	}
}

// <ideal-length> ::= "(" <non-zero-digit> ")"
// <non-zero-digit> ::= "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9"
