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

	// The first rows are the 24h rows of the WinUI probe on Windows: an en-US regional format customized to 24-hour.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25573")]
	[DataRow("en-US", true, "en-US", TwentyFourHour)]
	[DataRow("en-US", true, "en-CA", TwelveHour)]
	[DataRow("en-US", true, "de-DE", TwentyFourHour)]
	[DataRow("en-US", false, "en-GB", TwentyFourHour)]
	[DataRow("de-DE", false, "de-DE", TwelveHour)]
	[DataRow("en-GB", null, "en-GB", TwentyFourHour)]
	[DataRow("en-US-u-fw-mon", true, "en-US", TwentyFourHour)]
	[DataRow("en_US@rg=gbzzzz", true, "en-US", TwentyFourHour)]
	[DataRow("en_US@hours=h23", true, "en-US", TwentyFourHour)]
	[DataRow("zh-Hans_CN", false, "zh-CN", TwelveHour)]
	public void When_Device_Sets_Clock_Then_Only_Its_Locale_Follows(string deviceLocale, bool? device24Hour, string language, string expectedClock)
	{
		var originalOverride = ApplicationLanguages.PrimaryLanguageOverride;
		try
		{
			DeviceClock.TestDevice = (deviceLocale, null, device24Hour);

			Assert.AreEqual(expectedClock, new Calendar([language]).GetClock());
			Assert.AreEqual(expectedClock, new DateTimeFormatter("shorttime", [language]).Clock);

			ApplicationLanguages.PrimaryLanguageOverride = language;
			Assert.AreEqual(expectedClock, new Calendar().GetClock());
			// What TimePicker and TimePickerFlyout take their default ClockIdentifier from.
			Assert.AreEqual(expectedClock, new DateTimeFormatter("{hour.integer(1)}").Clock);
		}
		finally
		{
			DeviceClock.TestDevice = null;
			ApplicationLanguages.PrimaryLanguageOverride = originalOverride;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25573")]
	public void When_Device_Sets_Clock_Then_Explicit_Clock_Wins()
	{
		try
		{
			DeviceClock.TestDevice = ("en-US", null, true);

			Assert.AreEqual(TwelveHour, new Calendar(["en-US"], CalendarIdentifiers.Gregorian, TwelveHour).GetClock());
			Assert.AreEqual(TwelveHour, new DateTimeFormatter("shorttime", ["en-US"], "US", CalendarIdentifiers.Gregorian, TwelveHour).Clock);
		}
		finally
		{
			DeviceClock.TestDevice = null;
		}
	}

	// On iOS the current locale takes the language of the app's own localization: an app without Korean resources
	// on a Korean device gets en_KR, while its language is the first preferred one, ko-KR.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25573")]
	[DataRow("ko-KR", TwentyFourHour)]
	[DataRow("en-KR", TwentyFourHour)]
	[DataRow("en-US", TwelveHour)]
	public void When_Device_Locale_Differs_From_Preferred_Language_Then_Both_Follow(string language, string expectedClock)
	{
		var originalOverride = ApplicationLanguages.PrimaryLanguageOverride;
		try
		{
			DeviceClock.TestDevice = ("en_KR", "ko-KR", true);

			Assert.AreEqual(expectedClock, new Calendar([language]).GetClock());

			ApplicationLanguages.PrimaryLanguageOverride = language;
			Assert.AreEqual(expectedClock, new Calendar().GetClock());
		}
		finally
		{
			DeviceClock.TestDevice = null;
			ApplicationLanguages.PrimaryLanguageOverride = originalOverride;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25573")]
	[DataRow("HH", true)]
	[DataRow("H", true)]
	[DataRow("k", true)]
	[DataRow("h a", false)]
	[DataRow("a h", false)]
	[DataRow("K a", false)]
	[DataRow("HH 'h'", true)]
	[DataRow("'H' h a", false)]
	[DataRow("'o''clock H' h a", false)]
	[DataRow("mm", null)]
	[DataRow(null, null)]
	public void When_Apple_Hour_Format_Then_Clock_Follows(string? format, bool? expected)
		=> Assert.AreEqual(expected, DeviceClock.IsTwentyFourHourPattern(format));
}
