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
		[Test]
		[AutoRetry]
		public void RunRuntimeTests()
		{
			Run("SamplesApp.Samples.UnitTests.UnitTestsPage");

			var runButton = _app.Marked("runButton");
			var failedTests = _app.Marked("failedTests");

			_app.WaitForElement(runButton);

			_app.Tap(runButton);

			_app.WaitFor(() => failedTests.GetDependencyPropertyValue("Text").ToString() != "Pending...");


			var count = failedTests.GetDependencyPropertyValue("Text").ToString();

			if (count != "0")
			{
				var details = _app.Marked("failedTestDetails").GetDependencyPropertyValue("Text");

				Assert.Fail("A Unit test failed. Details:\n" + details);
			}

			_app.Screenshot("Runtime Tests Results");
		}
	}
}
