using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Globalization.DateTimeFormatting;
using System.Globalization;
using Windows.Globalization;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

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

		var formatted = formatter.Format(new DateTimeOffset(2024, 6, 17, 14, 30, 0, TimeSpan.Zero));
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
}
