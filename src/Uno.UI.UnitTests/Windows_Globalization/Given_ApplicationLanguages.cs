#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Globalization;

namespace Uno.UI.Tests.Windows_Globalization;

[UITestClass]
public class Given_ApplicationLanguages
{
	// The platform's patterns for a locale and a clock: Android 15's ("Hm"/"hm" and "Hmmss"/"hmmss") and macOS 27's
	// ("Hmm"/"hmm" and "Hmmss"/"hmmss": de, en_JP and en_KR). en_JP and en_KR, current locales on iOS, are traps that the
	// code must not ask for.
	private static readonly Dictionary<(string Locale, bool Is24Hour), (string Short, string Long)> _platformPatterns = new()
	{
		[("en-US", true)] = ("HH:mm", "HH:mm:ss"),
		[("en-US", false)] = ("h:mm a", "h:mm:ss a"),
		[("en-US-u-fw-mon", true)] = ("HH:mm", "HH:mm:ss"),
		[("de", false)] = ("h:mm\u202Fa", "h:mm:ss\u202Fa"),
		[("de-DE", false)] = ("h:mm\u202Fa", "h:mm:ss\u202Fa"),
		[("bg-BG", false)] = ("h:mm 'ч'. a", "h:mm:ss 'ч'. a"),
		[("kl-GL", false)] = ("h:mm a", "h:mm:ss a"),
		[("ja-JP", false)] = ("aK:mm", "aK:mm:ss"),
		[("en_JP", false)] = ("h:mm\u202Fa", "h:mm:ss\u202Fa"),
		[("ko-KR", true)] = ("HH:mm", "HH:mm:ss"),
		[("en_KR", true)] = ("HH:mm", "HH:mm:ss"),
		[("zh-TW", true)] = ("HH:mm", "HH:mm:ss"),
	};

	// The platform's patterns for the culture's own locale and the device's clock, when that isn't the culture's own.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25573")]
	[DataRow("en-US", true, "en-US", "HH:mm", "HH:mm:ss")]
	[DataRow("de-DE", false, "de-DE", "h:mm\u202Ftt", "h:mm:ss\u202Ftt")]
	[DataRow("bg-BG", false, "bg-BG", "h:mm 'ч'. tt", "h:mm:ss 'ч'. tt")]
	[DataRow("kl-GL", false, "kl-GL", @"h\:mm tt", @"h\:mm\:ss tt")]
	[DataRow("ko-KR", true, "ko-KR", "HH:mm", "HH:mm:ss")]
	[DataRow("en-US-u-fw-mon", true, "en-US-u-fw-mon", "HH:mm", "HH:mm:ss")]
	[DataRow("zh-Hant_TW", true, "zh-TW", "HH:mm", "HH:mm:ss")]
	public void When_Device_Sets_Clock_Then_Culture_Takes_Its_Locale_Time(
		string deviceLocale, bool device24Hour, string cultureName, string expectedShortTime, string expectedLongTime)
	{
		var runtimeCulture = CultureInfo.ReadOnly(new CultureInfo(cultureName, useUserOverride: false));
		try
		{
			var requested = UseDevice(deviceLocale, null, device24Hour);

			var culture = ApplicationLanguages.GetFormatCulture(new CultureInfo("fr-FR"), runtimeCulture, runtimeCulture);

			Assert.IsNotNull(culture);
			CollectionAssert.AreEqual(new[] { (cultureName, device24Hour) }, requested);
			Assert.AreEqual(runtimeCulture.Name, culture.Name);
			Assert.AreEqual(expectedShortTime, culture.DateTimeFormat.ShortTimePattern);
			Assert.AreEqual(expectedLongTime, culture.DateTimeFormat.LongTimePattern);
			Assert.AreEqual(runtimeCulture.DateTimeFormat.AMDesignator, culture.DateTimeFormat.AMDesignator);
			Assert.AreEqual(runtimeCulture.DateTimeFormat.PMDesignator, culture.DateTimeFormat.PMDesignator);
		}
		finally
		{
			ResetDevice();
		}
	}

	// On iOS, an app without resources in the device's language gets a current locale such as en_JP or en_KR, while its
	// culture takes the first preferred language, ja-JP or ko-KR: the patterns are that language's own (Japanese puts
	// the period first, en_JP after the time).
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25573")]
	[DataRow("en_JP", "ja-JP", false, "tth:mm", "tth:mm:ss")]
	[DataRow("en_KR", "ko-KR", true, "HH:mm", "HH:mm:ss")]
	public void When_Current_Locale_Differs_From_Culture_Then_Culture_Takes_Its_Language_Time(
		string currentLocale, string cultureName, bool device24Hour, string expectedShortTime, string expectedLongTime)
	{
		var runtimeCulture = CultureInfo.ReadOnly(new CultureInfo(cultureName, useUserOverride: false));
		try
		{
			var requested = UseDevice(currentLocale, cultureName, device24Hour);

			var culture = ApplicationLanguages.GetFormatCulture(new CultureInfo("fr-FR"), runtimeCulture, runtimeCulture);

			Assert.IsNotNull(culture);
			CollectionAssert.AreEqual(new[] { (cultureName, device24Hour) }, requested);
			Assert.AreEqual(expectedShortTime, culture.DateTimeFormat.ShortTimePattern);
			Assert.AreEqual(expectedLongTime, culture.DateTimeFormat.LongTimePattern);
		}
		finally
		{
			ResetDevice();
		}
	}

	// Read-only as the runtime's own culture is, since DefaultThreadCurrentCulture shares it across threads, a neutral
	// culture's formats included.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25573")]
	[DataRow("en-US", true)]
	[DataRow("de", false)]
	public void When_Device_Sets_Clock_On_Read_Only_Culture_Then_Fitted_Read_Only(string cultureName, bool device24Hour)
	{
		var runtimeCulture = CultureInfo.ReadOnly(new CultureInfo(cultureName, useUserOverride: false));
		try
		{
			UseDevice(cultureName, null, device24Hour);

			var culture = ApplicationLanguages.GetFormatCulture(new CultureInfo("fr-FR"), runtimeCulture, runtimeCulture);

			Assert.IsNotNull(culture);
			Assert.IsTrue(culture.IsReadOnly);
			Assert.IsTrue(culture.DateTimeFormat.IsReadOnly);
			Assert.ThrowsExactly<InvalidOperationException>(() => culture.DateTimeFormat.ShortTimePattern = "H:mm");
		}
		finally
		{
			ResetDevice();
		}
	}

	// Writable as the culture was, as Android's system locale culture is: the device's setting doesn't decide whether an
	// app can change it.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25573")]
	public void When_Device_Sets_Clock_On_Writable_Culture_Then_Fitted_Writable()
	{
		var runtimeCulture = new CultureInfo("en-US", useUserOverride: false);
		try
		{
			UseDevice("en-US", null, true);

			var culture = ApplicationLanguages.GetFormatCulture(new CultureInfo("fr-FR"), runtimeCulture, runtimeCulture);

			Assert.IsNotNull(culture);
			Assert.AreEqual("HH:mm", culture.DateTimeFormat.ShortTimePattern);
			Assert.IsFalse(culture.IsReadOnly);
			culture.DateTimeFormat.ShortDatePattern = "yyyy-MM-dd";
		}
		finally
		{
			ResetDevice();
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25573")]
	[DataRow("en-US", true, "01:05", "13:05", "13:05:09")]
	[DataRow("de-DE", false, "1:05\u202FAM", "1:05\u202FPM", "1:05:09\u202FPM")]
	[DataRow("kl-GL", false, "1:05 AM", "1:05 PM", "1:05:09 PM")]
	public void When_Device_Sets_Clock_Then_Culture_Formats_Follow(
		string locale, bool device24Hour, string expectedEarly, string expectedLate, string expectedLongTime)
	{
		var runtimeCulture = CultureInfo.ReadOnly(new CultureInfo(locale, useUserOverride: false));
		try
		{
			UseDevice(locale, null, device24Hour);

			var culture = ApplicationLanguages.GetFormatCulture(new CultureInfo("fr-FR"), runtimeCulture, runtimeCulture) ?? runtimeCulture;

			var late = new DateTime(2024, 1, 15, 13, 5, 9);
			Assert.AreEqual(expectedEarly, new DateTime(2024, 1, 15, 1, 5, 9).ToString("t", culture));
			Assert.AreEqual(expectedLate, late.ToString("t", culture));
			Assert.AreEqual(expectedLongTime, late.ToString("T", culture));
			// The combined formats end with the time; their dates are the culture's.
			StringAssert.EndsWith(late.ToString("g", culture), " " + expectedLate);
			StringAssert.EndsWith(late.ToString("G", culture), " " + expectedLongTime);
			StringAssert.EndsWith(late.ToString("f", culture), " " + expectedLate);
			StringAssert.EndsWith(late.ToString("F", culture), " " + expectedLongTime);
		}
		finally
		{
			ResetDevice();
		}
	}

	// Android counts the Japanese 12-hour clock from 0 ("aK:mm", 午後0:05), which .NET can't express.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25573")]
	public void When_Device_Counts_Japanese_Hour_From_Zero_Then_Hour_From_One()
	{
		var runtimeCulture = CultureInfo.ReadOnly(new CultureInfo("ja-JP", useUserOverride: false));
		try
		{
			UseDevice("ja-JP", null, false);

			var culture = ApplicationLanguages.GetFormatCulture(new CultureInfo("fr-FR"), runtimeCulture, runtimeCulture);

			Assert.IsNotNull(culture);
			Assert.AreEqual("午前1:05", new DateTime(2024, 1, 15, 1, 5, 9).ToString("t", culture));
			Assert.AreEqual("午後12:05", new DateTime(2024, 1, 15, 12, 5, 9).ToString("t", culture));
			Assert.AreEqual("午後1:05:09", new DateTime(2024, 1, 15, 13, 5, 9).ToString("T", culture));
		}
		finally
		{
			ResetDevice();
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25573")]
	[DataRow("en-US", true, "en-CA")]
	[DataRow("de-DE", false, "fr-FR")]
	[DataRow("sr-Latn-RS", false, "sr-Cyrl-RS")]
	[DataRow("en-US", false, "en-US")]
	[DataRow("en-US", null, "en-US")]
	public void When_Device_Clock_Not_For_Culture_Then_Culture_Kept(string deviceLocale, bool? device24Hour, string cultureName)
	{
		var runtimeCulture = CultureInfo.ReadOnly(new CultureInfo(cultureName, useUserOverride: false));
		try
		{
			UseDevice(deviceLocale, null, device24Hour);

			Assert.IsNull(ApplicationLanguages.GetFormatCulture(new CultureInfo("fr-FR"), runtimeCulture, runtimeCulture));
		}
		finally
		{
			ResetDevice();
		}
	}

	// No rewrite of the culture's own patterns stands in for them.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25573")]
	[DataRow(null, null)]
	[DataRow("HH:mm", null)]
	[DataRow(null, "HH:mm:ss")]
	[DataRow("HH:mm z", "HH:mm:ss")]
	public void When_Device_Patterns_Unusable_Then_Culture_Kept(string? shortPattern, string? longPattern)
	{
		var runtimeCulture = CultureInfo.ReadOnly(new CultureInfo("en-US", useUserOverride: false));
		try
		{
			var requested = 0;
			DeviceClock.TestDevice = ("en-US", null, true);
			DeviceClock.TestTimePatterns = (_, _) =>
			{
				requested++;
				return (shortPattern, longPattern);
			};

			Assert.IsNull(ApplicationLanguages.GetFormatCulture(new CultureInfo("fr-FR"), runtimeCulture, runtimeCulture));
			Assert.AreEqual(1, requested);
		}
		finally
		{
			ResetDevice();
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25573")]
	public void When_App_Set_Culture_Then_Device_Clock_Not_Applied()
	{
		var runtimeCulture = CultureInfo.ReadOnly(new CultureInfo("en-US", useUserOverride: false));
		var appCulture = new CultureInfo("en-US", useUserOverride: false);
		try
		{
			UseDevice("en-US", null, true);

			Assert.IsNull(ApplicationLanguages.GetFormatCulture(new CultureInfo("fr-FR"), appCulture, runtimeCulture));
		}
		finally
		{
			ResetDevice();
		}
	}

	// Stands in for the device and its platform's patterns; returns the locales and clocks the patterns are asked for.
	private static List<(string Locale, bool Is24Hour)> UseDevice(string localeTag, string? languageTag, bool? is24Hour)
	{
		var requested = new List<(string Locale, bool Is24Hour)>();
		DeviceClock.TestDevice = (localeTag, languageTag, is24Hour);
		DeviceClock.TestTimePatterns = (locale, is24) =>
		{
			requested.Add((locale, is24));
			return _platformPatterns.TryGetValue((locale, is24), out var patterns) ? patterns : default;
		};

		return requested;
	}

	private static void ResetDevice()
	{
		DeviceClock.TestDevice = null;
		DeviceClock.TestTimePatterns = null;
	}
}
