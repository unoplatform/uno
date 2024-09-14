#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Uno.UI.RuntimeTests.Extensions;
using Private.Infrastructure;
using System.Runtime.InteropServices.JavaScript;

#if !HAS_UNO
using Uno.Logging;
#endif

#if HAS_UNO_WINUI
using Microsoft.UI.Dispatching;
#else
using Windows.System;
#endif

#if __SKIA__ || __MACOS__
using System.CommandLine;
#endif

namespace SamplesApp;

partial class App
{
	private static ImmutableHashSet<int> _doneTests = ImmutableHashSet<int>.Empty;
	private static int _testIdCounter = 0;

#if __WASM__
	[System.Runtime.InteropServices.JavaScript.JSExport]
#endif
	public static string GetAllTests() => SampleControl.Presentation.SampleChooserViewModel.Instance.GetAllSamplesNames();

#if __WASM__
	[System.Runtime.InteropServices.JavaScript.JSExport]
#endif
	public static bool IsTestDone(string testId) => int.TryParse(testId, out var id) ? _doneTests.Contains(id) : false;

	public static async Task<bool> HandleRuntimeTests(string args)
	{
		var runRuntimeTestsResultsParam =
			args.Split(';').FirstOrDefault(a => a.StartsWith("--runtime-tests"));

		var runtimeTestResultFilePath = runRuntimeTestsResultsParam?.Split('=').LastOrDefault();

		// Used to autostart the runtime tests for iOS/Android Runtime tests
		runtimeTestResultFilePath ??= Environment.GetEnvironmentVariable("UITEST_RUNTIME_AUTOSTART_RESULT_FILE");

		Console.WriteLine($"Automated runtime tests output file: {runtimeTestResultFilePath}");

		if (!string.IsNullOrEmpty(runtimeTestResultFilePath))
		{
			Console.WriteLine($"Writing canary file {runtimeTestResultFilePath}.canary");
			System.IO.File.WriteAllText(runtimeTestResultFilePath + ".canary", DateTime.Now.ToString(), System.Text.Encoding.Unicode);

			// let the app finish its startup
			await Task.Delay(TimeSpan.FromSeconds(5));

			await SampleControl.Presentation.SampleChooserViewModel.Instance.RunRuntimeTests(
				CancellationToken.None,
				runtimeTestResultFilePath,
				() => System.Environment.Exit(0));

			return true;
		}

		return false;
	}

#if __WASM__
	[System.Runtime.InteropServices.JavaScript.JSExport]
#endif
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
		var autoScreenshotsOption = new Option<string>("--auto-screenshots");
		var totalGroupsOption = new Option<int>("--total-groups", getDefaultValue: () => 1);
		var currentGroupIndexOption = new Option<int>("--current-group-index", getDefaultValue: () => 0);

		// SamplesApp can be opened with --runtime-tests option, which is currently manually handled in HandleLaunchArguments.
		var runtimeTestsOption = new Option<string>("--runtime-tests");
		var rootCommand = new RootCommand
		{
			autoScreenshotsOption,
			totalGroupsOption,
			currentGroupIndexOption,
			runtimeTestsOption,
		};

		bool commandReturn = false;

		rootCommand.SetHandler<string, int, int>((screenshotsPath, totalGroups, currentGroupIndex) =>
		{
			if (totalGroups < 1)
			{
				throw new ArgumentException("Total groups must be >= 1");
			}

			if (currentGroupIndex < 0 || currentGroupIndex >= totalGroups)
			{
				throw new ArgumentException("Group index is out of range.");
			}

			Console.WriteLine($"Screenshots path: {screenshotsPath}");
			Console.WriteLine($"Total groups: {totalGroups}");
			Console.WriteLine($"Current group index: {currentGroupIndex}");

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
								await SampleControl.Presentation.SampleChooserViewModel.Instance.RecordAllTests(CancellationToken.None, screenshotsPath, totalGroups, currentGroupIndex, () => System.Environment.Exit(0));
							}
						);

					});

				commandReturn = true;
			}
		}, autoScreenshotsOption, totalGroupsOption, currentGroupIndexOption);

		rootCommand.Invoke(args);
		return commandReturn;
#else
		return false;
#endif
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

#if __WASM__
	[JSImport("globalThis.SampleRunner.init")]
	public static partial void InitWasmSampleRunner();
#endif
}
