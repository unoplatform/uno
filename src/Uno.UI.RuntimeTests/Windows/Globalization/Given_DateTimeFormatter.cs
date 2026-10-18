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
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24761")]
	[DataRow("de-DE", "en-US", "24HourClock", "13:05")]
	[DataRow("en-US", "en-GB", "12HourClock", "1:05 PM")]
	public void When_Language_Differs_From_CurrentCulture_Then_Time_Follows_Language(string language, string currentCulture, string expectedClock, string expectedTime)
	{
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
	public void When_Template_Names_Some_Parts_Then_Format_Has_Only_Those(string template, string language, int hour, string expected)
	{
		var formatter = new DateTimeFormatter(template, new[] { language });

		Assert.AreEqual(expected, NormalizeText(formatter.Format(new DateTimeOffset(new DateTime(2024, 1, 15, hour, 5, 7)))));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
	public void When_Created_From_Components_Then_Formats_With_Language_Clock()
	{
		var date = new DateTimeOffset(new DateTime(2024, 1, 15, 13, 5, 7));
		var defaultClock = new global::Windows.Globalization.Calendar().GetClock();

		var dateFormatter = new DateTimeFormatter(YearFormat.Default, MonthFormat.Default, DayFormat.Default, DayOfWeekFormat.None);
		Assert.AreEqual("year month day", dateFormatter.Template);
		Assert.AreEqual(defaultClock, dateFormatter.Clock);
		StringAssert.Contains(dateFormatter.Format(date), "2024");

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
