#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Uno.UI.RuntimeTests.Extensions;
using Private.Infrastructure;

#if !HAS_UNO
using Uno.Logging;
#endif

namespace SamplesApp;

partial class App
{
	private static ImmutableHashSet<int> _doneTests = ImmutableHashSet<int>.Empty;
	private static int _testIdCounter = 0;

	public static string GetAllTests() => SampleControl.Presentation.SampleChooserViewModel.Instance.GetAllSamplesNames();

	public static bool IsTestDone(string testId) => int.TryParse(testId, out var id) ? _doneTests.Contains(id) : false;

	public static async Task<bool> HandleRuntimeTests(string args)
	{
#if __SKIA__ || __MACOS__
		var runRuntimeTestsResultsParam =
			args.Split(';').FirstOrDefault(a => a.StartsWith("--runtime-tests"));

		var runtimeTestResultFilePath = runRuntimeTestsResultsParam?.Split('=').LastOrDefault();

		if (!string.IsNullOrEmpty(runtimeTestResultFilePath))
		{
			Console.WriteLine($"HandleSkiaRuntimeTests: {runtimeTestResultFilePath}");

			// let the app finish its startup
			await Task.Delay(TimeSpan.FromSeconds(5));

			await SampleControl.Presentation.SampleChooserViewModel.Instance.RunRuntimeTests(
				CancellationToken.None,
				runtimeTestResultFilePath,
				() => System.Environment.Exit(0));

			return true;
		}

		return false;
#else
		return await Task.FromResult(false);
#endif
	}

	public static string RunTest(string metadataName)
	{
		if (_mainWindow is null)
		{
			throw new InvalidOperationException("Cannot run tests until main window is initialized.");
		}

		try
		{
			Console.WriteLine($"Initiate Running Test {metadataName}");

			var testId = Interlocked.Increment(ref _testIdCounter);

			_ = UnitTestDispatcherCompat.From(_mainWindow).RunAsync(
				UnitTestDispatcherCompat.Priority.Normal,
				async () =>
				{
					try
					{
#if __IOS__ || __ANDROID__
						var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
						if (statusBar != null)
						{
							_ = UnitTestDispatcherCompat.From(_mainWindow).RunAsync(
								UnitTestDispatcherCompat.Priority.Normal,
								async () => await statusBar.HideAsync()
							);
						}
#endif

#if __ANDROID__
						Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = false;
						Uno.UI.FeatureConfiguration.ScrollViewer.AndroidScrollbarFadeDelay = TimeSpan.Zero;
#endif

#if HAS_UNO
						// Disable the TextBox caret for new instances
						Uno.UI.FeatureConfiguration.TextBox.HideCaret = true;
#endif

						var t = SampleControl.Presentation.SampleChooserViewModel.Instance.SetSelectedSample(CancellationToken.None, metadataName);
						var timeout = Task.Delay(30000);

						await Task.WhenAny(t, timeout);

						if (!(t.IsCompleted && !t.IsFaulted))
						{
							throw new TimeoutException();
						}

						ImmutableInterlocked.Update(ref _doneTests, lst => lst.Add(testId));
					}
					catch (Exception e)
					{
						Console.WriteLine($"Failed to run test {metadataName}, {e}");
					}
					finally
					{
#if HAS_UNO
						// Restore the caret for new instances
						Uno.UI.FeatureConfiguration.TextBox.HideCaret = false;
#endif
					}
				}
			);

			return testId.ToString();
		}
		catch (Exception e)
		{
			Console.WriteLine($"Failed Running Test {metadataName}, {e}");
			return "";
		}
	}


	private bool HandleAutoScreenshots(string args)
	{
#if __SKIA__ || __MACOS__
		var runAutoScreenshotsParam =
		args.Split(';').FirstOrDefault(a => a.StartsWith("--auto-screenshots"));

		var screenshotsPath = runAutoScreenshotsParam?.Split('=').LastOrDefault();

		if (!string.IsNullOrEmpty(screenshotsPath))
		{
			if (MainWindow is null)
			{
				throw new InvalidOperationException("Main window must be initialized before running screenshot tests");
			}

			var n = UnitTestDispatcherCompat.From(MainWindow).RunIdleAsync(
				_ =>
				{
					var n = UnitTestDispatcherCompat.From(MainWindow).RunAsync(
						UnitTestDispatcherCompat.Priority.Normal,
						async () =>
						{
							await SampleControl.Presentation.SampleChooserViewModel.Instance.RecordAllTests(CancellationToken.None, screenshotsPath, () => System.Environment.Exit(0));
						}
					);
				},
				DispatcherQueuePriority.Low
				);

			return true;
		}
#endif

		return false;
	}

	private bool TryNavigateToLaunchSample(string args)
	{
		const string samplePrefix = "sample=";
		try
		{
			args = Uri.UnescapeDataString(args);

			if (string.IsNullOrEmpty(args) || !args.StartsWith(samplePrefix))
			{
				return false;
			}

			args = args.Substring(samplePrefix.Length);

			var pathParts = args.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			var category = pathParts[0];
			var sampleName = pathParts[1];

			SampleControl.Presentation.SampleChooserViewModel.Instance.SetSelectedSample(CancellationToken.None, category, sampleName);
			return true;
		}
		catch (Exception ex)
		{
			_log?.Error($"Could not navigate to initial sample - {ex}");
		}
		return false;
	}

#if __IOS__
	[Foundation.Export("runTest:")] // notice the colon at the end of the method name
	public Foundation.NSString RunTestBackdoor(Foundation.NSString value) => new Foundation.NSString(RunTest(value));

	[Foundation.Export("isTestDone:")] // notice the colon at the end of the method name
	public Foundation.NSString IsTestDoneBackdoor(Foundation.NSString value) => new Foundation.NSString(IsTestDone(value).ToString());

	[Foundation.Export("getDisplayScreenScaling:")] // notice the colon at the end of the method name
	public Foundation.NSString GetDisplayScreenScalingBackdoor(Foundation.NSString value) => new Foundation.NSString(GetDisplayScreenScaling(value).ToString());
#endif
}
