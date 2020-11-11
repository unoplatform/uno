using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Media.GeometryTests
{
	[TestFixture]
	public partial class GeometryTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void LineSegment()
		{
			// Navigate to this x:Class control name
			Run("SamplesApp.Windows_UI_Xaml_Media.Geometry.LineSegmentPage");

			// Define elements that will be interacted with at the start of the test
			var path = _app.Marked("Path");

			// Specify what user interface element to wait on before starting test execution
			_app.WaitForElement(path);

			// Take an initial screenshot
			TakeScreenshot("Initial State", ignoreInSnapshotCompare: true);
		}

		[Test]
		[AutoRetry]
		public void PointMovement()
		{
			Run("SamplesApp.Windows_UI_Xaml_Media.Geometry.LineSegmentPage");

			var path = _app.Marked("Path");
			var button = _app.Marked("MovePointButton");

			_app.WaitForElement(path);

			using var before = TakeScreenshot("Before point movement", ignoreInSnapshotCompare: true);

			button.Tap();

			using var after = TakeScreenshot("After point movement");

			ImageAssert.AreNotEqual(before, after);
		}
	}

}
