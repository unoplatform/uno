using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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

namespace SamplesApp
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	sealed partial class App : Application
	{
		/// <summary>
		/// Initializes the singleton application object.  This is the first line of authored code
		/// executed, and as such is the logical equivalent of main() or WinMain().
		/// </summary>
		public App()
		{
			ConfigureFilters(LogExtensionPoint.AmbientLoggerFactory);

			AssertIssue1790();

			this.InitializeComponent();
			this.Suspending += OnSuspending;

		}

		/// <summary>
		/// Assert that ApplicationData.Current.[LocalFolder|RoamingFolder] is usable in the constructor of App.xaml.cs on all platforms.
		/// </summary>
		/// <seealso cref="https://github.com/unoplatform/uno/issues/1741"/>
		public void AssertIssue1790()
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning("Assert for issue 1790 should be enabled on all platforms when issue #2213 is fixed");
			}
#if !__MACOS__
			void AssertIsUsable(Windows.Storage.ApplicationDataContainer container)
			{
				const string issue1790 = nameof(issue1790);

				container.Values.Remove(issue1790);
				container.Values.Add(issue1790, "ApplicationData.Current.[LocalFolder|RoamingFolder] is usable in the constructor of App.xaml.cs on this platform.");

				Assert.IsTrue(container.Values.ContainsKey(issue1790));
			}

			AssertIsUsable(Windows.Storage.ApplicationData.Current.LocalSettings);
			AssertIsUsable(Windows.Storage.ApplicationData.Current.RoamingSettings);
#endif
		}

		/// <summary>
		/// Invoked when the application is launched normally by the end user.  Other entry points
		/// will be used such as when the application is launched to open a specific file.
		/// </summary>
		/// <param name="e">Details about the launch request and process.</param>
		protected override void OnLaunched(LaunchActivatedEventArgs e)
		{
#if __IOS__
			// requires Xamarin Test Cloud Agent
			Xamarin.Calabash.Start();
#endif
			var sw = Stopwatch.StartNew();
			var n = Windows.UI.Xaml.Window.Current.Dispatcher.RunIdleAsync(
				_ => Console.WriteLine("Done loading " + sw.Elapsed));

#if DEBUG
			if (System.Diagnostics.Debugger.IsAttached)
			{
				// this.DebugSettings.EnableFrameRateCounter = true;
			}
#endif
			Frame rootFrame = Windows.UI.Xaml.Window.Current.Content as Frame;

			// Do not repeat app initialization when the Window already has content,
			// just ensure that the window is active
			if (rootFrame == null)
			{
				// Create a Frame to act as the navigation context and navigate to the first page
				rootFrame = new Frame();

				rootFrame.NavigationFailed += OnNavigationFailed;

				if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
				{
					//TODO: Load state from previously suspended application
				}

				// Place the frame in the current Window
				Windows.UI.Xaml.Window.Current.Content = rootFrame;
			}

			if (e.PrelaunchActivated == false)
			{
				if (rootFrame.Content == null)
				{
					// When the navigation stack isn't restored navigate to the first page,
					// configuring the new page by passing required information as a navigation
					// parameter
					rootFrame.Navigate(typeof(MainPage), e.Arguments);
				}
				// Ensure the current window is active
				Windows.UI.Xaml.Window.Current.Activate();
			}

			DisplayLaunchArguments(e);
		}

		private async void DisplayLaunchArguments(LaunchActivatedEventArgs launchActivatedEventArgs)
		{
			if (!string.IsNullOrEmpty(launchActivatedEventArgs.Arguments))
			{
				var dlg = new MessageDialog(launchActivatedEventArgs.Arguments, "Launch arguments");
				await dlg.ShowAsync();
			}
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
			//TODO: Save application state and stop any background activity
			deferral.Complete();
		}

		static void ConfigureFilters(ILoggerFactory factory)
		{
			factory
				.WithFilter(new FilterLoggerSettings
					{
						{ "Uno", LogLevel.Warning },
						{ "Windows", LogLevel.Warning },

						// RemoteControl and HotReload related
						{ "Uno.UI.RemoteControl", LogLevel.Information },

						// { "Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug },
						// { "Windows.UI.Xaml.Controls.PopupPanel", LogLevel.Debug },

						// Generic Xaml events
						// { "Windows.UI.Xaml", LogLevel.Debug },
						// { "Windows.UI.Xaml.Shapes", LogLevel.Debug },
						// { "Windows.UI.Xaml.VisualStateGroup", LogLevel.Debug },
						// { "Windows.UI.Xaml.StateTriggerBase", LogLevel.Debug },
						// { "Windows.UI.Xaml.UIElement", LogLevel.Debug },
						// { "Windows.UI.Xaml.FrameworkElement", LogLevel.Trace },
						// { "Windows.UI.Xaml.Controls.TextBlock", LogLevel.Debug },

						// Layouter specific messages
						// { "Windows.UI.Xaml.Controls", LogLevel.Debug },
						// { "Windows.UI.Xaml.Controls.Layouter", LogLevel.Debug },
						// { "Windows.UI.Xaml.Controls.Panel", LogLevel.Debug },
						// { "Windows.Storage", LogLevel.Debug },

						// Binding related messages
						// { "Windows.UI.Xaml.Data", LogLevel.Debug },
						// { "Windows.UI.Xaml.Data", LogLevel.Debug },

						//  Binder memory references tracking
						// { "ReferenceHolder", LogLevel.Debug },

						// ListView-related messages
						// { "Windows.UI.Xaml.Controls.ListViewBase", LogLevel.Debug },
						// { "Windows.UI.Xaml.Controls.ListView", LogLevel.Debug },
						// { "Windows.UI.Xaml.Controls.GridView", LogLevel.Debug },
						// { "Windows.UI.Xaml.Controls.VirtualizingPanelLayout", LogLevel.Debug },
						// { "Windows.UI.Xaml.Controls.NativeListViewBase", LogLevel.Debug },
						// { "Windows.UI.Xaml.Controls.ListViewBaseSource", LogLevel.Debug }, //iOS
						// { "Windows.UI.Xaml.Controls.ListViewBaseInternalContainer", LogLevel.Debug }, //iOS
						// { "Windows.UI.Xaml.Controls.NativeListViewBaseAdapter", LogLevel.Debug }, //Android
						// { "Windows.UI.Xaml.Controls.BufferViewCache", LogLevel.Debug }, //Android
						// { "Windows.UI.Xaml.Controls.VirtualizingPanelGenerator", LogLevel.Debug }, //WASM
					}
				)
#if DEBUG
				//.AddConsole(LogLevel.Trace);
				.AddConsole(LogLevel.Debug);

#else
				.AddConsole(LogLevel.Warning);
#endif
		}


#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
		private static ImmutableHashSet<int> _doneTests = ImmutableHashSet<int>.Empty;
		private static int _testIdCounter = 0;

		public static string GetAllTests()
			=> SampleControl.Presentation.SampleChooserViewModel.Instance.GetAllSamplesNames();

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
#endif

		public static bool IsTestDone(string testId) => int.TryParse(testId, out var id) ? _doneTests.Contains(id) : false;
	}
}
