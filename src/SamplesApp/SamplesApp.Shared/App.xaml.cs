using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using System.Globalization;
using Windows.UI.ViewManagement;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging;
using Uno;
using Uno.Foundation.Extensibility;

#if __SKIA__
using Uno.UI.Xaml.Controls.Extensions;
#endif

#if !HAS_UNO
using Uno.Logging;
#endif

#if HAS_UNO_WINUI
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
#else
using LaunchActivatedEventArgs = Windows.ApplicationModel.Activation.LaunchActivatedEventArgs;
#endif

namespace SamplesApp
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	sealed public partial class App : Application
	{
#if HAS_UNO
		private static Uno.Foundation.Logging.Logger _log;
#else
		private static ILogger _log;
#endif

		static App()
		{
			ConfigureFilters();
		}

		/// <summary>
		/// Initializes the singleton application object.  This is the first line of authored code
		/// executed, and as such is the logical equivalent of main() or WinMain().
		/// </summary>
		public App()
		{
			// Fix language for UI tests
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
			Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

			ConfigureFeatureFlags();

			AssertIssue1790ApplicationSettingsUsable();

			this.InitializeComponent();
			this.Suspending += OnSuspending;
		}

		/// <summary>
		/// Invoked when the application is launched normally by the end user.  Other entry points
		/// will be used such as when the application is launched to open a specific file.
		/// </summary>
		/// <param name="e">Details about the launch request and process.</param>
		protected
#if HAS_UNO
			internal
#endif
			override void OnLaunched(LaunchActivatedEventArgs e)
		{
#if __IOS__ && !NET6_0
			// requires Xamarin Test Cloud Agent
			Xamarin.Calabash.Start();

			LaunchiOSWatchDog();
#endif

			var sw = Stopwatch.StartNew();
			var n = Windows.UI.Xaml.Window.Current.Dispatcher.RunIdleAsync(
				_ =>
				{
					Console.WriteLine("Done loading " + sw.Elapsed);
				});

#if DEBUG
			if (System.Diagnostics.Debugger.IsAttached)
			{
				// this.DebugSettings.EnableFrameRateCounter = true;
			}
#endif
			InitializeFrame(e.Arguments);

			AssertIssue8641NativeOverlayInitialized();

			Windows.UI.Xaml.Window.Current.Activate();

			ApplicationView.GetForCurrentView().Title = "Uno Samples";

			HandleLaunchArguments(e);
		}

		private static async Task<bool> HandleSkiaAutoScreenshots(LaunchActivatedEventArgs e)
		{
#if __SKIA__
			var runAutoScreenshotsParam =
			e.Arguments.Split(';').FirstOrDefault(a => a.StartsWith("--auto-screenshots"));

			var screenshotsPath = runAutoScreenshotsParam?.Split('=').LastOrDefault();

			if (!string.IsNullOrEmpty(screenshotsPath))
			{
				var sw = Stopwatch.StartNew();
				var n = Windows.UI.Xaml.Window.Current.Dispatcher.RunIdleAsync(
					_ =>
					{
						var n = Windows.UI.Xaml.Window.Current.Dispatcher.RunAsync(
							CoreDispatcherPriority.Normal,
							async () =>
							{
								await SampleControl.Presentation.SampleChooserViewModel.Instance.RecordAllTests(CancellationToken.None, screenshotsPath, () => System.Environment.Exit(0));
							}
						);

					});

				return true;
			}
#endif

			return false;
		}

		private static async Task<bool> HandleSkiaRuntimeTests(LaunchActivatedEventArgs e)
		{
#if __SKIA__
			var runRuntimeTestsResultsParam =
			e.Arguments.Split(';').FirstOrDefault(a => a.StartsWith("--runtime-tests"));

			var runtimeTestResultFilePath = runRuntimeTestsResultsParam?.Split('=').LastOrDefault();

			if (!string.IsNullOrEmpty(runtimeTestResultFilePath))
			{
				Console.WriteLine($"HandleSkiaRuntimeTests: {runtimeTestResultFilePath}");

				_ = Window.Current.Dispatcher.RunIdleAsync(async _ =>
				{
					// let the app finish its startup
					await Task.Delay(TimeSpan.FromSeconds(5));

					await Task.Run(
						async () =>
						{
							await SampleControl.Presentation.SampleChooserViewModel.Instance.RunRuntimeTests(
								CancellationToken.None,
								runtimeTestResultFilePath,
								() => System.Environment.Exit(0));
						}
					);
				});

				return true;
			}
#endif
			return false;
		}

#if __IOS__
		/// <summary>
		/// Launches a watchdog that will terminate the app if the dispatcher does not process
		/// messages within a specific time.
		///
		/// Restarting the app is required in some cases where either the test engine, or Xamarin.UITest stall
		/// while processing the events of the app.
		///
		/// See https://github.com/unoplatform/uno/issues/3363 for details
		/// </summary>
		private void LaunchiOSWatchDog()
		{
			if (!Debugger.IsAttached)
			{
				Console.WriteLine("Starting dispatcher WatchDog...");

				var dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
				var timeout = TimeSpan.FromSeconds(240);

				Task.Run(async () =>
				{

					while (true)
					{
						var delayTask = Task.Delay(timeout);
						var messageTask = dispatcher.RunAsync(CoreDispatcherPriority.High, () => { }).AsTask();

						if (await Task.WhenAny(delayTask, messageTask) == delayTask)
						{
							ThreadPool.QueueUserWorkItem(
								_ => {
								Console.WriteLine($"WatchDog detecting a stall in the dispatcher after {timeout}, terminating the app");
								throw new Exception($"Watchdog failed");
							});
						}

						await Task.Delay(TimeSpan.FromSeconds(5));
					}
				});
			}
		}
#endif

		protected
#if HAS_UNO
			internal
#endif
			override async void OnActivated(IActivatedEventArgs e)
		{
			base.OnActivated(e);

			InitializeFrame();
			Windows.UI.Xaml.Window.Current.Activate();

			if (e.Kind == ActivationKind.Protocol)
			{
				var protocolActivatedEventArgs = (ProtocolActivatedEventArgs)e;
				var dlg = new MessageDialog(
					$"PreviousState - {e.PreviousExecutionState}, " +
					$"Uri - {protocolActivatedEventArgs.Uri}",
					"Application activated via protocol");
				if (ApiInformation.IsMethodPresent("Windows.UI.Popups.MessageDialog", nameof(MessageDialog.ShowAsync)))
				{
					await dlg.ShowAsync();
				}
			}
		}

		private void InitializeFrame(string arguments = null)
		{
			Frame rootFrame = Windows.UI.Xaml.Window.Current.Content as Frame;

			// Do not repeat app initialization when the Window already has content,
			// just ensure that the window is active
			if (rootFrame == null)
			{
				// Create a Frame to act as the navigation context and navigate to the first page
				rootFrame = new Frame();

				rootFrame.NavigationFailed += OnNavigationFailed;

				// Place the frame in the current Window
				Windows.UI.Xaml.Window.Current.Content = rootFrame;
				Console.WriteLine($"RootFrame: {rootFrame}");
			}

			if (rootFrame.Content == null)
			{
				// When the navigation stack isn't restored navigate to the first page,
				// configuring the new page by passing required information as a navigation
				// parameter
				var startingPageType = typeof(MainPage);
				if (arguments != null)
				{
					rootFrame.Navigate(startingPageType, arguments);
				}
				else
				{
					rootFrame.Navigate(startingPageType);
				}
			}
		}

		private async void HandleLaunchArguments(LaunchActivatedEventArgs launchActivatedEventArgs)
		{
			Console.WriteLine($"HandleLaunchArguments: {launchActivatedEventArgs.Arguments}");

			if (await HandleSkiaAutoScreenshots(launchActivatedEventArgs))
			{
				return;
			}

			if (await HandleSkiaRuntimeTests(launchActivatedEventArgs))
			{
				return;
			}

			if (await TryNavigateToLaunchSampleAsync(launchActivatedEventArgs))
			{
				return;
			}

			if (!string.IsNullOrEmpty(launchActivatedEventArgs.Arguments))
			{
				var dlg = new MessageDialog(launchActivatedEventArgs.Arguments, "Launch arguments");
				await dlg.ShowAsync();
			}
		}

		private async Task<bool> TryNavigateToLaunchSampleAsync(LaunchActivatedEventArgs launchActivatedEventArgs)
		{
			const string samplePrefix = "sample=";
			try
			{
				if (launchActivatedEventArgs.Arguments == null)
				{
					return false;
				}

				var args = Uri.UnescapeDataString(launchActivatedEventArgs.Arguments);

				if (string.IsNullOrEmpty(args) || !args.StartsWith(samplePrefix))
				{
					return false;
				}

				args = args.Substring(samplePrefix.Length);

				var pathParts = args.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
				var category = pathParts[0];
				var sampleName = pathParts[1];

				await SampleControl.Presentation.SampleChooserViewModel.Instance.SetSelectedSample(CancellationToken.None, category, sampleName);
				return true;
			}
			catch (Exception ex)
			{
				_log.Error($"Could not navigate to initial sample - {ex}");
			}
			return false;
		}

		/// <summary>
		/// Invoked when Navigation to a certain page fails
		/// </summary>
		/// <param name="sender">The Frame which failed navigation</param>
		/// <param name="e">Details about the navigation failure</param>
		void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
		{
			throw new Exception($"Failed to load Page {e.SourcePageType}: {e.Exception}");
		}

		/// <summary>
		/// Invoked when application execution is being suspended.  Application state is saved
		/// without knowing whether the application will be terminated or resumed with the contents
		/// of memory still intact.
		/// </summary>
		/// <param name="sender">The source of the suspend request.</param>
		/// <param name="e">Details about the suspend request.</param>
		private void OnSuspending(object sender, SuspendingEventArgs e)
		{
			var deferral = e.SuspendingOperation.GetDeferral();

			Console.WriteLine($"OnSuspending (Deadline:{e.SuspendingOperation.Deadline})");

			deferral.Complete();
		}

		public static void ConfigureFilters()
		{
#if HAS_UNO
			System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) => _log.Error("UnobservedTaskException", e.Exception);
			AppDomain.CurrentDomain.UnhandledException += (s, e) => _log.Error("UnhandledException", e.ExceptionObject as Exception);
#endif
			var factory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
			{
#if __WASM__
				builder.AddProvider(new Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#else
				builder.AddConsole();
#endif


#if !DEBUG
				// Exclude logs below this level
				builder.SetMinimumLevel(LogLevel.Information);
#else
				// Exclude logs below this level
				builder.SetMinimumLevel(LogLevel.Debug);
#endif

				// Runtime Tests control logging
				builder.AddFilter("Uno.UI.Samples.Tests", LogLevel.Information);

				builder.AddFilter("Uno", LogLevel.Warning);
				builder.AddFilter("Windows", LogLevel.Warning);
				builder.AddFilter("Microsoft", LogLevel.Warning);

				// RemoteControl and HotReload related
				builder.AddFilter("Uno.UI.RemoteControl", LogLevel.Information);

				// builder.AddFilter("Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug );
				// builder.AddFilter("Windows.UI.Xaml.Controls.PopupPanel", LogLevel.Debug );

				// Generic Xaml events
				// builder.AddFilter("Windows.UI.Xaml", LogLevel.Debug );
				// builder.AddFilter("Windows.UI.Xaml.Media", LogLevel.Debug );
				// builder.AddFilter("Windows.UI.Xaml.Shapes", LogLevel.Debug );
				// builder.AddFilter("Windows.UI.Xaml.VisualStateGroup", LogLevel.Debug );
				// builder.AddFilter("Windows.UI.Xaml.StateTriggerBase", LogLevel.Debug );
				// builder.AddFilter("Windows.UI.Xaml.UIElement", LogLevel.Debug );
				// builder.AddFilter("Windows.UI.Xaml.FrameworkElement", LogLevel.Trace );
				// builder.AddFilter("Windows.UI.Xaml.Controls.TextBlock", LogLevel.Debug );

				// Layouter specific messages
				// builder.AddFilter("Windows.UI.Xaml.Controls", LogLevel.Debug );
				// builder.AddFilter("Windows.UI.Xaml.Controls.Layouter", LogLevel.Debug );
				// builder.AddFilter("Windows.UI.Xaml.Controls.Panel", LogLevel.Debug );
				// builder.AddFilter("Windows.Storage", LogLevel.Debug );

				// Binding related messages
				// builder.AddFilter("Windows.UI.Xaml.Data", LogLevel.Debug );
				// builder.AddFilter("Windows.UI.Xaml.Data", LogLevel.Debug );

				// Binder memory references tracking
				// builder.AddFilter("Uno.UI.DataBinding.BinderReferenceHolder", LogLevel.Debug );

				// builder.AddFilter(ListView-related messages
				// builder.AddFilter("Windows.UI.Xaml.Controls.ListViewBase", LogLevel.Debug );
				// builder.AddFilter("Windows.UI.Xaml.Controls.ListView", LogLevel.Debug );
				// builder.AddFilter("Windows.UI.Xaml.Controls.GridView", LogLevel.Debug );
				// builder.AddFilter("Windows.UI.Xaml.Controls.VirtualizingPanelLayout", LogLevel.Debug );
				// builder.AddFilter("Windows.UI.Xaml.Controls.NativeListViewBase", LogLevel.Debug );
				// builder.AddFilter("Windows.UI.Xaml.Controls.ListViewBaseSource", LogLevel.Debug ); //iOS
				// builder.AddFilter("Windows.UI.Xaml.Controls.ListViewBaseInternalContainer", LogLevel.Debug ); //iOS
				// builder.AddFilter("Windows.UI.Xaml.Controls.NativeListViewBaseAdapter", LogLevel.Debug ); //Android
				// builder.AddFilter("Windows.UI.Xaml.Controls.BufferViewCache", LogLevel.Debug ); //Android
				// builder.AddFilter("Windows.UI.Xaml.Controls.VirtualizingPanelGenerator", LogLevel.Debug ); //WASM


			});


			Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;
#if HAS_UNO
			global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
			_log = Uno.Foundation.Logging.LogExtensionPoint.Factory.CreateLogger(typeof(App));
#endif
		}

		static void ConfigureFeatureFlags()
		{
#if __IOS__
			Uno.UI.FeatureConfiguration.CommandBar.AllowNativePresenterContent = true;
#endif
#if __IOS__ || __ANDROID__
			WinRTFeatureConfiguration.Focus.EnableExperimentalKeyboardFocus = true;
#endif
#if __IOS__
			Uno.UI.FeatureConfiguration.DatePicker.UseLegacyStyle = true;
			Uno.UI.FeatureConfiguration.TimePicker.UseLegacyStyle = true;
#endif
		}


#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
		private static ImmutableHashSet<int> _doneTests = ImmutableHashSet<int>.Empty;
		private static int _testIdCounter = 0;

		public static string GetAllTests()
			=> SampleControl.Presentation.SampleChooserViewModel.Instance.GetAllSamplesNames();

		public static string GetDisplayScreenScaling(string displayId)
			=> (DisplayInformation.GetForCurrentView().LogicalDpi * 100f / 96f).ToString(CultureInfo.InvariantCulture);

		public static string RunTest(string metadataName)
		{
			try
			{
				Console.WriteLine($"Initiate Running Test {metadataName}");

				var testId = Interlocked.Increment(ref _testIdCounter);

				Windows.UI.Xaml.Window.Current.Dispatcher.RunAsync(
					CoreDispatcherPriority.Normal,
					async () =>
					{
						try
						{
#if __IOS__ || __ANDROID__
							var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
							if (statusBar != null)
							{
								Windows.UI.Xaml.Window.Current.Dispatcher.RunAsync(
									Windows.UI.Core.CoreDispatcherPriority.Normal,
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

#if __IOS__
		[Foundation.Export("runTest:")] // notice the colon at the end of the method name
		public Foundation.NSString RunTestBackdoor(Foundation.NSString value) => new Foundation.NSString(RunTest(value));

		[Foundation.Export("isTestDone:")] // notice the colon at the end of the method name
		public Foundation.NSString IsTestDoneBackdoor(Foundation.NSString value) => new Foundation.NSString(IsTestDone(value).ToString());

		[Foundation.Export("getDisplayScreenScaling:")] // notice the colon at the end of the method name
		public Foundation.NSString GetDisplayScreenScalingBackdoor(Foundation.NSString value) => new Foundation.NSString(GetDisplayScreenScaling(value).ToString());
#endif

		public static bool IsTestDone(string testId) => int.TryParse(testId, out var id) ? _doneTests.Contains(id) : false;

		/// <summary>
		/// Assert that ApplicationData.Current.[LocalFolder|RoamingFolder] is usable in the constructor of App.xaml.cs on all platforms.
		/// </summary>
		/// <seealso href="https://github.com/unoplatform/uno/issues/1741"/>
		public void AssertIssue1790ApplicationSettingsUsable()
		{
			void AssertIsUsable(Windows.Storage.ApplicationDataContainer container)
			{
				const string issue1790 = nameof(issue1790);

				container.Values.Remove(issue1790);
				container.Values.Add(issue1790, "ApplicationData.Current.[LocalFolder|RoamingFolder] is usable in the constructor of App.xaml.cs on this platform.");

				Assert.IsTrue(container.Values.ContainsKey(issue1790));
			}

			AssertIsUsable(Windows.Storage.ApplicationData.Current.LocalSettings);
			AssertIsUsable(Windows.Storage.ApplicationData.Current.RoamingSettings);
		}

		/// <summary>
		/// Assert that the native overlay layer for Skia targets is initialized in time for UI to appear.
		/// </summary>
		public void AssertIssue8641NativeOverlayInitialized()
		{
#if __SKIA__
			var textBox = new TextBox();
			var textBoxView = new TextBoxView(textBox);
			ApiExtensibility.CreateInstance<ITextBoxViewExtension>(textBoxView, out var textBoxViewExtension);
			Assert.IsTrue(textBoxViewExtension.IsNativeOverlayLayerInitialized);
#endif
		}
	}
}
