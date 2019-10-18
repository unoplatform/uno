using System;
using System.Collections.Generic;
using System.Globalization;

namespace Windows.Globalization.DateTimeFormatting
{
	public sealed partial class DateTimeFormatter
	{
		private static readonly IReadOnlyList<string> _defaultPatterns =
			new List<string>
			{
				"‎{month.full}‎ ‎{day.integer}‎, ‎{year.full}",
				"‎{day.integer}‎ ‎{month.full}‎, ‎{year.full}",
			}.AsReadOnly();

		private static readonly IReadOnlyList<string> _emptyLanguages =
			new List<string>(0).AsReadOnly();

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

		public IReadOnlyList<string> Languages { get; } = _emptyLanguages;

		public string Calendar { get; }

		public IReadOnlyList<string> Patterns { get; } = _defaultPatterns;

		public string ResolvedGeographicRegion { get; }

		public string ResolvedLanguage { get; }

		public string Template { get; }

		public static DateTimeFormatter LongDate { get; } = new DateTimeFormatter("longdate");

		public static DateTimeFormatter LongTime { get; } = new DateTimeFormatter("longtime");

		public static DateTimeFormatter ShortDate { get; } = new DateTimeFormatter("shortdate");

		public static DateTimeFormatter ShortTime { get; } = new DateTimeFormatter("shorttime");

		public DateTimeFormatter(string formatTemplate)
			:this(formatTemplate, languages: null)
		{
		}

		public DateTimeFormatter(
			string formatTemplate,
			IEnumerable<string> languages)
		{
			Template = formatTemplate ?? throw new ArgumentNullException(nameof(formatTemplate));
			if (languages != null)
			{
				Languages = new List<string>(languages).AsReadOnly();
			}

			BuildLookup();
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

		private static void BuildLookup()
		{
			_info = DateTimeFormatInfo.CurrentInfo;
			if (_info == _static_info)
			{
				_map = _static_map;
				return;
			}

			_static_info = _info;

			_map = _static_map = new Dictionary<string, string>
			{
				{ "longdate"                             , _info.LongDatePattern } ,
				{ "shortdate"                            , _info.ShortDatePattern } ,
				{ "longtime"                             , _info.LongTimePattern } ,
				{ "shorttime"                            , _info.ShortTimePattern } ,
				{ "dayofweek day month year"             , "D" } ,
				{ "dayofweek day month"                  , "D" } ,
				{ "day month year"                       , "MMMM dd, yyyy" } ,
				{ "day month"                            , "MMMM dd" } ,
				{ "month year"                           , "MMMM yyyy" } ,
				{ "dayofweek.full"                       , "dddd" } ,
				{ "dayofweek.abbreviated"                , "ddd" } ,
				{ "month.full"                           , "MMMM" } ,
				{ "month.abbreviated"                    , "MMM" } ,
				{ "month.numeric"                        , "%M" } ,
				{ "year.abbreviated"                     , "yy" } ,
				{ "year.full"                            , "yyyy" } ,
				{ "hour minute second"                   , "T" },
				{ "hour minute"                          , "t" },
				{ "timezone.abbreviated"                 , "zz" },
				{ "timezone.full"                        , "zzz" },
				{ "dayofweek"                            , "dddd" } ,
				{ "day"                                  , "%d" } ,
				{ "month"                                , "MMMM" } ,
				{ "year"                                 , "yyyy" } ,
				{ "hour"                                 , "h tt" } ,
				{ "minute"                               , "m" },
				{ "second"                               , "s" },
				{ "timezone"                             , "%z" },
                // { "year month day hour"                  , "" } ,
            };
		}

		public string Format(DateTimeOffset value)
		{
			var format = GetSystemTemplate();
			try
			{
				return value.ToLocalTime().ToString(format, _info);
			}
			catch (Exception e)
			{
				return format + " : " + e.Message;
			}
		}

		private static DateTimeFormatInfo _static_info;
		private static Dictionary<string, string> _static_map;

		private static DateTimeFormatInfo _info;
		private static Dictionary<string, string> _map;

		private string GetSystemTemplate()
		{
			var result = Template.Replace("{", "").Replace("}", "");

			foreach (var p in _map)
			{
				result = result.Replace(p.Key, p.Value);
			}

			return result;
		}
	}
}
