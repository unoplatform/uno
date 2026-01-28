#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Uno;
using Uno.Globalization.NumberFormatting;
using _Calendar = global::System.Globalization.Calendar;

namespace Windows.Globalization
{
	public partial class Calendar
	{
		#region Static id parsing helpers
		private static _Calendar GetCalendar(string calendar)
		{
			return calendar switch
			{
				CalendarIdentifiers.JulianValue => new global::System.Globalization.JulianCalendar(),
				CalendarIdentifiers.GregorianValue => new global::System.Globalization.GregorianCalendar(),
				CalendarIdentifiers.HebrewValue => new global::System.Globalization.HebrewCalendar(),
				CalendarIdentifiers.HijriValue => new global::System.Globalization.HijriCalendar(),
				CalendarIdentifiers.JapaneseValue => new global::System.Globalization.JapaneseCalendar(),
				CalendarIdentifiers.KoreanValue => new global::System.Globalization.KoreanCalendar(),
				CalendarIdentifiers.TaiwanValue => new global::System.Globalization.TaiwanCalendar(),
				CalendarIdentifiers.ThaiValue => new global::System.Globalization.ThaiBuddhistCalendar(),
				CalendarIdentifiers.UmAlQuraValue => new global::System.Globalization.UmAlQuraCalendar(),
				CalendarIdentifiers.PersianValue => new global::System.Globalization.PersianCalendar(),
				CalendarIdentifiers.ChineseLunarValue => new global::System.Globalization.ChineseLunisolarCalendar(),
				// Not supported by UWP as of 2019-05-23
				// https://docs.microsoft.com/en-us/uwp/api/windows.globalization.calendaridentifiers
				// case CalendarIdentifiers.VietnameseLunarValue: return new global::System.Globalization.VietnameseLunarCalendar();
				// case CalendarIdentifiers.TaiwanLunarValue: return new global::System.Globalization.TaiwanLunarCalendar();
				// case CalendarIdentifiers.KoreanLunarValue: return new global::System.Globalization.KoreanLunarCalendar();
				// case CalendarIdentifiers.JapaneseLunarValue: return new global::System.Globalization.JapaneseLunarCalendar();
				_ => throw new ArgumentException(nameof(calendar), $"Unknown calendar {calendar}."),
			};
		}

		private static string GetCalendarSystem(_Calendar calendar)
		{
			return calendar switch
			{
				global::System.Globalization.JulianCalendar _ => CalendarIdentifiers.Julian,
				global::System.Globalization.GregorianCalendar _ => CalendarIdentifiers.Gregorian,
				global::System.Globalization.HebrewCalendar _ => CalendarIdentifiers.Hebrew,
				global::System.Globalization.HijriCalendar _ => CalendarIdentifiers.Hijri,
				global::System.Globalization.JapaneseCalendar _ => CalendarIdentifiers.Japanese,
				global::System.Globalization.KoreanCalendar _ => CalendarIdentifiers.Korean,
				global::System.Globalization.TaiwanCalendar _ => CalendarIdentifiers.Taiwan,
				global::System.Globalization.ThaiBuddhistCalendar _ => CalendarIdentifiers.Thai,
				global::System.Globalization.UmAlQuraCalendar _ => CalendarIdentifiers.UmAlQura,
				global::System.Globalization.PersianCalendar _ => CalendarIdentifiers.Persian,
				global::System.Globalization.ChineseLunisolarCalendar _ => CalendarIdentifiers.ChineseLunar,
				global::System.Globalization.TaiwanLunisolarCalendar _ => CalendarIdentifiers.TaiwanLunar,
				global::System.Globalization.KoreanLunisolarCalendar _ => CalendarIdentifiers.KoreanLunar,
				global::System.Globalization.JapaneseLunisolarCalendar _ => CalendarIdentifiers.JapaneseLunar,
				// Missing support in System.Globalization for VietnameseLunar calendar.
				// case CalendarIdentifiers.VietnameseLunar: return new global::System.Globalization.VietnameseLunarCalendar();
				_ => throw new ArgumentException(nameof(calendar), $"Unknown calendar {calendar}."),
			};
		}

		private static string GetDefaultClock() =>
			// Windows.System.UserProfile.GlobalizationPreferences.Clocks.FirstOrDefault();
			new DateTime(1983, 9, 9, 13, 0, 0).ToString("g", CultureInfo.CurrentCulture).Contains("PM")
				? ClockIdentifiers.TwelveHour
				: ClockIdentifiers.TwentyFourHour;

		private static string GetClock(string clock)
		{
			switch (clock)
			{
				case ClockIdentifiers.TwelveHourValue:
				case ClockIdentifiers.TwentyFourHourValue:
					return clock;

				default: throw new ArgumentException(nameof(clock));
			}
		}

		/// <summary>
		/// Finds a TimeZoneInfo by its ID, supporting both IANA and Windows timezone IDs.
		/// </summary>
		private static TimeZoneInfo FindTimeZoneById(string timeZoneId)
		{
			// .NET 6+ supports both IANA and Windows timezone IDs through TimeZoneInfo.FindSystemTimeZoneById
			// It will automatically convert between the two formats as needed
			try
			{
				return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
			}
			catch (TimeZoneNotFoundException)
			{
				// Try to convert between IANA and Windows IDs
				if (TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out var windowsId))
				{
					return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
				}
				if (TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZoneId, out var ianaId))
				{
					return TimeZoneInfo.FindSystemTimeZoneById(ianaId);
				}
				throw;
			}
		}

		/// <summary>
		/// Gets the IANA timezone ID from a TimeZoneInfo if available, otherwise returns the Windows ID.
		/// </summary>
		private static string GetTimeZoneId(TimeZoneInfo timeZone)
		{
			// Prefer IANA IDs as they are more portable across platforms
			if (timeZone.HasIanaId)
			{
				return timeZone.Id;
			}

			// Try to convert Windows ID to IANA ID
			if (TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZone.Id, out var ianaId))
			{
				return ianaId;
			}

			// Fall back to the Windows ID
			return timeZone.Id;
		}
		#endregion

		private IReadOnlyList<string> _languages;
		private readonly CultureInfo _resolvedCulture;
		private _Calendar _calendar;
		private TimeZoneInfo _timeZone;
		private string _clock;
		private string _numeralSystem;

		// This is needed to be WindowsFoundationDateTime rather than System.DateTimeOffset.
		// It's done this way to ensure that setting _time = someSystemDateTimeOffset will go through
		// the implicit conversion from WindowsFoundationDateTime to System.DateTimeOffset
		// Then retrieving the _time as System.DateTimeOffset will go through the opposite implicit conversion.
		private global::Windows.Foundation.WindowsFoundationDateTime _time;

		private DateTime DateTime => ((DateTimeOffset)_time).DateTime;

		public Calendar()
		{
			_languages = ApplicationLanguages.Languages;
			_resolvedCulture = CultureInfo.CurrentCulture;
			_calendar = CultureInfo.CurrentCulture.Calendar;
			_timeZone = TimeZoneInfo.Local;
			_clock = GetDefaultClock();
			_numeralSystem = NumeralSystemTranslatorHelper.GetNumeralSystem(_languages[0]);
			_time = DateTimeOffset.Now;
		}

		public Calendar(IEnumerable<string> languages)
		{
			_languages = languages.ToList();
			_calendar = CultureInfo.CurrentCulture.Calendar;
			_resolvedCulture = CultureInfo.CurrentCulture;
			_timeZone = TimeZoneInfo.Local;
			_clock = GetDefaultClock();
			_numeralSystem = NumeralSystemTranslatorHelper.GetNumeralSystem(_languages[0]);
			_time = DateTimeOffset.Now;
		}

		public Calendar(IEnumerable<string> languages, string calendar, string clock)
		{
			_languages = languages.ToList();
			_resolvedCulture = CultureInfo.CurrentCulture;
			_calendar = GetCalendar(calendar);
			_timeZone = TimeZoneInfo.Local;
			_clock = GetClock(clock);
			_numeralSystem = NumeralSystemTranslatorHelper.GetNumeralSystem(_languages[0]);
			_time = DateTimeOffset.Now;
		}

		public Calendar(IEnumerable<string> languages, string calendar, string clock, string timeZoneId)
		{
			_languages = languages.ToList();
			_resolvedCulture = CultureInfo.CurrentCulture;
			_calendar = GetCalendar(calendar);
			_timeZone = FindTimeZoneById(timeZoneId);
			_clock = GetClock(clock);
			_numeralSystem = NumeralSystemTranslatorHelper.GetNumeralSystem(_languages[0]);
			_time = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _timeZone);
		}

		private Calendar(IReadOnlyList<string> languages, _Calendar calendar, TimeZoneInfo timeZone, string clock, string numeralSystem, DateTimeOffset time)
		{
			_languages = languages;
			_resolvedCulture = CultureInfo.CurrentCulture;
			_calendar = calendar;
			_timeZone = timeZone;
			_clock = clock;
			_numeralSystem = numeralSystem;
			_time = time;
		}

		public void CopyTo(Calendar other)
		{
			other._languages = _languages;
			other._calendar = _calendar;
			other._timeZone = _timeZone;
			other._clock = _clock;
			other._numeralSystem = _numeralSystem;
			other._time = _time;
		}

		public Calendar Clone()
			=> new Calendar(_languages, _calendar, _timeZone, _clock, _numeralSystem, _time);

		#region Read / Write settings (_languages, _calendar, _clock, _timeZone)
		public string NumeralSystem
		{
			get => _numeralSystem;
			set
			{
				// Validate that the numeral system is supported
				var digits = NumeralSystemTranslatorHelper.GetDigitsSource(value);
				if (digits == NumeralSystemTranslatorHelper.EmptyDigits && value != "Latn")
				{
					throw new ArgumentException($"The numeral system '{value}' is not supported.", nameof(value));
				}
				_numeralSystem = value;
			}
		}

		public IReadOnlyList<string> Languages => _languages;

		public string ResolvedLanguage => _languages[0];

		public string GetCalendarSystem()
			=> GetCalendarSystem(_calendar);

		public void ChangeCalendarSystem(string value)
			=> _calendar = GetCalendar(value);

		public string GetClock()
			=> _clock;

		public void ChangeClock(string value)
			=> _clock = GetClock(value);

		public string GetTimeZone()
			=> GetTimeZoneId(_timeZone);

		public void ChangeTimeZone(string timeZoneId)
		{
			var newTimeZone = FindTimeZoneById(timeZoneId);
			// Convert the current time to the new time zone
			_time = TimeZoneInfo.ConvertTime((DateTimeOffset)_time, newTimeZone);
			_timeZone = newTimeZone;
		}
		#endregion

		#region Read / Write _time
		public int Era
		{
			get => _calendar.GetEra(DateTime);
			set
			{
				var currentEra = _calendar.GetEra(DateTime);
				if (value != currentEra)
				{
					AddEras(value - currentEra);
				}
			}
		}

		public int Year
		{
			get => _calendar.GetYear(DateTime);
			set => AddYears(value - Year);
		}

		public int Month
		{
			get => _calendar.GetMonth(DateTime);
			set => AddMonths(value - Month);
		}

		public global::Windows.Globalization.DayOfWeek DayOfWeek => (global::Windows.Globalization.DayOfWeek)_calendar.GetDayOfWeek(DateTime);

		public int Day
		{
			get => _calendar.GetDayOfMonth(DateTime);
			set => AddDays(value - Day);
		}

		public int Hour
		{
			get
			{
				var hour = _calendar.GetHour(DateTime);

				if (_clock == ClockIdentifiers.TwelveHour)
				{
					if (hour == 0 || hour == 12)
					{
						return 12;
					}
					else if (hour > 12)
					{
						return hour - 12;
					}
				}

				// For 24-hour clock, or 12-hour clock with hour < 12
				return hour;
			}
			set
			{
				// Validate value against both 12-hour and 24-hour clock
				if (value < 0 || value >= 24)
				{
					throw new ArgumentException(nameof(value));
				}

				if (_clock == ClockIdentifiers.TwelveHour &&
					(value == 0 || value > 12))
				{
					throw new ArgumentException(nameof(value));
				}

				var twentyFourHourValue = value;

				if (_clock == ClockIdentifiers.TwelveHour)
				{
					if (Period == 1 && value == 12)
					{
						// 12 AM == 0
						twentyFourHourValue = 0;
					}
					else if (Period == 2 && value != 12)
					{
						// 12 PM == 12, 1 PM == 13, etc.
						twentyFourHourValue += 12;
					}
				}

				var currentHours = _calendar.GetHour(DateTime);
				AddHours(twentyFourHourValue - currentHours);
			}
		}

		public int Minute
		{
			get => _calendar.GetMinute(DateTime);
			set => AddMinutes(value - Minute);
		}

		public int Second
		{
			get => _calendar.GetSecond(DateTime);
			set => AddSeconds(value - Second);
		}

		public int Period
		{
			get => _clock == ClockIdentifiers.TwentyFourHour || _time.Hour < 12 ? 1 : 2;
			set
			{
				var currentPeriod = Period;
				if (value != currentPeriod)
				{
					if ((value <= 0 || value > 2) ||
						(value != 1 && _clock == ClockIdentifiers.TwentyFourHour))
					{
						throw new ArgumentException(nameof(value));
					}

					if (value == 1)
					{
						AddHours(-12);
					}
					else
					{
						AddHours(12);
					}
				}
			}
		}

		public int Nanosecond
		{
			get => (int)(_calendar.GetMilliseconds(DateTime) * 1000);
			set => AddNanoseconds(value - Nanosecond);
		}

		public void SetDateTime(global::System.DateTimeOffset value)
			=> _time = value;

		internal void SetDateTime(global::Windows.Foundation.WindowsFoundationDateTime value)
			=> _time = value;

		public void SetToNow()
			=> _time = DateTimeOffset.Now;

		internal void SetToday() // Useful helper not part of UWP contract
			=> _time = DateTime.Today;

		public void SetToMin()
		{
			// We add a Year to avoid issues with different calendars when used by CalendarView.
			// The problem happens when CalendarView calls SetToMin, then attempts to set either Day or Month to an earlier value.
			// This works in C++, but not in C# (due to the way .NET DateTimeOffset behaves)
			// So, we add a Year to the min date to avoid CalendarView from crashing.
			// Even though the extra year is, unfortunately, not correct, it's unlikely to badly affect anything.
			// NOTE: We need to add the extra year even if MinSupportedDateTime is not 01/01/0001.
			// For example, Japanese calendar min date is 08/09/1868. Then, CalendarView will attempt to change the month to 1
			// which will fail.
			var calendarMinSupportedDateTime = _calendar.MinSupportedDateTime.AddYears(1);

			_time = calendarMinSupportedDateTime;
		}

		public void SetToMax()
		{
			var calendarMaxSupportedDateTime = _calendar.MaxSupportedDateTime;
			if (calendarMaxSupportedDateTime == DateTime.MaxValue)
			{
				// Workaround for CalendarView crash when the local time zone is negative.
				// When the max supported date is 9999/12/31, it can cause issues if the local time zone
				// is negative (e.g., UTC - 5) because trying to convert that to local will overflow.
				// This is a known issue even in WinUI.
				// If SetToMax is called followed by GetDateTime, it will crash in WinUI.
				// The crash doesn't happen on the native side, so CalendarView usually works.
				// It happens when transitioning from the native Windows.Foundation.DateTime to System.DateTime.
				// So far, we only know of issues when MaxSupportedDateTime is DateTime.MaxValue.
				// If, in the future, we found similar issues with non-MaxValue date times, we could make this unconditional.
				calendarMaxSupportedDateTime = calendarMaxSupportedDateTime.AddYears(-1);
			}

			_time = calendarMaxSupportedDateTime;
		}

		public DateTimeOffset GetDateTime()
			=> _time;

		public void AddEras(int eras)
		{
			if (eras == 0)
			{
				return;
			}

			// For most calendars, eras are just historical markers and don't have a consistent
			// duration that can be added. However, for calendars like Japanese calendar where
			// eras have actual meaning, we need to handle this specially.
			var currentEra = _calendar.GetEra(DateTime);
			var targetEra = currentEra + eras;

			// Validate the target era is within bounds
			if (targetEra < FirstEra || targetEra > LastEra)
			{
				throw new ArgumentOutOfRangeException(nameof(eras), $"The resulting era {targetEra} is out of range.");
			}

			// For calendars with multiple eras (like Japanese), we need to find a date in the target era
			// We do this by moving year by year until we reach the target era
			var step = eras > 0 ? 1 : -1;
			while (_calendar.GetEra(DateTime) != targetEra)
			{
				AddYears(step);

				// Safety check to avoid infinite loops
				if (_calendar.GetEra(DateTime) == targetEra)
				{
					break;
				}

				// Check if we've gone too far (past the target)
				var newEra = _calendar.GetEra(DateTime);
				if ((step > 0 && newEra > targetEra) || (step < 0 && newEra < targetEra))
				{
					// Back up and try moving by months instead
					AddYears(-step);
					while (_calendar.GetEra(DateTime) != targetEra)
					{
						AddMonths(step);
						newEra = _calendar.GetEra(DateTime);
						if (newEra == targetEra || (step > 0 && newEra > targetEra) || (step < 0 && newEra < targetEra))
						{
							break;
						}
					}
					break;
				}
			}
		}

		public void AddYears(int years)
			=> _time = _calendar.AddYears(DateTime, years);

		public void AddMonths(int months)
			=> _time = _calendar.AddMonths(DateTime, months);

		public void AddWeeks(int weeks)
			=> _time = _calendar.AddWeeks(DateTime, weeks);

		public void AddDays(int days)
			=> _time = _calendar.AddDays(DateTime, days);

		public void AddPeriods(int periods)
			=> AddHours((_clock == ClockIdentifiers.TwentyFourHour ? 24 : 12) * periods);

		public void AddHours(int hours)
			=> _time = _calendar.AddHours(DateTime, hours);

		public void AddMinutes(int minutes)
			=> _time = _calendar.AddMinutes(DateTime, minutes);

		public void AddSeconds(int seconds)
			=> _time = _calendar.AddSeconds(DateTime, seconds);

		public void AddNanoseconds(int nanoseconds)
			=> _time = _calendar.AddMilliseconds(DateTime, nanoseconds / 1000d);
		#endregion

		#region IComparable
		public int Compare(global::Windows.Globalization.Calendar other)
			=> _time.CompareTo(other._time);

		public int CompareDateTime(global::System.DateTimeOffset other)
			=> _time.CompareTo(other);
		#endregion

		public bool IsDaylightSavingTime
			=> TimeZoneInfo.Local.IsDaylightSavingTime(_time);

		public int NumberOfEras => _calendar.Eras.Length;
		public int NumberOfYearsInThisEra => LastYearInThisEra - FirstYearInThisEra;
		public int NumberOfMonthsInThisYear => _calendar.GetMonthsInYear(Year, Era);
		public int NumberOfDaysInThisMonth => _calendar.GetDaysInMonth(Year, Month, Era);
		public int NumberOfPeriodsInThisDay => _clock == ClockIdentifiers.TwentyFourHour ? 1 : 2;
		public int NumberOfHoursInThisPeriod => _clock == ClockIdentifiers.TwentyFourHour ? 24 : 12;
		public int NumberOfMinutesInThisHour => 60;
		public int NumberOfSecondsInThisMinute => 60;

		public int FirstEra => _calendar.Eras.First();
		public int FirstYearInThisEra => _calendar.MinSupportedDateTime.Year;
		public int FirstMonthInThisYear => 1;
		public int FirstDayInThisMonth => 1;
		public int FirstPeriodInThisDay => 1;
		public int FirstHourInThisPeriod => _clock == ClockIdentifiers.TwentyFourHour ? 0 : 12;
		public int FirstMinuteInThisHour => 0;
		public int FirstSecondInThisMinute => 0;

		public int LastEra => _calendar.Eras.Last();
		public int LastYearInThisEra => _calendar.MaxSupportedDateTime.Year;
		public int LastMonthInThisYear => _calendar.GetMonthsInYear(Year);
		public int LastDayInThisMonth => _calendar.GetDaysInMonth(Year, Month, Era);
		public int LastPeriodInThisDay => _clock == ClockIdentifiers.TwentyFourHour ? 1 : 2;
		public int LastHourInThisPeriod => _clock == ClockIdentifiers.TwentyFourHour ? 23 : 11;
		public int LastMinuteInThisHour => 59;
		public int LastSecondInThisMinute => 59;

		public string EraAsString()
			=> GetEraName(_calendar, Era, _resolvedCulture, idealLength: 0);

		public string EraAsString(int idealLength)
			=> GetEraName(_calendar, Era, _resolvedCulture, idealLength);

		private static string GetEraName(_Calendar calendar, int era, CultureInfo culture, int idealLength)
		{
			// Try to get era name from DateTimeFormatInfo
			var eraNames = culture.DateTimeFormat.GetEraName(era);
			if (!string.IsNullOrEmpty(eraNames))
			{
				// If idealLength is specified and shorter than the full name, try abbreviated
				if (idealLength > 0 && idealLength < eraNames.Length)
				{
					var abbreviated = culture.DateTimeFormat.GetAbbreviatedEraName(era);
					if (!string.IsNullOrEmpty(abbreviated))
					{
						return abbreviated;
					}
				}
				return eraNames;
			}

			// Fallback for calendars where .NET doesn't provide era names
			return calendar switch
			{
				global::System.Globalization.JapaneseCalendar => GetJapaneseEraName(era, idealLength),
				global::System.Globalization.HebrewCalendar => idealLength > 0 && idealLength < 3 ? "AM" : "A.M.",
				global::System.Globalization.HijriCalendar => idealLength > 0 && idealLength < 3 ? "AH" : "A.H.",
				global::System.Globalization.ThaiBuddhistCalendar => idealLength > 0 && idealLength < 3 ? "BE" : "B.E.",
				global::System.Globalization.KoreanCalendar => idealLength > 0 && idealLength < 4 ? "단기" : "단기",
				global::System.Globalization.TaiwanCalendar => idealLength > 0 && idealLength < 4 ? "民國" : "民國",
				_ => idealLength > 0 && idealLength < 4 ? "AD" : "A.D."
			};
		}

		private static string GetJapaneseEraName(int era, int idealLength)
		{
			// Japanese era names (most recent first in era numbering)
			// Era numbers are in reverse chronological order in .NET
			// Era 1 is Meiji, Era 2 is Taisho, Era 3 is Showa, Era 4 is Heisei, Era 5 is Reiwa
			var (fullName, abbreviation) = era switch
			{
				5 => ("令和", "R"),
				4 => ("平成", "H"),
				3 => ("昭和", "S"),
				2 => ("大正", "T"),
				1 => ("明治", "M"),
				_ => ("", "")
			};

			if (string.IsNullOrEmpty(fullName))
			{
				return string.Empty;
			}

			// Return abbreviated if idealLength suggests it
			if (idealLength > 0 && idealLength == 1)
			{
				return abbreviation;
			}

			return fullName;
		}

		public string YearAsString()
			=> _time.Year.ToString(_resolvedCulture);

		public string YearAsTruncatedString(int remainingDigits)
			=> _time.ToString("yy", _resolvedCulture);

		public string YearAsPaddedString(int minDigits)
			=> _time.Year.ToString(new string('0', minDigits), _resolvedCulture);

		internal string MonthAsFullString()
			=> MonthAsString();

		public string MonthAsString()
			=> _time.ToString("MMM", _resolvedCulture);

		public string MonthAsString(int idealLength)
			=> _time.ToString("MMM", _resolvedCulture);

		public string MonthAsSoloString()
			=> _time.ToString("MMM", _resolvedCulture);
		public string MonthAsSoloString(int idealLength)
			=> _time.ToString("MMM", _resolvedCulture);

		public string MonthAsNumericString()
			=> _time.Month.ToString(_resolvedCulture);

		public string MonthAsPaddedNumericString(int minDigits)
			=> _time.Month.ToString(new string('0', minDigits), _resolvedCulture);

		public string DayAsString()
			=> Day.ToString(_resolvedCulture);

		public string DayAsPaddedString(int minDigits)
			=> Day.ToString(new string('0', minDigits), _resolvedCulture);

		public string DayOfWeekAsString()
			=> _resolvedCulture.DateTimeFormat.GetDayName(_calendar.GetDayOfWeek(DateTime));

		internal string DayOfWeekAsFullString()
			=> DayOfWeekAsString();

		public string DayOfWeekAsString(int idealLength)
			=> _resolvedCulture.DateTimeFormat.GetAbbreviatedDayName(_calendar.GetDayOfWeek(DateTime));

		public string DayOfWeekAsSoloString()
			=> _resolvedCulture.DateTimeFormat.GetDayName(_calendar.GetDayOfWeek(DateTime));

		public string DayOfWeekAsSoloString(int idealLength)
			=> _resolvedCulture.DateTimeFormat.GetDayName(_calendar.GetDayOfWeek(DateTime));

		public string PeriodAsString()
			=> _time.ToString("tt", _resolvedCulture);

		public string PeriodAsString(int idealLength)
			=> _time.ToString("tt", _resolvedCulture);

		public string HourAsString() =>
			_time
				.ToString(_clock == ClockIdentifiers.TwentyFourHour ? "HH" : "hh", _resolvedCulture)
				.TrimStart('0');

		public string HourAsPaddedString(int minDigits) =>
			_clock == ClockIdentifiers.TwentyFourHour
				? _time.ToString(minDigits < 2 ? "H" : "HH", _resolvedCulture)
				: _time.ToString(minDigits < 2 ? "h" : "hh", _resolvedCulture);

		public string MinuteAsString()
			=> _time.Minute.ToString(_resolvedCulture);

		public string MinuteAsPaddedString(int minDigits)
			=> _time.Minute.ToString(new string('0', minDigits), _resolvedCulture);

		public string SecondAsString()
			=> _time.Second.ToString(_resolvedCulture);

		public string SecondAsPaddedString(int minDigits)
			=> _time.Second.ToString(new string('0', minDigits), _resolvedCulture);

		public string NanosecondAsString()
			=> (_time.Millisecond * 1000).ToString(_resolvedCulture);

		public string NanosecondAsPaddedString(int minDigits)
			=> (_time.Millisecond * 1000).ToString(new string('0', minDigits), _resolvedCulture);

		public string TimeZoneAsString()
			=> _time.ToString("z", _resolvedCulture);

		public string TimeZoneAsString(int idealLength)
			=> _time.ToString("zz", _resolvedCulture);

		public static implicit operator DateTimeOffset(Calendar c)
		{
			return c.GetDateTime();
		}
		public static implicit operator Calendar(DateTimeOffset dto)
		{
			var calendar = new Calendar();
			calendar.SetDateTime(dto);
			return calendar;
		}
	}
}
