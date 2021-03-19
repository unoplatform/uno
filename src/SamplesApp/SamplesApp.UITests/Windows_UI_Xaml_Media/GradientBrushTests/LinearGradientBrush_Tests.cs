using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Media.GradientBrushTests
{
	[TestFixture]
	public partial class LinearGradientBrush_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)] // This should be enabled for WASM once it no longer uses the LEGACY_SHAPE_MEASURE code path - https://github.com/unoplatform/uno/issues/2983
		public void When_GradientStops_Changed()
		{
			Run("UITests.Windows_UI_Xaml_Media.GradientBrushTests.LinearGradientBrush_Change_Stops");

			var rectangle = _app.Marked("GradientBrushRectangle");

			_app.WaitForElement(rectangle);

			var screenRect = _app.GetPhysicalRect(rectangle);

			using var before = TakeScreenshot("Before", ignoreInSnapshotCompare: true);

			_app.FastTap("ChangeBrushButton");

			_app.WaitForText("StatusTextBlock", "Changed");

			using var after = TakeScreenshot("After");

			ImageAssert.AreNotEqual(before, after, screenRect);
		}
	}
}
