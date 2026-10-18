using System;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Globalization;
using CultureInfo = System.Globalization.CultureInfo;

namespace Uno.UI.RuntimeTests.Tests.Windows_Globalization;

[TestClass]
public class Given_Calendar
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Julian_Calendar()
	{
		// It's very important that this test doesn't crash, because that's what CalendarView does when using Julian calendar.
		// NOTE: Running this test on WinUI will crash!!
		// However, the crash happens when transitioning from Windows.Foundation.DateTime to System.DateTime (due to negative ticks).
		// So, the same code in C++ won't crash, which is why we care about this test as CalendarView is ported from C++.
		var calendar = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Julian, ClockIdentifiers.TwelveHour);
		calendar.SetToMin();

		// Doesn't match Windows, but to avoid crashes with CalendarView
		Assert.AreEqual(new DateTime(2, 1, 1), calendar.GetDateTime().Date);

		Assert.AreEqual(3, calendar.Day);

		calendar.Day = 1;
		Assert.AreEqual(1, calendar.Day);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24761")]
	public void When_Languages_Differ_From_CurrentCulture_Then_Names_Follow_Languages()
	{
		var culture = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = new CultureInfo("en-US");

			var calendar = new Calendar(new[] { "fr-FR" }, CalendarIdentifiers.Gregorian, ClockIdentifiers.TwentyFourHour);
			calendar.SetDateTime(new DateTimeOffset(new DateTime(2024, 1, 15, 12, 0, 0)));

			Assert.AreEqual("lundi", calendar.DayOfWeekAsString());
			Assert.AreEqual("lundi", calendar.Clone().DayOfWeekAsString());

			var other = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
			calendar.CopyTo(other);
			Assert.AreEqual("lundi", other.DayOfWeekAsString());
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24761")]
	[DataRow("de-DE", "en-US", "24HourClock", "GregorianCalendar")]
	[DataRow("en-US", "de-DE", "12HourClock", "GregorianCalendar")]
	[DataRow("th-TH", "en-US", "24HourClock", "ThaiCalendar")]
	public void When_Languages_Differ_From_CurrentCulture_Then_Clock_And_System_Follow_Languages(string language, string currentCulture, string expectedClock, string expectedSystem)
	{
		DefaultClockHelper.AssumeStockTimeFormat(language);
		using var device = DefaultClockHelper.UseDeviceWithoutClockSetting();

		var culture = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = new CultureInfo(currentCulture);

			var calendar = new Calendar(new[] { language });

			Assert.AreEqual(expectedSystem, calendar.GetCalendarSystem());
			Assert.AreEqual(expectedClock, calendar.GetClock());
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24761")]
	public void When_Calendar_Differs_From_Language_Default_Then_Names_Follow_Calendar()
	{
		var arabicGregorian = (CultureInfo)new CultureInfo("ar-SA").Clone();
		arabicGregorian.DateTimeFormat.Calendar = new global::System.Globalization.GregorianCalendar();
		if (arabicGregorian.DateTimeFormat.GetMonthName(1) == CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(1)
			|| new CultureInfo("he-IL").DateTimeFormat.GetMonthName(1) == CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(1))
		{
			Assert.Inconclusive("The ICU data loaded here has no Arabic or Hebrew names (the WebAssembly shard for the browser language).");
		}

		var date = new DateTimeOffset(new DateTime(2024, 1, 15, 12, 0, 0));

		// ar-SA defaults to the Um al-Qura calendar, in which this date falls in Rajab.
		var gregorian = new Calendar(new[] { "ar-SA" }, CalendarIdentifiers.Gregorian, ClockIdentifiers.TwentyFourHour);
		gregorian.SetDateTime(date);
		Assert.AreEqual("يناير", gregorian.MonthAsString());
		Assert.AreEqual("يناير", gregorian.Clone().MonthAsString());

		var other = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
		gregorian.CopyTo(other);
		Assert.AreEqual("يناير", other.MonthAsString());

		// Um al-Qura ends in 2077.
		gregorian.SetDateTime(new DateTimeOffset(new DateTime(2100, 3, 1, 12, 0, 0)));
		Assert.AreEqual("مارس", gregorian.MonthAsString());

		var changed = new Calendar(new[] { "ar-SA" });
		changed.ChangeCalendarSystem(CalendarIdentifiers.Gregorian);
		changed.SetDateTime(date);
		Assert.AreEqual("يناير", changed.MonthAsString());

		// en-US has no names for the Hebrew calendar, so, as in WinRT, they come from Hebrew, but the
		// period stays English.
		var hebrew = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Hebrew, ClockIdentifiers.TwelveHour);
		hebrew.SetDateTime(date);
		Assert.AreEqual(5, hebrew.Month);
		Assert.AreEqual("שבט", hebrew.MonthAsString());
		Assert.AreEqual("PM", hebrew.PeriodAsString());
		// WinRT separates the words with a no-break space.
		Assert.AreEqual("יום שני", hebrew.DayOfWeekAsString().Replace('\u00A0', ' '));

		// No culture formats with the Julian calendar, whose months are the Gregorian ones. January 20 is in
		// January in both calendars.
		var julian = new Calendar(new[] { "ar-SA" }, CalendarIdentifiers.Julian, ClockIdentifiers.TwentyFourHour);
		julian.SetDateTime(new DateTimeOffset(new DateTime(2024, 1, 20, 12, 0, 0)));
		Assert.AreEqual("يناير", julian.MonthAsString());
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	[DataRow("en-US", "ThaiCalendar", "Jan", "Monday", "2567")]
	[DataRow("en-US", "KoreanCalendar", "Jan", "Monday", "4357")]
	[DataRow("en-US", "TaiwanCalendar", "Jan", "Monday", "113")]
	[DataRow("de-DE", "ThaiCalendar", "Jan", "Montag", "2567")]
	[DataRow("de-DE", "KoreanCalendar", "Jan", "Montag", "4357")]
	[DataRow("de-DE", "TaiwanCalendar", "Jan", "Montag", "113")]
	public void When_Calendar_Has_Gregorian_Months_Then_Names_Follow_Language(string language, string calendarIdentifier, string expectedMonthPrefix, string expectedDayOfWeek, string expectedYear)
	{
		// As in WinRT, a language without names for these calendars keeps its own ("January 2567" for en-US).
		var calendar = new Calendar(new[] { language }, calendarIdentifier, ClockIdentifiers.TwentyFourHour);
		calendar.SetDateTime(new DateTimeOffset(new DateTime(2024, 1, 15, 12, 0, 0)));

		StringAssert.StartsWith(calendar.MonthAsString(), expectedMonthPrefix);
		Assert.AreEqual(expectedDayOfWeek, calendar.DayOfWeekAsString());
		Assert.AreEqual(expectedYear, calendar.YearAsString());
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // WinRT throws after SetToMin, see When_Julian_Calendar.
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24761")]
	[DataRow("ar-SA", "JulianCalendar")]
	[DataRow("fa-IR", "JulianCalendar")]
	[DataRow("fa-IR", "")]
	public void When_Calendar_At_Min_Then_MonthAsString_Does_Not_Throw(string language, string calendarIdentifier)
	{
		// The year view of CalendarView does this. Um al-Qura starts in 1900 and Persian in 622.
		var calendar = calendarIdentifier.Length > 0
			? new Calendar(new[] { language }, calendarIdentifier, ClockIdentifiers.TwentyFourHour)
			: new Calendar(new[] { language });
		calendar.SetToMin();

		Assert.IsFalse(string.IsNullOrEmpty(calendar.MonthAsString()));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24761")]
	[DataRow("th-TH", "", 2024, 1, 15, "2567", "67", "002567", "1", "01", "15", "")]
	[DataRow("ja-JP", "JapaneseCalendar", 2024, 1, 15, "6", "06", "000006", "1", "01", "15", "")]
	[DataRow("en-US", "JulianCalendar", 2024, 1, 5, "2023", "23", "002023", "12", "12", "23", "Dec")]
	public void When_Calendar_Is_Not_Gregorian_Then_Date_Strings_Follow_It(
		string language,
		string calendarIdentifier,
		int year,
		int month,
		int day,
		string expectedYear,
		string expectedTruncatedYear,
		string expectedPaddedYear,
		string expectedMonth,
		string expectedPaddedMonth,
		string expectedDay,
		string expectedMonthNamePrefix)
	{
		var calendar = calendarIdentifier.Length > 0
			? new Calendar(new[] { language }, calendarIdentifier, ClockIdentifiers.TwentyFourHour)
			: new Calendar(new[] { language });
		calendar.SetDateTime(new DateTimeOffset(new DateTime(year, month, day, 12, 0, 0)));

		Assert.AreEqual(expectedYear, calendar.YearAsString());
		Assert.AreEqual(expectedTruncatedYear, calendar.YearAsTruncatedString(2));
		Assert.AreEqual(expectedPaddedYear, calendar.YearAsPaddedString(6));
		Assert.AreEqual(expectedMonth, calendar.MonthAsNumericString());
		Assert.AreEqual(expectedPaddedMonth, calendar.MonthAsPaddedNumericString(2));
		Assert.AreEqual(expectedDay, calendar.DayAsString());

		// Uno abbreviates the names WinRT gives in full (Dec, December). The other rows skip the name, which
		// the WebAssembly ICU shard for an English browser lacks.
		StringAssert.StartsWith(calendar.MonthAsString(), expectedMonthNamePrefix);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24761")]
	[DataRow("en-US", "GregorianCalendar", 2024, 1, "4")]
	[DataRow("en-US", "GregorianCalendar", 2024, 2, "24")]
	[DataRow("en-US", "GregorianCalendar", 2024, 3, "024")]
	[DataRow("en-US", "GregorianCalendar", 2024, 4, "2024")]
	[DataRow("en-US", "GregorianCalendar", 2024, 5, "02024")]
	[DataRow("en-US", "GregorianCalendar", 2024, 6, "002024")]
	[DataRow("en-US", "GregorianCalendar", 2024, 10, "0000002024")]
	[DataRow("en-US", "GregorianCalendar", 5, 2, "05")]
	[DataRow("en-US", "GregorianCalendar", 5, 4, "0005")]
	[DataRow("en-US", "GregorianCalendar", 5, 6, "000005")]
	[DataRow("ja-JP", "JapaneseCalendar", 2024, 4, "0006")]
	[DataRow("ja-JP", "JapaneseCalendar", 2024, 6, "000006")]
	[DataRow("th-TH", "ThaiCalendar", 2024, 6, "002567")]
	public void When_Year_Is_Truncated_Then_Last_Digits_Are_Padded_To_Length(string language, string calendarIdentifier, int year, int remainingDigits, string expected)
	{
		// WinRT pads to the length even when it is longer than the year, which its docs don't say.
		var calendar = new Calendar(new[] { language }, calendarIdentifier, ClockIdentifiers.TwentyFourHour);
		calendar.SetDateTime(new DateTimeOffset(new DateTime(year, 1, 15, 12, 0, 0)));

		Assert.AreEqual(expected, calendar.YearAsTruncatedString(remainingDigits));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // WinRT uses the language's own digits (ה׳תשפ״ד, ١٤٤٥).
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24761")]
	[DataRow("en-US", "HebrewCalendar", "5784", "5", "5")]
	[DataRow("ar-SA", "", "1445", "7", "3")]
	public void When_Calendar_Is_Not_Gregorian_Then_Numbers_Follow_It(string language, string calendarIdentifier, string expectedYear, string expectedMonth, string expectedDay)
	{
		var calendar = calendarIdentifier.Length > 0
			? new Calendar(new[] { language }, calendarIdentifier, ClockIdentifiers.TwentyFourHour)
			: new Calendar(new[] { language });
		calendar.SetDateTime(new DateTimeOffset(new DateTime(2024, 1, 15, 12, 0, 0)));

		Assert.AreEqual(expectedYear, calendar.YearAsString());
		Assert.AreEqual(expectedMonth, calendar.MonthAsNumericString());
		Assert.AreEqual(expectedDay, calendar.DayAsString());
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24761")]
	[DataRow("24HourClock", "0", "00", "")]
	[DataRow("12HourClock", "12", "12", "AM")]
	public void When_Midnight_Then_Time_Strings_Follow_Clock(string clock, string expectedHour, string expectedPaddedHour, string expectedPeriod)
	{
		var calendar = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Gregorian, clock);
		calendar.SetDateTime(new DateTimeOffset(new DateTime(2024, 1, 15, 0, 5, 0)));

		Assert.AreEqual(expectedHour, calendar.HourAsString());
		Assert.AreEqual(expectedPaddedHour, calendar.HourAsPaddedString(2));
		Assert.AreEqual("05", calendar.MinuteAsPaddedString(2));
		Assert.AreEqual(expectedPeriod, calendar.PeriodAsString());
	}

	[TestMethod]
	public void When_Julian_Calendar_Day_Name()
	{
		var calendar = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Julian, ClockIdentifiers.TwelveHour);
		calendar.SetDateTime(new DateTimeOffset(new DateTime(2024, 2, 29)));

		Assert.AreEqual("16", calendar.DayAsString());
		Assert.AreEqual(DateTimeKind.Unspecified, calendar.GetDateTime().DateTime.Kind);
	}

	[TestMethod]
	[DataRow("JulianCalendar", "16", -1)]
	[DataRow("JulianCalendar", "16", 0)]
	[DataRow("JulianCalendar", "15", 1)]
	[DataRow("GregorianCalendar", "29", -1)]
	[DataRow("GregorianCalendar", "29", 0)]
	[DataRow("GregorianCalendar", "28", 1)]
#if RUNTIME_NATIVE_AOT
	[Ignore("DataRowAttribute.GetData() wraps data in an extra array under NativeAOT; not yet understood why.")]
#endif  // RUNTIME_NATIVE_AOT
	public void When_Calendar_Unspecified_DateTimeKind_Different_Offsets(string identifier, string expectedDayAsString, double offset)
	{
		if (TimeZoneInfo.Local.IsDaylightSavingTime(DateTimeOffset.Now))
		{
			// Don't attempt to change the specific datetime below to DateTime.Now or something similar. This test
			// tackles a very specific regression and changes might not capture the problem.
			Assert.Inconclusive("Local timezones with daylight saving can crash the DateTimeOffset constructor.");
		}

		offset += DateTimeOffset.Now.Offset.TotalHours;

		var calendar = new Calendar(new[] { "en-US" }, identifier, ClockIdentifiers.TwelveHour);
		var dateTime = new DateTimeOffset(new DateTime(2024, 2, 29, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.FromHours(offset));
		calendar.SetDateTime(dateTime);

		Assert.AreEqual(expectedDayAsString, calendar.DayAsString());
		Assert.IsTrue(dateTime.Equals(calendar.GetDateTime()));
		Assert.AreEqual(DateTimeKind.Unspecified, calendar.GetDateTime().DateTime.Kind);
		Assert.AreEqual(DateTimeOffset.Now.Offset, calendar.GetDateTime().Offset);
	}

	[TestMethod]
	[DataRow("JulianCalendar", "16")]
	[DataRow("GregorianCalendar", "29")]
	public void When_Calendar_Local_DateTimeKind(string identifier, string expectedDayAsString)
	{
		if (TimeZoneInfo.Local.IsDaylightSavingTime(DateTimeOffset.Now))
		{
			// Don't attempt to change the specific datetime below to DateTime.Now or something similar. This test
			// tackles a very specific regression and changes might not capture the problem.
			Assert.Inconclusive("Local timezones with daylight saving can crash the DateTimeOffset constructor.");
		}
		var calendar = new Calendar(new[] { "en-US" }, identifier, ClockIdentifiers.TwelveHour);
		var offset = DateTimeOffset.Now.Offset;
		var dateTime = new DateTimeOffset(new DateTime(2024, 2, 29, 0, 0, 0, DateTimeKind.Local), offset);
		calendar.SetDateTime(dateTime);

		Assert.AreEqual(expectedDayAsString, calendar.DayAsString());
		Assert.IsTrue(dateTime.Equals(calendar.GetDateTime()));
		Assert.AreEqual(DateTimeKind.Unspecified, calendar.GetDateTime().DateTime.Kind);
		Assert.AreEqual(DateTimeOffset.Now.Offset, calendar.GetDateTime().Offset);
	}

	[TestMethod]
	[DataRow("JulianCalendar", "16")]
	[DataRow("GregorianCalendar", "29")]
	public void When_Calendar_Utc_DateTimeKind(string identifier, string expectedDayAsString)
	{
		if (TimeZoneInfo.Local.IsDaylightSavingTime(DateTimeOffset.Now))
		{
			// Don't attempt to change the specific datetime below to DateTime.Now or something similar. This test
			// tackles a very specific regression and changes might not capture the problem.
			Assert.Inconclusive("Local timezones with daylight saving fail the offset assert.");
		}

		var calendar = new Calendar(new[] { "en-US" }, identifier, ClockIdentifiers.TwelveHour);
		var dateTime = new DateTimeOffset(new DateTime(2024, 2, 29, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);
		calendar.SetDateTime(dateTime);

		Assert.AreEqual(expectedDayAsString, calendar.DayAsString());
		Assert.IsTrue(dateTime.Equals(calendar.GetDateTime()));
		Assert.AreEqual(DateTimeKind.Unspecified, calendar.GetDateTime().DateTime.Kind);
		Assert.AreEqual(DateTimeOffset.Now.Offset, calendar.GetDateTime().Offset);
	}

	[TestMethod]
	public void When_Switch_Period()
	{
		var calendar = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
		calendar.Period = 1;
		calendar.Hour = 1;
		Assert.AreEqual(1, calendar.Hour);
		Assert.AreEqual(1, calendar.Period);
		var dateTime = calendar.GetDateTime();
		Assert.AreEqual(1, dateTime.Hour);

		calendar.Period = 2;
		Assert.AreEqual(1, calendar.Hour);
		Assert.AreEqual(2, calendar.Period);
		dateTime = calendar.GetDateTime();
		Assert.AreEqual(13, dateTime.Hour);

		calendar.Hour = 11;
		calendar.Period = 1;
		Assert.AreEqual(11, calendar.Hour);
		Assert.AreEqual(1, calendar.Period);
		dateTime = calendar.GetDateTime();
		Assert.AreEqual(11, dateTime.Hour);

		calendar.Period = 2;
		Assert.AreEqual(11, calendar.Hour);
		Assert.AreEqual(2, calendar.Period);
		dateTime = calendar.GetDateTime();
		Assert.AreEqual(23, dateTime.Hour);

		calendar.Hour = 12;
		calendar.Period = 1;
		Assert.AreEqual(12, calendar.Hour);
		Assert.AreEqual(1, calendar.Period);
		dateTime = calendar.GetDateTime();
		Assert.AreEqual(0, dateTime.Hour);

		calendar.Period = 2;
		Assert.AreEqual(12, calendar.Hour);
		Assert.AreEqual(2, calendar.Period);
		dateTime = calendar.GetDateTime();
		Assert.AreEqual(12, dateTime.Hour);
	}

	[TestMethod]
	public void When_TwentyFourHour_Invalid_Hours()
	{
		var calendar = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Gregorian, ClockIdentifiers.TwentyFourHour);
		Assert.ThrowsExactly<ArgumentException>(() => calendar.Hour = -1);
		Assert.ThrowsExactly<ArgumentException>(() => calendar.Hour = 24);
	}

	[TestMethod]
	public void When_TwentyFourHour_Invalid_Period()
	{
		var calendar = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Gregorian, ClockIdentifiers.TwentyFourHour);
		Assert.ThrowsExactly<ArgumentException>(() => calendar.Period = 2);
	}

	[TestMethod]
	public void When_TwelveHour_Invalid_Period()
	{
		var calendar = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
		Assert.ThrowsExactly<ArgumentException>(() => calendar.Period = 0);
		Assert.ThrowsExactly<ArgumentException>(() => calendar.Period = 3);
	}

	[TestMethod]
	public void When_TwentyFourHour()
	{
		var calendar = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Gregorian, ClockIdentifiers.TwentyFourHour);
		for (int hour = 0; hour <= 23; hour++)
		{
			calendar.Hour = hour;
			Assert.AreEqual(hour, calendar.Hour);
			var dateTime = calendar.GetDateTime();
			Assert.AreEqual(hour, dateTime.Hour);
		}
	}

	[TestMethod]
	public void When_TwelveHour_Invalid_Hours()
	{
		var calendar = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
		Assert.ThrowsExactly<ArgumentException>(() => calendar.Hour = 0);
		for (int hour = 13; hour <= 23; hour++)
		{
			Assert.ThrowsExactly<ArgumentException>(() => calendar.Hour = hour);
		}
	}

	[TestMethod]
	public void When_TwelveHour_First_Period()
	{
		var calendar = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
		calendar.Period = 1;
		for (int hour = 1; hour <= 12; hour++)
		{
			calendar.Hour = hour;
			Assert.AreEqual(hour, calendar.Hour);
			Assert.AreEqual(1, calendar.Period);
			var dateTime = calendar.GetDateTime();
			if (hour == 12)
			{
				Assert.AreEqual(0, dateTime.Hour);
			}
			else
			{
				Assert.AreEqual(hour, dateTime.Hour);
			}
		}
	}

	[TestMethod]
	public void When_TwelveHour_Second_Period()
	{
		var calendar = new Calendar(new[] { "en-US" }, CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
		calendar.Period = 2;
		for (int hour = 1; hour <= 12; hour++)
		{
			calendar.Hour = hour;
			Assert.AreEqual(hour, calendar.Hour);
			Assert.AreEqual(2, calendar.Period);
			var dateTime = calendar.GetDateTime();
			if (hour == 12)
			{
				Assert.AreEqual(12, dateTime.Hour);
			}
			else
			{
				Assert.AreEqual(hour + 12, dateTime.Hour);
			}
		}
	}
}
