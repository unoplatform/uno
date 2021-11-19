using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
		private readonly TimeSpan TestRunTimeout = TimeSpan.FromMinutes(2);
		private const string TestResultsOutputFilePath = "UNO_UITEST_RUNTIMETESTS_RESULTS_FILE_PATH";

		[Test]
		[AutoRetry(tryCount: 1)]
		[Timeout(7200000)] // Adjust this timeout based on average test run duration
		public async Task RunRuntimeTests()
		{
			Run("SamplesApp.Samples.UnitTests.UnitTestsPage");

			IAppQuery AllQuery(IAppQuery query)
				// .All() is not yet supported for wasm.
				=> AppInitializer.GetLocalPlatform() == Platform.Browser ? query : query.All();

			var runButton = new QueryEx(q => AllQuery(q).Marked("runButton"));
			var failedTestsCount = new QueryEx(q => AllQuery(q).Marked("failedTestCount"));
			var failedTests = new QueryEx(q => AllQuery(q).Marked("failedTests"));
			var failedTestsDetails = new QueryEx(q => AllQuery(q).Marked("failedTestDetails"));
			var runningState = new QueryEx(q => AllQuery(q).Marked("runningState"));
			var runTestCount = new QueryEx(q => AllQuery(q).Marked("runTestCount"));
			var unitTestsControl = new QueryEx(q => AllQuery(q).Marked("UnitTestsRootControl"));

			async Task<bool> IsTestExecutionDone()
			{
				return await GetWithRetry("IsTestExecutionDone", () => runningState.GetDependencyPropertyValue("Text")?.ToString().Equals("Finished", StringComparison.OrdinalIgnoreCase) ?? false);
			}

			async Task<T> GetWithRetry<T>(string logName, Func<T> getter, int timeoutSeconds = 10)
			{
				var sw = Stopwatch.StartNew();
				Exception lastException = null;
				do
				{
					try
					{
						return getter();
					}
					catch (Exception e)
					{
						lastException = e;
						Console.WriteLine($"{logName} failed with {e.Message}");
					}

					await Task.Delay(TimeSpan.FromSeconds(.5));

					Console.WriteLine($"{logName} retrying");
				}
				while (sw.Elapsed < TimeSpan.FromSeconds(timeoutSeconds));

				throw lastException;
			}

			_app.WaitForElement(runButton);

			_app.FastTap(runButton);

			var lastChange = DateTimeOffset.Now;
			var lastValue = "";

			while(DateTimeOffset.Now - lastChange < TestRunTimeout)
			{
				var newValue = await GetWithRetry("GetRunTestCount", () => runTestCount.GetDependencyPropertyValue("Text")?.ToString());

				if (lastValue != newValue)
				{
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

			TestContext.AddTestAttachment(ArchiveResults(unitTestsControl), "runtimetests-results.zip");

			var count = GetValue(nameof(failedTestsCount), failedTestsCount);
			if (count != "0")
			{
				var tests = GetValue(nameof(failedTests), failedTests)
					.Split(new char[] { '§' }, StringSplitOptions.RemoveEmptyEntries)
					.Select((x, i) => $"\t{i + 1}. {x}\n")
					.ToArray();
				var details = GetValue(nameof(failedTestsDetails), failedTestsDetails);

				Assert.Fail($"{tests.Length} unit test(s) failed.\n\tFailing Tests:\n{string.Join("", tests)}\n\n---\n\tDetails:\n{details}");
			}

			TakeScreenshot("Runtime Tests Results",	ignoreInSnapshotCompare: true);
		}

		private static string ArchiveResults(QueryEx unitTestsControl)
		{
			var document = GetValue(nameof(unitTestsControl), unitTestsControl, "NUnitTestResultsDocument");

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
