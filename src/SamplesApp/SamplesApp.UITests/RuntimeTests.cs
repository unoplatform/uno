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
			var failedTests = _app.Marked("failedTests");
			var runTestCount = _app.Marked("runTestCount");

			bool IsTestExecutionDone()
				=> failedTests.GetDependencyPropertyValue("Text").ToString() != PendingTestsText;

			_app.WaitForElement(runButton);

			_app.Tap(runButton);

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

			var count = failedTests.GetDependencyPropertyValue("Text").ToString();

			if (count != "0")
			{
				var details = _app.Marked("failedTestDetails").GetDependencyPropertyValue("Text");

				Assert.Fail("A Unit test failed. Details:\n" + details);
			}

			TakeScreenshot("Runtime Tests Results",	ignoreInSnapshotCompare: true);
		}
	}
}
