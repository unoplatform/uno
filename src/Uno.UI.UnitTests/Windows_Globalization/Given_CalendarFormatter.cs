using System.Globalization;
using System.Linq;
using AwesomeAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Globalization.DateTimeFormatting;

namespace Uno.UI.Tests.Windows_Globalization
{
	[UITestClass]
	public class Given_CalendarFormatter
	{
		// As in WinRT, the long date without the day of week. A packaged WinRT probe on Windows 11 gives these
		// patterns for every language but en-GB, whose row comes from .NET's data.
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25572")]
		[DataRow("day month year", "en-US", "{month.full} {day.integer}, {year.full}")]
		[DataRow("day month year", "en-CA", "{month.full} {day.integer}, {year.full}")]
		[DataRow("day month year", "en-GB", "{day.integer} {month.full} {year.full}")]
		[DataRow("day month year", "fr-CA", "{day.integer} {month.full} {year.full}")]
		[DataRow("day month year", "fr-FR", "{day.integer} {month.full} {year.full}")]
		[DataRow("day month year", "hu-HU", "{year.full}. {month.full} {day.integer}.")]
		[DataRow("day month year", "ja-JP", "{year.full}年{month.integer}月{day.integer}日")]
		[DataRow("day month year", "ko-KR", "{year.full}년 {month.integer}월 {day.integer}일")]
		public void When_UsingVariousLanguages(string format, string language, string expectedPattern)
		{
			var sut = new DateTimeFormatter(format, new[] { language });
			var firstPattern = sut.Patterns[0];
			firstPattern.Should().Be(expectedPattern);
		}

		[TestMethod]
		[DataRow("day", "en-US|fr-CA|ru-RU", "{day.integer}|{day.integer}|{day.integer}")]
		[DataRow("day month year", "en-US|fr-CA", "{month.numeric}/{day.integer}/{year.full}|{year.full}-{month.numeric}-{day.integer(2)}")]
		[DataRow("month year", "en-US|fr-CA", "{month.full} {year.full}|{month.full}, {year.full}")]

		[DataRow("day month", "en-US|fr-CA", "{month.full} {day.integer}|{day.integer} {month.full}")]
		[DataRow("hour minute second", "en-US|fr-CA", "{hour}:{minute}:{second} {period.abbreviated}|{hour}:{minute}:{second}")]
		[DataRow("hour minute", "en-US|fr-CA", "{hour}:{minute} {period.abbreviated}|{hour}:{minute}")]
		[Ignore("https://github.com/unoplatform/uno/issues/9080")]
#if RUNTIME_NATIVE_AOT
		[Ignore(".BeEquivalentTo() unsupported under NativeAOT; see: https://github.com/AwesomeAssertions/AwesomeAssertions/issues/290")]
#endif  // RUNTIME_NATIVE_AOT
		public void When_UsingMultipleLanguages(string format, string languages, string expectedPatterns)
		{
			var sut = new DateTimeFormatter(format, languages.Split('|'));

			sut.Patterns.Should().BeEquivalentTo(expectedPatterns.Split('|'));
		}
	}
}
