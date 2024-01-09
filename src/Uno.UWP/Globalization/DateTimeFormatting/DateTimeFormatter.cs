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

		var languagesArray = languages?.Distinct().ToArray();

		if (languagesArray == null || languagesArray.Length == 0)
		{
			var currentUiLanguage = CultureInfo.CurrentUICulture.Name;
			var currentLanguage = CultureInfo.CurrentCulture.Name;

			if (currentUiLanguage != currentLanguage)
			{
				Languages = languagesArray = new[]
				{
					currentUiLanguage,
					currentLanguage
				};
			}
			else
			{
				Languages = languagesArray = new[]
				{
					currentUiLanguage
				};
			}
		}
		else
		{
			Languages = languagesArray;
		}

		_firstCulture = new CultureInfo(languagesArray[0]);

		_maps = languagesArray.SelectToArray(BuildLookup);

		Patterns = BuildPatterns().ToArray();

		var calendar = new Calendar(languagesArray);
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
			{ "longdate"                             , info.LongDatePattern } ,
			{ "shortdate"                            , info.ShortDatePattern } ,
			{ "longtime"                             , info.LongTimePattern } ,
			{ "shorttime"                            , info.ShortTimePattern } ,
			{ "dayofweek day month year"             , info.FullDateTimePattern } ,
			{ "dayofweek day month"                  , "D" } ,
			{ "day month year"                       , info.ShortDatePattern } ,
			{ "day month.full year"                  , info.ShortDatePattern } ,
			{ "day month"                            , info.MonthDayPattern } ,
			{ "month year"                           , info.YearMonthPattern } ,
			{ "dayofweek.full"                       , "dddd" } ,
			{ "dayofweek.abbreviated"                , "ddd" } ,
			{ "month.full"                           , "MMMM" } ,
			{ "month.abbreviated"                    , "MMM" } ,
			{ "month.numeric"                        , "%M" } ,
			{ "year.abbreviated"                     , "yy" } ,
			{ "year.full"                            , "yyyy" } ,
			{ "hour minute second"                   , info.LongTimePattern },
			{ "hour minute"                          , info.ShortTimePattern },
			{ "timezone.abbreviated"                 , "zz" },
			{ "timezone.full"                        , "zzz" },
			{ "dayofweek"                            , "dddd" } ,
			{ "day"                                  , "%d" } ,
			{ "month"                                , "MMMM" } ,
			{ "year"                                 , "yyyy" } ,
			{ "hour.integer(1)"                      , "%h" },
			{ "hour"                                 , "H tt" } ,
			{ "minute.integer(2)"                    , "mm" },
			{ "minute.integer"                       , "%m" },
			{ "minute"                               , "%m" },
			{ "second"                               , "%s" },
			{ "timezone"                             , "%z" },
			{ "period.abbreviated(2)"                , "tt" },
			// { "year month day hour"                  , "" } ,
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

		foreach (var p in map)
		{
			result = result.Replace(p.Key, p.Value);

		}

		if (result.Contains("h") && Clock == ClockIdentifiers.TwentyFourHour)
		{
			result = result.Replace("h", "H");
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
