using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Globalization.DateTimeFormatting;
using System.Globalization;
using Windows.Globalization;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_Globalization;

[TestClass]
public class Given_DateTimeFormatter
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24761")]
	[DataRow("de-DE", "en-US", "24HourClock", "13:05")]
	[DataRow("en-US", "en-GB", "12HourClock", "1:05 PM")]
	public void When_Language_Differs_From_CurrentCulture_Then_Time_Follows_Language(string language, string currentCulture, string expectedClock, string expectedTime)
	{
		DefaultClockHelper.AssumeStockTimeFormat(language);
		using var device = DefaultClockHelper.UseDeviceWithoutClockSetting();

		var culture = CultureInfo.CurrentCulture;
		var primaryLanguageOverride = ApplicationLanguages.PrimaryLanguageOverride;
		try
		{
			ApplicationLanguages.PrimaryLanguageOverride = language;
			CultureInfo.CurrentCulture = new CultureInfo(currentCulture);

			var formatter = new DateTimeFormatter("shorttime");
			var time = NormalizeText(formatter.Format(new DateTimeOffset(new DateTime(2024, 1, 15, 13, 5, 0))));

			// The pattern and the clock both come from the language, so the hour and the period agree.
			Assert.AreEqual(expectedClock, formatter.Clock);
			Assert.AreEqual(expectedClock, new global::Windows.Globalization.Calendar().GetClock());
			Assert.AreEqual(expectedTime, time);
		}
		finally
		{
			ApplicationLanguages.PrimaryLanguageOverride = primaryLanguageOverride;
			CultureInfo.CurrentCulture = culture;
		}
	}

	// WinRT marks the text direction (U+200E), and ICU may separate the period with a no-break space.
	private static string NormalizeText(string text)
		=> text.Replace("\u200E", "").Replace("\u200F", "").Replace('\u202F', ' ').Replace('\u00A0', ' ');

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	[DataRow("shorttime", "en-US", 13, "1:05 PM")]
	[DataRow("shorttime", "en-US", 0, "12:05 AM")]
	[DataRow("longtime", "en-US", 13, "1:05:07 PM")]
	[DataRow("hour minute", "en-US", 13, "1:05 PM")]
	[DataRow("hour", "en-US", 13, "1 PM")]
	[DataRow("shorttime", "de-DE", 13, "13:05")]
	[DataRow("hour", "de-DE", 13, "13:00")]
	[DataRow("month", "en-US", 13, "January")]
	[DataRow("month.abbreviated", "en-US", 13, "Jan")]
	[DataRow("dayofweek", "en-US", 13, "Monday")]
	[DataRow("dayofweek month day", "en-US", 13, "Monday, January 15")]
	[DataRow("dayofweek month day", "de-DE", 13, "Montag, 15. Januar")]
	[DataRow("year", "en-US", 13, "2024")]
	[DataRow("day month year", "en-US", 13, "January 15, 2024")]
	[DataRow("day month.full year", "de-DE", 13, "15. Januar 2024")]
	[DataRow("month.abbreviated day", "en-US", 13, "Jan 15")]
	[DataRow("month.numeric day", "en-US", 13, "1/15")]
	[DataRow("month.numeric day", "de-DE", 13, "15.1.")]
	[DataRow("month.numeric day", "fr-FR", 13, "15/1")]
	[DataRow("year.abbreviated month.abbreviated", "en-US", 13, "Jan 24")]
	[DataRow("dayofweek.abbreviated month.abbreviated day", "en-US", 13, "Mon, Jan 15")]
	[DataRow("year month.abbreviated day dayofweek.abbreviated", "en-US", 13, "Mon, Jan 15, 2024")]
	[DataRow("year month.numeric day", "en-US", 13, "1/15/2024")]
	[DataRow("year.abbreviated month day", "en-US", 13, "1/15/24")]
	public void When_Template_Names_Some_Parts_Then_Format_Has_Only_Those(string template, string language, int hour, string expected)
	{
		DefaultClockHelper.AssumeStockTimeFormat(language);
		using var device = DefaultClockHelper.UseDeviceWithoutClockSetting();

		var formatter = new DateTimeFormatter(template, new[] { language });

		Assert.AreEqual(expected, NormalizeText(formatter.Format(new DateTimeOffset(new DateTime(2024, 1, 15, hour, 5, 7)))));
	}

	// WinRT's text for each date production with each of its parts' formats, from a packaged WinRT probe. Some
	// formats don't count in some productions ("year month.numeric" is "January 2024").
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	[DataRow("en-US", "year month day dayofweek", "Friday, January 5, 2024")]
	[DataRow("de-DE", "year.full month.full day dayofweek.full", "Freitag, 5. Januar 2024")]
	[DataRow("fr-FR", "year.abbreviated month.abbreviated day dayofweek.abbreviated", "ven. 5 janv. 24")]
	[DataRow("hu-HU", "year month.numeric day dayofweek", "2024. január 5., péntek")]
	[DataRow("en-US", "year month day", "January 5, 2024")]
	[DataRow("de-DE", "year.full month.full day", "5. Januar 2024")]
	[DataRow("fr-FR", "year.abbreviated month.abbreviated day", "5 janv. 24")]
	[DataRow("hu-HU", "year month.numeric day", "2024. 01. 05.")]
	[DataRow("en-US", "month day dayofweek", "Friday, January 5")]
	[DataRow("de-DE", "month.full day dayofweek.full", "Freitag, 5. Januar")]
	[DataRow("fr-FR", "month.abbreviated day dayofweek.abbreviated", "ven. 5 janv.")]
	[DataRow("hu-HU", "month.numeric day dayofweek", "január 5., péntek")]
	[DataRow("en-US", "month day", "January 5")]
	[DataRow("de-DE", "month.full day", "5. Januar")]
	[DataRow("fr-FR", "month.abbreviated day", "5 janv.")]
	[DataRow("hu-HU", "month.numeric day", "1. 5.")]
	[DataRow("en-US", "day dayofweek", "Fri 5")]
	[DataRow("fr-FR", "day dayofweek.full", "ven. 5")]
	[DataRow("fr-FR", "day dayofweek.abbreviated", "ven. 5")]
	[DataRow("en-US", "dayofweek", "Friday")]
	[DataRow("de-DE", "dayofweek.full", "Freitag")]
	[DataRow("fr-FR", "dayofweek.abbreviated", "ven.")]
	[DataRow("en-US", "year month", "January 2024")]
	[DataRow("de-DE", "year.full month.full", "Januar 2024")]
	[DataRow("fr-FR", "year.abbreviated month.abbreviated", "janv. 24")]
	[DataRow("hu-HU", "year month.numeric", "2024. január")]
	[DataRow("en-US", "year", "2024")]
	[DataRow("de-DE", "year.full", "2024")]
	[DataRow("fr-FR", "year.abbreviated", "24")]
	[DataRow("en-US", "month", "January")]
	[DataRow("de-DE", "month.full", "Januar")]
	[DataRow("fr-FR", "month.abbreviated", "janv.")]
	[DataRow("hu-HU", "month.numeric", "1")]
	[DataRow("en-US", "day", "5")]
	[DataRow("en-US", "year month.numeric", "January 2024")]
	[DataRow("en-US", "month.numeric day dayofweek", "Friday, January 5")]
	[DataRow("en-US", "day dayofweek.full", "Fri 5")]
	[DataRow("de-DE", "year.abbreviated month.full day", "5. Januar 24")]
	[DataRow("fr-FR", "year.abbreviated month.full day", "5 janvier 24")]
	[DataRow("hu-HU", "year.abbreviated month.full day", "24. január 5.")]
	[DataRow("hu-HU", "year.abbreviated month.abbreviated day", "24. jan. 5.")]
	[DataRow("ja-JP", "year.abbreviated month.full day", "24年1月5日")]
	[DataRow("ja-JP", "year.abbreviated month.abbreviated day", "24年1月5日")]
	public void When_Parts_Have_Formats_Then_Text_Follows_WinRT(string language, string template, string expected)
	{
		if (language is not "en-US" && new CultureInfo(language).DateTimeFormat.GetMonthName(1) == CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(1))
		{
			Assert.Inconclusive($"The ICU data loaded here has no {language} names (the WebAssembly shard for the browser language).");
		}

		var formatter = new DateTimeFormatter(template, new[] { language });

		Assert.AreEqual(expected, NormalizeText(formatter.Format(new DateTimeOffset(new DateTime(2024, 1, 5, 13, 5, 7)))));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	[DataRow("ja-JP", "year", "2024年")]
	[DataRow("ja-JP", "day", "15日")]
	[DataRow("ko-KR", "year", "2024년")]
	[DataRow("ko-KR", "day", "15일")]
	[DataRow("de-DE", "day", "15")]
	[DataRow("hu-HU", "year", "2024")]
	[DataRow("ru-RU", "year", "2024 г.")]
	[DataRow("ru-RU", "year.abbreviated", "24 г.")]
	[DataRow("uk-UA", "year", "2024 р.")]
	[DataRow("lt-LT", "year", "2024 m.")]
	[DataRow("lt-LT", "day", "15")]
	[DataRow("zh-CN", "year", "2024年")]
	[DataRow("zh-CN", "day", "15日")]
	public void When_Template_Is_One_Part_Then_Format_Keeps_Language_Letters(string language, string template, string expected)
	{
		if (new CultureInfo(language).DateTimeFormat.GetMonthName(1) == CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(1))
		{
			Assert.Inconclusive($"The ICU data loaded here has no {language} names (the WebAssembly shard for the browser language).");
		}

		var formatter = new DateTimeFormatter(template, new[] { language });

		Assert.AreEqual(expected, NormalizeText(formatter.Format(new DateTimeOffset(new DateTime(2024, 1, 15, 13, 5, 7)))));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	public void When_Created_From_Components_Then_Formats_With_Language_Clock()
	{
		DefaultClockHelper.AssumeStockTimeFormat("en-US");
		using var device = DefaultClockHelper.UseDeviceWithoutClockSetting();

		var date = new DateTimeOffset(new DateTime(2024, 1, 15, 13, 5, 7));
		var defaultClock = new global::Windows.Globalization.Calendar().GetClock();

		var dateFormatter = new DateTimeFormatter(YearFormat.Default, MonthFormat.Default, DayFormat.Default, DayOfWeekFormat.None);
		Assert.AreEqual("year month day", dateFormatter.Template);
		Assert.AreEqual(defaultClock, dateFormatter.Clock);
		Assert.AreEqual(new DateTimeFormatter("year month day").Format(date), dateFormatter.Format(date));

		var timeFormatter = new DateTimeFormatter(HourFormat.Default, MinuteFormat.Default, SecondFormat.None);
		Assert.AreEqual("hour minute", timeFormatter.Template);
		Assert.AreEqual(defaultClock, timeFormatter.Clock);
		Assert.AreEqual(new DateTimeFormatter("shorttime").Format(date), timeFormatter.Format(date));

		var english = new DateTimeFormatter(
			YearFormat.None,
			MonthFormat.None,
			DayFormat.None,
			DayOfWeekFormat.None,
			HourFormat.Default,
			MinuteFormat.Default,
			SecondFormat.None,
			new[] { "en-US" });
		Assert.AreEqual(ClockIdentifiers.TwelveHour, english.Clock);
		Assert.AreEqual("1:05 PM", NormalizeText(english.Format(date)));

		var numeric = new DateTimeFormatter(YearFormat.None, MonthFormat.Numeric, DayFormat.Default, DayOfWeekFormat.None);
		Assert.AreEqual("month.numeric day", numeric.Template);
	}

	// WinRT gets only the instant, and formats it in local time whatever the value's offset.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	public void When_Offset_Is_Not_Local_Then_Format_Is_In_Local_Time()
	{
		var instant = new DateTimeOffset(2024, 1, 15, 3, 0, 0, TimeSpan.Zero);
		var local = TimeZoneInfo.ConvertTime(instant, TimeZoneInfo.Local);
		var formatter = new DateTimeFormatter("{year.full}-{month.integer(2)}-{day.integer(2)} {hour.integer(2)}:{minute.integer(2)}", new[] { "en-US" }, "US", CalendarIdentifiers.Gregorian, ClockIdentifiers.TwentyFourHour);

		foreach (var offset in new[] { TimeSpan.Zero, TimeSpan.FromHours(9), TimeSpan.FromHours(-11), local.Offset })
		{
			Assert.AreEqual(local.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), formatter.Format(instant.ToOffset(offset)), offset.ToString());
		}
	}

	// Just after the local clocks move forward, the same instant written with the standard offset reads as an hour
	// that doesn't exist locally. The zone and the time still follow the instant.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	public void When_Daylight_Time_Starts_Then_Zone_Follows_The_Instant()
	{
		var zone = TimeZoneInfo.Local;
		var instant = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
		while (instant.Year == 2024 && (zone.IsDaylightSavingTime(instant) || !zone.IsDaylightSavingTime(instant.AddHours(1))))
		{
			instant = instant.AddHours(1);
		}

		if (instant.Year != 2024)
		{
			Assert.Inconclusive("The local time zone has no daylight saving time.");
		}

		instant = instant.AddMinutes(90);
		var standardOffset = zone.GetUtcOffset(instant.AddDays(-1));
		var offset = zone.GetUtcOffset(instant);
		var expectedOffset = $"GMT{(offset < TimeSpan.Zero ? "-" : "+")}{offset.Duration().Hours}{(offset.Minutes == 0 ? "" : $":{offset.Duration().Minutes:00}")}";
		var expectedTime = TimeZoneInfo.ConvertTime(instant, zone).ToString("HH:mm", CultureInfo.InvariantCulture);
		var time = new DateTimeFormatter("{hour.integer(2)}:{minute.integer(2)}", new[] { "hu-HU" }, "ZZ", CalendarIdentifiers.Gregorian, ClockIdentifiers.TwentyFourHour);
		var abbreviated = new DateTimeFormatter("{timezone.abbreviated}", new[] { "hu-HU" });
		var full = new DateTimeFormatter("{timezone.full}", new[] { "en-US" });

		foreach (var value in new[] { instant.ToOffset(TimeSpan.Zero), instant.ToOffset(TimeSpan.FromHours(9)), instant.ToOffset(standardOffset), instant.ToOffset(offset) })
		{
			Assert.AreEqual(expectedTime, NormalizeText(time.Format(value)), value.ToString("O", CultureInfo.InvariantCulture));
			Assert.AreEqual(expectedOffset, NormalizeText(abbreviated.Format(value)), value.ToString("O", CultureInfo.InvariantCulture));
			Assert.AreEqual(zone.DaylightName, NormalizeText(full.Format(value)), value.ToString("O", CultureInfo.InvariantCulture));
		}
	}

	// WinRT's month in a date takes the date form ("15 января", Polish "15 maja"), and its standalone form keeps the
	// name on its own ("styczeń 2024"), with a day or without.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	[DataRow("ru-RU", "year month day", 1, "15 января 2024 г.")]
	[DataRow("ru-RU", "month day", 5, "15 мая")]
	[DataRow("ru-RU", "{month.full}", 1, "января")]
	[DataRow("ru-RU", "{month.full} {year.full}", 5, "мая 2024")]
	[DataRow("ru-RU", "{month.abbreviated}", 5, "мая")]
	[DataRow("pl-PL", "year month day dayofweek", 1, "poniedziałek, 15 stycznia 2024")]
	[DataRow("pl-PL", "month day", 5, "15 maja")]
	[DataRow("pl-PL", "year month", 1, "styczeń 2024")]
	[DataRow("pl-PL", "month", 5, "maj")]
	[DataRow("pl-PL", "{day.integer} {month.solo.full}", 1, "15 styczeń")]
	[DataRow("cs-CZ", "year month day", 5, "15. května 2024")]
	[DataRow("cs-CZ", "year month", 5, "květen 2024")]
	[DataRow("uk-UA", "year month day", 1, "15 січня 2024 р.")]
	[DataRow("uk-UA", "year month", 1, "січень 2024 р.")]
	[DataRow("el-GR", "month day", 1, "15 Ιανουαρίου")]
	[DataRow("el-GR", "year month", 5, "Μάιος 2024")]
	public void When_Month_Is_In_A_Date_Then_It_Takes_The_Date_Form(string language, string template, int month, string expected)
	{
		if (new CultureInfo(language).DateTimeFormat.GetMonthName(1) == CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(1))
		{
			Assert.Inconclusive($"The ICU data loaded here has no {language} names (the WebAssembly shard for the browser language).");
		}

		var formatter = new DateTimeFormatter(template, new[] { language });

		Assert.AreEqual(expected, NormalizeText(formatter.Format(new DateTimeOffset(new DateTime(2024, month, 15, 13, 5, 7)))));
	}

	// Hebrew leap years have two Adars, in a date or not.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	[DataRow("{day.integer} {month.full}")]
	[DataRow("{month.solo.full}")]
	public void When_Hebrew_Month_Is_In_A_Date_Then_Leap_Months_Stay_Apart(string pattern)
	{
		if (new CultureInfo("he-IL").DateTimeFormat.GetMonthName(1) == CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(1))
		{
			Assert.Inconclusive("The ICU data loaded here has no Hebrew names (the WebAssembly shard for the browser language).");
		}

		var formatter = new DateTimeFormatter(pattern, new[] { "en-US" }, "ZZ", CalendarIdentifiers.Hebrew, ClockIdentifiers.TwentyFourHour);

		StringAssert.Contains(NormalizeText(formatter.Format(new DateTimeOffset(new DateTime(2024, 2, 15, 13, 5, 7)))), "אדר א");
		StringAssert.Contains(NormalizeText(formatter.Format(new DateTimeOffset(new DateTime(2024, 3, 15, 13, 5, 7)))), "אדר ב");
	}

	// WinRT names the local time zone at the given time: in full, its standard or daylight name; abbreviated, the
	// GMT offset in a language without a short name for the zone (hu-HU "GMT-5" and "GMT-4" in New York, where
	// en-US writes "EST"), whatever the ideal length. The full names are the OS's, so these rows are in English.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	[DataRow("hu-HU", "timezone", "{timezone.abbreviated}")]
	[DataRow("hu-HU", "timezone.abbreviated", "{timezone.abbreviated}")]
	[DataRow("hu-HU", "{timezone.abbreviated(2)}", "{timezone.abbreviated(2)}")]
	[DataRow("en-US", "timezone.full", "{timezone.full}")]
	[DataRow("en-US", "{timezone.full}", "{timezone.full}")]
	public void When_Template_Is_Time_Zone_Then_Local_Zone_Is_Named(string language, string template, string expectedPattern)
	{
		var formatter = new DateTimeFormatter(template, new[] { language });
		Assert.AreEqual(expectedPattern, NormalizeText(formatter.Patterns[0]));

		var zone = TimeZoneInfo.Local;
		foreach (var date in new[] { new DateTime(2024, 1, 15, 13, 5, 7), new DateTime(2024, 7, 15, 13, 5, 7) })
		{
			var offset = zone.GetUtcOffset(date);
			var expected = expectedPattern.StartsWith("{timezone.full", StringComparison.Ordinal)
				? (zone.IsDaylightSavingTime(date) ? zone.DaylightName : zone.StandardName)
				: offset == TimeSpan.Zero
					? "GMT"
					: $"GMT{(offset < TimeSpan.Zero ? "-" : "+")}{offset.Duration().Hours}{(offset.Minutes == 0 ? "" : $":{offset.Duration().Minutes:00}")}";

			Assert.AreEqual(expected, NormalizeText(formatter.Format(new DateTimeOffset(date))), $"{template} in {date:MMMM}");
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	public void When_Template_Is_Day_And_Day_Of_Week_Or_Time_Zone_Then_Accepted()
	{
		var date = new DateTimeOffset(new DateTime(2024, 1, 15, 13, 5, 7));

		foreach (var template in new[] { "day dayofweek", "dayofweek day", "day dayofweek.abbreviated" })
		{
			var formatter = new DateTimeFormatter(template, new[] { "en-US" });
			Assert.AreEqual(template.Replace("dayofweek day", "day dayofweek"), formatter.Template);
			Assert.AreEqual("Mon 15", NormalizeText(formatter.Format(date)), template);
		}

		var components = new DateTimeFormatter(YearFormat.None, MonthFormat.None, DayFormat.Default, DayOfWeekFormat.Default);
		Assert.AreEqual("day dayofweek", components.Template);

		var timeZone = new DateTimeFormatter("timezone", new[] { "en-US" });
		Assert.AreEqual("timezone", timeZone.Template);
		StringAssert.StartsWith(NormalizeText(timeZone.Patterns[0]), "{timezone");

		// As in WinRT, a time zone can't go with a date.
		foreach (var template in new[] { "shortdate timezone", "longdate timezone", "month day timezone", "day dayofweek timezone", "timezone dayofweek" })
		{
			Assert.ThrowsExactly<ArgumentException>(() => new DateTimeFormatter(template, new[] { "en-US" }), template);
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	public void When_Components_Are_Not_A_Template_Then_Throws()
	{
		// As in WinRT, the components make a template, which has no year and day without a month.
		Assert.ThrowsExactly<ArgumentException>(() => new DateTimeFormatter(YearFormat.Default, MonthFormat.None, DayFormat.Default, DayOfWeekFormat.None));
		Assert.ThrowsExactly<ArgumentException>(() => new DateTimeFormatter(YearFormat.None, MonthFormat.None, DayFormat.None, DayOfWeekFormat.None));
		Assert.ThrowsExactly<ArgumentException>(() => new DateTimeFormatter(HourFormat.Default, MinuteFormat.None, SecondFormat.Default));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	public void When_Calendar_Is_Given_Then_Dates_Follow_It()
	{
		var noon = new DateTimeOffset(new DateTime(2024, 1, 15, 12, 0, 0));

		// en-US has no names for the Thai calendar, whose months are the Gregorian ones, so as in WinRT they stay English.
		var thai = new DateTimeFormatter("month year", new[] { "en-US" }, "ZZ", CalendarIdentifiers.Thai, ClockIdentifiers.TwelveHour);
		Assert.AreEqual(CalendarIdentifiers.Thai, thai.Calendar);
		Assert.AreEqual("January 2567", NormalizeText(thai.Format(noon)));
		Assert.AreEqual("01/15/2567", NormalizeText(new DateTimeFormatter("{month.integer(2)}/{day.integer(2)}/{year.full}", new[] { "en-US" }, "ZZ", CalendarIdentifiers.Thai, ClockIdentifiers.TwelveHour).Format(noon)));

		// March 1, 2100 is February 16 in the Julian calendar.
		var julian = new DateTimeFormatter("month year", new[] { "en-US" }, "ZZ", CalendarIdentifiers.Julian, ClockIdentifiers.TwelveHour);
		Assert.AreEqual("February 2100", NormalizeText(julian.Format(new DateTimeOffset(new DateTime(2100, 3, 1, 12, 0, 0)))));

		// The time parts and their AM/PM designators stay in the language.
		var time = new DateTimeOffset(new DateTime(2024, 1, 15, 13, 5, 0));
		Assert.AreEqual("1:05 PM", NormalizeText(new DateTimeFormatter("shorttime", new[] { "en-US" }, "ZZ", CalendarIdentifiers.Hebrew, ClockIdentifiers.TwelveHour).Format(time)));
		Assert.AreEqual("1:05 PM", NormalizeText(new DateTimeFormatter("{hour.integer}:{minute.integer(2)} {period.abbreviated}", new[] { "en-US" }, "ZZ", CalendarIdentifiers.Hebrew, ClockIdentifiers.TwelveHour).Format(time)));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	public void When_Calendar_Differs_From_Language_Default_Then_Names_Follow_It()
	{
		var arabicGregorian = (CultureInfo)new CultureInfo("ar-SA").Clone();
		arabicGregorian.DateTimeFormat.Calendar = new GregorianCalendar();
		if (arabicGregorian.DateTimeFormat.GetMonthName(1) == CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(1)
			|| new CultureInfo("he-IL").DateTimeFormat.GetMonthName(1) == CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(1))
		{
			Assert.Inconclusive("The ICU data loaded here has no Arabic or Hebrew names (the WebAssembly shard for the browser language).");
		}

		var january = new DateTimeOffset(new DateTime(2024, 1, 15, 12, 0, 0));
		var march2100 = new DateTimeOffset(new DateTime(2100, 3, 1, 12, 0, 0));

		// ar-SA defaults to the Um al-Qura calendar, in which January 15, 2024 falls in Rajab, and which ends in 2077.
		var arabic = new DateTimeFormatter("month year", new[] { "ar-SA" }, "ZZ", CalendarIdentifiers.Gregorian, ClockIdentifiers.TwentyFourHour);
		StringAssert.Contains(NormalizeText(arabic.Format(january)), "يناير");
		StringAssert.Contains(NormalizeText(arabic.Format(march2100)), "مارس");

		// DatePicker's month column.
		var month = new DateTimeFormatter("{month.full}", new[] { "ar-SA" }, "ZZ", CalendarIdentifiers.Gregorian, ClockIdentifiers.TwentyFourHour);
		Assert.AreEqual("مارس", NormalizeText(month.Format(march2100)));

		var components = new DateTimeFormatter(
			YearFormat.Default,
			MonthFormat.Full,
			DayFormat.None,
			DayOfWeekFormat.None,
			HourFormat.None,
			MinuteFormat.None,
			SecondFormat.None,
			new[] { "ar-SA" },
			"ZZ",
			CalendarIdentifiers.Gregorian,
			ClockIdentifiers.TwentyFourHour);
		StringAssert.Contains(NormalizeText(components.Format(march2100)), "مارس");

		// en-US has no names for the Hebrew calendar, so, as in WinRT, they come from Hebrew, with the two Adars of a
		// leap year. WinRT writes them without the geresh (אדר א) that .NET adds (אדר א׳).
		var hebrew = new DateTimeFormatter("month.full", new[] { "en-US" }, "ZZ", CalendarIdentifiers.Hebrew, ClockIdentifiers.TwelveHour);
		Assert.AreEqual("שבט", NormalizeText(hebrew.Format(january)));
		StringAssert.StartsWith(NormalizeText(hebrew.Format(new DateTimeOffset(new DateTime(2024, 2, 15, 12, 0, 0)))), "אדר א");
		StringAssert.StartsWith(NormalizeText(hebrew.Format(new DateTimeOffset(new DateTime(2024, 3, 15, 12, 0, 0)))), "אדר ב");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19349")]
	[DataRow("cs-CZ", "12HourClock", "shorttime", "1:05 dop.", "1:05 odp.")]
	[DataRow("cs-CZ", "12HourClock", "longtime", "1:05:07 dop.", "1:05:07 odp.")]
	[DataRow("cs-CZ", "12HourClock", "hour", "1 dop.", "1 odp.")]
	[DataRow("ja-JP", "12HourClock", "shorttime", "午前 1:05", "午後 1:05")]
	[DataRow("en-US", "24HourClock", "shorttime", "1:05", "13:05")]
	[DataRow("en-US", "24HourClock", "hour", "1:00", "13:00")]
	[DataRow("en-US", "24HourClock", "shortdate shorttime", "1/15/2024 1:05", "1/15/2024 13:05")]
	[DataRow("ko-KR", "24HourClock", "shorttime", "1:05", "13:05")]
	public void When_Clock_Differs_From_Language_Then_Time_Follows_Clock(string language, string clock, string template, string expectedEarly, string expectedLate)
	{
		if (language is not "en-US" && new CultureInfo(language).DateTimeFormat.PMDesignator == CultureInfo.InvariantCulture.DateTimeFormat.PMDesignator)
		{
			Assert.Inconclusive($"The ICU data loaded here has no {language} names (the WebAssembly shard for the browser language).");
		}

		// The language's own time pattern has the other clock (H:mm for cs-CZ, h:mm tt for en-US).
		var formatter = new DateTimeFormatter(template, new[] { language }, "US", CalendarIdentifiers.Gregorian, clock);

		Assert.AreEqual(expectedEarly, NormalizeText(formatter.Format(new DateTimeOffset(new DateTime(2024, 1, 15, 1, 5, 7)))));
		Assert.AreEqual(expectedLate, NormalizeText(formatter.Format(new DateTimeOffset(new DateTime(2024, 1, 15, 13, 5, 7)))));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	public void When_Languages_Change_Then_Default_Formatters_Follow()
	{
		DefaultClockHelper.AssumeStockTimeFormat("en-US");
		DefaultClockHelper.AssumeStockTimeFormat("de-DE");
		using var device = DefaultClockHelper.UseDeviceWithoutClockSetting();

		var primaryLanguageOverride = ApplicationLanguages.PrimaryLanguageOverride;
		try
		{
			var date = new DateTimeOffset(new DateTime(2024, 1, 15, 13, 5, 7));

			ApplicationLanguages.PrimaryLanguageOverride = "en-US";
			Assert.AreEqual("1:05 PM", NormalizeText(DateTimeFormatter.ShortTime.Format(date)));

			ApplicationLanguages.PrimaryLanguageOverride = "de-DE";
			Assert.AreEqual(ClockIdentifiers.TwentyFourHour, DateTimeFormatter.ShortTime.Clock);
			CollectionAssert.AreEqual(ApplicationLanguages.Languages.ToList(), DateTimeFormatter.ShortTime.Languages.ToList());
			Assert.AreEqual("13:05", NormalizeText(DateTimeFormatter.ShortTime.Format(date)));
			Assert.AreEqual("13:05:07", NormalizeText(DateTimeFormatter.LongTime.Format(date)));
			Assert.AreEqual("15.01.2024", NormalizeText(DateTimeFormatter.ShortDate.Format(date)));
			Assert.AreEqual("Montag, 15. Januar 2024", NormalizeText(DateTimeFormatter.LongDate.Format(date)));

			// As in WinRT, each read gives a new formatter.
			Assert.AreNotSame(DateTimeFormatter.ShortTime, DateTimeFormatter.ShortTime);
		}
		finally
		{
			ApplicationLanguages.PrimaryLanguageOverride = primaryLanguageOverride;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	[DataRow("de-DE", "GregorianCalendar", "{era.abbreviated}", "n. Chr.")]
	[DataRow("en-US", "GregorianCalendar", "{era.abbreviated(1)}", "AD")]
	[DataRow("ja-JP", "GregorianCalendar", "{era.abbreviated(1)}", "西暦")]
	[DataRow("ja-JP", "JapaneseCalendar", "{era.abbreviated}", "令和")]
	[DataRow("ja-JP", "JapaneseCalendar", "{era.abbreviated(1)}", "令")]
	[DataRow("en-US", "KoreanCalendar", "{era.abbreviated}", "")]
	[DataRow("en-US", "TaiwanCalendar", "{era.abbreviated}", "")]
	[DataRow("en-US", "JulianCalendar", "{era.abbreviated}", "")]
	[DataRow("en-US", "JapaneseCalendar", "{year.abbreviated}", "6")]
	[DataRow("en-US", "JapaneseCalendar", "{year.abbreviated(3)}", "006")]
	[DataRow("en-US", "TaiwanCalendar", "{year.abbreviated}", "13")]
	public void When_Calendar_Is_Given_Then_Era_And_Year_Follow_It(string language, string calendar, string pattern, string expected)
	{
		if (language is "ja-JP" && new CultureInfo(language).DateTimeFormat.PMDesignator == CultureInfo.InvariantCulture.DateTimeFormat.PMDesignator)
		{
			Assert.Inconclusive("The ICU data loaded here has no Japanese names (the WebAssembly shard for the browser language).");
		}

		// The era of the calendar in use, and none when the language has no names for it (en-US with Korean).
		var formatter = new DateTimeFormatter(pattern, new[] { language }, "ZZ", calendar, ClockIdentifiers.TwentyFourHour);

		Assert.AreEqual(expected, NormalizeText(formatter.Format(new DateTimeOffset(new DateTime(2024, 1, 15, 12, 0, 0)))));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // WinRT uses Hebrew numerals (ה׳/ה׳/ה׳תשפ״ד).
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	public void When_Calendar_Is_Hebrew_Then_Numbers_Follow_It()
	{
		var formatter = new DateTimeFormatter("{month.integer(2)}/{day.integer(2)}/{year.full}", new[] { "en-US" }, "ZZ", CalendarIdentifiers.Hebrew, ClockIdentifiers.TwelveHour);

		Assert.AreEqual("05/05/5784", formatter.Format(new DateTimeOffset(new DateTime(2024, 1, 15, 12, 0, 0))));
	}

	[TestMethod]
	public void When_CreatingWithFormatTemplate_ShouldInitializeCorrectly()
	{
		var formatter = new DateTimeFormatter("longdate");
		Assert.IsNotNull(formatter);
		Assert.AreEqual("longdate", formatter.Template);
	}

	[TestMethod]
	public void When_UsingCustomLanguages_ShouldSetLanguagesCorrectly()
	{
		var languages = new[] { "fr-FR", "en-US" };
		var formatter = new DateTimeFormatter("longdate", languages);

		CollectionAssert.AreEqual(languages, formatter.Languages.ToList());
	}

	[TestMethod]
	public void When_FormattingDateTimeOffset_ShouldProduceFormattedString()
	{
		var formatter = new DateTimeFormatter("longdate");
		var dateToFormat = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero);

		var formattedDate = formatter.Format(dateToFormat);
		Assert.IsNotNull(formattedDate);
		Assert.IsGreaterThan(0, formattedDate.Length);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_SettingNumeralSystem_ShouldUpdateProperty()
	{
		var formatter = new DateTimeFormatter("longdate");
		formatter.NumeralSystem = "arab";

		Assert.AreEqual("arab", formatter.NumeralSystem);
	}

	[TestMethod]
	public void When_CheckingPatterns_ShouldNotBeEmpty()
	{
		var formatter = new DateTimeFormatter("longdate");

		Assert.IsGreaterThan(0, formatter.Patterns.Count);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_CallingFormatWithTimeZone_ShouldThrowNotSupportedException()
	{
		var formatter = new DateTimeFormatter("longdate");
		var dateToFormat = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero);

		Assert.ThrowsExactly<NotSupportedException>(() =>
			formatter.Format(dateToFormat, "UTC"));
	}

	[TestMethod]
	public void When_CreatingFormatter_ShouldUseApplicationLanguages()
	{
		var formatter = new DateTimeFormatter("longdate");

		CollectionAssert.AreEqual(ApplicationLanguages.Languages.ToList(), formatter.Languages.ToList());
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_FormattingDateVariants_ShouldProduceExpectedFormats()
	{
		var expectedResults = new Dictionary<string, string>
		{
			{ "day", "27" },
			{ "year", "2024" },
			{ "{day.integer}/{month.integer}/{year.full}", "27/6/2024" },
			{ "{month.full} {year.full}", "June 2024" },
			{ "{month.full} {day.integer}", "June 27" },
			{ "{dayofweek.abbreviated}, {day.integer} {month.abbreviated} {year.full}", "Thu, 27 Jun 2024" },
			{ "{year.full}-{month.integer}-{day.integer}", "2024-6-27" },
			{ "{day.integer}/{month.integer}/{year.abbreviated}", "27/6/24" },
			{ "{day.integer} {month.abbreviated} {year.full}", "27 Jun 2024" },
			{ "{month.full} {day.integer}, {year.full}", "June 27, 2024" },
			{ "{month.abbreviated} {day.integer}, {year.full}", "Jun 27, 2024" },
			{ "{dayofweek.full}, {day.integer} {month.full} {year.full}", "Thursday, 27 June 2024" },
			{ "{dayofweek.abbreviated}, {day.integer} {month.abbreviated} {year.abbreviated}", "Thu, 27 Jun 24" }
		};

		foreach (var kvp in expectedResults)
		{
			var template = kvp.Key;
			var expected = kvp.Value;

			var formatter = new DateTimeFormatter(template);
			var formattedDate = formatter.Format(new DateTimeOffset(new DateTime(2024, 6, 27, 14, 30, 0)));

			Assert.AreEqual(expected, formattedDate, $"Mismatch for template: {template}");
		}
	}

	[TestMethod]
	public void When_FormattingTimeVariants_ShouldProduceExpectedFormats()
	{
		var expectedResults = new Dictionary<string, string>
		{
			{ "{hour.integer}:{minute.integer}", "14:30" },
			{ "{hour.integer}:{minute.integer}:{second.integer(2)}", "14:30:00" },
			{ "{hour.integer}:{minute.integer} {period.abbreviated(2)}", "2:30 PM" },
			{ "{hour.integer}:{minute.integer}:{second.integer} {period.abbreviated(2)}", "2:30:0 PM" },
			{ "{minute.integer}:{second.integer}", "30:0" },
			{ "{second.integer}", "0" },
			{ "{hour.integer}:{minute.integer}:{second.integer(2)} UTC", "14:30:00 UTC" }
		};

		foreach (var kvp in expectedResults)
		{
			var template = kvp.Key;
			var expected = kvp.Value;

			var clock = template.Contains("period") ? ClockIdentifiers.TwelveHour : ClockIdentifiers.TwentyFourHour;

			var formatter = new DateTimeFormatter(template, ["en-US"], "US", CalendarIdentifiers.Gregorian, clock);
			var formattedTime = formatter.Format(new DateTime(2024, 6, 17, 14, 30, 0));

			Assert.AreEqual(expected, formattedTime, $"Mismatch for template: {template}");
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23718")]
	public void When_CreatingWithHourPattern_ShouldNotThrowFirstChanceException()
	{
		// The pattern "{hour.integer(1)}" is what TimePicker/TimePickerFlyout uses to resolve the
		// default clock. Constructing the formatter must not raise a first-chance ArgumentException
		// from the internal template parser, which was noise when debugging with all CLR exceptions on.
		// FirstChanceException can fire from any thread, so we filter to the specific template-parser
		// message in the handler and guard the shared flag to avoid races and false positives.
		var sync = new object();
		var sawTemplateParseException = false;
		void OnFirstChance(object sender, FirstChanceExceptionEventArgs e)
		{
			if (e.Exception is ArgumentException && e.Exception.Message.Contains("Failed to parse date time template"))
			{
				lock (sync)
				{
					sawTemplateParseException = true;
				}
			}
		}

		AppDomain.CurrentDomain.FirstChanceException += OnFirstChance;
		try
		{
			var formatter = new DateTimeFormatter("{hour.integer(1)}");
			Assert.IsNotNull(formatter);
		}
		finally
		{
			AppDomain.CurrentDomain.FirstChanceException -= OnFirstChance;
		}

		bool sawException;
		lock (sync)
		{
			sawException = sawTemplateParseException;
		}

		Assert.IsFalse(
			sawException,
			"Constructing a DateTimeFormatter from a format pattern should not raise a first-chance template-parse exception (#23718).");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23718")]
	public void When_CreatingWithHourPattern_ShouldFormatCorrectly()
	{
		var formatter = new DateTimeFormatter("{hour.integer(1)}", ["en-US"], "US", CalendarIdentifiers.Gregorian, ClockIdentifiers.TwentyFourHour);

		Assert.AreEqual("{hour.integer(1)}", formatter.Template);
		CollectionAssert.Contains(formatter.Patterns.ToList(), "{hour.integer(1)}");

		var formatted = formatter.Format(new DateTimeOffset(new DateTime(2024, 6, 17, 14, 30, 0)));
		Assert.AreEqual("14", formatted);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23718")]
	public void When_TimeThenSpecificDate_ShouldKeepDatePortion()
	{
		// When a time component comes before a specific date (e.g. "hour shortdate"), the parsed
		// date node must be preserved in the normalized template rather than dropped.
		var formatter = new DateTimeFormatter("hour shortdate");

		Assert.AreEqual("shortdate hour", formatter.Template);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23718")]
	public void When_LongTimeTemplate_ShouldNormalizeTemplate()
	{
		// The IsLongTime flag must be copied from the parsed template so normalization keeps "longtime".
		var formatter = new DateTimeFormatter("longtime");

		Assert.AreEqual("longtime", formatter.Template);
	}

	// "it-US" is well-formed BCP-47, but runtimes with strict culture data (iOS in #12423) have no culture for it.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/12423")]
	public void When_Languages_Contains_Unsupported_Culture_Should_Not_Throw()
	{
		var formatter = new DateTimeFormatter("longdate", new[] { "it-US", "en-US" });

		Assert.IsNotNull(formatter);
		var formatted = formatter.Format(new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero));
		Assert.IsFalse(string.IsNullOrEmpty(formatted), "Formatter should produce a non-empty string even when the preferred language is unsupported.");
	}
}
