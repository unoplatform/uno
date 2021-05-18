using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Runtime.CompilerServices;
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
	public class TestServices
	{
		public class WindowHelper
		{
			public static object WindowContent
			{
				get => RootControl.Content;
				internal set
				{
					if (RootControl is ContentControl content)
					{
						content.Content = value;
					}
					else
					{
						Console.WriteLine("Failed to get test content control");
					}
				}
			}

			public static ContentControl RootControl { get; set; }

			internal static async Task WaitForIdle()
			{
				await RootControl.Dispatcher.RunIdleAsync(_ => { /* Empty to wait for the idle queue to be reached */ });
				await RootControl.Dispatcher.RunIdleAsync(_ => { /* Empty to wait for the idle queue to be reached */ });
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
		}

		public class Utilities
		{
			internal static void VerifyMockDCompOutput(MockDComp.SurfaceComparison comparison, string step) { }
			internal static void VerifyMockDCompOutput(MockDComp.SurfaceComparison comparison) { }
		}

		internal static async Task RunOnUIThread(Action action)
		{
#if __WASM__
			action();
#else
			await WindowHelper.RootControl.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => action());
#endif
		}

		internal static void EnsureInitialized() { }

		public static void VERIFY_IS_LESS_THAN(double actual, double expected)
		{
			Assert.IsTrue(actual < expected, $"{actual} is not less than {expected}");
		}

		public static void VERIFY_THROWS_WINRT(Action action, Type exceptionType)
		{
			try
			{
				action();
				Assert.Fail($"Exception of type {exceptionType} is expected");
			}
			catch (Exception e)
			{
				if (e.GetType() != exceptionType)
				{
					Assert.Fail($"Exception of type {exceptionType} is expected (was {e.GetType()}");
				}
			}
		}

		internal static void VERIFY_ARE_EQUAL<T>(T actual, T expected)
		{
			Assert.AreEqual(expected: expected, actual: actual);
		}

		internal static void VERIFY_ARE_VERY_CLOSE(double actual, double expected, double tolerance = 0.1d)
		{
			var difference = Math.Abs(actual - expected);
			Assert.IsTrue(difference <= tolerance, $"Expected <{expected}>, actual <{actual}> (tolerance = {tolerance})");
		}
	}

	public class MockDComp
	{
		public enum SurfaceComparison
		{
			NoComparison
		}
	}
}
