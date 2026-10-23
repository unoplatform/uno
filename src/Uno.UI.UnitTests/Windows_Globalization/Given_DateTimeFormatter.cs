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

	private static string FormatPeriod(string pattern, string cultureName)
		=> new DateTimeFormatter(pattern, [cultureName], "ZZ", CalendarIdentifiers.Gregorian, ClockIdentifiers.TwelveHour)
			.Format(new DateTimeOffset(2024, 1, 15, 17, 15, 0, TimeSpan.Zero));
}
