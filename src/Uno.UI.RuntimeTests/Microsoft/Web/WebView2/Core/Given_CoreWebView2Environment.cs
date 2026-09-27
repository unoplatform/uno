using System;
using System.IO;
using Microsoft.Web.WebView2.Core;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_Web_WebView2_Core;

/// <remarks>
/// These members are statics over the browser loader, so nothing here needs a WebView2 in the visual tree.
/// That is exactly what makes them worth testing: they must answer before any WebView2 exists.
/// </remarks>
[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
public class Given_CoreWebView2Environment
{
	[TestMethod]
	public void When_GetAvailableBrowserVersionString_Returns_Parsable_Version()
	{
		var version = RequireBrowserVersion();

		// Non-stable channels append a space-separated suffix, for example "120.0.2210.91 beta".
		var versionToken = version.Split(' ')[0];

		Assert.IsTrue(Version.TryParse(versionToken, out var parsed), $"'{version}' does not start with a parsable version.");
		Assert.IsTrue(parsed.Major > 0, $"'{version}' has an unexpected major version.");
	}

	[TestMethod]
	[DataRow(null)]
	[DataRow("")]
	public void When_GetAvailableBrowserVersionString_Default_Folder_Matches(string browserExecutableFolder)
	{
		Assert.AreEqual(RequireBrowserVersion(), CoreWebView2Environment.GetAvailableBrowserVersionString(browserExecutableFolder));
	}

	[TestMethod]
	public void When_GetAvailableBrowserVersionString_Missing_Folder_Throws()
	{
		var missingFolder = Path.Combine(Path.GetTempPath(), "uno-webview2-missing-" + Guid.NewGuid().ToString("N"));

		// An explicit folder is authoritative: it must fail rather than fall back to the installed browser.
		// The type is the contract, not an implementation detail: this is how an app detects a missing runtime,
		// so it has to match what WinAppSDK surfaces for the same ERROR_FILE_NOT_FOUND.
		Assert.Throws<FileNotFoundException>(() => CoreWebView2Environment.GetAvailableBrowserVersionString(missingFolder));
	}

	[TestMethod]
	[DataRow("1.0.0.0", "2.0.0.0", -1)]
	[DataRow("2.0.0.0", "1.0.0.0", 1)]
	[DataRow("1.0.0.0", "1.0.0.0", 0)]
	[DataRow("120.0.2210.91", "120.0.2210.133", -1)]
	public void When_CompareBrowserVersionString_Orders_Versions(string left, string right, int expectedSign)
	{
		// The contract is a sign, not a magnitude. This needs the loader but no installed browser.
		Assert.AreEqual(expectedSign, Math.Sign(CoreWebView2Environment.CompareBrowserVersionString(left, right)));
	}

	private static string RequireBrowserVersion()
	{
		string version = null;
		Exception failure = null;

		try
		{
			version = CoreWebView2Environment.GetAvailableBrowserVersionString();
		}
		catch (Exception ex)
		{
			// Deliberately broad: a missing runtime is FileNotFoundException, a missing loader DllNotFoundException,
			// and a target without the extension NotImplementedException. All of them mean inconclusive, not failed.
			failure = ex;
		}

		// Outside the catch, so the AssertInconclusiveException is not swallowed.
		if (failure is not null)
		{
			Assert.Inconclusive($"The WebView2 Runtime is unavailable: {failure.GetType().FullName}: {failure.Message}");
		}

		if (string.IsNullOrEmpty(version))
		{
			Assert.Inconclusive("The WebView2 Runtime reported no version.");
		}

		return version;
	}
}
