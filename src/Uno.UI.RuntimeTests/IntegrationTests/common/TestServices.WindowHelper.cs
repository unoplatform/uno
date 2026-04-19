
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Tests.Enterprise;
using MUXControlsTestApp.Utilities;
using UIElement = Microsoft.UI.Xaml.UIElement;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Linq;
using ToolTip = Microsoft.UI.Xaml.Controls.ToolTip;

using Windows.Foundation;

#if HAS_UNO
using DirectUI;
using Uno.UI.Xaml.Input;
#endif

#if WINAPPSDK
using Uno.UI.Extensions;
#elif __APPLE_UIKIT__
using UIKit;
#else
using Uno.UI;
#endif

namespace Private.Infrastructure
{
	public partial class TestServices
	{
		public static class WindowHelper
		{
			private static UIElement _originalWindowContent;
			private static Window _currentTestWindow;

			public static XamlRoot XamlRoot { get; set; }

			public static bool IsXamlIsland { get; set; }

			public static Microsoft.UI.Xaml.Window CurrentTestWindow
			{
				get => _currentTestWindow;
				set
				{
					_currentTestWindow = value;

#if !HAS_UNO
					// Inject the current test window in the input injection services to avoid a
					// dependency on TestServices in the Uno.UI.Toolkit project
					Uno.UI.Toolkit.DevTools.Input.Finger.TestServices_WindowHelper_CurrentTestWindow = value;
					Uno.UI.Toolkit.DevTools.Input.Mouse.TestServices_WindowHelper_CurrentTestWindow = value;
#endif
				}
			}

			public static bool UseActualWindowRoot { get; set; }

			public static UIElement WindowContent
			{
				get => UseActualWindowRoot
					? (IsXamlIsland ? GetXamlIslandRootContentControl().Content as UIElement : CurrentTestWindow.Content)
					: EmbeddedTestRoot.getContent?.Invoke();
				internal set
				{
					if (UseActualWindowRoot)
					{
						if (IsXamlIsland)
						{
							GetXamlIslandRootContentControl().Content = value;
						}
						else
						{
							CurrentTestWindow.Content = value;
						}
					}
					else if (EmbeddedTestRoot.setContent is { } setter)
					{
						setter(value);
					}
					else
					{
						Console.WriteLine("Failed to get test content control");
					}
				}
			}

			private static ContentControl GetXamlIslandRootContentControl()
			{
				var islandContentRoot = EmbeddedTestRoot.control.XamlRoot.Content;
				if (islandContentRoot is not ContentControl contentControl)
				{
					contentControl = VisualTreeUtils.FindVisualChildByType<ContentControl>(islandContentRoot);
				}

				return contentControl;
			}

			public static void SaveOriginalWindowContent()
			{
				if (IsXamlIsland)
				{
					_originalWindowContent = GetXamlIslandRootContentControl().Content as UIElement;
				}
				else
				{
					_originalWindowContent = CurrentTestWindow.Content;
				}
			}

			public static void RestoreOriginalWindowContent()
			{
				if (_originalWindowContent != null)
				{
					if (IsXamlIsland)
					{
						GetXamlIslandRootContentControl().Content = _originalWindowContent;
					}
					else
					{
						CurrentTestWindow.Content = _originalWindowContent;
					}
					_originalWindowContent = null;
				}
			}

			public static (UIElement control, Func<UIElement> getContent, Action<UIElement> setContent) EmbeddedTestRoot { get; set; }

			public static UIElement RootElement => UseActualWindowRoot ?
				XamlRoot.Content : EmbeddedTestRoot.control;

			// Dispatcher is a separate property, as accessing CurrentTestWindow.COntent when
			// not on the UI thread will throw an exception in WinUI.
			public static UnitTestDispatcherCompat RootElementDispatcher => UseActualWindowRoot
				? (CurrentTestWindow is { } ? UnitTestDispatcherCompat.From(CurrentTestWindow) : UnitTestDispatcherCompat.Instance)
				: UnitTestDispatcherCompat.From(EmbeddedTestRoot.control);

			public static Rect WindowBounds => CurrentTestWindow?.Bounds ?? default;

			internal static Page SetupSimulatedAppPage()
			{
				var spFrame = new Frame();

				var spRootFrameAsCC = spFrame as ContentControl;

				var spRootFrameAsUI = spRootFrameAsCC as UIElement;
				WindowContent = spRootFrameAsUI;

				spFrame.Navigate(typeof(Page));
				return spRootFrameAsCC.Content as Page;
			}

			internal static async Task WaitForIdle()
			{
#if (HAS_UNO && __SKIA__) || WINAPPSDK
				// Mirrors WinUI's IdleSynchronizer.SynchronouslyTickUIThread(1)
				// (D:\Mux\Mux\Samples\AppTestAutomationHelpers\IdleSynchronizer.cpp:364):
				// subscribe to CompositionTarget.Rendering, wait for the callback to fire.
				// That proves a full FrameTick / NWDrawTree (layout, Loaded events, Rendering
				// callbacks, render) has run end-to-end.
				//
				// On Uno Skia: required because layout is coupled into FrameTick, which can
				// be deferred behind the render throttle (_waitingForPresent). A pure
				// dispatcher-idle wait would miss this and return before layout had run.
				//
				// On WinAppSDK: the same Rendering-event idiom is what WinUI's own
				// IdleSynchronizer uses, so we adopt it for consistency.
				//
				// The dispatcher-idle path below is intentionally skipped here — once the
				// Rendering callback fires, the test continuation is posted to the dispatcher
				// and naturally serialises after FrameTick completes (UpdateLayout + Render
				// in the post-Rendering steps will have run by the time the test reads
				// state). Adding RunIdleAsync would be redundant.
				await ForceFrameTickAsync();
#else
				await RootElementDispatcher.RunIdleAsync(_ => { /* Empty to wait for the idle queue to be reached */ });
				await RootElementDispatcher.RunIdleAsync(_ => { /* Empty to wait for the idle queue to be reached */ });
#endif
			}

#if (HAS_UNO && __SKIA__) || WINAPPSDK
			private static async Task ForceFrameTickAsync()
			{
				var tcs = new TaskCompletionSource<object>();

				// Subscription must run on the UI thread — Rendering.add asserts thread access
				// and (on Uno) arms ScheduleFrameTick by calling RequestNewFrame on each
				// known CompositionTarget. That guarantees a FrameTick will fire even if
				// nothing else has requested one.
				await RootElementDispatcher.RunAsync(() =>
				{
					EventHandler<object> handler = null;
					handler = (_, _) =>
					{
						Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= handler;
						tcs.TrySetResult(null);
					};
					Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += handler;
				});

				// 5s bound — a FrameTick at 60Hz takes ~16ms even when throttled, so 5s is
				// generous. Tests with no content root won't tick at all; we don't fail loudly,
				// the test will surface its own assertion failure on whatever it's checking.
				var timeout = Task.Delay(TimeSpan.FromSeconds(5));
				if (await Task.WhenAny(tcs.Task, timeout) == timeout)
				{
					global::System.Diagnostics.Debug.WriteLine(
						"WaitForIdle: ForceFrameTickAsync timed out after 5s. " +
						"FrameTick did not fire — likely no active CompositionTarget.");
				}
			}
#endif

			/// <summary>
			/// Waits for <paramref name="element"/> to be loaded and measured in the visual tree.
			/// </summary>
			/// <remarks>
			/// On UWP, <see cref="WaitForIdle"/> may not always wait long enough for the control to be properly measured.
			///
			/// This method assumes that the control will have a non-zero size once loaded, so it's not appropriate for elements that are
			/// collapsed, empty, etc.
			/// </remarks>
			internal static async Task WaitForLoaded(FrameworkElement element, Func<FrameworkElement, bool> isLoaded = null, int timeoutMS = 1000)
			{
				async Task Do()
				{
					bool IsLoaded()
					{
						if (element.ActualHeight == 0 || element.ActualWidth == 0)
						{
							return false;
						}

						if (element is Control control && control.FindFirstChild<FrameworkElement>(includeCurrent: false) == null)
						{
							return false;
						}

						if (element is ListView listView && listView.Items.Count > 0 && listView.ContainerFromIndex(0) == null)
						{
							// If it's a ListView, wait for items to be populated
							return false;
						}

						return true;
					}

					await WaitFor(
						isLoaded is { } ? () => isLoaded(element) : IsLoaded,
						message: $"Timeout waiting on {element} to be loaded",
						timeoutMS: timeoutMS);
				}
#if __WASM__   // Adjust for re-layout failures in When_Inline_Items_SelectedIndex, When_Observable_ItemsSource_And_Added, When_Presenter_Doesnt_Take_Up_All_Space
				await Do();
#else
				var dispatcher = UnitTestDispatcherCompat.From(element);

				if (dispatcher.HasThreadAccess)
				{
					await Do();
				}
				else
				{
					TaskCompletionSource<bool> cts = new();

					_ = dispatcher.RunAsync(() =>
					{
						try
						{

							cts.TrySetResult(true);
						}
						catch (Exception e)
						{
							cts.TrySetException(e);
						}
					});

					await cts.Task;
				}
#endif
			}

			internal static async Task WaitForRelayouted(FrameworkElement frameworkElement)
			{
				var isRelayouted = false;

				void OnLayoutUpdated(object _, object __)
				{
					frameworkElement.LayoutUpdated -= OnLayoutUpdated;
					isRelayouted = true;
				}

				if (!frameworkElement.DispatcherQueue.HasThreadAccess)
				{
					await RunOnUIThread(() => frameworkElement.LayoutUpdated += OnLayoutUpdated);
				}
				else
				{
					frameworkElement.LayoutUpdated += OnLayoutUpdated;
				}

				await WaitFor(() => isRelayouted, message: $"{frameworkElement} re-layouted");
			}

			internal static async Task WaitForEqual(double expected, Func<double> actualFunc, double tolerance = 1.0, int timeoutMS = 1000)
			{
				if (actualFunc is null)
				{
					throw new ArgumentNullException(nameof(actualFunc));
				}

				var actual = actualFunc();
				if (ApproxEquals(actual))
				{
					return;
				}

				var stopwatch = Stopwatch.StartNew();
				while (stopwatch.ElapsedMilliseconds < timeoutMS)
				{
					await WaitForIdle();
					actual = actualFunc();
					if (ApproxEquals(actual))
					{
						return;
					}
				}

				throw new AssertFailedException($"Timed out waiting for equality condition to be met. Expected {expected} but last received value was {actual}.");

				bool ApproxEquals(double actualValue) => Math.Abs(expected - actualValue) < tolerance;
			}

			internal static async Task WaitForResultEqual<T>(T expected, Func<T> actualFunc, int timeoutMS = 1000)
			{
				if (actualFunc is null)
				{
					throw new ArgumentNullException(nameof(actualFunc));
				}

				var actual = actualFunc();
				if (Equals(expected, actual))
				{
					return;
				}

				var stopwatch = Stopwatch.StartNew();
				while (stopwatch.ElapsedMilliseconds < timeoutMS)
				{
					await WaitForIdle();
					actual = actualFunc();
					if (Equals(expected, actual))
					{
						return;
					}
				}

				throw new AssertFailedException($"Timed out waiting for equality condition to be met. Expected {expected} but last received value was {actual}.");
			}

			/// <summary>
			/// Wait until a specified <paramref name="condition"/> is met. 
			/// </summary>
			/// <param name="timeoutMS">The maximum time to wait before failing the test, in milliseconds.</param>
			internal static async Task WaitFor(Func<bool> condition, int timeoutMS = 1000, string message = null, [CallerMemberName] string callerMemberName = null, [CallerLineNumber] int lineNumber = 0)
			{
				if (condition())
				{
					return;
				}

				var stopwatch = Stopwatch.StartNew();
				while (stopwatch.ElapsedMilliseconds < timeoutMS)
				{
					await WaitForIdle();
					if (condition())
					{
						return;
					}
				}

				message ??= $"{callerMemberName}():{lineNumber}";

				throw new AssertFailedException("Timed out waiting for condition to be met. " + message);
			}

			internal static async Task WaitFor<T>(
				Func<T> condition,
				T expected,
				Func<T, string> messageBuilder = null,
				Func<T, T, bool> comparer = null,
				int timeoutMS = 1000,
				[CallerMemberName] string callerMemberName = null,
				[CallerLineNumber] int lineNumber = 0)
			{
				comparer ??= (v1, v2) => Equals(v1, v2);

				T value = condition();
				if (comparer(value, expected))
				{
					return;
				}

				var stopwatch = Stopwatch.StartNew();
				while (stopwatch.ElapsedMilliseconds < timeoutMS)
				{
					await WaitForIdle();
					value = condition();
					if (comparer(value, expected))
					{
						return;
					}
				}

				var customMsg = messageBuilder != null ? messageBuilder(value) : $"Got {value}, expected {expected}";
				var message = $"{callerMemberName}():{lineNumber} {customMsg}";

				throw new AssertFailedException("Timed out waiting for condition to be met. " + message);
			}

			internal static async Task<T> WaitForNonNull<T>(
				Func<T> getT,
				int timeoutMS = 1000,
				string message = null,
				[CallerMemberName] string callerMemberName = null,
				[CallerLineNumber] int lineNumber = 0)
				where T : class
			{
				if (getT is null)
				{
					throw new ArgumentNullException(nameof(getT));
				}

				if (getT() is { } t)
				{
					return t;
				}

				var stopwatch = Stopwatch.StartNew();
				while (stopwatch.ElapsedMilliseconds < timeoutMS)
				{
					await WaitForIdle();


					if (getT() is { } t2)
					{
						return t2;
					}
				}

				message ??= $"{callerMemberName}():{lineNumber} Never received non-null value";

				throw new AssertFailedException("Timed out waiting for condition to be met. " + message);
			}

#if DEBUG
			/// <summary>
			/// This will wait. Forever. Useful when debugging a runtime test if you wish to visually inspect or interact with a view added
			/// by the test. (To break out of the loop, just set 'shouldWait = false' via the Immediate Window.)
			/// </summary>
			internal static async Task WaitForever()
			{
				var shouldWait = true;
				while (shouldWait)
				{
					await Task.Delay(1000);
				}
			}
#endif

			internal static void ShutdownXaml() { }
			internal static void VerifyTestCleanup() { }

			internal static void SetWindowSizeOverride(object p) { }

			private static readonly TimeSpan FiveMinutes = TimeSpan.FromMinutes(5);

			public static async Task SynchronouslyTickUIThread(int ticks)
			{
				var tickCompleteEvent = new Event();

				for (var i = 0; i < ticks; i++)
				{
					await RunOnUIThread(() =>
					{
						CompositionTarget.Rendering += OnCompositionTargetOnRendering;
					});

					void OnCompositionTargetOnRendering(object sender, object o)
					{
						CompositionTarget.Rendering -= OnCompositionTargetOnRendering;
						tickCompleteEvent.Set();
					}

					await tickCompleteEvent.WaitFor(FiveMinutes);
				}
			}

			internal static void ResetWindowContentAndWaitForIdle()
			{

			}

			internal static void CloseAllSecondaryWindows()
			{
#if HAS_UNO_WINUI && !WINAPPSDK
				var windows = Uno.UI.ApplicationHelper.Windows.ToArray();
				foreach (var window in windows)
				{
					if (window != TestServices.WindowHelper.XamlRoot.HostWindow)
					{
						window.Close();
					}
				}
#endif
			}

			internal static async Task WaitForOpened(BitmapImage source, int timeoutMS = 10000)
			{
				var tcs = new TaskCompletionSource<bool>();

				source.ImageOpened += (s, e) =>
				{
					tcs.TrySetResult(true);
				};

				source.ImageFailed += (s, e) =>
				{
					tcs.TrySetException(new Exception(e.ErrorMessage));
				};

#if HAS_UNO
				if (source.IsOpened)
				{
					tcs.TrySetResult(true);
				}
#endif

				// Bound the wait so a stuck image-load (e.g. dispatcher starvation, thread-pool
				// exhaustion, BitmapImage chain not reaching RaiseImageOpened/RaiseImageFailed)
				// surfaces as an actionable test failure instead of hanging CI for the job timeout.
				var timeout = Task.Delay(timeoutMS);
				if (await Task.WhenAny(tcs.Task, timeout) == timeout)
				{
					throw new TimeoutException(
						$"WaitForOpened(BitmapImage) timed out after {timeoutMS}ms. " +
						$"UriSource: {source.UriSource}. " +
						"Neither ImageOpened nor ImageFailed fired and IsOpened stayed false.");
				}

				await tcs.Task; // surface any ImageFailed exception
			}

#if HAS_UNO
			internal async static Task SetLastInputMethod(InputDeviceType lastInputType, XamlRoot xamlRoot)
			{
				// Uno specific: Implementation is a bit different in WinUI.
				await RunOnUIThread(() =>
				{
					if (TestServices.WindowHelper.XamlRoot?.VisualTree?.ContentRoot?.InputManager is { } inputManager)
					{
						inputManager.LastInputDeviceType = lastInputType;
					}
				});
			}

			internal async static Task<ToolTip> TestGetActualToolTip(UIElement element)
			{
				ToolTip toolTip = null;
				await RunOnUIThread(() =>
				{
					toolTip = DXamlTestHooks.TestGetActualToolTip(element);
				});
				return toolTip;
			}

			public static void SetTestScaling(float scalingOverride)
			{
				WindowHelper.XamlRoot.VisualTree.RootScale.SetTestOverride(scalingOverride);
			}

			public static void UnsetTestScaling()
			{
				WindowHelper.XamlRoot.VisualTree.RootScale.SetTestOverride(0.0f);
			}

			internal static async Task WaitForOpened(ImageBrush source, int timeoutMS = 10000)
			{
				var tcs = new TaskCompletionSource<bool>();

				source.ImageOpened += (s, e) =>
				{
					tcs.TrySetResult(true);
				};

				source.ImageFailed += (s, e) =>
				{
					tcs.TrySetException(new Exception(e.ErrorMessage));
				};

				// Bound the wait so a stuck image-load surfaces as an actionable test failure
				// instead of hanging CI for the job timeout. See BitmapImage overload above.
				var timeout = Task.Delay(timeoutMS);
				if (await Task.WhenAny(tcs.Task, timeout) == timeout)
				{
					throw new TimeoutException(
						$"WaitForOpened(ImageBrush) timed out after {timeoutMS}ms. " +
						"Neither ImageOpened nor ImageFailed fired.");
				}

				await tcs.Task; // surface any ImageFailed exception
			}
#endif
		}
	}
}
