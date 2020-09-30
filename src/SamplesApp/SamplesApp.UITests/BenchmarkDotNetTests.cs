using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
	public partial class BenchmarkDotNetTests : SampleControlUITestBase
	{
		private const string PendingTestsText = "Pending...";
		private const string BenchmarkOutputPath = "UNO_UITEST_BENCHMARKS_PATH";
		private readonly TimeSpan TestRunTimeout = TimeSpan.FromMinutes(5);

		[Test]
		[AutoRetry(tryCount: 1)]
		[Timeout(2000000)] // Adjust this timeout based on average test run duration
		public async Task RunBenchmarks()
		{
			Run("Benchmarks.Shared.Controls.BenchmarkDotNetTestsPage");

			IAppQuery AllQuery(IAppQuery query)
				// .All() is not yet supported for wasm.
				=> AppInitializer.GetLocalPlatform() == Platform.Browser ? query : query.All();

			var runButton = new QueryEx(q => AllQuery(q).Marked("runButton"));
			var runStatus = new QueryEx(q => AllQuery(q).Marked("runStatus"));
			var runCount = new QueryEx(q => AllQuery(q).Marked("runCount"));
			var benchmarkControl = new QueryEx(q => AllQuery(q).Marked("benchmarkControl"));

			bool IsTestExecutionDone()
			{
				try
				{ 
					var text = runStatus.GetDependencyPropertyValue("Text")?.ToString();
					var r2 = text?.Equals("Finished", StringComparison.OrdinalIgnoreCase) ?? false;

					Console.WriteLine($"IsTestExecutionDone: {text} {r2}");

					return r2;
				}
				catch
				{
					Console.WriteLine("Skip IsTestExecutionDone");

					// Skip exceptions as they may be timeouts
					return false;
				}
			}

			_app.WaitForElement(runButton);

			TakeScreenshot("Begin", ignoreInSnapshotCompare: true);

			_app.FastTap(runButton);

			var lastChange = DateTimeOffset.Now;
			var lastValue = "";

			while (DateTimeOffset.Now - lastChange < TestRunTimeout)
			{
				try
				{ 
					if (IsTestExecutionDone())
					{
						break;
					}

					var newValue = runCount.GetDependencyPropertyValue("Text")?.ToString();

					if (lastValue != newValue)
					{
						Console.WriteLine($"Loop: Test changed now:{DateTimeOffset.Now} lastChange: {lastChange}");

						lastChange = DateTimeOffset.Now;
						TakeScreenshot($"Run {newValue}", ignoreInSnapshotCompare: true);
					}
				}
				catch(Exception e)
				{
					// Skip exceptions as they may be timeouts
				}

				await Task.Delay(TimeSpan.FromSeconds(.5));

				Console.WriteLine($"Loop: now:{DateTimeOffset.Now} lastChange: {lastChange}");
			}

			if (!IsTestExecutionDone())
			{
				Assert.Fail("A test run timed out");
			}

			var finalFile = ArchiveResults(benchmarkControl);

			TestContext.AddTestAttachment(finalFile, "benchmark-results.zip");

			TakeScreenshot("Runtime Tests Results", ignoreInSnapshotCompare: true);
		}

		private static string ArchiveResults(QueryEx benchmarkControl)
		{
			var base64 = benchmarkControl.GetDependencyPropertyValue<string>("ResultsAsBase64");

			var file = Path.GetTempFileName();
			File.WriteAllBytes(file, Convert.FromBase64String(base64));

			if (Environment.GetEnvironmentVariable(BenchmarkOutputPath) is { } path)
			{

				ZipFile.ExtractToDirectory(file, path);
			}
			else
			{
				Console.WriteLine($"The environment variable {BenchmarkOutputPath} is not defined, skipping file system extraction");
			}

			var finalFile = Path.Combine(Path.GetDirectoryName(file), $"benchmark-results.zip");
			if (File.Exists(finalFile))
			{
				File.Delete(finalFile);
			}

			File.Move(file, finalFile);

			return finalFile;
		}
	}
}
