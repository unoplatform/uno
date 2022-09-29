#nullable disable

using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace SamplesApp.UITests.TestFramework
{
	/// <summary>
	/// Specifies that a test method should be rerun on failure up to the specified 
	/// maximum number of times.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public partial class AutoRetryAttribute : NUnitAttribute, IRepeatTest
	{
		public const int AutoRetryDefaultCount = 2;

		private readonly int _tryCount;

		/// <summary>
		/// Construct a <see cref="RetryAttribute" />
		/// </summary>
		/// <param name="tryCount">The maximum number of times the test should be run if it fails</param>
		public AutoRetryAttribute(int tryCount = AutoRetryDefaultCount)
		{
			_tryCount = tryCount;
		}

		#region IRepeatTest Members

		/// <summary>
		/// Wrap a command and return the result.
		/// </summary>
		/// <param name="command">The command to be wrapped</param>
		/// <returns>The wrapped command</returns>
		public TestCommand Wrap(TestCommand command)
		{
			return new RetryCommand(command, _tryCount);
		}

		#endregion

		#region Nested RetryCommand Class

		/// <summary>
		/// The test command for the <see cref="RetryAttribute"/>
		/// </summary>
		public class RetryCommand : DelegatingTestCommand
		{
			private readonly int _tryCount;

			/// <summary>
			/// Initializes a new instance of the <see cref="RetryCommand"/> class.
			/// </summary>
			/// <param name="innerCommand">The inner command.</param>
			/// <param name="tryCount">The maximum number of repetitions</param>
			public RetryCommand(TestCommand innerCommand, int tryCount)
				: base(innerCommand)
			{
				_tryCount = tryCount;
			}

			/// <summary>
			/// Runs the test, saving a TestResult in the supplied TestExecutionContext.
			/// </summary>
			/// <param name="context">The context in which the test should run.</param>
			/// <returns>A TestResult</returns>
			public override TestResult Execute(TestExecutionContext context)
			{
				int count = _tryCount;

				while (count-- > 0)
				{
					try
					{
						TryResetTimeoutCommand(innerCommand);

						context.CurrentResult = innerCommand.Execute(context);
					}
					// Commands are supposed to catch exceptions, but some don't
					// and we want to look at restructuring the API in the future.
					catch (Exception ex)
					{
						if (context.CurrentResult == null)
						{
							context.CurrentResult = context.CurrentTest.MakeTestResult();
						}

						context.CurrentResult.RecordException(ex);
					}

					if (context.CurrentResult.ResultState != ResultState.Failure
						&& context.CurrentResult.ResultState != ResultState.Error
						&& context.CurrentResult.ResultState != ResultState.SetUpError
						&& context.CurrentResult.ResultState != ResultState.SetUpFailure)
					{
						break;
					}

					// Clear result for retry
					if (count > 0)
					{
						context.CurrentResult = context.CurrentTest.MakeTestResult();
						context.CurrentRepeatCount++; // increment Retry count for next iteration. will only happen if we are guaranteed another iteration

						Console.WriteLine($"Test {context.CurrentTest.FullName} failed, retrying ({count} left)");
					}
				}

				return context.CurrentResult;
			}

			private void TryResetTimeoutCommand(TestCommand innerCommand)
			{
				// Apply workaround for https://github.com/nunit/nunit/issues/3284
				// Resets the command timeout flag on every new test run

				if (innerCommand is TimeoutCommand tc)
				{
					if (tc.GetType().GetField(
						"_commandTimedOut",
						System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) is { } timedOut)
					{
						timedOut.SetValue(innerCommand, false);
					}
					else
					{
						throw new Exception("This version of NUnit is not supported");
					}
				}
			}
		}

		#endregion
	}
}
