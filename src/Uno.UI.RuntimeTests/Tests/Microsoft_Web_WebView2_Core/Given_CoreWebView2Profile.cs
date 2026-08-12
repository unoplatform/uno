using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_Web_WebView2_Core;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
public class Given_CoreWebView2Profile
{
	private static readonly TimeSpan ClearBrowsingDataTimeout = TimeSpan.FromSeconds(30);

	/// <summary>
	/// Undoes the footprint <see cref="CreateCoreWebView2Async"/> leaves in the visual tree.
	/// </summary>
	/// <remarks>
	/// Every test here loads a live WebView2, which is a whole browser process tree rather than an ordinary
	/// element. The harness only unloads test content when IsUnloadingTestContent is set, and the headless
	/// runner behind --runtime-tests builds its config without it, so nothing would clear these. Left in place
	/// they stay loaded and pumping messages for the remainder of the run, which is both a leak and a way for
	/// these tests to destabilize unrelated ones.
	/// </remarks>
	[TestCleanup]
	public void Cleanup() => TestServices.WindowHelper.WindowContent = null;

	[TestMethod]
	public async Task When_Profile_And_Environment_Are_Available()
	{
		var coreWebView = await CreateCoreWebView2Async();

		Assert.IsNotNull(coreWebView.Profile);
		Assert.IsNotNull(coreWebView.Environment);

#if HAS_UNO
		// Uno caches both facades per CoreWebView2. A WinRT projection may hand back distinct wrappers,
		// so this identity guarantee is Uno-specific.
		Assert.AreSame(coreWebView.Profile, coreWebView.Profile);
		Assert.AreSame(coreWebView.Environment, coreWebView.Environment);
#endif
	}

	[TestMethod]
	public async Task When_Environment_Reports_Paths_And_Version()
	{
		var environment = (await CreateCoreWebView2Async()).Environment;

		Assert.IsTrue(Path.IsPathRooted(environment.UserDataFolder), $"'{environment.UserDataFolder}' is not rooted.");
		Assert.IsTrue(Directory.Exists(environment.UserDataFolder), $"'{environment.UserDataFolder}' does not exist.");

		// Created lazily on the first crash, so only the shape is asserted.
		Assert.IsFalse(string.IsNullOrEmpty(environment.FailureReportFolderPath));
		Assert.IsTrue(Path.IsPathRooted(environment.FailureReportFolderPath));

		// The environment is created without an explicit browserExecutableFolder, so it must resolve to
		// the same browser the static reports.
		Assert.AreEqual(CoreWebView2Environment.GetAvailableBrowserVersionString(), environment.BrowserVersionString);
	}

	[TestMethod]
	public async Task When_Profile_Paths_Are_Consistent_With_Environment()
	{
		var coreWebView = await CreateCoreWebView2Async();
		var profile = coreWebView.Profile;

		// Empty, not "Default": the controller is created without ICoreWebView2ControllerOptions, so no
		// profile name is ever requested even though the profile directory resolves to "Default".
		Assert.IsNotNull(profile.ProfileName);
		Assert.IsTrue(Path.IsPathRooted(profile.ProfilePath), $"'{profile.ProfilePath}' is not rooted.");
		Assert.IsTrue(
			profile.ProfilePath.StartsWith(coreWebView.Environment.UserDataFolder, StringComparison.OrdinalIgnoreCase),
			$"'{profile.ProfilePath}' is not under '{coreWebView.Environment.UserDataFolder}'.");
		Assert.IsFalse(profile.IsInPrivateModeEnabled);
		Assert.IsTrue(Path.IsPathRooted(profile.DefaultDownloadFolderPath), $"'{profile.DefaultDownloadFolderPath}' is not rooted.");
	}

	[TestMethod]
	public async Task When_PreferredColorScheme_Round_Trips()
	{
		var profile = (await CreateCoreWebView2Async()).Profile;
		var original = profile.PreferredColorScheme;

		try
		{
			profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Dark;
			Assert.AreEqual(CoreWebView2PreferredColorScheme.Dark, profile.PreferredColorScheme);

			profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Light;
			Assert.AreEqual(CoreWebView2PreferredColorScheme.Light, profile.PreferredColorScheme);
		}
		finally
		{
			// Unlike the visual tree, this is persisted on disk and outlives the process, so it cannot be
			// left to Cleanup: it must be restored or it leaks into every later test and into manual runs.
			profile.PreferredColorScheme = original;
		}
	}

	[TestMethod]
	public async Task When_BrowserProcessId_Is_Reported()
	{
		Assert.AreNotEqual(0u, (await CreateCoreWebView2Async()).BrowserProcessId);
	}

	[TestMethod]
	public async Task When_GetProcessInfos_Includes_The_Browser_Process()
	{
		var coreWebView = await CreateCoreWebView2Async();
		var processInfos = coreWebView.Environment.GetProcessInfos();

		Assert.IsNotNull(processInfos);

		// Renderer, GPU and utility processes appear asynchronously, so neither the count nor the set of
		// kinds is deterministic. Only the browser process is guaranteed once the controller exists.
		Assert.IsTrue(
			processInfos.Any(info => info.Kind == CoreWebView2ProcessKind.Browser && (uint)info.ProcessId == coreWebView.BrowserProcessId),
			"GetProcessInfos did not report the browser process.");
		Assert.IsTrue(processInfos.All(info => info.ProcessId != 0));
	}

	[TestMethod]
	[DataRow(0)]
	[DataRow(1)]
	[DataRow(2)]
	public async Task When_ClearBrowsingDataAsync_Completes(int overload)
	{
		var profile = (await CreateCoreWebView2Async()).Profile;

		// There is no cheap positive observable for "the data is gone", so the assertion is that the
		// operation completes rather than hangs. The bound turns a native hang into one failed test
		// instead of a stalled run.
		var clearing = overload switch
		{
			0 => profile.ClearBrowsingDataAsync(),
			1 => profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.AllProfile),
			_ => profile.ClearBrowsingDataAsync(
				CoreWebView2BrowsingDataKinds.Cookies,
				DateTimeOffset.UtcNow.AddHours(-1),
				DateTimeOffset.UtcNow),
		};

		await clearing.AsTask().WaitAsync(ClearBrowsingDataTimeout);
	}

	/// <remarks>
	/// The WebView2 this loads is removed by <see cref="Cleanup"/>.
	/// </remarks>
	private static async Task<CoreWebView2> CreateCoreWebView2Async()
	{
		var border = new Border();
		var webView = new WebView2 { Width = 200, Height = 200 };
		border.Child = webView;

		await UITestHelper.Load(border);
		await webView.EnsureCoreWebView2Async();

		Assert.IsNotNull(webView.CoreWebView2, "The CoreWebView2 was not initialized.");
		return webView.CoreWebView2;
	}
}
