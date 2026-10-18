#nullable enable

using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Globalization;
using Windows.Globalization.DateTimeFormatting;

namespace Uno.UI.Tests.Windows_Globalization;

[UITestClass]
public class Given_DateTimeFormatter
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/18834")]
	[DataRow("en-US")]
	[DataRow("en-CA")]
	[DataRow("es-US")]
	[DataRow("ko-KR")]
	public void When_Period_Has_Ideal_Length_Keeps_Whole_Designator(string cultureName)
		=> Assert.AreEqual(new CultureInfo(cultureName).DateTimeFormat.PMDesignator, FormatPeriod("{period.abbreviated(2)}", cultureName));

	[TestMethod]
	public void When_Period_Has_Ideal_Length_One_Uses_First_Character()
		=> Assert.AreEqual("P", FormatPeriod("{period.abbreviated(1)}", "en-US"));

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	[DataRow("shorttime", "{hour.integer(1)}:{minute.integer(2)} {period.abbreviated(2)}")]
	[DataRow("hour minute", "{hour.integer(1)}:{minute.integer(2)} {period.abbreviated(2)}")]
	[DataRow("hour", "{hour.integer(1)} {period.abbreviated(2)}")]
	[DataRow("longtime", "{hour.integer(1)}:{minute.integer(2)}:{second.integer(2)} {period.abbreviated(2)}")]
	[DataRow("month", "{month.solo.full}")]
	[DataRow("month.numeric", "{month.integer}")]
	[DataRow("dayofweek.abbreviated", "{dayofweek.solo.abbreviated}")]
	[DataRow("year.abbreviated", "{year.abbreviated}")]
	[DataRow("dayofweek month day", "{dayofweek.full}, {month.full} {day.integer}")]
	[DataRow("shortdate shorttime", "{month.integer}/{day.integer}/{year.full} {hour.integer(1)}:{minute.integer(2)} {period.abbreviated(2)}")]
	public void When_Template_Names_Some_Parts_Then_Pattern_Has_Only_Those(string template, string expectedPattern)
		=> Assert.AreEqual(expectedPattern, new DateTimeFormatter(template, ["en-US"]).Patterns[0].Replace('\u202F', ' '));

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	[DataRow("de-DE", "{hour.integer(2)}:00")]
	[DataRow("ko-KR", "{period.abbreviated(2)} {hour.integer(1)}")]
	public void When_Template_Is_Hour_Then_Pattern_Follows_Short_Time(string language, string expectedPattern)
		=> Assert.AreEqual(expectedPattern, new DateTimeFormatter("hour", [language]).Patterns[0].Replace('\u202F', ' '));

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	[DataRow("en-CA", ClockIdentifiers.TwelveHourValue)]
	[DataRow("ko-KR", ClockIdentifiers.TwelveHourValue)]
	[DataRow("en-GB", ClockIdentifiers.TwentyFourHourValue)]
	public void When_Created_From_Components_Then_Clock_Follows_Languages(string language, string expectedClock)
	{
		var originalOverride = ApplicationLanguages.PrimaryLanguageOverride;
		try
		{
			ApplicationLanguages.PrimaryLanguageOverride = language;

			Assert.AreEqual(expectedClock, new DateTimeFormatter(HourFormat.Default, MinuteFormat.Default, SecondFormat.None).Clock);
			Assert.AreEqual(expectedClock, new DateTimeFormatter(YearFormat.Default, MonthFormat.Default, DayFormat.Default, DayOfWeekFormat.None).Clock);
			Assert.AreEqual(
				expectedClock,
				new DateTimeFormatter(
					YearFormat.None,
					MonthFormat.None,
					DayFormat.None,
					DayOfWeekFormat.None,
					HourFormat.Default,
					MinuteFormat.Default,
					SecondFormat.None,
					[language]).Clock);
		}
		finally
		{
			ApplicationLanguages.PrimaryLanguageOverride = originalOverride;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	[DataRow("en-US", "HebrewCalendar", "{month.integer(2)}/{day.integer(2)}/{year.full}", 2024, 1, 15, "05/05/5784")]
	[DataRow("en-US", "HebrewCalendar", "{month.full}", 2024, 1, 15, "שבט")]
	[DataRow("en-US", "HebrewCalendar", "{month.full}", 2024, 2, 15, "אדר א׳")]
	[DataRow("en-US", "HebrewCalendar", "{month.full}", 2024, 3, 15, "אדר ב׳")]
	[DataRow("en-US", "HebrewCalendar", "{hour.integer}:{minute.integer(2)} {period.abbreviated}", 2024, 1, 15, "1:05 PM")]
	[DataRow("en-US", "ThaiCalendar", "month year", 2024, 1, 15, "January 2567")]
	[DataRow("en-US", "ThaiCalendar", "{year.abbreviated(2)}", 2100, 3, 1, "43")]
	[DataRow("en-US", "JulianCalendar", "{month.integer(2)}/{day.integer(2)}/{year.full}", 2024, 1, 15, "01/02/2024")]
	[DataRow("en-US", "JulianCalendar", "month year", 2100, 3, 1, "February 2100")]
	[DataRow("en-US", "JapaneseCalendar", "{era.abbreviated} {year.full}", 2024, 1, 15, "令和 6")]
	[DataRow("ar-SA", "GregorianCalendar", "month year", 2024, 1, 15, "يناير 2024")]
	[DataRow("ar-SA", "GregorianCalendar", "month year", 2100, 3, 1, "مارس 2100")]
	[DataRow("en-US", "KoreanCalendar", "{era.abbreviated}", 2024, 1, 15, "")]
	[DataRow("en-US", "JulianCalendar", "{era.abbreviated}", 2024, 1, 15, "")]
	[DataRow("de-DE", "GregorianCalendar", "{era.abbreviated}", 2024, 1, 15, "n. Chr.")]
	[DataRow("en-US", "GregorianCalendar", "{era.abbreviated(1)}", 2024, 1, 15, "AD")]
	[DataRow("ja-JP", "GregorianCalendar", "{era.abbreviated(1)}", 2024, 1, 15, "西暦")]
	[DataRow("ja-JP", "JapaneseCalendar", "{era.abbreviated(1)}", 2024, 1, 15, "令")]
	public void When_Calendar_Is_Given_Then_Dates_Follow_It(string language, string calendar, string template, int year, int month, int day, string expected)
	{
		var formatter = new DateTimeFormatter(template, [language], "ZZ", calendar, ClockIdentifiers.TwelveHour);

		Assert.AreEqual(calendar, formatter.Calendar);
		Assert.AreEqual(expected, formatter.Format(new DateTimeOffset(new DateTime(year, month, day, 13, 5, 7))).Replace('\u202F', ' '));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	public void When_Calendar_Has_No_DotNet_Calendar_Then_Formatter_Is_Created()
	{
		// .NET has no Korean lunar calendar, so the language's calendar formats, which WinRT may not do.
		// This only checks that the identifier is still accepted, as it was before calendars were used.
		var formatter = new DateTimeFormatter("month year", ["en-US"], "ZZ", CalendarIdentifiers.KoreanLunar, ClockIdentifiers.TwelveHour);

		Assert.AreEqual(CalendarIdentifiers.KoreanLunar, formatter.Calendar);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	public void When_Created_From_Components_With_Calendar_Then_Dates_Follow_It()
	{
		var formatter = new DateTimeFormatter(
			YearFormat.Default,
			MonthFormat.Full,
			DayFormat.None,
			DayOfWeekFormat.None,
			HourFormat.None,
			MinuteFormat.None,
			SecondFormat.None,
			["en-US"],
			"ZZ",
			CalendarIdentifiers.Thai,
			ClockIdentifiers.TwelveHour);

		Assert.AreEqual(CalendarIdentifiers.Thai, formatter.Calendar);
		Assert.AreEqual("January 2567", formatter.Format(new DateTimeOffset(new DateTime(2024, 1, 15, 12, 0, 0))));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	public void When_Languages_Change_Then_Default_Formatters_Follow()
	{
		var originalOverride = ApplicationLanguages.PrimaryLanguageOverride;
		try
		{
			ApplicationLanguages.PrimaryLanguageOverride = "en-US";
			Assert.AreEqual(ClockIdentifiers.TwelveHour, DateTimeFormatter.ShortTime.Clock);

			ApplicationLanguages.PrimaryLanguageOverride = "de-DE";
			Assert.AreEqual(ClockIdentifiers.TwentyFourHour, DateTimeFormatter.ShortTime.Clock);
			Assert.AreEqual(ClockIdentifiers.TwentyFourHour, DateTimeFormatter.LongTime.Clock);
			Assert.AreEqual("{day.integer(2)}.{month.integer(2)}.{year.full}", DateTimeFormatter.ShortDate.Patterns[0]);
			Assert.AreEqual("de-DE", DateTimeFormatter.LongDate.Languages[0]);
		}
		finally
		{
			ApplicationLanguages.PrimaryLanguageOverride = originalOverride;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19349")]
	[DataRow("de-DE", ClockIdentifiers.TwelveHourValue, "shorttime", "{hour.integer}:{minute.integer(2)} {period.abbreviated}")]
	[DataRow("de-DE", ClockIdentifiers.TwelveHourValue, "longtime", "{hour.integer}:{minute.integer(2)}:{second.integer(2)} {period.abbreviated}")]
	[DataRow("de-DE", ClockIdentifiers.TwelveHourValue, "hour", "{hour.integer} {period.abbreviated}")]
	[DataRow("ja-JP", ClockIdentifiers.TwelveHourValue, "shorttime", "{period.abbreviated} {hour.integer}:{minute.integer(2)}")]
	[DataRow("en-US", ClockIdentifiers.TwentyFourHourValue, "shorttime", "{hour.integer(1)}:{minute.integer(2)}")]
	[DataRow("en-US", ClockIdentifiers.TwentyFourHourValue, "hour", "{hour.integer(1)}:00")]
	[DataRow("en-US", ClockIdentifiers.TwentyFourHourValue, "shortdate shorttime", "{month.integer}/{day.integer}/{year.full} {hour.integer(1)}:{minute.integer(2)}")]
	[DataRow("ko-KR", ClockIdentifiers.TwentyFourHourValue, "shorttime", "{hour.integer(1)}:{minute.integer(2)}")]
	[DataRow("en-US", ClockIdentifiers.TwelveHourValue, "{hour.integer}:{minute.integer(2)}", "{hour.integer}:{minute.integer(2)}")]
	public void When_Clock_Differs_From_Language_Then_Pattern_Follows_Clock(string language, string clock, string template, string expectedPattern)
		=> Assert.AreEqual(expectedPattern, new DateTimeFormatter(template, [language], "US", CalendarIdentifiers.Gregorian, clock).Patterns[0].Replace('\u202F', ' '));

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19349")]
	[DataRow("cs-CZ", ClockIdentifiers.TwelveHourValue, "1:05 dop.", "1:05 odp.")]
	[DataRow("en-US", ClockIdentifiers.TwentyFourHourValue, "1:05", "13:05")]
	public void When_Clock_Differs_From_Language_Then_Format_Tells_Hours_Apart(string language, string clock, string expectedEarly, string expectedLate)
	{
		var formatter = new DateTimeFormatter("shorttime", [language], "US", CalendarIdentifiers.Gregorian, clock);

		Assert.AreEqual(expectedEarly, formatter.Format(new DateTimeOffset(new DateTime(2024, 1, 15, 1, 5, 0))).Replace('\u202F', ' '));
		Assert.AreEqual(expectedLate, formatter.Format(new DateTimeOffset(new DateTime(2024, 1, 15, 13, 5, 0))).Replace('\u202F', ' '));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19349")]
	public void When_Period_Is_Glued_To_Hour_Then_24_Hour_Pattern_Drops_It()
	{
		// zh-TW's short time is "tth:mm". WinRT pads its 24-hour hour, which .NET's data doesn't.
		var pattern = new DateTimeFormatter("shorttime", ["zh-TW"], "US", CalendarIdentifiers.Gregorian, ClockIdentifiers.TwentyFourHour).Patterns[0];

		StringAssert.StartsWith(pattern, "{hour.integer");
		Assert.IsFalse(pattern.Contains("{period", StringComparison.Ordinal), pattern);
	}

	private static string FormatPeriod(string pattern, string cultureName)
		=> new DateTimeFormatter(pattern, [cultureName], "ZZ", CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour)
			.Format(new DateTimeOffset(2024, 1, 15, 17, 15, 0, TimeSpan.Zero));
}
