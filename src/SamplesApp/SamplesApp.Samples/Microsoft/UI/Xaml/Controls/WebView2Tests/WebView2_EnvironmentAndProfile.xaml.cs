using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Uno.UI.Samples.Controls;

namespace SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	/// <remarks>
	/// Not gated to non-Windows: these APIs exist natively on WinAppSDK, so running the same page on both heads
	/// is how their behaviour is compared. Every call is therefore behind a button and reports the exception it
	/// gets, which is what a target that does not implement them will show.
	/// </remarks>
	[Sample("WebView", Name = "WebView2_EnvironmentAndProfile", Description = "Exercises the CoreWebView2Environment statics and the CoreWebView2.Environment / CoreWebView2.Profile surface, including ClearBrowsingDataAsync. Implemented on Skia Desktop for Windows; other targets report the exception they throw.", IsManualTest = true, IgnoreInSnapshotTests = true)]
	public sealed partial class WebView2_EnvironmentAndProfile : Page
	{
		public WebView2_EnvironmentAndProfile()
		{
			this.InitializeComponent();
		}

		private void GetAvailableVersionClick(object sender, RoutedEventArgs e)
			=> Report("GetAvailableBrowserVersionString()", () => CoreWebView2Environment.GetAvailableBrowserVersionString() ?? "(null)");

		private void GetAvailableVersionFromFolderClick(object sender, RoutedEventArgs e)
			=> Report(
				$"GetAvailableBrowserVersionString(\"{FolderInput.Text}\")",
				() => CoreWebView2Environment.GetAvailableBrowserVersionString(FolderInput.Text) ?? "(null)");

		private void CompareVersionsClick(object sender, RoutedEventArgs e)
			=> Report("CompareBrowserVersionString", () =>
			{
				var result = CoreWebView2Environment.CompareBrowserVersionString(VersionLeftInput.Text, VersionRightInput.Text);
				return $"{result} (sign {Math.Sign(result)})";
			});

		private void ReadEnvironmentClick(object sender, RoutedEventArgs e)
			=> ReportCoreAsync("CoreWebView2.Environment", core =>
			{
				var environment = core.Environment;
				return Task.FromResult(string.Join(
					Environment.NewLine,
					$"BrowserVersionString    = {environment.BrowserVersionString}",
					$"UserDataFolder          = {environment.UserDataFolder}",
					$"FailureReportFolderPath = {environment.FailureReportFolderPath}",
					$"BrowserProcessId        = {core.BrowserProcessId}"));
			});

		private void ReadProfileClick(object sender, RoutedEventArgs e)
			=> ReportCoreAsync("CoreWebView2.Profile", core =>
			{
				var profile = core.Profile;
				return Task.FromResult(string.Join(
					Environment.NewLine,
					$"ProfileName               = {profile.ProfileName}",
					$"ProfilePath               = {profile.ProfilePath}",
					$"IsInPrivateModeEnabled    = {profile.IsInPrivateModeEnabled}",
					$"DefaultDownloadFolderPath = {profile.DefaultDownloadFolderPath}",
					$"PreferredColorScheme      = {profile.PreferredColorScheme}"));
			});

		private void ToggleColorSchemeClick(object sender, RoutedEventArgs e)
			=> ReportCoreAsync("Profile.PreferredColorScheme", core =>
			{
				var profile = core.Profile;
				profile.PreferredColorScheme = profile.PreferredColorScheme switch
				{
					CoreWebView2PreferredColorScheme.Auto => CoreWebView2PreferredColorScheme.Light,
					CoreWebView2PreferredColorScheme.Light => CoreWebView2PreferredColorScheme.Dark,
					_ => CoreWebView2PreferredColorScheme.Auto,
				};

				return Task.FromResult(profile.PreferredColorScheme.ToString());
			});

		private void DumpProcessInfosClick(object sender, RoutedEventArgs e)
			=> ReportCoreAsync("Environment.GetProcessInfos()", core => Task.FromResult(
				string.Join(Environment.NewLine, core.Environment.GetProcessInfos().Select(info => $"{info.Kind} = {info.ProcessId}"))));

		private void ClearAllClick(object sender, RoutedEventArgs e)
			=> ReportCoreAsync("Profile.ClearBrowsingDataAsync()", async core =>
			{
				await core.Profile.ClearBrowsingDataAsync();
				return "OK";
			});

		private void ClearKindsClick(object sender, RoutedEventArgs e)
			=> ReportCoreAsync("Profile.ClearBrowsingDataAsync(AllProfile)", async core =>
			{
				await core.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.AllProfile);
				return "OK";
			});

		private void ClearRangeClick(object sender, RoutedEventArgs e)
			=> ReportCoreAsync("Profile.ClearBrowsingDataAsync(Cookies, -1h, now)", async core =>
			{
				await core.Profile.ClearBrowsingDataAsync(
					CoreWebView2BrowsingDataKinds.Cookies,
					DateTimeOffset.UtcNow.AddHours(-1),
					DateTimeOffset.UtcNow);
				return "OK";
			});

		private void Report(string operation, Func<string> action)
		{
			try
			{
				OutputText.Text = $"{operation}:{Environment.NewLine}{action()}";
			}
			catch (Exception ex)
			{
				OutputText.Text = $"{operation}:{Environment.NewLine}{ex.GetType().FullName}: {ex.Message}";
			}
		}

		private async void ReportCoreAsync(string operation, Func<CoreWebView2, Task<string>> action)
		{
			OutputText.Text = $"{operation}:{Environment.NewLine}(running)";

			try
			{
				await SUT.EnsureCoreWebView2Async();
				OutputText.Text = $"{operation}:{Environment.NewLine}{await action(SUT.CoreWebView2)}";
			}
			catch (Exception ex)
			{
				OutputText.Text = $"{operation}:{Environment.NewLine}{ex.GetType().FullName}: {ex.Message}";
			}
		}
	}
}
