using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Uno;
using Uno.Extensions;

namespace Windows.Globalization.DateTimeFormatting
{
	public sealed partial class DateTimeFormatter
	{
		private static readonly IReadOnlyList<string> _defaultPatterns;

		private static readonly IReadOnlyList<string> _emptyLanguages;

		public string NumeralSystem { get; set; }

		public string Clock { get; }

		public string GeographicRegion { get; }

		public DayFormat IncludeDay { get; }

		public DayOfWeekFormat IncludeDayOfWeek { get; }

		public HourFormat IncludeHour { get; }

		public MinuteFormat IncludeMinute { get; }

		public MonthFormat IncludeMonth { get; }

		public SecondFormat IncludeSecond { get; }

		public YearFormat IncludeYear { get; }

		public IReadOnlyList<string> Languages { get; } = ApplicationLanguages.Languages;

		public string Calendar { get; }

		public IReadOnlyList<string> Patterns { get; } = _defaultPatterns;

		public string ResolvedGeographicRegion { get; }

		public string ResolvedLanguage { get; }

		public string Template { get; }

		public static DateTimeFormatter LongDate { get; }

		public static DateTimeFormatter LongTime { get; }

		public static DateTimeFormatter ShortDate { get; }

		public static DateTimeFormatter ShortTime { get; }

		static DateTimeFormatter()
		{
			_map_cache = new Dictionary<string, IDictionary<string, string>>();
			_patterns_cache = new Dictionary<(string language, string template), string>();

			_defaultPatterns = new []
			{
				"{month.full}‎ ‎{day.integer}‎, ‎{year.full}",
				"{day.integer}‎ ‎{month.full}‎, ‎{year.full}",
			};

			_emptyLanguages = new string[0];

			LongDate = new DateTimeFormatter("longdate");
			LongTime = new DateTimeFormatter("longtime");
			ShortDate = new DateTimeFormatter("shortdate");
			ShortTime = new DateTimeFormatter("shorttime");
		}

		public DateTimeFormatter(string formatTemplate)
			:this(formatTemplate, languages: null)
		{
		}

		public DateTimeFormatter(
			string formatTemplate,
			IEnumerable<string> languages)
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
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Globalization.DateTimeFormatting.DateTimeFormatter", "DateTimeFormatter.DateTimeFormatter(YearFormat yearFormat, MonthFormat monthFormat, DayFormat dayFormat, DayOfWeekFormat dayOfWeekFormat)");
		}

		public DateTimeFormatter(
			HourFormat hourFormat,
			MinuteFormat minuteFormat,
			SecondFormat secondFormat)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Globalization.DateTimeFormatting.DateTimeFormatter", "DateTimeFormatter.DateTimeFormatter(HourFormat hourFormat, MinuteFormat minuteFormat, SecondFormat secondFormat)");
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
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Globalization.DateTimeFormatting.DateTimeFormatter", "DateTimeFormatter.DateTimeFormatter(YearFormat yearFormat, MonthFormat monthFormat, DayFormat dayFormat, DayOfWeekFormat dayOfWeekFormat, HourFormat hourFormat, MinuteFormat minuteFormat, SecondFormat secondFormat, IEnumerable<string> languages)");
		}

		public DateTimeFormatter(YearFormat yearFormat, MonthFormat monthFormat, DayFormat dayFormat, DayOfWeekFormat dayOfWeekFormat, HourFormat hourFormat, MinuteFormat minuteFormat, SecondFormat secondFormat, IEnumerable<string> languages, string geographicRegion, string calendar, string clock)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Globalization.DateTimeFormatting.DateTimeFormatter", "DateTimeFormatter.DateTimeFormatter(YearFormat yearFormat, MonthFormat monthFormat, DayFormat dayFormat, DayOfWeekFormat dayOfWeekFormat, HourFormat hourFormat, MinuteFormat minuteFormat, SecondFormat secondFormat, IEnumerable<string> languages, string geographicRegion, string calendar, string clock)");
		}

		private IDictionary<string, string> BuildLookup(string language)
		{
			if(_map_cache.TryGetValue(language, out var map))
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
				{ "hour"                                 , "H tt" } ,
				{ "minute"                               , "m" },
				{ "second"                               , "s" },
				{ "timezone"                             , "%z" },
				// { "year month day hour"                  , "" } ,
			};

			return _map_cache[language] = map;
		}

		public string Format(DateTimeOffset value)
		{
			var format = GetSystemTemplate();
			try
			{
				return value.ToString(format, _firstCulture.DateTimeFormat);
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

		private static readonly IDictionary<string, IDictionary<string, string>> _map_cache;
		private static readonly IDictionary<(string language, string template), string> _patterns_cache;

		private readonly CultureInfo _firstCulture;

		private readonly IDictionary<string, string>[] _maps;

		private string GetSystemTemplate()
		{
			var result = Template.Replace("{", "").Replace("}", "");

			var map = _maps[0];

			foreach (var p in map)
			{
				result = result.Replace(p.Key, p.Value);
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
			string sanitizedFormat = default;

			for (var i = 0; i < Languages.Count; i++)
			{
				var language = Languages[i];
				if (_patterns_cache.TryGetValue((language, format), out var pattern))
				{
					yield return pattern;
				}
				else
				{
					var map = _maps[i];
					sanitizedFormat ??= format
						.Replace("{", "")
						.Replace("}", "");
					if (map.TryGetValue(sanitizedFormat, out var r))
					{
						foreach (var p in PatternsReplacements)
						{
							r = p.pattern.Replace(r, p.replacement);
						}

						yield return _patterns_cache[(language, format)] = r;
					}
				}
			}
		}
	}
}
