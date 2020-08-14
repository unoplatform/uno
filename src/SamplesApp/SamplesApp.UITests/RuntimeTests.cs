using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;

namespace SamplesApp.UITests
{
	[TestFixture]
	public partial class RuntimeTests : SampleControlUITestBase
	{
		private const string PendingTestsText = "Pending...";
		private readonly TimeSpan TestRunTimeout = TimeSpan.FromMinutes(2);

		[Test]
		[AutoRetry]
		[Timeout(300000)] // Adjust this timeout based on average test run duration
		public async Task RunRuntimeTests()
		{
			Run("SamplesApp.Samples.UnitTests.UnitTestsPage");

			IAppQuery AllQuery(IAppQuery query)
				// .All() is not yet supported for wasm.
				=> AppInitializer.GetLocalPlatform() == Platform.Browser ? query : query.All();

			var runButton = new QueryEx(q => AllQuery(q).Marked("runButton"));
			var failedTestsCount = new QueryEx(q => AllQuery(q).Marked("failedTestCount"));
			var failedTests = new QueryEx(q => AllQuery(q).Marked("failedTests"));
			var runningState = new QueryEx(q => AllQuery(q).Marked("runningState"));
			var runTestCount = new QueryEx(q => AllQuery(q).Marked("runTestCount"));

			bool IsTestExecutionDone()
				=> runningState.GetDependencyPropertyValue("Text")?.ToString().Equals("Finished", StringComparison.OrdinalIgnoreCase) ?? false;

			_app.WaitForElement(runButton);

			_app.FastTap(runButton);

			var lastChange = DateTimeOffset.Now;
			var lastValue = "";

			while(DateTimeOffset.Now - lastChange < TestRunTimeout)
			{
				var newValue = runTestCount.GetDependencyPropertyValue("Text")?.ToString();

				if (lastValue != newValue)
				{
					lastChange = DateTimeOffset.Now;
				}

				await Task.Delay(TimeSpan.FromSeconds(.5));

				if (IsTestExecutionDone())
				{
					break;
				}
			}

			if (!IsTestExecutionDone())
			{
				Assert.Fail("A test run timed out");
			}

			var count = failedTestsCount.GetDependencyPropertyValue("Text").ToString();

			if (count != "0")
			{
				var tests = failedTests.GetDependencyPropertyValue<string>("Text")
					.Split(new char[] { '§' }, StringSplitOptions.RemoveEmptyEntries)
					.Select((x, i) => $"\t{i + 1}. {x}\n")
					.ToArray();

				var details = _app.Marked("failedTestDetails").GetDependencyPropertyValue("Text");

				Assert.Fail(
					$"{tests.Length} unit test(s) failed.\n\tFailing Tests:\n{string.Join("", tests)}\n\n---\n\tDetails:\n{details}");
			}

			TakeScreenshot("Runtime Tests Results",	ignoreInSnapshotCompare: true);
		}
	}
}
