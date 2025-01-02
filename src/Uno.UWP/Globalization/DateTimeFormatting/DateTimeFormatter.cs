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
	private static readonly IReadOnlyList<string> _defaultPatterns;
	private static readonly IReadOnlyList<string> _emptyLanguages;
	private static readonly IDictionary<string, IDictionary<string, string>> _mapCache;
	private static readonly IDictionary<(string language, string template), string> _patternsCache;

	private readonly CultureInfo? _firstCulture;
	private readonly IDictionary<string, string>[]? _maps;

	static DateTimeFormatter()
	{
		_mapCache = new Dictionary<string, IDictionary<string, string>>();
		_patternsCache = new Dictionary<(string language, string template), string>();

		_defaultPatterns = new[]
		{
			"{month.full}‎ ‎{day.integer}‎, ‎{year.full}",
			"{day.integer}‎ ‎{month.full}‎, ‎{year.full}",
		};

		_emptyLanguages = Array.Empty<string>();

		LongDate = new DateTimeFormatter("longdate");
		LongTime = new DateTimeFormatter("longtime");
		ShortDate = new DateTimeFormatter("shortdate");
		ShortTime = new DateTimeFormatter("shorttime");
	}

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
		Template = formatTemplate ?? throw new ArgumentNullException(nameof(formatTemplate));

		if (languages != null)
		{
			Languages = languages.Distinct().ToArray();
		}

		_firstCulture = new CultureInfo(Languages[0]);

		_maps = Languages.SelectToArray(BuildLookup);

		Patterns = BuildPatterns().ToArray();

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

	/// <summary>
	/// Gets the priority list of language identifiers that is used when formatting dates and times.
	/// </summary>
	public IReadOnlyList<string> Languages { get; } = ApplicationLanguages.Languages;

	/// <summary>
	/// Gets the DateTimeFormatter object that formats dates according to the user's choice of long date pattern.
	/// </summary>
	public static DateTimeFormatter LongDate { get; }

	/// <summary>
	/// Gets the DateTimeFormatter object that formats times according to the user's choice of long time pattern.
	/// </summary>
	public static DateTimeFormatter LongTime { get; }

	/// <summary>
	/// Gets or sets the numbering system that is used to format dates and times.
	/// </summary>
	public string? NumeralSystem { get; set; }

	/// <summary>
	/// Gets the patterns corresponding to this template that are used when formatting dates and times.
	/// </summary>
	public IReadOnlyList<string> Patterns { get; } = _defaultPatterns;

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
	public static DateTimeFormatter ShortDate { get; }

	/// <summary>
	/// Gets the DateTimeFormatter object that formats times according to the user's choice of short time pattern.
	/// </summary>
	public static DateTimeFormatter ShortTime { get; }

	/// <summary>
	/// Gets a string representation of this format template.
	/// </summary>
	public string Template { get; }

	private IDictionary<string, string> BuildLookup(string language)
	{
		if (_mapCache.TryGetValue(language, out var map))
		{
			return map;
		}

		var info = new CultureInfo(language).DateTimeFormat;

		map = new Dictionary<string, string>
		{	
			// Predefined patterns
			{ "longdate"                             , info.LongDatePattern } ,
			{ "shortdate"                            , info.ShortDatePattern } ,
			{ "longtime"                             , info.LongTimePattern } ,
			{ "shorttime"                            , info.ShortTimePattern } ,
    
			// Compound patterns
			{ "dayofweek day month year"             , info.FullDateTimePattern } ,
			{ "dayofweek day month"                  , "D" } ,
			{ "day month year"                       , info.ShortDatePattern } ,
			{ "day month.full year"                  , info.ShortDatePattern } ,
			{ "day month"                            , info.MonthDayPattern } ,
			{ "month year"                           , info.YearMonthPattern } ,
			{ "hour minute second"                   , info.LongTimePattern },
			{ "hour minute"                          , info.ShortTimePattern },
			//{ "year month day hour"                  , "" },

			// Day of week formats
			{ "dayofweek"                            , "dddd" } ,
			{ "dayofweek.full"                       , "dddd" } ,
			{ "dayofweek.abbreviated"                , "ddd" } ,
			{ "dayofweek.abbreviated(1)"             , "dd" } ,
			{ "dayofweek.abbreviated(2)"             , "ddd" } ,
			{ "dayofweek.solo.full"                  , "dddd" } ,
			{ "dayofweek.solo.abbreviated"           , "ddd" } ,

			// Day formats
			{ "day"                                  , "%d" } ,
			{ "day.integer"                          , "%d" },
			{ "day.integer(1)"                       , "%d" },
			{ "day.integer(2)"                       , "dd" },

			// Month formats
			{ "month"                                , "MMMM" } ,
			{ "month.full"                           , "MMMM" } ,
			{ "month.abbreviated"                    , "MMM" } ,
			{ "month.abbreviated(1)"                 , "%M" } ,
			{ "month.abbreviated(2)"                 , "MMM" } ,
			{ "month.numeric"                        , "%M" } ,
			{ "month.integer"                        , "%M" } ,
			{ "month.integer(1)"                     , "%M" } ,
			{ "month.integer(2)"                     , "MM" } ,
			{ "month.solo.full"                      , "MMMM" } ,
			{ "month.solo.abbreviated"               , "MMM" } ,

			// Year formats
			{ "year"                                 , "yyyy" } ,
			{ "year.full"                            , "yyyy" } ,
			{ "year.abbreviated"                     , "yy" } ,
			{ "year.abbreviated(1)"                  , "%y" } ,
			{ "year.abbreviated(2)"                  , "yy" } ,

			// Hour formats
			{ "hour"                                 , "%H" } ,
			{ "hour.integer"                         , "%H" } ,
			{ "hour.integer(1)"                      , "%h" } ,
			{ "hour.integer(2)"                      , "HH" } ,

			// Period (AM/PM) formats
			{ "period"                               , "tt" } ,
			{ "period.full"                          , "tt" } ,
			{ "period.abbreviated"                   , "tt" } ,
			{ "period.abbreviated(1)"                , "t" } ,
			{ "period.abbreviated(2)"                , "tt" } ,

			// Minute formats
			{ "minute"                               , "%m" } ,
			{ "minute.integer"                       , "%m" } ,
			{ "minute.integer(1)"                    , "%m" } ,
			{ "minute.integer(2)"                    , "mm" } ,

			// Second formats
			{ "second"                               , "%s" } ,
			{ "second.integer"                       , "%s" } ,
			{ "second.integer(1)"                    , "%s" } ,
			{ "second.integer(2)"                    , "ss" } ,

			// Timezone formats
			{ "timezone"                             , "%z" } ,
			{ "timezone.full"                        , "zzz" } ,
			{ "timezone.abbreviated"                 , "zz" } ,
			{ "timezone.abbreviated(1)"              , "%z" } ,
			{ "timezone.abbreviated(2)"              , "zz" }
		};

		return _mapCache[language] = map;
	}

	public string Format(DateTimeOffset value)
	{
		var format = GetSystemTemplate();
		try
		{
			return value.ToString(format, _firstCulture!.DateTimeFormat);
		}
		catch (Exception e)
		{
			return format + " : " + e.Message;
		}
	}

	[NotImplemented]
	public string Format(DateTimeOffset datetime, string timeZoneId)
	{
		throw new NotSupportedException();
	}

	private string GetSystemTemplate()
	{
		var result = Template.Replace("{", "").Replace("}", "");

		var map = _maps![0];

		var sortedKeys = map.Keys.OrderByDescending(k => k.Length);

		foreach (var key in sortedKeys)
		{
			result = result.Replace(key, map[key]);
		}

		if (result.Contains("h") && Clock == ClockIdentifiers.TwentyFourHour)
		{
			result = result.Replace("h", "H");
		}
		else if (result.Contains("H") && Clock == ClockIdentifiers.TwelveHour)
		{
			result = result.Replace("H", "h");
		}

		return result;
	}

	private static readonly (Regex pattern, string replacement)[] PatternsReplacements =
		new (string pattern, string replacement)[]
			{
				(@"\bMMMM\b", "{month.full}"),
				(@"\bMMM\b", "{month.abbreviated}"),
				(@"\bMM\b", "{month.numeric}"),
				(@"%M\b", "{month.numeric}"),
				(@"\bM\b", "{month.numeric}"),
				(@"\bdddd\b", "{dayofweek.full}"),
				(@"\bddd\b", "{dayofweek.abbreviated}"),
				(@"\byyyy\b", "{year.full}"),
				(@"\byy\b", "{year.abbreviated}"),
				(@"\b(z|zz)\b", "{timezone.abbreviated}"),
				(@"\byyyy\b", "{year.full}"),
				(@"\bMMMM\b", "{month.full}"),
				(@"\bdd\b", "{day.integer(2)}"),
				(@"%d\b", "{day.integer}"),
				(@"\bd\b", "{day.integer}"),
				(@"\bzzz\b", "{timezone.full}"),
				(@"\bzz\b", "{timezone.abbreviated}"),
				(@"%z\b", "{timezone}"),
				(@"\b(HH|hh|H|h)\b", "{hour}"),
				(@"\b(mm)\b", "{minute.integer(2)}"),
				(@"\b(m)\b", "{minute.integer}"),
				(@"\b(mm|m)\b", "{minute}"),
				(@"\b(ss|s)\b", "{second}"),
				(@"\btt\b", "{period.abbreviated}"),
			}
			.SelectToArray(x =>
				(new Regex(x.pattern, RegexOptions.CultureInvariant | RegexOptions.Compiled),
					x.replacement));

	private IEnumerable<string> BuildPatterns()
	{
		var format = Template;
		string? sanitizedFormat = default;

		for (var i = 0; i < Languages.Count; i++)
		{
			var language = Languages[i];
			if (_patternsCache.TryGetValue((language, format), out var pattern))
			{
				yield return pattern;
			}
			else
			{
				var map = _maps![i];
				sanitizedFormat ??= format
					.Replace("{", "")
					.Replace("}", "");
				if (map.TryGetValue(sanitizedFormat, out var r))
				{
					foreach (var p in PatternsReplacements)
					{
						r = p.pattern.Replace(r, p.replacement);
					}

					yield return _patternsCache[(language, format)] = r;
				}
			}
		}
	}

	private string BuildTemplate()
	{
		var templateBuilder = new StringBuilder();
		return templateBuilder.ToString();
	}
}
