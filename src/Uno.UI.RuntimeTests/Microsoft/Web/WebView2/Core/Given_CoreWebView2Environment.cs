using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_Web_WebView2_Core;

/// <remarks>
/// These members are statics over the browser loader, or detached environments, so nothing here needs a WebView2
/// in the visual tree. That is exactly what makes them worth testing: they must answer before any WebView2 exists.
/// </remarks>
[TestClass]
public class Given_CoreWebView2Environment
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
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
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public void When_GetAvailableBrowserVersionString_Default_Folder_Matches(string browserExecutableFolder)
	{
		Assert.AreEqual(RequireBrowserVersion(), CoreWebView2Environment.GetAvailableBrowserVersionString(browserExecutableFolder));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public void When_GetAvailableBrowserVersionString_Missing_Folder_Throws()
	{
		var missingFolder = Path.Combine(Path.GetTempPath(), "uno-webview2-missing-" + Guid.NewGuid().ToString("N"));

		// An explicit folder is authoritative: it must fail rather than fall back to the installed browser.
		// The type is the contract, not an implementation detail: this is how an app detects a missing runtime,
		// so it has to match what WinAppSDK surfaces for the same ERROR_FILE_NOT_FOUND.
		Assert.Throws<FileNotFoundException>(() => CoreWebView2Environment.GetAvailableBrowserVersionString(missingFolder));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	[DataRow("1.0.0.0", "2.0.0.0", -1)]
	[DataRow("2.0.0.0", "1.0.0.0", 1)]
	[DataRow("1.0.0.0", "1.0.0.0", 0)]
	[DataRow("120.0.2210.91", "120.0.2210.133", -1)]
	public void When_CompareBrowserVersionString_Orders_Versions(string left, string right, int expectedSign)
	{
		// The contract is a sign, not a magnitude. This needs the loader but no installed browser.
		Assert.AreEqual(expectedSign, Math.Sign(CoreWebView2Environment.CompareBrowserVersionString(left, right)));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_CreateWithOptionsAsync_Records_User_Data_Folder()
	{
		var userDataFolder = Path.Combine(Path.GetTempPath(), "uno-webview2-data-" + Guid.NewGuid().ToString("N"));

		var environment = await CreateOrInconclusive(() => CoreWebView2Environment.CreateWithOptionsAsync(null, userDataFolder, new CoreWebView2EnvironmentOptions()).AsTask());

		Assert.AreEqual(userDataFolder, environment.UserDataFolder);
		Assert.IsNotNull(environment.BrowserVersionString);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_CreateAsync_Has_Empty_User_Data_Folder()
	{
		var environment = await CreateOrInconclusive(() => CoreWebView2Environment.CreateAsync().AsTask());

		Assert.AreEqual("", environment.UserDataFolder);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public async Task When_CreateWithOptionsAsync_Missing_Browser_Folder_Throws()
	{
		var missingFolder = Path.Combine(Path.GetTempPath(), "uno-webview2-missing-" + Guid.NewGuid().ToString("N"));

		await Assert.ThrowsAsync<FileNotFoundException>(() => CoreWebView2Environment.CreateWithOptionsAsync(missingFolder, null, null).AsTask());
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_CreateWebResourceResponse()
	{
		var environment = await CreateOrInconclusive(() => CoreWebView2Environment.CreateAsync().AsTask());
		var content = new MemoryStream(Encoding.UTF8.GetBytes("<html></html>")).AsRandomAccessStream();

		var response = environment.CreateWebResourceResponse(content, 200, "OK", "Content-Type: text/html\r\nSet-Cookie: a=1\r\nset-cookie: b=2");

		Assert.AreSame(content, response.Content);
		Assert.AreEqual(200, response.StatusCode);
		Assert.AreEqual("OK", response.ReasonPhrase);
		Assert.IsTrue(response.Headers.Contains("content-type"));
		Assert.AreEqual("text/html", response.Headers.GetHeader("CONTENT-TYPE"));
		Assert.AreEqual(3, response.Headers.Count());

		var cookies = response.Headers.GetHeaders("Set-Cookie");
		Assert.IsTrue(cookies.HasCurrent);
		Assert.AreEqual("a=1", cookies.Current.Value);
		Assert.IsTrue(cookies.MoveNext());
		Assert.AreEqual("b=2", cookies.Current.Value);
		Assert.IsFalse(cookies.MoveNext());
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_EnsureCoreWebView2Async_With_Environment()
	{
		var environment = await CreateOrInconclusive(() => CoreWebView2Environment.CreateWithOptionsAsync(null, "folder", null).AsTask());
		var webView = new WebView2();

		Assert.Throws<ArgumentNullException>(() => webView.EnsureCoreWebView2Async(null));

		// Not awaited: the WebView is not in the visual tree, so its native WebView never initializes.
		_ = webView.EnsureCoreWebView2Async(environment);

		Assert.AreSame(environment, webView.CoreWebView2.Environment);
	}

	private static async Task<CoreWebView2Environment> CreateOrInconclusive(Func<Task<CoreWebView2Environment>> create)
	{
		CoreWebView2Environment environment = null;
		Exception failure = null;

		try
		{
			environment = await create();
		}
		catch (Exception ex) when (ex is FileNotFoundException or DllNotFoundException)
		{
			// Platforms that can probe for a browser fail creation when none is installed.
			failure = ex;
		}

		if (failure is not null)
		{
			Assert.Inconclusive($"The WebView2 Runtime is unavailable: {failure.GetType().FullName}: {failure.Message}");
		}

		return environment;
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
