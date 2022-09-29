#nullable disable

using System.Drawing;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;

namespace SamplesApp.UITests.Microsoft_UI_Xaml_Controls.ExpanderTests
{
	public partial class Expander_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void ToggleCollapsedStateToVerifyClipping()
		{
			Run("UITests.Shared.Microsoft_UI_Xaml_Controls.ExpanderTests.ExpanderColorValidationPage");

			var expanderResult = _app.WaitForElement("ExpanderWithColor");
			var expander = _app.Marked("ExpanderWithColor");
			var expanderAppRect = ToPhysicalRect(expanderResult[0].Rect);

			expander.FastTap();
			using var screenshot = TakeScreenshot("Toggle Expanded property", new ScreenshotOptions() { IgnoreInSnapshotCompare = true });
			var expanderRectangle = expanderAppRect.ToRectangle();
			var centerPoint = new Point(expanderRectangle.Left + expanderRectangle.Width / 2, expanderRectangle.Top + expanderRectangle.Height / 2);
			// R:128 G:0 B:127 is the color that should be visible if the translucent content reigion occludes the rest of the control
			ImageAssert.DoesNotHaveColorAt(screenshot, centerPoint.X, centerPoint.Y, Color.FromArgb(128, 0, 127));
		}
	}
}
