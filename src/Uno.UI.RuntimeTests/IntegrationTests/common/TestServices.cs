using System;
using System.Globalization;
using System.Threading.Tasks;
using Windows.UI.Xaml.Tests.Enterprise;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Private.Infrastructure
{
	public static partial class TestServices
	{

		public class Utilities
		{
			internal static void VerifyMockDCompOutput(MockDComp.SurfaceComparison comparison, string step) { }
			internal static void VerifyMockDCompOutput(MockDComp.SurfaceComparison comparison) { }

			internal static void VerifyUIElementTree()
			{
			}

			public static void SetTimeZone(string tzid)
			{
				throw new NotImplementedException();
			}

			internal static void EnableChangingTimeZone(bool isEnabled)
			{

			}
		}

		internal static async Task RunOnUIThread(Action action)
		{
#if __WASM__
			action();
#else
			await WindowHelper.RootElement.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => action());
#endif
		}

		internal static void EnsureInitialized() { }

		public static void VERIFY_IS_NOT_NULL(object value)
		{
			Assert.IsNotNull(value);
		}

		public static void VERIFY_IS_NULL(object value)
		{
			Assert.IsNull(value);
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

		internal static void VERIFY_ARE_EQUAL<T>(T actual, T expected)
		{
			//Assert.AreEqual(expected: expected, actual: actual);
			actual­.Should().Be(expected);
		}

		internal static void VERIFY_ARE_VERY_CLOSE(double actual, double expected, double tolerance = 0.1d)
		{
			var difference = Math.Abs(actual - expected);
			Assert.IsTrue(difference <= tolerance, $"Expected <{expected}>, actual <{actual}> (tolerance = {tolerance})");
		}

		internal static void LOG_OUTPUT(string log, params object[] arguments)
		{
			if (arguments != null && arguments.Length != 0)
			{
				var offset = 0;
				for (var i = 0;; i++)
				{
					offset = log.IndexOf('%', offset + 1);
					if (offset < 0 || offset + 1 >= log.Length)
					{
						break; // finished
					}

					string replacement = default;

					switch(log[offset + 1])
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

		internal static SafeEventRegistration<T, TDelegate> CreateSafeEventRegistration<T, TDelegate>(string eventName)
			where T : class
			where TDelegate : Delegate
		{
			return new SafeEventRegistration<T, TDelegate>(eventName);
		}
	}
}
