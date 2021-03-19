using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.SplitViewTests
{
	[TestFixture]
    public partial class SplitViewTests : SampleControlUITestBase
    {
        [Test]
		[AutoRetry]
		public void When_RightPanne_Clipped()
		{
			Run("UITests.Windows_UI_Xaml_Controls.SplitView.SplitViewClip");

			_app.WaitForElement("Split");

			var targetGridRectangle = _app.GetPhysicalRect("TargetRect");

			using var compactScreenshot = TakeScreenshot("Compact", ignoreInSnapshotCompare: true);
			// Compact pane is 48 pixels wide
			ImageAssert.HasColorAt(compactScreenshot, targetGridRectangle.Right - 4, targetGridRectangle.CenterY, Color.Blue);

			var toggleButton = _app.Marked("PaneToggle");
			toggleButton.Tap();

			using var expandedScreenshot = TakeScreenshot("Expanded");
			ImageAssert.HasColorAt(expandedScreenshot, targetGridRectangle.Right - 4, targetGridRectangle.CenterY, Color.Red);

			toggleButton.Tap();

			using var compactAgainScreenshot = TakeScreenshot("Compact again");
			ImageAssert.HasColorAt(compactAgainScreenshot, targetGridRectangle.Right - 4, targetGridRectangle.CenterY, Color.Blue);
		}
	}
}
