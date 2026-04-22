
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
				// Mirrors WinUI's IdleSynchronizer::WaitInternal
				// (D:\Mux\Mux\Samples\AppTestAutomationHelpers\IdleSynchronizer.cpp:115-174)
				// by running both load-bearing steps per iteration:
				//
				//   1. SynchronouslyTickUIThread(1) (IdleSynchronizer.cpp:364) — subscribe to
				//      CompositionTarget.Rendering inside a dispatcher work item, wait for
				//      the callback. On Uno Skia, Rendering.add arms ScheduleFrameTick via
				//      RequestNewFrame, so a FrameTick (UpdateLayout + Loaded events +
				//      Rendering callbacks + Draw) is guaranteed to run end-to-end.
				//
				//   2. WaitForIdleDispatcher (IdleSynchronizer.cpp:212) — a non-repeating
				//      DispatcherQueueTimer with zero Interval fires once the normal queue
				//      has drained. Tick alone leaves normal-priority work items (Loaded
				//      handlers, COM/clipboard continuations, awaited async completions)
				//      pending when the Rendering callback returns.
				//
				// WinUI itself uses an infinite wait per tick (IdleSynchronizer.cpp:385 →
				// Event.h:52). We keep a 30 s safety net in ForceFrameTickAsync that throws
				// rather than silently proceeding — hangs in CI are worse than a visible
				// failure, but silent "idle" skew on every subsequent assertion is worse
				// still.
				await ForceFrameTickAsync();
				await WaitForIdleDispatcherAsync();
#else
				await RootElementDispatcher.RunIdleAsync(_ => { /* Empty to wait for the idle queue to be reached */ });
				await RootElementDispatcher.RunIdleAsync(_ => { /* Empty to wait for the idle queue to be reached */ });
#endif
			}

#if (HAS_UNO && __SKIA__) || WINAPPSDK
			private static async Task ForceFrameTickAsync()
			{
				// RunContinuationsAsynchronously: the Rendering handler runs on the UI thread,
				// so completing the TCS inline would re-enter UI work from inside the rendering
				// callback. Hop continuations off the rendering callback before they run.
				var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
				EventHandler<object> handler = null;

				// Subscription must run on the UI thread — Rendering.add asserts thread access
				// and (on Uno) arms ScheduleFrameTick by calling RequestNewFrame on each
				// known CompositionTarget. That guarantees a FrameTick will fire even if
				// nothing else has requested one.
				await RootElementDispatcher.RunAsync(() =>
				{
					handler = (_, _) =>
					{
						Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= handler;
						tcs.TrySetResult(null);
					};
					Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += handler;
				});

				// 30s bound — purely a safety net for a stalled composition/render pipeline.
				// WinUI's SynchronouslyTickUIThread waits infinitely (IdleSynchronizer.cpp:385);
				// we prefer a loud failure over a hang in CI, but silently proceeding on a
				// missed tick would skew every subsequent assertion.
				var timeout = Task.Delay(TimeSpan.FromSeconds(30));
				if (await Task.WhenAny(tcs.Task, timeout) == timeout)
				{
					await RootElementDispatcher.RunAsync(() =>
					{
						Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= handler;
					});
					throw new TimeoutException(
						"WaitForIdle: CompositionTarget.Rendering did not fire within 30s. " +
						"No active CompositionTarget, or the composition/render pipeline is stalled.");
				}
			}

			private static async Task WaitForIdleDispatcherAsync()
			{
				// Mirrors WinUI's IdleSynchronizer::WaitForIdleDispatcher
				// (D:\Mux\Mux\Samples\AppTestAutomationHelpers\IdleSynchronizer.cpp:212):
				// a non-repeating DispatcherQueueTimer with zero Interval ticks once the
				// normal-priority queue has drained.
				var dispatcherQueue = CurrentTestWindow?.DispatcherQueue
					?? Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
				if (dispatcherQueue is null)
				{
					return;
				}

				var tcs = new TaskCompletionSource<object>();
				Microsoft.UI.Dispatching.DispatcherQueueTimer timer = null;
				global::Windows.Foundation.TypedEventHandler<Microsoft.UI.Dispatching.DispatcherQueueTimer, object> handler = null;

				await RootElementDispatcher.RunAsync(() =>
				{
					timer = dispatcherQueue.CreateTimer();
					timer.Interval = TimeSpan.Zero;
					timer.IsRepeating = false;
					handler = (t, _) =>
					{
						t.Tick -= handler;
						tcs.TrySetResult(null);
					};
					timer.Tick += handler;
					timer.Start();
				});

				// Safety net only — a zero-interval DispatcherQueueTimer should tick within
				// one dispatcher cycle. 10s means the dispatcher is genuinely stalled; fail
				// loudly rather than proceeding with work items still pending.
				var timeout = Task.Delay(TimeSpan.FromSeconds(10));
				if (await Task.WhenAny(tcs.Task, timeout) == timeout)
				{
					await RootElementDispatcher.RunAsync(() =>
					{
						if (timer is not null)
						{
							timer.Stop();
							if (handler is not null)
							{
								timer.Tick -= handler;
							}
						}
					});
					throw new TimeoutException(
						"WaitForIdle: DispatcherQueue did not reach idle within 10s. " +
						"A zero-interval DispatcherQueueTimer did not tick — the UI thread is stalled.");
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
				// RunContinuationsAsynchronously: ImageOpened/ImageFailed fire on the UI thread;
				// without this an inline continuation would run UI work from the event raiser.
				var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

				RoutedEventHandler openedHandler = (_, _) => tcs.TrySetResult(true);
				ExceptionRoutedEventHandler failedHandler = (_, e) => tcs.TrySetException(new Exception(e.ErrorMessage));
				source.ImageOpened += openedHandler;
				source.ImageFailed += failedHandler;

				try
				{
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
				finally
				{
					source.ImageOpened -= openedHandler;
					source.ImageFailed -= failedHandler;
				}
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
				// See BitmapImage overload for the RunContinuationsAsynchronously rationale.
				var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

				RoutedEventHandler openedHandler = (_, _) => tcs.TrySetResult(true);
				ExceptionRoutedEventHandler failedHandler = (_, e) => tcs.TrySetException(new Exception(e.ErrorMessage));
				source.ImageOpened += openedHandler;
				source.ImageFailed += failedHandler;

				try
				{
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
				finally
				{
					source.ImageOpened -= openedHandler;
					source.ImageFailed -= failedHandler;
				}
			}
#endif
		}
	}
}
