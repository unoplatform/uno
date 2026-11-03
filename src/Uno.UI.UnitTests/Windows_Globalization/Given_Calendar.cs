#nullable enable

using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Globalization;
using Windows.Globalization.DateTimeFormatting;
using Calendar = Windows.Globalization.Calendar;

namespace Uno.UI.Tests.Windows_Globalization;

[UITestClass]
public class Given_Calendar
{
	private const string TwelveHour = ClockIdentifiers.TwelveHourValue;
	private const string TwentyFourHour = ClockIdentifiers.TwentyFourHourValue;

	// Expectations are ICU's culture data.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/18834")]
	[DataRow("en-US", TwelveHour)]
	[DataRow("en-CA", TwelveHour)]
	[DataRow("en-AU", TwelveHour)]
	[DataRow("en-GB", TwentyFourHour)]
	[DataRow("ko-KR", TwelveHour)]
	[DataRow("ja-JP", TwentyFourHour)]
	[DataRow("fr-CA", TwentyFourHour)]
	[DataRow("zh-TW", TwelveHour)]
	[DataRow("ar-SA", TwelveHour)]
	public void When_Default_Clock_For_Culture(string cultureName, string expectedClock)
	{
		var originalCulture = CultureInfo.CurrentCulture;
		var originalOverride = ApplicationLanguages.PrimaryLanguageOverride;
		try
		{
			// The clock follows the calendar's language, not CurrentCulture (the regional format).
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(expectedClock == TwelveHour ? "en-GB" : "en-US");

			Assert.AreEqual(expectedClock, new Calendar([cultureName]).GetClock());
			Assert.AreEqual(expectedClock, new DateTimeFormatter("shorttime", [cultureName]).Clock);

			ApplicationLanguages.PrimaryLanguageOverride = cultureName;
			Assert.AreEqual(expectedClock, new Calendar().GetClock());
			Assert.AreEqual(expectedClock, new DateTimeFormatter("shorttime").Clock);
		}
		finally
		{
			ApplicationLanguages.PrimaryLanguageOverride = originalOverride;
			CultureInfo.CurrentCulture = originalCulture;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/18834")]
	[DataRow("h:mm:ss tt", "h:mm tt", "p.m.", TwelveHour)]
	[DataRow("tt h:mm:ss", "tt h:mm", "오후", TwelveHour)]
	[DataRow("h:mm:ss", "h:mm", "PM", TwelveHour)]
	[DataRow("HH:mm:ss", "HH:mm", "PM", TwentyFourHour)]
	[DataRow("'heure' HH:mm:ss", "'heure' HH:mm", "p.m.", TwentyFourHour)]
	[DataRow("\"heure\" HH:mm:ss", "\"heure\" HH:mm", "p.m.", TwentyFourHour)]
	[DataRow("\\h HH:mm:ss", "\\h HH:mm", "p.m.", TwentyFourHour)]
	[DataRow("'it\\'s h' HH:mm:ss", "'it\\'s h' HH:mm", "p.m.", TwentyFourHour)]
	[DataRow("'H' h:mm:ss tt", "'H' h:mm tt", "p.m.", TwelveHour)]
	[DataRow("mm:ss", "h:mm tt", "p.m.", TwelveHour)]
	public void When_Default_Clock_For_Time_Pattern(string longTimePattern, string shortTimePattern, string pmDesignator, string expectedClock)
	{
		var culture = (CultureInfo)CultureInfo.GetCultureInfo("en-US").Clone();
		culture.DateTimeFormat.LongTimePattern = longTimePattern;
		culture.DateTimeFormat.ShortTimePattern = shortTimePattern;
		culture.DateTimeFormat.PMDesignator = pmDesignator;

		Assert.AreEqual(expectedClock, Calendar.GetDefaultClock(culture));
	}
}
