using System.Globalization;
using System.Linq;
using Windows.Globalization.DateTimeFormatting;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_Globalization
{
	[TestClass]
	public class Given_CalendarFormatter
	{
		[TestMethod]
		[DataRow("day month year", "en-US", "{month.numeric}/{day.integer}/{year.full}")]
		[DataRow("day month year", "en-CA", "{year.full}-{month.numeric}-{day.integer(2)}")]
		[DataRow("day month year", "en-GB", "{day.integer(2)}/{month.numeric}/{year.full}")]
		[DataRow("day month year", "fr-CA", "{year.full}-{month.numeric}-{day.integer(2)}")]
		[DataRow("day month year", "fr-FR", "{day.integer(2)}/{month.numeric}/{year.full}")]
		[DataRow("day month year", "hu-HU", "{year.full}. {month.numeric}. {day.integer(2)}.")]
		public void When_UsingVariousLanguages(string format, string language, string expectedPattern)
		{
			var sut = new DateTimeFormatter(format, new[] {language});

			var firstPattern = sut.Patterns.First();

			using var _ = new AssertionScope();

			firstPattern.Should().Be(expectedPattern);
			firstPattern.Length.Should().Be(expectedPattern.Length);
		}

		[TestMethod]
		[DataRow("day", "en-US|fr-CA|ru-RU", "{day.integer}|{day.integer}|{day.integer}")]
		[DataRow("day month year", "en-US|fr-CA", "{month.numeric}/{day.integer}/{year.full}|{year.full}-{month.numeric}-{day.integer(2)}")]
		[DataRow("month year", "en-US|fr-CA", "{month.full} {year.full}|{month.full}, {year.full}")]
		[DataRow("day month", "en-US|fr-CA", "{month.full} {day.integer}|{day.integer} {month.full}")]
		[DataRow("hour minute second", "en-US|fr-CA", "{hour}:{minute}:{second} {period.abbreviated}|{hour}:{minute}:{second}")]
		[DataRow("hour minute", "en-US|fr-CA", "{hour}:{minute} {period.abbreviated}|{hour}:{minute}")]
		public void When_UsingMultipleLanguages(string format, string languages, string expectedPatterns)
		{
			var sut = new DateTimeFormatter(format, languages.Split('|'));

			sut.Patterns.Should().BeEquivalentTo(expectedPatterns.Split('|'));
		}
	}
}
