using System.Drawing;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;

namespace SamplesApp.UITests.Windows_UI_Xaml_Media.GeometryTests
{
	[TestFixture]
	public partial class GeometryTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void ClosedPath()
		{
			// Navigate to this x:Class control name
			Run("SamplesApp.Windows_UI_Xaml_Media.Geometry.ClosedFigurePage");

			// Define elements that will be interacted with at the start of the test
			var path = _app.Marked("Path");

			// Specify what user interface element to wait on before starting test execution
			_app.WaitForElement(path);

			// if closed path, there should be black color on left hand side

			using var screenshot = TakeScreenshot("Closed state");

			ImageAssert.HasColorAt(screenshot, 50, 300, Color.Black);
		}
	}
}
