#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Uno;
using Uno.Extensions;

namespace Windows.Globalization.DateTimeFormatting;

/// <summary>
/// Formats dates and times.
/// </summary>
public sealed partial class DateTimeFormatter
{
	private readonly CultureInfo _firstCulture;

	private readonly PatternRootNode _patternRootNode;

	/// <summary>
	/// Creates a DateTimeFormatter object that is initialized by a format template string.
	/// </summary>
	/// <param name="formatTemplate">
	/// A format template string that specifies the requested components. 
	/// The order of the components is irrelevant. 
	/// This can also be a format pattern.
	/// </param>
	public DateTimeFormatter(string formatTemplate)
		: this(formatTemplate, languages: null)
	{
	}

	/// <summary>
	/// Creates a DateTimeFormatter object that is initialized by a format template string.
	/// </summary>
	/// <param name="formatTemplate">
	/// A format template string that specifies the requested components. 
	/// The order of the components is irrelevant. 
	/// This can also be a format pattern.
	/// </param>
	/// <param name="languages">
	/// The list of language identifiers, in priority order, that represent the choice of languages.
	/// </param>
	/// <exception cref="ArgumentNullException"></exception>
	public DateTimeFormatter(
		string formatTemplate,
		IEnumerable<string>? languages)
	{
		ArgumentNullException.ThrowIfNull(formatTemplate);

		if (languages != null)
		{
			Languages = languages.Distinct().ToArray();
		}

		_firstCulture = new CultureInfo(Languages[0]);

		try
		{
			// Template example:
			// "year month day dayofweek hour timezone" (that's just an example)
			var templateParser = new TemplateParser(formatTemplate);
			templateParser.Parse();
			IncludeYear = templateParser.Info.IncludeYear;
			IncludeMonth = templateParser.Info.IncludeMonth;
			IncludeDay = templateParser.Info.IncludeDay;
			IncludeDayOfWeek = templateParser.Info.IncludeDayOfWeek;
			IncludeHour = templateParser.Info.IncludeHour;
			IncludeMinute = templateParser.Info.IncludeMinute;
			IncludeSecond = templateParser.Info.IncludeSecond;
			IncludeTimeZone = templateParser.Info.IncludeTimeZone;
			IsShortDate = templateParser.Info.IsShortDate;
			IsLongDate = templateParser.Info.IsLongDate;
			IsShortTime = templateParser.Info.IsShortTime;
			IsShortDate = templateParser.Info.IsShortDate;

			// NOTE: We intentionally don't set the user provided template.
			// Instead, we parse and re-build the template string.
			// That's how WinUI works.
			// Basically, the order of tokens in the template string does NOT matter.
			// So, this kinda normalizes the order the right way.
			Template = BuildTemplate();

			string patternBuiltFromTemplate = BuildPattern();
			Patterns = [patternBuiltFromTemplate];
			_patternRootNode = new PatternParser(patternBuiltFromTemplate).Parse();
		}
		catch (Exception ex)
		{
			try
			{
				// Pattern example:
				// "Hello {year.full} Hello2 {month.full}" (that's just an example)
				_patternRootNode = new PatternParser(formatTemplate).Parse();
				Template = formatTemplate;
				Patterns = [formatTemplate];
			}
			catch (Exception ex2)
			{
				throw new AggregateException(ex, ex2);
			}
		}

		var calendar = new Calendar(Languages);
		Calendar = calendar.GetCalendarSystem();
		Clock = calendar.GetClock();

		// TODO:MZ:
		GeographicRegion = "ZZ";
	}

	public DateTimeFormatter(
		string formatTemplate,
		IEnumerable<string> languages,
		string geographicRegion,
		string calendar,
		string clock)
		: this(formatTemplate, languages)
	{
		GeographicRegion = geographicRegion;
		Calendar = calendar;
		Clock = clock;
	}

	public DateTimeFormatter(
		YearFormat yearFormat,
		MonthFormat monthFormat,
		DayFormat dayFormat,
		DayOfWeekFormat dayOfWeekFormat)
	{
		IncludeYear = yearFormat;
		IncludeMonth = monthFormat;
		IncludeDay = dayFormat;
		IncludeDayOfWeek = dayOfWeekFormat;
		Template = BuildTemplate();
		string patternBuiltFromTemplate = BuildPattern();
		Patterns = [patternBuiltFromTemplate];
		_patternRootNode = new PatternParser(patternBuiltFromTemplate).Parse();
		_firstCulture = new CultureInfo(Languages[0]);

		// TODO:MZ:
		Calendar = CalendarIdentifiers.Gregorian;
		Clock = ClockIdentifiers.TwentyFourHour;
		GeographicRegion = "ZZ";
	}

	public DateTimeFormatter(
		HourFormat hourFormat,
		MinuteFormat minuteFormat,
		SecondFormat secondFormat)
	{
		IncludeHour = hourFormat;
		IncludeMinute = minuteFormat;
		IncludeSecond = secondFormat;
		Template = BuildTemplate();
		string patternBuiltFromTemplate = BuildPattern();
		Patterns = [patternBuiltFromTemplate];
		_patternRootNode = new PatternParser(patternBuiltFromTemplate).Parse();
		_firstCulture = new CultureInfo(Languages[0]);

		// TODO:MZ:
		Calendar = CalendarIdentifiers.Gregorian;
		Clock = ClockIdentifiers.TwentyFourHour;
		GeographicRegion = "ZZ";
	}

	public DateTimeFormatter(
		YearFormat yearFormat,
		MonthFormat monthFormat,
		DayFormat dayFormat,
		DayOfWeekFormat dayOfWeekFormat,
		HourFormat hourFormat,
		MinuteFormat minuteFormat,
		SecondFormat secondFormat,
		IEnumerable<string> languages)
	{
		IncludeYear = yearFormat;
		IncludeMonth = monthFormat;
		IncludeDay = dayFormat;
		IncludeDayOfWeek = dayOfWeekFormat;
		IncludeHour = hourFormat;
		IncludeMinute = minuteFormat;
		IncludeSecond = secondFormat;
		Languages = languages.ToArray();
		Template = BuildTemplate();
		string patternBuiltFromTemplate = BuildPattern();
		Patterns = [patternBuiltFromTemplate];
		_patternRootNode = new PatternParser(patternBuiltFromTemplate).Parse();
		_firstCulture = new CultureInfo(Languages[0]);

		// TODO:MZ:
		Calendar = CalendarIdentifiers.Gregorian;
		Clock = ClockIdentifiers.TwentyFourHour;
		GeographicRegion = "ZZ";
	}

	public DateTimeFormatter(
		YearFormat yearFormat,
		MonthFormat monthFormat,
		DayFormat dayFormat,
		DayOfWeekFormat dayOfWeekFormat,
		HourFormat hourFormat,
		MinuteFormat minuteFormat,
		SecondFormat secondFormat,
		IEnumerable<string> languages,
		string geographicRegion,
		string calendar,
		string clock)
	{
		IncludeYear = yearFormat;
		IncludeMonth = monthFormat;
		IncludeDay = dayFormat;
		IncludeDayOfWeek = dayOfWeekFormat;
		IncludeHour = hourFormat;
		IncludeMinute = minuteFormat;
		IncludeSecond = secondFormat;
		Languages = languages.ToArray();
		GeographicRegion = geographicRegion;
		Calendar = calendar;
		Clock = clock;
		Template = BuildTemplate();
		string patternBuiltFromTemplate = BuildPattern();
		Patterns = [patternBuiltFromTemplate];
		_patternRootNode = new PatternParser(patternBuiltFromTemplate).Parse();
		_firstCulture = new CultureInfo(Languages[0]);
	}

	/// <summary>
	/// Gets the calendar that is used when formatting dates.
	/// </summary>
	public string Calendar { get; }

	/// <summary>
	/// Gets the clock that is used when formatting times.
	/// </summary>
	public string Clock { get; }

	/// <summary>
	/// Gets the region that is used when formatting dates and times.
	/// </summary>
	public string GeographicRegion { get; }

	/// <summary>
	/// Gets the DayFormat in the template.
	/// </summary>
	public DayFormat IncludeDay { get; }

	/// <summary>
	/// Gets the DayOfWeekFormat in the template.
	/// </summary>
	public DayOfWeekFormat IncludeDayOfWeek { get; }

	/// <summary>
	/// Gets the HourFormat in the template.
	/// </summary>
	public HourFormat IncludeHour { get; }

	/// <summary>
	/// Gets the MinuteFormat in the template.
	/// </summary>
	public MinuteFormat IncludeMinute { get; }

	/// <summary>
	/// Gets the MonthFormat in the template.
	/// </summary>
	public MonthFormat IncludeMonth { get; }

	/// <summary>
	/// Gets the SecondFormat in the template.
	/// </summary>
	public SecondFormat IncludeSecond { get; }

	/// <summary>
	/// Gets the YearFormat in the template.
	/// </summary>
	public YearFormat IncludeYear { get; }

	internal TimeZoneFormat IncludeTimeZone { get; }

	internal bool IsShortTime { get; }

	internal bool IsLongTime { get; }

	internal bool IsShortDate { get; }

	internal bool IsLongDate { get; }

	/// <summary>
	/// Gets the priority list of language identifiers that is used when formatting dates and times.
	/// </summary>
	public IReadOnlyList<string> Languages { get; } = ApplicationLanguages.Languages;

	/// <summary>
	/// Gets the DateTimeFormatter object that formats dates according to the user's choice of long date pattern.
	/// </summary>
	public static DateTimeFormatter LongDate { get; } = new DateTimeFormatter("longdate");

	/// <summary>
	/// Gets the DateTimeFormatter object that formats times according to the user's choice of long time pattern.
	/// </summary>
	public static DateTimeFormatter LongTime { get; } = new DateTimeFormatter("longtime");

	/// <summary>
	/// Gets or sets the numbering system that is used to format dates and times.
	/// </summary>
	public string? NumeralSystem { get; set; }

	/// <summary>
	/// Gets the patterns corresponding to this template that are used when formatting dates and times.
	/// </summary>
	public IReadOnlyList<string> Patterns { get; }

	/// <summary>
	/// Gets the geographic region that was most recently used to format dates and times.
	/// </summary>
	public string? ResolvedGeographicRegion { get; }

	/// <summary>
	/// Gets the language that was most recently used to format dates and times.
	/// </summary>
	public string? ResolvedLanguage { get; }

	/// <summary>
	/// Gets the DateTimeFormatter object that formats dates according to the user's choice of short date pattern.
	/// </summary>
	public static DateTimeFormatter ShortDate { get; } = new DateTimeFormatter("shortdate");

	/// <summary>
	/// Gets the DateTimeFormatter object that formats times according to the user's choice of short time pattern.
	/// </summary>
	public static DateTimeFormatter ShortTime { get; } = new DateTimeFormatter("shorttime");

	/// <summary>
	/// Gets a string representation of this format template.
	/// </summary>
	public string Template { get; }

	public string Format(DateTimeOffset value)
	{
		try
		{
			return _patternRootNode.Format(value, _firstCulture, isTwentyFourHours: Clock == ClockIdentifiers.TwentyFourHour);
		}
		catch (Exception e)
		{
			return Template + " : " + e.Message;
		}
	}

	[NotImplemented]
	public string Format(DateTimeOffset datetime, string timeZoneId)
	{
		throw new NotSupportedException();
	}

	private static string ToTemplateString(YearFormat yearFormat)
		=> yearFormat switch
		{
			YearFormat.None => string.Empty,
			YearFormat.Default => "year",
			YearFormat.Full => "year.full",
			YearFormat.Abbreviated => "year.abbreviated",
			_ => throw new ArgumentOutOfRangeException(nameof(yearFormat)),
		};

	private static string ToTemplateString(MonthFormat monthFormat)
		=> monthFormat switch
		{
			MonthFormat.None => string.Empty,
			MonthFormat.Default => "month",
			MonthFormat.Abbreviated => "month.abbreviated",
			MonthFormat.Full => "month.full",
			MonthFormat.Numeric => "month.integer",
			_ => throw new ArgumentOutOfRangeException(nameof(monthFormat)),
		};

	private static string ToTemplateString(DayFormat dayFormat)
		=> dayFormat switch
		{
			DayFormat.None => string.Empty,
			DayFormat.Default => "day",
			_ => throw new ArgumentOutOfRangeException(nameof(dayFormat)),
		};

	private static string ToTemplateString(DayOfWeekFormat dayOfWeekFormat)
		=> dayOfWeekFormat switch
		{
			DayOfWeekFormat.None => string.Empty,
			DayOfWeekFormat.Default => "dayofweek",
			DayOfWeekFormat.Abbreviated => "dayofweek.abbreviated",
			DayOfWeekFormat.Full => "dayofweek.full",
			_ => throw new ArgumentOutOfRangeException(nameof(dayOfWeekFormat)),
		};

	private static string ToTemplateString(HourFormat hourFormat)
		=> hourFormat switch
		{
			HourFormat.None => string.Empty,
			HourFormat.Default => "hour",
			_ => throw new ArgumentOutOfRangeException(nameof(hourFormat)),
		};

	private static string ToTemplateString(MinuteFormat minuteFormat)
		=> minuteFormat switch
		{
			MinuteFormat.None => string.Empty,
			MinuteFormat.Default => "minute",
			_ => throw new ArgumentOutOfRangeException(nameof(minuteFormat)),
		};

	private static string ToTemplateString(SecondFormat secondFormat)
		=> secondFormat switch
		{
			SecondFormat.None => string.Empty,
			SecondFormat.Default => "second",
			_ => throw new ArgumentOutOfRangeException(nameof(secondFormat)),
		};

	private static string ToTemplateString(TimeZoneFormat timeZoneFormat)
		=> timeZoneFormat switch
		{
			TimeZoneFormat.None => string.Empty,
			TimeZoneFormat.Default => "timezone",
			TimeZoneFormat.Abbreviated => "timezone.abbreviated",
			TimeZoneFormat.Full => "timezone.full",
			_ => throw new ArgumentOutOfRangeException(nameof(timeZoneFormat)),
		};

	private string BuildTemplate()
	{
		var templateBuilder = new StringBuilder();
		if (IsLongDate)
			Append("longdate");
		else if (IsShortDate)
			Append("shortdate");
		else
		{
			Append(ToTemplateString(IncludeYear));
			Append(ToTemplateString(IncludeMonth));
			Append(ToTemplateString(IncludeDay));
			Append(ToTemplateString(IncludeDayOfWeek));
		}

		if (IsLongTime)
			Append("longtime");
		else if (IsShortTime)
			Append("shorttime");
		else
		{
			Append(ToTemplateString(IncludeHour));
			Append(ToTemplateString(IncludeMinute));
			Append(ToTemplateString(IncludeSecond));
			Append(ToTemplateString(IncludeTimeZone));
		}

		return templateBuilder.ToString();

		void Append(string value)
		{
			if (value.Length == 0)
			{
				return;
			}

			if (templateBuilder.Length != 0)
			{
				templateBuilder.Append(' ');
			}

			templateBuilder.Append(value);
		}
	}

	private string BuildPattern()
	{
		string datePattern;
		if (IsLongDate)
		{
			datePattern = _firstCulture.DateTimeFormat.LongDatePattern;
		}
		else if (IsShortDate)
		{
			datePattern = _firstCulture.DateTimeFormat.ShortDatePattern;
		}
		else
		{
			// NOTE: The original order that was specified in the template string does NOT matter.
			// That's why all we need to care about is the values of the mentioned properties.
			// However, we should actually be checking the actual enum value and not just being "None" or not.
			// But the actual correct implementation that will 100% match WinUI isn't yet clear.
			// This is a best effort approach.
			bool hasYear = IncludeYear != YearFormat.None;
			bool hasMonth = IncludeMonth != MonthFormat.None;
			bool hasDay = IncludeDay != DayFormat.None;
			bool hasDayOfWeek = IncludeDayOfWeek != DayOfWeekFormat.None;

			if (hasYear && hasMonth && hasDay && hasDayOfWeek)
			{
				datePattern = _firstCulture.DateTimeFormat.LongDatePattern;
			}
			else if (hasYear && hasMonth && hasDay)
			{
				datePattern = _firstCulture.DateTimeFormat.ShortDatePattern;
			}
			else if (hasYear && hasMonth && !hasDayOfWeek)
			{
				datePattern = _firstCulture.DateTimeFormat.YearMonthPattern;
			}
			else if (hasYear && !hasMonth && !hasDay && !hasDayOfWeek)
			{
				datePattern = "yyyy";
			}
			else if (hasMonth && hasDay && !hasYear && !hasDayOfWeek)
			{
				datePattern = _firstCulture.DateTimeFormat.MonthDayPattern;
			}
			else if (hasDay && !hasMonth && !hasYear && !hasDayOfWeek)
			{
				datePattern = "d";
			}
			else
			{
				// Fallback case.
				// Add more cases as they arise.
				datePattern = _firstCulture.DateTimeFormat.LongDatePattern;
			}
		}

		string timePattern;
		if (IsLongTime)
		{
			timePattern = _firstCulture.DateTimeFormat.LongTimePattern;
		}
		else if (IsShortTime)
		{
			timePattern = _firstCulture.DateTimeFormat.ShortTimePattern;
		}
		else
		{
			// NOTE: The original order that was specified in the template string does NOT matter.
			// That's why all we need to care about is the values of the mentioned properties.
			bool hasHour = IncludeHour != HourFormat.None;
			bool hasMinute = IncludeMinute != MinuteFormat.None;
			bool hasSecond = IncludeSecond != SecondFormat.None;
			bool hasTimeZone = IncludeTimeZone != TimeZoneFormat.None;

			if (hasHour && hasMinute && hasSecond)
			{
				timePattern = _firstCulture.DateTimeFormat.LongTimePattern;
			}
			else if (hasHour && hasMinute)
			{
				timePattern = _firstCulture.DateTimeFormat.ShortTimePattern;
			}
			else if (hasHour)
			{
				timePattern = "h";
			}
			else if (!hasHour && !hasMinute && !hasSecond)
			{
				timePattern = string.Empty;
			}
			else
			{
				// Shouldn't really be reachable. But a fallback in place just in case.
				timePattern = _firstCulture.DateTimeFormat.LongTimePattern;
			}

			if (hasTimeZone)
			{
				if (timePattern.Length == 0)
				{
					timePattern = "zzz";
				}
				else
				{
					timePattern = $"{timePattern} zzz";
				}
			}
		}

		string finalSystemFormat;
		if (datePattern.Length > 0 && timePattern.Length > 0)
		{
			finalSystemFormat = $"{datePattern} {timePattern}";
		}
		else if (datePattern.Length > 0)
		{
			finalSystemFormat = datePattern;
		}
		else
		{
			finalSystemFormat = timePattern;
		}

		return ConstructPattern(finalSystemFormat);

		void AddToBuilder(StringBuilder builder, char lastChar, int count)
		{
			// Best effort implementation.
			switch (lastChar)
			{
				case 'g':
					while (count >= 2)
					{
						builder.Append("{era.abbreviated}");
						count -= 2;
					}

					while (count >= 1)
					{
						builder.Append("{era.abbreviated}");
						count -= 1;
					}

					break;

				case 'y':
					while (count >= 5)
					{
						builder.Append("{year.full(5)}");
						count -= 5;
					}

					while (count >= 4)
					{
						builder.Append("{year.full}");
						count -= 4;
					}

					while (count >= 3)
					{
						builder.Append("{year.full(3)}");
						count -= 3;
					}

					while (count >= 2)
					{
						builder.Append("{year.full(2)}");
						count -= 2;
					}

					while (count >= 1)
					{
						builder.Append("{year.full(1)}");
						count -= 1;
					}

					break;

				case 'M':
					while (count >= 4)
					{
						builder.Append("{month.full}");
						count -= 4;
					}

					while (count >= 3)
					{
						builder.Append("{month.abbreviated}");
						count -= 3;
					}

					while (count >= 2)
					{
						builder.Append("{month.integer(2)}");
						count -= 2;
					}

					while (count >= 1)
					{
						builder.Append("{month.integer}");
						count -= 1;
					}

					break;

				case 'd':
					while (count >= 4)
					{
						builder.Append("{dayofweek.full}");
						count -= 4;
					}

					while (count >= 3)
					{
						builder.Append("{dayofweek.abbreviated}");
						count -= 3;
					}

					while (count >= 2)
					{
						builder.Append("{day.integer(2)}");
						count -= 2;
					}

					while (count >= 1)
					{
						builder.Append("{day.integer}");
						count -= 1;
					}

					break;

				case 't':
					while (count >= 2)
					{
						builder.Append("{period.abbreviated(2)}");
						count -= 2;
					}

					while (count >= 1)
					{
						builder.Append("{period.abbreviated(1)}");
						count -= 1;
					}

					break;

				case 'h' or 'H':

					while (count >= 2)
					{
						builder.Append("{hour.integer(2)}");
						count -= 2;
					}

					while (count >= 1)
					{
						builder.Append("{hour.integer(1)}");
						count -= 1;
					}

					break;

				case 'm':

					while (count >= 2)
					{
						builder.Append("{minute.integer(2)}");
						count -= 2;
					}

					while (count >= 1)
					{
						builder.Append("{minute.integer(1)}");
						count -= 1;
					}

					break;

				case 's':

					while (count >= 2)
					{
						builder.Append("{second.integer(2)}");
						count -= 2;
					}

					while (count >= 1)
					{
						builder.Append("{second.integer(1)}");
						count -= 1;
					}

					break;

				case 'z':
					while (count >= 3)
					{
						builder.Append("{timezone.full}");
						count -= 3;
					}

					while (count >= 2)
					{
						builder.Append("{timezone.abbreviated(2)}");
						count -= 2;
					}

					while (count >= 1)
					{
						builder.Append("{timezone.abbreviated(1)}");
						count -= 1;
					}

					break;

				default:
					builder.Append(lastChar, count);
					break;
			}
		}

		string ConstructPattern(string str)
		{
			if (string.IsNullOrEmpty(str))
			{
				return string.Empty;
			}

			var builder = new StringBuilder();

			foreach (var part in IsWithinSingleQuotesRegex().Split(str))
			{
				// skip empty block
				if (part.Length == 0)
				{
					continue;
				}
				// add quoted literal as is, without the quotes
				else if (part.StartsWith('\''))
				{
					if (part.Length > 2)
					{
						builder.Append(part[1..^1]);
					}
				}
				// for non-quoted parts, further segment them by grouping repeated character(s)
				else
				{
					foreach (var segment in DateTimeFormatPartsRegex().EnumerateMatches(part))
					{
						AddToBuilder(builder, part[segment.Index], segment.Length);
					}
				}
			}

			return builder.ToString();
		}
	}

	[GeneratedRegex(@"('[^']+')")]
	private static partial Regex IsWithinSingleQuotesRegex();

	[GeneratedRegex(@"(.)(\1+)?")]
	private static partial Regex DateTimeFormatPartsRegex();
}
