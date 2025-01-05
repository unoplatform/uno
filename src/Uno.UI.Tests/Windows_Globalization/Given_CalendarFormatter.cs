using System.Globalization;
using System.Linq;
using Windows.Globalization.DateTimeFormatting;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Uno.UI.Tests.Windows_Globalization
{
	[TestClass]
	public class Given_CalendarFormatter
	{
		[TestMethod]
		[DataRow("day month year", "en-US", "{month.integer}/{day.integer}/{year.full}")]
		[DataRow("day month year", "en-CA", "{year.full}-{month.integer(2)}-{day.integer(2)}")]
		[DataRow("day month year", "en-GB", "{day.integer(2)}/{month.integer(2)}/{year.full}")]
		[DataRow("day month year", "fr-CA", "{year.full}-{month.integer(2)}-{day.integer(2)}")]
		[DataRow("day month year", "fr-FR", "{day.integer(2)}/{month.integer(2)}/{year.full}")]
		[DataRow("day month year", "hu-HU", "{year.full}. {month.integer(2)}. {day.integer(2)}.")]
		public void When_UsingVariousLanguages(string format, string language, string expectedPattern)
		{
			var sut = new DateTimeFormatter(format, new[] { language });
			var firstPattern = sut.Patterns[0];
			firstPattern.Should().Be(expectedPattern);
		}

		[TestMethod]
		public void When_FormattingDateVariants_ShouldProduceExpectedFormats()
		{
			var expectedResults = new Dictionary<string, string>
		{
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

#if !NET7_0_OR_GREATER // https://github.com/unoplatform/uno/issues/9080
		[TestMethod]
		[DataRow("day", "en-US|fr-CA|ru-RU", "{day.integer}|{day.integer}|{day.integer}")]
		[DataRow("day month year", "en-US|fr-CA", "{month.numeric}/{day.integer}/{year.full}|{year.full}-{month.numeric}-{day.integer(2)}")]
		[DataRow("month year", "en-US|fr-CA", "{month.full} {year.full}|{month.full}, {year.full}")]

		[DataRow("day month", "en-US|fr-CA", "{month.full} {day.integer}|{day.integer} {month.full}")]
		[DataRow("hour minute second", "en-US|fr-CA", "{hour}:{minute}:{second} {period.abbreviated}|{hour}:{minute}:{second}")]
		[DataRow("hour minute", "en-US|fr-CA", "{hour}:{minute} {period.abbreviated}|{hour}:{minute}")]
#endif
		public void When_UsingMultipleLanguages(string format, string languages, string expectedPatterns)
		{
			var sut = new DateTimeFormatter(format, languages.Split('|'));

			sut.Patterns.Should().BeEquivalentTo(expectedPatterns.Split('|'));
		}
	}
}
