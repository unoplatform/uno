using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests
{
	[TestFixture]
	public partial class RuntimeTests : SampleControlUITestBase
	{
		[Test]
		public void RunRuntimeTests()
		{
			Run("SamplesApp.Samples.UnitTests.UnitTestsPage");

			var runButton = _app.Marked("runButton");
			var failedTests = _app.Marked("failedTests");

			_app.Tap(runButton);

			_app.WaitFor(() => failedTests.GetDependencyPropertyValue("Text").ToString() != "Pending...");

			Assert.AreEqual("0", failedTests.GetDependencyPropertyValue("Text").ToString());

			_app.Screenshot("Runtime Tests Results");
		}
	}
}
