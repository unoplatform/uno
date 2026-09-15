#nullable enable
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Helpers;

namespace Uno.UI.Tests.Helpers;

[TestClass]
public class Given_DeviceTargetHelper
{
	private const string IPadDesktopSiteUserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15";
	private const string IPadMobileSiteUserAgent = "Mozilla/5.0 (iPad; CPU OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1";
	private const string IPadChromeUserAgent = "Mozilla/5.0 (iPad; CPU OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/118.0.5993.92 Mobile/15E148 Safari/604.1";
	private const string IPhoneUserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1";
	private const string AndroidPhoneUserAgent = "Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Mobile Safari/537.36";
	private const string AndroidTabletUserAgent = "Mozilla/5.0 (Linux; Android 13; SM-X710) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36";
	private const string MacSafariUserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15";
	private const string WindowsChromeUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36";
	private const string LinuxFirefoxUserAgent = "Mozilla/5.0 (X11; Linux x86_64; rv:120.0) Gecko/20100101 Firefox/120.0";

	// expected is passed by name because BrowserHostPlatform is internal and test methods must be public.
	[TestMethod]
	[DataRow(IPadMobileSiteUserAgent, "iPad", true, nameof(BrowserHostPlatform.iOS), DisplayName = "iPad requesting the mobile site")]
	[DataRow(IPadChromeUserAgent, "iPad", true, nameof(BrowserHostPlatform.iOS), DisplayName = "Chrome on iPad")]
	[DataRow(IPhoneUserAgent, "iPhone", true, nameof(BrowserHostPlatform.iOS), DisplayName = "iPhone")]
	// iPadOS 13+ requests the desktop site by default, which reports the very same user agent and platform as a Mac.
	[DataRow(IPadDesktopSiteUserAgent, "MacIntel", true, nameof(BrowserHostPlatform.iOS), DisplayName = "iPad requesting the desktop site")]
	[DataRow(MacSafariUserAgent, "MacIntel", false, nameof(BrowserHostPlatform.Other), DisplayName = "Safari on macOS")]
	// "Linux armv81" (with a digit one) is not a typo: it is the value Chrome 107+ and Firefox freeze on Android for
	// UA reduction. "Linux armv8l" is what older browsers report, so both spellings are covered.
	[DataRow(AndroidPhoneUserAgent, "Linux armv81", true, nameof(BrowserHostPlatform.Android), DisplayName = "Android phone")]
	[DataRow(AndroidTabletUserAgent, "Linux armv8l", true, nameof(BrowserHostPlatform.Android), DisplayName = "Android tablet")]
	// A touchscreen alone doesn't make a desktop OS follow mobile conventions - WinUI itself doesn't.
	[DataRow(WindowsChromeUserAgent, "Win32", true, nameof(BrowserHostPlatform.Other), DisplayName = "Windows touch device")]
	[DataRow(WindowsChromeUserAgent, "Win32", false, nameof(BrowserHostPlatform.Other), DisplayName = "Windows desktop")]
	[DataRow(LinuxFirefoxUserAgent, "Linux x86_64", false, nameof(BrowserHostPlatform.Other), DisplayName = "Linux desktop")]
	[DataRow("", "", false, nameof(BrowserHostPlatform.Other), DisplayName = "Unavailable navigator values")]
	public void When_GetBrowserHostPlatform(string userAgent, string platform, bool isTouchCapable, string expected)
		=> Assert.AreEqual(
			Enum.Parse<BrowserHostPlatform>(expected),
			DeviceTargetHelper.GetBrowserHostPlatform(userAgent, platform, isTouchCapable));
}
