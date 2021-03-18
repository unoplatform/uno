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
		[ActivePlatforms(Platform.Browser)]
		public void BezierSegment()
		{
			// Navigate to this x:Class control name
			Run("SamplesApp.Windows_UI_Xaml_Media.Geometry.BezierSegment");

			// Define elements that will be interacted with at the start of the test
			var path = _app.Marked("Path");

			// Specify what user interface element to wait on before starting test execution
			_app.WaitForElement(path);

			// Take an initial screenshot
			TakeScreenshot("Initial State");
		}
	}

}
