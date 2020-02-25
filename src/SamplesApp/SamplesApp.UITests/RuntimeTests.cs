using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests
{
	[TestFixture]
	public partial class RuntimeTests : SampleControlUITestBase
	{
		private const string PendingTestsText = "Pending...";
		private readonly TimeSpan TestRunTimeout = TimeSpan.FromMinutes(2);

		[Test]
		[AutoRetry]
		public async Task RunRuntimeTests()
		{
			Run("SamplesApp.Samples.UnitTests.UnitTestsPage");

			var runButton = _app.Marked("runButton");
			var failedTestsCount = _app.Marked("failedTestCount");
			var failedTests = _app.Marked("failedTests");
			var runningState = _app.Marked("runningState");
			var runTestCount = _app.Marked("runTestCount");

			bool IsTestExecutionDone()
				=> runningState.GetDependencyPropertyValue<string>("Text").Equals("Finished", StringComparison.OrdinalIgnoreCase);

			_app.WaitForElement(runButton);

			_app.FastTap(runButton);

			var lastChange = DateTimeOffset.Now;
			var lastValue = "";

			while(DateTimeOffset.Now - lastChange < TestRunTimeout)
			{
				var newValue = runTestCount.GetDependencyPropertyValue<string>("Text");

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
