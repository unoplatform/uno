using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.BorderTests
{
	public class Border_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void Change_Manipulation_Property()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.BorderTests.Border_Clipped_Change_Property");

			_app.WaitForElement("ClippedBorder");

			var before = TakeScreenshot("Before property change");

			_app.FastTap("ClippedBorder");

			_app.WaitForDependencyPropertyValue(_app.Marked("ClippedBorder"), "ManipulationMode", "None");

			var after = TakeScreenshot("After property change");

			ImageAssert.AreEqual(before, after);
		}

		[Test]
		[AutoRetry]
		public void Check_CornerRadius_Border()
		{
			// Verify that border is drawn with the same thickness with/without CornerRadius
			Run("UITests.Shared.Windows_UI_Xaml_Controls.BorderTests.Border_CornerRadius_Toggle");

			_app.WaitForElement("SubjectBorder");

			var verificationRect = _app.GetRect("SnapshotBorder");

			var scrBefore = TakeScreenshot("No CornerRadius");

			_app.FastTap("ToggleCornerRadiusButton");

			_app.WaitForText("StatusTextBlock", "5");

			var scrAfter = TakeScreenshot("CornerRadius=5");

			ImageAssert.AreAlmostEqual(scrBefore, scrAfter, verificationRect, permittedPixelError: 5);
		}
	}
}
