#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Uno;
using WinCalendar = Windows.Globalization.Calendar;

namespace Windows.Globalization.DateTimeFormatting;

/// <summary>
/// Formats dates and times.
/// </summary>
public sealed partial class DateTimeFormatter
{
	private static DateTimeFormatter? s_longDate;
	private static DateTimeFormatter? s_longTime;
	private static DateTimeFormatter? s_shortDate;
	private static DateTimeFormatter? s_shortTime;

	private readonly PatternFormatContext _context;

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
		: this(formatTemplate, languageList: ToLanguageList(languages), region: null, calendarIdentifier: null, clockIdentifier: null)
	{
	}

	// Every public constructor ends here. A null calendar or clock is the languages' default, as in WinRT.
	private DateTimeFormatter(
		string formatTemplate,
		IReadOnlyList<string>? languageList,
		string? region,
		string? calendarIdentifier,
		string? clockIdentifier)
	{
		ArgumentNullException.ThrowIfNull(formatTemplate);

		Languages = languageList ?? ApplicationLanguages.Languages;
		var languageCulture = ApplicationLanguages.ResolveCulture(Languages);
		// The identifiers .NET has no calendar for (the lunar ones but Chinese) format in the language's calendar.
		var calendar = (calendarIdentifier is null ? null : WinCalendar.FindCalendar(calendarIdentifier)) ?? languageCulture.Calendar;
		Calendar = calendarIdentifier ?? WinCalendar.GetCalendarSystem(calendar);
		Clock = clockIdentifier ?? WinCalendar.GetDefaultClock(languageCulture);

		// TODO:MZ:
		GeographicRegion = region ?? "ZZ";

		// The date parts are numbered in the calendar and named through a culture that has names for it, as in WinRT
		// (Hebrew names for en-US with the Hebrew calendar). The time parts and their AM/PM designators stay in the
		// language. The cultures and the pattern are built here, once, rather than on each Format call.
		_context = new PatternFormatContext(
			calendar,
			WinCalendar.ResolveFormatCulture(languageCulture, calendar),
			languageCulture,
			isTwentyFourHours: Clock == ClockIdentifiers.TwentyFourHour);

		// The input can be either a template or a pattern. We try parsing it as a template
		// first (without throwing), and fall back to treating it as a pattern otherwise.
		// Using a non-throwing TryParse here avoids first-chance exceptions for the common
		// pattern case (e.g. TimePicker's "{hour.integer(1)}"), which are noise when debugging.
		var templateParser = new TemplateParser(formatTemplate);
		if (templateParser.TryParse(out _))
		{
			// Template example:
			// "year month day dayofweek hour timezone" (that's just an example)
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
			IsLongTime = templateParser.Info.IsLongTime;

			// NOTE: We intentionally don't set the user provided template.
			// Instead, we parse and re-build the template string.
			// That's how WinUI works.
			// Basically, the order of tokens in the template string does NOT matter.
			// So, this kinda normalizes the order the right way.
			Template = BuildTemplate(
				IncludeYear,
				IncludeMonth,
				IncludeDay,
				IncludeDayOfWeek,
				IncludeHour,
				IncludeMinute,
				IncludeSecond,
				IncludeTimeZone,
				IsLongDate,
				IsShortDate,
				IsLongTime,
				IsShortTime);

			string patternBuiltFromTemplate = BuildPattern();
			Patterns = [patternBuiltFromTemplate];
			_patternRootNode = new PatternParser(patternBuiltFromTemplate).Parse();
		}
		else
		{
			// Pattern example:
			// "Hello {year.full} Hello2 {month.full}" (that's just an example)
			_patternRootNode = new PatternParser(formatTemplate).Parse();
			Template = formatTemplate;
			Patterns = [formatTemplate];
		}
	}

	public DateTimeFormatter(
		string formatTemplate,
		IEnumerable<string> languages,
		string geographicRegion,
		string calendar,
		string clock)
		: this(formatTemplate, languageList: ToLanguageList(languages), region: geographicRegion, calendarIdentifier: calendar, clockIdentifier: clock)
	{
	}

	// As in WinRT, the components make a template, so a combination the template grammar lacks throws.
	public DateTimeFormatter(
		YearFormat yearFormat,
		MonthFormat monthFormat,
		DayFormat dayFormat,
		DayOfWeekFormat dayOfWeekFormat)
		: this(
			BuildTemplate(yearFormat, monthFormat, dayFormat, dayOfWeekFormat, HourFormat.None, MinuteFormat.None, SecondFormat.None),
			languageList: null,
			region: null,
			calendarIdentifier: null,
			clockIdentifier: null)
	{
	}

	public DateTimeFormatter(
		HourFormat hourFormat,
		MinuteFormat minuteFormat,
		SecondFormat secondFormat)
		: this(
			BuildTemplate(YearFormat.None, MonthFormat.None, DayFormat.None, DayOfWeekFormat.None, hourFormat, minuteFormat, secondFormat),
			languageList: null,
			region: null,
			calendarIdentifier: null,
			clockIdentifier: null)
	{
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
		: this(
			BuildTemplate(yearFormat, monthFormat, dayFormat, dayOfWeekFormat, hourFormat, minuteFormat, secondFormat),
			languageList: ToLanguageList(languages),
			region: null,
			calendarIdentifier: null,
			clockIdentifier: null)
	{
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
		: this(
			BuildTemplate(yearFormat, monthFormat, dayFormat, dayOfWeekFormat, hourFormat, minuteFormat, secondFormat),
			languageList: ToLanguageList(languages),
			region: geographicRegion,
			calendarIdentifier: calendar,
			clockIdentifier: clock)
	{
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
	public IReadOnlyList<string> Languages { get; }

	/// <summary>
	/// Gets the DateTimeFormatter object that formats dates according to the user's choice of long date pattern.
	/// </summary>
	public static DateTimeFormatter LongDate => GetDefault(ref s_longDate, "longdate");

	/// <summary>
	/// Gets the DateTimeFormatter object that formats times according to the user's choice of long time pattern.
	/// </summary>
	public static DateTimeFormatter LongTime => GetDefault(ref s_longTime, "longtime");

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
	public static DateTimeFormatter ShortDate => GetDefault(ref s_shortDate, "shortdate");

	/// <summary>
	/// Gets the DateTimeFormatter object that formats times according to the user's choice of short time pattern.
	/// </summary>
	public static DateTimeFormatter ShortTime => GetDefault(ref s_shortTime, "shorttime");

	/// <summary>
	/// Gets a string representation of this format template.
	/// </summary>
	public string Template { get; }

	public string Format(DateTimeOffset value)
	{
		try
		{
			// As in WinRT, which gets only the instant, in local time.
			return _patternRootNode.Format(TimeZoneInfo.ConvertTime(value, TimeZoneInfo.Local), _context);
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

	private static IReadOnlyList<string>? ToLanguageList(IEnumerable<string>? languages)
		=> languages?.Distinct().ToArray();

	// As in WinRT, each read gives a new formatter for the current application languages. The one built for them
	// is kept until they change, so a read otherwise costs a reference comparison and a copy.
	private static DateTimeFormatter GetDefault(ref DateTimeFormatter? cache, string template)
	{
		var formatter = cache;
		if (formatter is null || !ReferenceEquals(formatter.Languages, ApplicationLanguages.Languages))
		{
			cache = formatter = new DateTimeFormatter(template);
		}

		return new DateTimeFormatter(formatter);
	}

	// Shares the other formatter's cultures and parsed pattern, which nothing changes after construction.
	private DateTimeFormatter(DateTimeFormatter other)
	{
		_context = other._context;
		_patternRootNode = other._patternRootNode;
		Calendar = other.Calendar;
		Clock = other.Clock;
		GeographicRegion = other.GeographicRegion;
		IncludeYear = other.IncludeYear;
		IncludeMonth = other.IncludeMonth;
		IncludeDay = other.IncludeDay;
		IncludeDayOfWeek = other.IncludeDayOfWeek;
		IncludeHour = other.IncludeHour;
		IncludeMinute = other.IncludeMinute;
		IncludeSecond = other.IncludeSecond;
		IncludeTimeZone = other.IncludeTimeZone;
		IsShortDate = other.IsShortDate;
		IsLongDate = other.IsLongDate;
		IsShortTime = other.IsShortTime;
		IsLongTime = other.IsLongTime;
		Languages = other.Languages;
		Patterns = other.Patterns;
		Template = other.Template;
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
			MonthFormat.Numeric => "month.numeric",
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

	private static string BuildTemplate(
		YearFormat year,
		MonthFormat month,
		DayFormat day,
		DayOfWeekFormat dayOfWeek,
		HourFormat hour,
		MinuteFormat minute,
		SecondFormat second,
		TimeZoneFormat timeZone = TimeZoneFormat.None,
		bool isLongDate = false,
		bool isShortDate = false,
		bool isLongTime = false,
		bool isShortTime = false)
	{
		var templateBuilder = new StringBuilder();
		if (isLongDate)
			Append("longdate");
		else if (isShortDate)
			Append("shortdate");
		else
		{
			Append(ToTemplateString(year));
			Append(ToTemplateString(month));
			Append(ToTemplateString(day));
			Append(ToTemplateString(dayOfWeek));
		}

		if (isLongTime)
			Append("longtime");
		else if (isShortTime)
			Append("shorttime");
		else
		{
			Append(ToTemplateString(hour));
			Append(ToTemplateString(minute));
			Append(ToTemplateString(second));
			Append(ToTemplateString(timeZone));
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
		// The date parts follow the calendar in use.
		var dateFormat = _context.DateCulture.DateTimeFormat;

		// One case per date production of the template grammar, each with only the parts it names.
		string datePattern;
		if (IsLongDate)
		{
			datePattern = ConstructPattern(dateFormat.LongDatePattern);
		}
		else if (IsShortDate)
		{
			datePattern = ConstructPattern(dateFormat.ShortDatePattern);
		}
		else
		{
			datePattern = (IncludeYear != YearFormat.None, IncludeMonth != MonthFormat.None, IncludeDay != DayFormat.None, IncludeDayOfWeek != DayOfWeekFormat.None) switch
			{
				(false, false, false, false) => string.Empty,
				(true, true, true, true) => ConstructPattern(dateFormat.LongDatePattern),

				(true, true, true, false) => GetYearMonthDayPattern(),

				// WinRT takes the standalone month here ("{month.solo.full} {year.full}").
				(true, true, false, false) => ConstructPattern(dateFormat.YearMonthPattern).Replace("{month.full}", "{month.solo.full}"),
				(false, true, true, true) => RemoveFields(ConstructPattern(dateFormat.LongDatePattern), YearOrEraFieldRegex()),
				(false, true, true, false) => GetMonthDayPattern(),

				// The abbreviated day of week before the day, as WinRT writes it for most languages ("Mon 15").
				(false, false, true, true) => "{dayofweek.abbreviated} " + GetSinglePartPattern(dateFormat.MonthDayPattern, DayFieldRegex(), "{day.integer}", keepSpacedSuffix: false),

				// The patterns WinRT gives for a single part. The year and the day keep the letters the language writes
				// after them, from its year and month, and month and day patterns.
				(true, false, false, false) => GetSinglePartPattern(dateFormat.YearMonthPattern, FullYearFieldRegex(), "{year.full}", keepSpacedSuffix: true),
				(false, true, false, false) => IncludeMonth == MonthFormat.Numeric ? "{month.integer}" : "{month.solo.full}",
				(false, false, true, false) => GetSinglePartPattern(dateFormat.MonthDayPattern, DayFieldRegex(), "{day.integer}", keepSpacedSuffix: false),
				(false, false, false, true) => "{dayofweek.solo.full}",

				_ => throw new ArgumentException($"The template '{Template}' has no date pattern."),
			};

			// As in WinRT, a part's format applies in any combination ("Jan 15", "Mon, Jan 15, 2024").
			if (IncludeMonth == MonthFormat.Abbreviated)
			{
				datePattern = datePattern.Replace("{month.full}", "{month.abbreviated}").Replace("{month.solo.full}", "{month.solo.abbreviated}");
			}

			if (IncludeDayOfWeek == DayOfWeekFormat.Abbreviated)
			{
				datePattern = datePattern.Replace("{dayofweek.full}", "{dayofweek.abbreviated}").Replace("{dayofweek.solo.full}", "{dayofweek.solo.abbreviated}");
			}

			if (IncludeYear == YearFormat.Abbreviated)
			{
				datePattern = FullYearFieldRegex().Replace(datePattern, "{year.abbreviated}");
			}
		}

		if (dateFormat.Calendar is GregorianCalendar)
		{
			// WinRT names no era in Gregorian dates, which some .NET patterns have ("MMMM yyyy g" for ar-SA).
			datePattern = RemoveFields(datePattern, EraFieldRegex());
		}

		// The time parts follow the language, with its patterns adapted to the clock in use.
		var timeFormat = _context.TimeCulture.DateTimeFormat;
		string timePattern;
		if (IsLongTime)
		{
			timePattern = AdaptToClock(ConstructPattern(timeFormat.LongTimePattern));
		}
		else if (IsShortTime)
		{
			timePattern = AdaptToClock(ConstructPattern(timeFormat.ShortTimePattern));
		}
		else
		{
			timePattern = (IncludeHour != HourFormat.None, IncludeMinute != MinuteFormat.None, IncludeSecond != SecondFormat.None) switch
			{
				(false, false, false) => string.Empty,
				(true, true, true) => AdaptToClock(ConstructPattern(timeFormat.LongTimePattern)),
				(true, true, false) => AdaptToClock(ConstructPattern(timeFormat.ShortTimePattern)),
				(true, false, false) => GetHourPattern(AdaptToClock(ConstructPattern(timeFormat.ShortTimePattern))),
				_ => throw new ArgumentException($"The template '{Template}' has no time pattern."),
			};

			if (IncludeTimeZone != TimeZoneFormat.None)
			{
				// As in WinRT, a time zone is abbreviated unless the template asks for its full name.
				var timeZone = IncludeTimeZone == TimeZoneFormat.Full ? "{timezone.full}" : "{timezone.abbreviated}";
				timePattern = timePattern.Length == 0 ? timeZone : $"{timePattern} {timeZone}";
			}
		}

		return datePattern.Length > 0 && timePattern.Length > 0
			? $"{datePattern} {timePattern}"
			: datePattern + timePattern;

		// As in WinRT, the long date without the day of week, though the grammar calls the combination a short
		// date. A numeric month takes the short date, unless the long date's month is numeric too (ja-JP). An
		// abbreviated year takes the short date with two digits ("1/15/24"), unless the month is named, full or
		// abbreviated ("15. Januar 24").
		string GetYearMonthDayPattern()
		{
			if (IncludeYear == YearFormat.Abbreviated && IncludeMonth is MonthFormat.Default or MonthFormat.Numeric)
			{
				return FullYearFieldRegex().Replace(ConstructPattern(dateFormat.ShortDatePattern), "{year.abbreviated(2)}");
			}

			var longDate = RemoveFields(ConstructPattern(dateFormat.LongDatePattern), DayOfWeekFieldRegex());
			return IncludeMonth == MonthFormat.Numeric && !longDate.Contains("{month.integer", StringComparison.Ordinal)
				? ConstructPattern(dateFormat.ShortDatePattern)
				: longDate;
		}

		// The part as WinRT writes it alone: with the letters the language puts after the year ("2024年", "2024 г."),
		// but only those glued to the day ("15日", and "15" for lt-LT's "15 d.").
		string GetSinglePartPattern(string pattern, Regex part, string fallback, bool keepSpacedSuffix)
		{
			pattern = ConstructPattern(pattern);
			if (part.Match(pattern) is not { Success: true } field)
			{
				return fallback;
			}

			var start = field.Index + field.Length;
			var end = pattern.IndexOf('{', start);
			var suffix = pattern.AsSpan(start, (end < 0 ? pattern.Length : end) - start);
			if (!keepSpacedSuffix)
			{
				var glued = 0;
				while (glued < suffix.Length && !IsSeparator(suffix[glued]))
				{
					glued++;
				}

				suffix = suffix[..glued];
			}

			while (suffix.Length > 0 && IsSeparator(suffix[^1]))
			{
				suffix = suffix[..^1];
			}

			foreach (var c in suffix)
			{
				if (char.IsLetter(c))
				{
					return string.Concat(field.Value, suffix);
				}
			}

			return field.Value;
		}

		// A numeric month takes the short date without its year and its padding ("1/15", "15.1."), unless the month
		// and day pattern's month is numeric already (ja-JP).
		string GetMonthDayPattern()
		{
			var monthDay = ConstructPattern(dateFormat.MonthDayPattern);
			return IncludeMonth == MonthFormat.Numeric && !monthDay.Contains("{month.integer", StringComparison.Ordinal)
				? RemoveFields(ConstructPattern(dateFormat.ShortDatePattern), YearOrEraFieldRegex())
					.Replace("{day.integer(2)}", "{day.integer}")
					.Replace("{month.integer(2)}", "{month.integer}")
				: monthDay;
		}

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

			foreach (var part in StringLiteralRegex().Split(str))
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
					foreach (var segment in RepeatedCharactersRegex().EnumerateMatches(part))
					{
						AddToBuilder(builder, part[segment.Index], segment.Length);
					}
				}
			}

			return builder.ToString();
		}
	}

	// As in WinRT, the language's time pattern follows the clock in use. A 12-hour clock adds the period after the time
	// ("1:05 PM"), or before it in Chinese, Japanese and Korean ("午後 1:05"), with an unpadded hour. A 24-hour clock
	// drops the period and its separator.
	private string AdaptToClock(string timePattern)
	{
		var hasPeriod = timePattern.Contains("{period", StringComparison.Ordinal);
		if (_context.IsTwentyFourHours)
		{
			return hasPeriod ? RemoveFields(timePattern, PeriodFieldRegex()) : timePattern;
		}

		if (hasPeriod)
		{
			return timePattern;
		}

		timePattern = HourFieldRegex().Replace(timePattern, "{hour.integer}");
		return _context.TimeCulture.TwoLetterISOLanguageName is "ja" or "zh" or "ko"
			? "{period.abbreviated} " + timePattern
			: timePattern + " {period.abbreviated}";
	}

	// As in WinRT, the short time without its minutes on a 12-hour pattern ("1 PM"), or with them at
	// "00" on a 24-hour one ("13:00").
	private static string GetHourPattern(string shortTimePattern)
	{
		var minute = MinuteFieldRegex().Match(shortTimePattern);
		if (!minute.Success)
		{
			return shortTimePattern;
		}

		if (!shortTimePattern.Contains("{period", StringComparison.Ordinal))
		{
			return shortTimePattern.Remove(minute.Index, minute.Length).Insert(minute.Index, "00");
		}

		// Drop the minutes and the separator before them.
		var start = shortTimePattern.LastIndexOf('}', minute.Index) + 1;
		return shortTimePattern.Remove(start, minute.Index + minute.Length - start);
	}

	// Removes the matching fields with the text that only separated or qualified them: the separator before
	// the last field and anything after it (", 2024", " 2024 г."), or the text up to the next field ("2024年").
	// Text glued to the field before the last one stays ("15日" in "15日月曜日", "15." in "15., hétfő").
	private static string RemoveFields(string pattern, Regex fields)
	{
		while (fields.Match(pattern) is { Success: true } field)
		{
			var start = field.Index;
			var end = pattern.IndexOf('{', field.Index + field.Length);
			if (end < 0)
			{
				start = pattern.LastIndexOf('}', field.Index) + 1;
				while (start < field.Index && !IsSeparator(pattern[start]))
				{
					start++;
				}

				end = pattern.Length;
			}

			pattern = pattern.Remove(start, end - start);
		}

		return pattern;
	}

	private static bool IsSeparator(char c) => char.IsWhiteSpace(c) || c is ',' or '\u060C' or '/' or '-';

	[GeneratedRegex(@"('[^']+')")]
	private static partial Regex StringLiteralRegex();

	[GeneratedRegex(@"(.)(\1+)?")]
	private static partial Regex RepeatedCharactersRegex();

	[GeneratedRegex(@"\{minute[^}]*\}")]
	private static partial Regex MinuteFieldRegex();

	[GeneratedRegex(@"\{(?:year|era)[^}]*\}")]
	private static partial Regex YearOrEraFieldRegex();

	[GeneratedRegex(@"\{era[^}]*\}")]
	private static partial Regex EraFieldRegex();

	[GeneratedRegex(@"\{dayofweek[^}]*\}")]
	private static partial Regex DayOfWeekFieldRegex();

	[GeneratedRegex(@"\{day\.integer[^}]*\}")]
	private static partial Regex DayFieldRegex();

	[GeneratedRegex(@"\{year\.full[^}]*\}")]
	private static partial Regex FullYearFieldRegex();

	[GeneratedRegex(@"\{period[^}]*\}")]
	private static partial Regex PeriodFieldRegex();

	[GeneratedRegex(@"\{hour[^}]*\}")]
	private static partial Regex HourFieldRegex();
}
