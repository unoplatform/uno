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

	private static string FormatPeriod(string pattern, string cultureName)
		=> new DateTimeFormatter(pattern, [cultureName], "ZZ", CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour)
			.Format(new DateTimeOffset(2024, 1, 15, 17, 15, 0, TimeSpan.Zero));
}
