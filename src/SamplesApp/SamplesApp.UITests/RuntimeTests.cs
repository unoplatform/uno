using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;

namespace SamplesApp.UITests.Runtime
{
	[TestFixture]
	public partial class RuntimeTests : SampleControlUITestBase
	{
		private const string PendingTestsText = "Pending...";
		private readonly TimeSpan TestRunTimeout = TimeSpan.FromMinutes(5);
		private const string TestResultsOutputFilePath = "UNO_UITEST_RUNTIMETESTS_RESULTS_FILE_PATH";
		private const string TestResultsOutputTempFilePath = "UNO_UITEST_RUNTIMETESTS_RESULTS_TEMP_FILE_PATH";
		private const string TestGroupVariable = "UITEST_RUNTIME_TEST_GROUP";
		private const string TestGroupCountVariable = "UITEST_RUNTIME_TEST_GROUP_COUNT";

		[Test]
		[AutoRetry(tryCount: 2)]
		// [Timeout(3000000)] // Timeout is now moved to individual platforms runners configuration in CI
		public async Task RunRuntimeTests()
		{
			Run("SamplesApp.Samples.UnitTests.UnitTestsPage");

			IAppQuery AllQuery(IAppQuery query)
				// .All() is not yet supported for wasm.
				=> AppInitializer.GetLocalPlatform() == Platform.Browser ? query : query.All();

			var runButton = new QueryEx(q => AllQuery(q).Marked("runButton"));
			var failedTests = new QueryEx(q => AllQuery(q).Marked("failedTests"));
			var failedTestsDetails = new QueryEx(q => AllQuery(q).Marked("failedTestDetails"));
			var unitTestsControl = new QueryEx(q => AllQuery(q).Marked("UnitTestsRootControl"));

			async Task<bool> IsTestExecutionDone()
			{
				return await GetWithRetry("IsTestExecutionDone", () => unitTestsControl.GetDependencyPropertyValue("RunningStateForUITest")?.ToString().Equals("Finished", StringComparison.OrdinalIgnoreCase) ?? false);
			}

			_app.WaitForElement(runButton);

			if (Environment.GetEnvironmentVariable(TestResultsOutputFilePath) is { } path)
			{
				// Used to disable showing the test output visually
				unitTestsControl.SetDependencyPropertyValue("IsRunningOnCI", "true");

				// Used to perform test grouping on CI to reduce the impact of re-runs
				if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(TestGroupVariable)))
				{
					unitTestsControl.SetDependencyPropertyValue("CITestGroup", Environment.GetEnvironmentVariable(TestGroupVariable));
					unitTestsControl.SetDependencyPropertyValue("CITestGroupCount", Environment.GetEnvironmentVariable(TestGroupCountVariable));
				}
			}

			_app.FastTap(runButton);

			var lastChange = DateTimeOffset.Now;
			var lastValue = "";

			while (DateTimeOffset.Now - lastChange < TestRunTimeout)
			{
				var newValue = await GetWithRetry("GetRunTestCount", () => unitTestsControl.GetDependencyPropertyValue("RunTestCountForUITest")?.ToString());

				if (lastValue != newValue)
				{
					lastValue = newValue;
					lastChange = DateTimeOffset.Now;
				}

				await Task.Delay(TimeSpan.FromSeconds(.5));

				if (await IsTestExecutionDone())
				{
					break;
				}
			}

			if (!await IsTestExecutionDone())
			{
				Assert.Fail("A test run timed out");
			}

			TestContext.AddTestAttachment(await ArchiveResults(unitTestsControl), "runtimetests-results.zip");

			var count = GetValue(nameof(unitTestsControl), unitTestsControl, "FailedTestCountForUITest");
			if (count != "0")
			{
				var tests = GetValue(nameof(failedTests), failedTests)
					.Split(new char[] { '§' }, StringSplitOptions.RemoveEmptyEntries)
					.Select((x, i) => $"\t{i + 1}. {x}\n")
					.ToArray();
				var details = GetValue(nameof(failedTestsDetails), failedTestsDetails);

				Assert.Fail($"{tests.Length} unit test(s) failed (count={count}).\n\tFailing Tests:\n{string.Join("", tests)}\n\n---\n\tDetails:\n{details}");
			}

			TakeScreenshot("Runtime Tests Results", ignoreInSnapshotCompare: true);
		}

		async Task<T> GetWithRetry<T>(string logName, Func<T> getter, int timeoutSeconds = 120)
		{
			var sw = Stopwatch.StartNew();
			Exception lastException = null;
			do
			{
				try
				{
					var result = getter();

					if (sw.Elapsed > TimeSpan.FromSeconds(timeoutSeconds / 2))
					{
						Console.WriteLine($"{logName} succeeded after retries");
					}

					return result;
				}
				catch (Exception e)
				{
					lastException = e;
					Console.WriteLine($"{logName} failed with {e.Message}");
				}

				await Task.Delay(TimeSpan.FromSeconds(2));

				Console.WriteLine($"{logName} retrying");
			}
			while (sw.Elapsed < TimeSpan.FromSeconds(timeoutSeconds));

			throw lastException;
		}

		private static async Task<string> ArchiveResults(QueryEx unitTestsControl)
		{
			var document = await GetNUnitTestResultsDocument(unitTestsControl);

			var file = Path.GetTempFileName();
			File.WriteAllText(file, document, Encoding.Unicode);

			if (Environment.GetEnvironmentVariable(TestResultsOutputFilePath) is { } path)
			{
				File.Copy(file, path, true);
			}
			else
			{
				Console.WriteLine($"The environment variable {TestResultsOutputFilePath} is not defined, skipping file system extraction");
			}

			var finalFile = Path.Combine(Path.GetDirectoryName(file), "test-results.xml");

			if (File.Exists(finalFile))
			{
				File.Delete(finalFile);
			}

			File.Move(file, finalFile);

			return finalFile;
		}

		private static async Task<string> GetNUnitTestResultsDocument(QueryEx unitTestsControl)
		{
			int counter = 0;

			do
			{
				var document = GetValue(nameof(unitTestsControl), unitTestsControl, "NUnitTestResultsDocument");

				if (!string.IsNullOrEmpty(document))
				{
					return document;
				}

				// The results are built asynchronously, it may not be available right away.
				await Task.Delay(1000);

			} while (counter++ < 3);

			throw new InvalidOperationException($"Failed to get the test results document");
		}

		private static string GetValue(string elementName, QueryEx element, string dpName = "Text", [CallerLineNumber] int line = -1)
		{
			try
			{
				return element
					.GetDependencyPropertyValue(dpName)
					?.ToString();
			}
			catch (Exception e)
			{
				Assert.Fail($"Failed to get DP ${dpName} on {elementName} (@{line}), {e}", e);
				throw new InvalidOperationException($"Failed to get DP ${dpName} on {elementName} (@{line}), {e}");
			}
		}
	}
}
