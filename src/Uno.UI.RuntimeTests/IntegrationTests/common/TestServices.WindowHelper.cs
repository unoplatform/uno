using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Tests.Enterprise;
#if NETFX_CORE
using Uno.UI.Extensions;
#elif __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#else
using Uno.UI;
#endif

namespace Private.Infrastructure
{
	public partial class TestServices
	{
		public static class WindowHelper
		{
			private static UIElement _originalWindowContent = null;

			public static bool UseActualWindowRoot { get; set; }

			public static UIElement WindowContent
			{
				get
				{
					if (UseActualWindowRoot)
					{
						return Windows.UI.Xaml.Window.Current.Content;
					}
					return EmbeddedTestRootControl.Content as UIElement;
				}

				internal set
				{
					if (UseActualWindowRoot)
					{
						Windows.UI.Xaml.Window.Current.Content = value;
					}
					else if (EmbeddedTestRootControl is ContentControl content)
					{
						content.Content = value;
					}
					else
					{
						Console.WriteLine("Failed to get test content control");
					}
				}
			}

			public static void SaveOriginalWindowContent()
			{
				_originalWindowContent = Windows.UI.Xaml.Window.Current.Content;
			}

			public static void RestoreOriginalWindowContent()
			{
				if (_originalWindowContent != null)
				{
					Windows.UI.Xaml.Window.Current.Content = _originalWindowContent;
					_originalWindowContent = null;
				}
			}

			public static ContentControl EmbeddedTestRootControl { get; set; }

			public static UIElement RootElement => UseActualWindowRoot ?
					Windows.UI.Xaml.Window.Current.Content :
					EmbeddedTestRootControl;

			internal static async Task WaitForIdle()
			{
				await RootElement.Dispatcher.RunIdleAsync(_ => { /* Empty to wait for the idle queue to be reached */ });
				await RootElement.Dispatcher.RunIdleAsync(_ => { /* Empty to wait for the idle queue to be reached */ });
			}

			/// <summary>
			/// Waits for <paramref name="element"/> to be loaded and measured in the visual tree.
			/// </summary>
			/// <remarks>
			/// On UWP, <see cref="WaitForIdle"/> may not always wait long enough for the control to be properly measured.
			///
			/// This method assumes that the control will have a non-zero size once loaded, so it's not appropriate for elements that are
			/// collapsed, empty, etc.
			/// </remarks>
			internal static async Task WaitForLoaded(FrameworkElement element)
			{
				await WaitFor(IsLoaded, message: $"{element} loaded");
				bool IsLoaded()
				{
					if (element.ActualHeight == 0 || element.ActualWidth == 0)
					{
						return false;
					}

					if (element is Control control && control.FindFirstChild<UIElement>(includeCurrent: false) == null)
					{
						return false;
					}

					return true;
				}
			}

			internal static async Task WaitForRelayouted(FrameworkElement frameworkElement)
			{
				var isRelayouted = false;

				void OnLayoutUpdated(object _, object __)
				{
					frameworkElement.LayoutUpdated -= OnLayoutUpdated;
					isRelayouted = true;
				}

				frameworkElement.LayoutUpdated += OnLayoutUpdated;

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
		}
	}
}
