using System.Drawing;
using NUnit.Framework;
using SamplesApp.UITests.Extensions;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;

namespace SamplesApp.UITests.Windows_UI_Xaml_Shapes
{
	public partial class Basics_Shapes_Tests : SampleControlUITestBase
	{
		// The shape stretch/alignment golden comparison that used to live here is now a Skia runtime
		// test: Uno.UI.RuntimeTests ... Given_Basic_Shapes_Parity (the WinUI goldens moved with it).

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // Flaky on iOS/Android native https://github.com/unoplatform/uno/issues/22688
		public void Validate_Offscreen_Shapes()
		{
			Run("UITests.Windows_UI_Xaml_Shapes.Offscreen_Shapes");

			_app.WaitForElement("deferredShape6");

			using var screensnot = TakeScreenshot("offscreen_shapes", ignoreInSnapshotCompare: true);

			var xamlShape6 = _app.GetPhysicalRect("xamlShape6");
			var deferredShape6 = _app.GetPhysicalRect("deferredShape6");

			ImageAssert.HasColorAt(screensnot, xamlShape6.CenterX, xamlShape6.CenterY, Color.Yellow, tolerance: 5);
			ImageAssert.HasColorAt(screensnot, deferredShape6.CenterX, xamlShape6.CenterY, Color.Yellow, tolerance: 5);
		}
	}
}
