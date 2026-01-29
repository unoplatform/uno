using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Tests.Enterprise;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace Private.Infrastructure
{
	public static partial class TestServices
	{

		public class Utilities
		{
			internal static bool IsXBox { get; }
			internal static void VerifyMockDCompOutput(MockDComp.SurfaceComparison comparison, string step) { }
			internal static void VerifyMockDCompOutput(MockDComp.SurfaceComparison comparison) { }

			internal static void VerifyUIElementTree()
			{
			}

			internal static void InjectBackButtonPress(ref bool backButtonPressHandled)
			{
			}

			public static void SetTimeZone(string tzid)
			{
				throw new NotImplementedException();
			}

			internal static void EnableChangingTimeZone(bool isEnabled)
			{

			}

#if !WINAPPSDK
			internal static FrameworkElement GetPopupOverlayElement(Popup popup)
			{
				return null;
			}
#endif
		}

		internal static async Task RunOnUIThread(Action action)
		{
			await WindowHelper.RootElementDispatcher.RunAsync(() => action());
		}

		internal static async Task RunOnUIThread(Func<Task> asyncAction)
		{
			var tsc = new TaskCompletionSource<bool>();

			await WindowHelper.RootElementDispatcher.RunAsync(async () =>
			{
				try
				{
					await asyncAction();
					tsc.TrySetResult(true);
				}
				catch (Exception e)
				{
					tsc.TrySetException(e);
				}
			});

			await tsc.Task;
		}

		internal static bool HasDispatcherAccess
		{
			get
			{
				return WindowHelper.RootElementDispatcher.HasThreadAccess;
			}
		}

		internal static void EnsureInitialized() { }

		public static void VERIFY_IS_NOT_NULL(object value)
		{
			Assert.IsNotNull(value);
		}

		public static void VERIFY_IS_NOT_NULL(object value, string msg)
		{
			Assert.IsNotNull(value, msg);
		}

		public static void VERIFY_IS_NULL(object value)
		{
			Assert.IsNull(value);
		}

		public static void THROW_IF_NULL(object value)
		{
			Assert.IsNotNull(value);
		}

		public static void THROW_IF_NULL_WITH_MSG(object value, string msg)
		{
			Assert.IsNotNull(value, msg);
		}

		public static void VERIFY_IS_TRUE(bool value, string message = null)
		{
			Assert.IsTrue(value, message);
		}

		public static void VERIFY_IS_FALSE(bool value, string message = null)
		{
			Assert.IsFalse(value, message);
		}

		public static void VERIFY_IS_LESS_THAN(double actual, double expected)
		{
			Assert.IsLessThan(expected, actual, $"{actual} is not less than {expected}");
		}

		public static void VERIFY_IS_LESS_THAN_OR_EQUAL(double actual, double expected)
		{
			Assert.IsLessThanOrEqualTo(expected, actual, $"{actual} is not less than or equal to {expected}");
		}

		public static void VERIFY_IS_GREATER_THAN(double actual, double expected)
		{
			Assert.IsGreaterThan(expected, actual, $"{actual} is not greater than {expected}");
		}

		public static void VERIFY_IS_GREATER_THAN_OR_EQUAL(double actual, double expected)
		{
			Assert.IsGreaterThanOrEqualTo(expected, actual, $"{actual} is not greater than or equal to {expected}");
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

		public static void VERIFY_THROWS_WINRT<TExpectedException>(Action action, string text)
			where TExpectedException : Exception
		{
			var exceptionType = typeof(TExpectedException);
			try
			{
				action();
				Assert.Fail($"Exception of type {exceptionType} is expected: {text}");
			}
			catch (Exception e)
			{
				if (e.GetType() != exceptionType)
				{
					Assert.Fail($"Exception of type {exceptionType} is expected (was {e.GetType()}. {text}");
				}
			}
		}

		internal static void VERIFY_ARE_EQUAL<T>(T actual, T expected, string message = null)
		{
			//Assert.AreEqual(expected: expected, actual: actual);
			actual­.Should().Be(expected, message);
		}

		internal static void VERIFY_ARE_NOT_EQUAL<T>(T actual, T unexpected, string message = null)
		{
			actual­.Should().NotBe(unexpected, message);
		}

		internal static void VERIFY_ARE_VERY_CLOSE(double actual, double expected, double tolerance = 0.1d, string message = null)
		{
			var difference = Math.Abs(actual - expected);
			Assert.IsLessThanOrEqualTo(tolerance, difference, $"Expected <{expected}>, actual <{actual}> (tolerance = {tolerance}) {message}");
		}

		internal static void VERIFY_DATES_ARE_EQUAL(DateTimeOffset actual, DateTimeOffset expected, string message = null)
		{
			actual.Date.Should().Be(expected.Date, message);
		}

		internal static void VERIFY_DATES_ARE_EQUAL(long actualTicks, long expectedTicks, string message = null)
		{
			VERIFY_DATES_ARE_EQUAL(
				new DateTimeOffset(actualTicks, TimeSpan.Zero),
				new DateTimeOffset(expectedTicks, TimeSpan.Zero),
				message);

		}

		internal static void LOG_OUTPUT(string log, params object[] arguments)
		{
			if (arguments != null && arguments.Length != 0)
			{
				var offset = 0;
				for (var i = 0; ; i++)
				{
					offset = log.IndexOf('%', offset + 1);
					if (offset < 0 || offset + 1 >= log.Length)
					{
						break; // finished
					}

					string replacement = default;

					switch (log[offset + 1])
					{
						case 's':
						case 'd':
						case 'f':
						case 'i':
						case 'g':
							replacement = $"{{{i}}}";
							break;
						case 'x':
							replacement = $"{{{i}:x}}";
							break;
						case 'X':
							replacement = $"{{{i}:X}}";
							break;
					}

					if (replacement is { })
					{
						log = log.Substring(0, offset) + replacement + log.Substring(offset + 2);
					}
				}

				log = string.Format(CultureInfo.InvariantCulture, log, args: arguments);
			}

			Console.WriteLine(log);
		}

		internal static SafeEventRegistration<T, TDelegate> CreateSafeEventRegistration<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)] T, TDelegate>(string eventName)
			where T : class
			where TDelegate : Delegate
		{
			return new SafeEventRegistration<T, TDelegate>(eventName);
		}
	}
}
