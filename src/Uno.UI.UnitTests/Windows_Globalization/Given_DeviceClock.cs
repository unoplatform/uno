#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Globalization;

namespace Uno.UI.Tests.Windows_Globalization;

[UITestClass]
public class Given_DeviceClock
{
	// Real patterns, from Android 15 and macOS 27, as .NET patterns, with ":" as the culture's time separator and "/" as
	// its date separator.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25573")]
	[DataRow("HH:mm", "HH:mm")] // en-US
	[DataRow("HH:mm:ss", "HH:mm:ss")]
	[DataRow("h:mm a", "h:mm tt")] // en-US (Android)
	[DataRow("h:mm\u202Fa", "h:mm\u202Ftt")] // de-DE
	[DataRow("h:mm:ss\u202Fa", "h:mm:ss\u202Ftt")]
	[DataRow("h:mm\u00A0a", "h:mm\u00A0tt")] // ar-SA (Apple)
	[DataRow("hh:mm\u202Fa", "hh:mm\u202Ftt")] // hr-HR
	[DataRow("H.mm", "H.mm")] // fi-FI
	[DataRow("h.mm.ss\u202Fa", "h.mm.ss\u202Ftt")]
	[DataRow("a h:mm", "tt h:mm")] // ko-KR, my-MM
	[DataRow("a\u202Fh:mm", "tt\u202Fh:mm")] // tr-TR, hu-HU
	[DataRow("ah:mm", "tth:mm")] // zh-CN (Android)
	[DataRow("aK:mm:ss", "tth:mm:ss")] // ja-JP
	[DataRow("Bh:mm", "tth:mm")] // zh-Hans-CN (Apple), a day period
	[DataRow("B h:mm", "tt h:mm")] // hi-IN (Apple)
	[DataRow("H시 m분 s초", "H시 m분 s초")] // ko-KR, "Hms" (Android): a converter case only, as Uno asks for "Hmmss"
	[DataRow("h:mm 'ч'. a", "h:mm 'ч'. tt")] // bg-BG (Android)
	[DataRow("h 'h' mm\u202Fa", "h 'h' mm\u202Ftt")] // fr-CA (Android)
	[DataRow("HH 'h' mm 'min' ss 's'", "HH 'h' mm 'min' ss 's'")]
	[DataRow("'Klock' h.mm:ss\u202Fa", "'Klock' h.mm:ss\u202Ftt")] // nds-DE (Android)
	[DataRow("k:mm", "H:mm")]
	[DataRow("KK:mm a", "hh:mm tt")]
	[DataRow("h 'o''clock' a", @"h 'o\'clock' tt")]
	[DataRow("HH''mm", @"HH\'mm")]
	[DataRow("HH/mm%ss", @"HH/mm\%ss")]
	[DataRow(@"HH\mm", @"HH\\mm")]
	[DataRow("HH\"mm", "HH\\\"mm")]
	[DataRow(@"'a\b' H", @"'a\\b' H")]
	public void When_Device_Pattern_Then_Net_Pattern(string devicePattern, string expected)
		=> Assert.AreEqual(expected, DeviceClock.ToNetTimePattern(devicePattern, ":", "/"));

	// ":" and "/" are the culture's separators in .NET: they stay literal where the culture's are others.
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25573")]
	[DataRow("h:mm a", ".", "-", @"h\:mm tt")] // kl-GL
	[DataRow("H.mm", ".", "-", "H.mm")]
	[DataRow("HH:mm/ss", ":", "-", @"HH:mm\/ss")]
	[DataRow("HH:mm/ss", ".", "/", @"HH\:mm/ss")]
	public void When_Culture_Separators_Differ_Then_Escaped(string devicePattern, string timeSeparator, string dateSeparator, string expected)
		=> Assert.AreEqual(expected, DeviceClock.ToNetTimePattern(devicePattern, timeSeparator, dateSeparator));

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25573")]
	[DataRow(null)]
	[DataRow("")]
	[DataRow("mm:ss")]
	[DataRow("HH:mm z")]
	[DataRow("HH:mm v")]
	[DataRow("HH 'h")]
	public void When_Device_Pattern_Unusable_Then_Null(string? devicePattern)
		=> Assert.IsNull(DeviceClock.ToNetTimePattern(devicePattern, ":", "/"));
}
