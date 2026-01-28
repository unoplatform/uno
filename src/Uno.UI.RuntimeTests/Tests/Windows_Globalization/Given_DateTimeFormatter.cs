using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Globalization.DateTimeFormatting;
using System.Globalization;
using Windows.Globalization;
using System.Linq;
using System.Collections.Generic;

namespace Uno.UI.RuntimeTests.Tests.Windows_Globalization;

[TestClass]
public class Given_DateTimeFormatter
{
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
		// Use explicit pattern instead of "longdate" template which requires template parsing
		var formatter = new DateTimeFormatter("{dayofweek.full}, {month.full} {day.integer}, {year.full}", ["en-US"]);
		var dateToFormat = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero);

		var formattedDate = formatter.Format(dateToFormat);
		Assert.IsNotNull(formattedDate);
		Assert.AreEqual("Monday, January 15, 2024", formattedDate);
	}

	[TestMethod]
	public void When_SettingNumeralSystem_ShouldUpdateProperty()
	{
		var formatter = new DateTimeFormatter("longdate");
		formatter.NumeralSystem = "arab";

		Assert.AreEqual("arab", formatter.NumeralSystem);
	}

	[TestMethod]
	public void When_CheckingPatterns_ShouldNotBeEmpty()
	{
		// Use explicit pattern instead of "longdate" template
		var formatter = new DateTimeFormatter("{day.integer}/{month.integer}/{year.full}", ["en-US"]);

		Assert.AreEqual(1, formatter.Patterns.Count);
		Assert.AreEqual("{day.integer}/{month.integer}/{year.full}", formatter.Patterns[0]);
	}

	[TestMethod]
	public void When_CallingFormatWithTimeZone_ShouldFormatCorrectly()
	{
		var formatter = new DateTimeFormatter("{hour.integer}:{minute.integer(2)}", ["en-US"], "US", CalendarIdentifiers.Gregorian, ClockIdentifiers.TwentyFourHour);
		var dateToFormat = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.FromHours(-5)); // EST time

		var result = formatter.Format(dateToFormat, "UTC");

		// The time should be converted to UTC (10:30 EST = 15:30 UTC)
		StringAssert.Contains(result, "15:30", $"Expected 15:30 UTC, got: {result}");
	}

	[TestMethod]
	public void When_CreatingFormatter_ShouldUseApplicationLanguages()
	{
		var formatter = new DateTimeFormatter("longdate");

		CollectionAssert.AreEqual(ApplicationLanguages.Languages.ToList(), formatter.Languages.ToList());
	}

	[TestMethod]
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
			var formattedDate = formatter.Format(new(2024, 6, 27, 14, 30, 0, TimeSpan.Zero));

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

	#region Clock Override Tests - Issue #19349

	[TestMethod]
	public void When_TwelveHourClock_WithPattern_ShouldShowCorrectHour()
	{
		// Test that 12-hour clock properly formats midnight as 12
		var formatter = new DateTimeFormatter(
			"{hour.integer}:{minute.integer(2)} {period.abbreviated}",
			["en-US"],
			"US",
			CalendarIdentifiers.Gregorian,
			ClockIdentifiers.TwelveHour);

		var midnight = new DateTimeOffset(2024, 1, 1, 0, 30, 0, TimeSpan.Zero);
		var result = formatter.Format(midnight);

		StringAssert.StartsWith(result, "12:30", $"Midnight should be 12:30, got: {result}");
		StringAssert.Contains(result, "AM", $"Midnight should be AM, got: {result}");
	}

	[TestMethod]
	public void When_TwelveHourClock_AtNoon_ShouldShowTwelve()
	{
		var formatter = new DateTimeFormatter(
			"{hour.integer}:{minute.integer(2)} {period.abbreviated}",
			["en-US"],
			"US",
			CalendarIdentifiers.Gregorian,
			ClockIdentifiers.TwelveHour);

		var noon = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
		var result = formatter.Format(noon);

		StringAssert.StartsWith(result, "12:00", $"Noon should be 12:00, got: {result}");
		StringAssert.Contains(result, "PM", $"Noon should be PM, got: {result}");
	}

	[TestMethod]
	public void When_TwentyFourHourClock_ShouldNotShowPeriod()
	{
		// When 24-hour clock is specified, period should not appear even if pattern includes it
		var formatter = new DateTimeFormatter(
			"{hour.integer(2)}:{minute.integer(2)} {period.abbreviated}",
			["en-US"],
			"US",
			CalendarIdentifiers.Gregorian,
			ClockIdentifiers.TwentyFourHour);

		var afternoon = new DateTimeOffset(2024, 1, 1, 14, 30, 0, TimeSpan.Zero);
		var result = formatter.Format(afternoon);

		StringAssert.Contains(result, "14:30", $"Expected 14:30, got: {result}");
		Assert.IsFalse(result.Contains("AM") || result.Contains("PM"), $"24-hour clock should not have AM/PM, got: {result}");
	}

	[TestMethod]
	public void When_TwelveHourClock_AtPM_ShouldConvertHour()
	{
		var formatter = new DateTimeFormatter(
			"{hour.integer}:{minute.integer(2)} {period.abbreviated}",
			["en-US"],
			"US",
			CalendarIdentifiers.Gregorian,
			ClockIdentifiers.TwelveHour);

		var afternoon = new DateTimeOffset(2024, 1, 1, 14, 30, 0, TimeSpan.Zero);
		var result = formatter.Format(afternoon);

		StringAssert.StartsWith(result, "2:30", $"2:30 PM expected, got: {result}");
		StringAssert.Contains(result, "PM", $"Should be PM, got: {result}");
	}

	#endregion

	#region Calendar Tests

	[TestMethod]
	public void When_CalendarHour_TwelveHourClock_Midnight_ShouldBeTwelve()
	{
		var calendar = new Windows.Globalization.Calendar(["en-US"], CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
		calendar.SetDateTime(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));

		Assert.AreEqual(12, calendar.Hour, "Midnight in 12-hour clock should be hour 12");
	}

	[TestMethod]
	public void When_CalendarHour_TwelveHourClock_Noon_ShouldBeTwelve()
	{
		var calendar = new Windows.Globalization.Calendar(["en-US"], CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
		calendar.SetDateTime(new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero));

		Assert.AreEqual(12, calendar.Hour, "Noon in 12-hour clock should be hour 12");
	}

	[TestMethod]
	public void When_CalendarHour_TwelveHourClock_1PM_ShouldBeOne()
	{
		var calendar = new Windows.Globalization.Calendar(["en-US"], CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
		calendar.SetDateTime(new DateTimeOffset(2024, 1, 1, 13, 0, 0, TimeSpan.Zero));

		Assert.AreEqual(1, calendar.Hour, "1 PM in 12-hour clock should be hour 1");
	}

	[TestMethod]
	public void When_CalendarHour_TwentyFourHourClock_1PM_ShouldBeThirteen()
	{
		var calendar = new Windows.Globalization.Calendar(["en-US"], CalendarIdentifiers.Gregorian, ClockIdentifiers.TwentyFourHour);
		calendar.SetDateTime(new DateTimeOffset(2024, 1, 1, 13, 0, 0, TimeSpan.Zero));

		Assert.AreEqual(13, calendar.Hour, "1 PM in 24-hour clock should be hour 13");
	}

	[TestMethod]
	public void When_CalendarPeriod_AM_ShouldBeFirst()
	{
		var calendar = new Windows.Globalization.Calendar(["en-US"], CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
		calendar.SetDateTime(new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero));

		Assert.AreEqual(1, calendar.Period, "10 AM should be period 1");
	}

	[TestMethod]
	public void When_CalendarPeriod_PM_ShouldBeSecond()
	{
		var calendar = new Windows.Globalization.Calendar(["en-US"], CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
		calendar.SetDateTime(new DateTimeOffset(2024, 1, 1, 14, 0, 0, TimeSpan.Zero));

		Assert.AreEqual(2, calendar.Period, "2 PM should be period 2");
	}

	[TestMethod]
	public void When_CalendarHourAsString_TwelveHourClock_ShouldRespectClock()
	{
		var calendar = new Windows.Globalization.Calendar(["en-US"], CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour);
		calendar.SetDateTime(new DateTimeOffset(2024, 1, 1, 14, 0, 0, TimeSpan.Zero)); // 2 PM

		var hourStr = calendar.HourAsString();

		Assert.AreEqual("2", hourStr, "2 PM should display as '2' in 12-hour clock");
	}

	[TestMethod]
	public void When_CalendarHourAsString_TwentyFourHourClock_ShouldRespectClock()
	{
		var calendar = new Windows.Globalization.Calendar(["en-US"], CalendarIdentifiers.Gregorian, ClockIdentifiers.TwentyFourHour);
		calendar.SetDateTime(new DateTimeOffset(2024, 1, 1, 14, 0, 0, TimeSpan.Zero)); // 2 PM

		var hourStr = calendar.HourAsString();

		Assert.AreEqual("14", hourStr, "2 PM should display as '14' in 24-hour clock");
	}

	#endregion
}
