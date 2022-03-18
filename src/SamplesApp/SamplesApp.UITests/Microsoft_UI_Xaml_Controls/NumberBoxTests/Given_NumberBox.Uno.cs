using System.Drawing;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Microsoft_UI_Xaml_Controls.NumberBoxTests
{
	public partial class NumberBoxTests
    {
		[Test]
		[AutoRetry]
		public void NumberBox_Header()
		{
			Run("UITests.Microsoft_UI_Xaml_Controls.NumberBoxTests.NumberBox_Header", skipInitialScreenshot: true);

			var headerContentTextBlock = _app.Marked("NumberBoxHeaderContent");
			_app.WaitForElement(headerContentTextBlock);

			Assert.AreEqual("This is a NumberBox Header", headerContentTextBlock.GetDependencyPropertyValue("Text").ToString());
		}

		[Test]
		[AutoRetry]
		public void NumberBox_Description()
		{
			Run("UITests.Microsoft_UI_Xaml_Controls.NumberBoxTests.NumberBox_Description", skipInitialScreenshot: true);

			var numberBoxRect = ToPhysicalRect(_app.WaitForElement("DescriptionNumberBox")[0].Rect);
			using var screenshot = TakeScreenshot("NumberBox Description", new ScreenshotOptions() { IgnoreInSnapshotCompare = true });

			ImageAssert.HasColorAt(screenshot, numberBoxRect.X + numberBoxRect.Width / 2, numberBoxRect.Y + numberBoxRect.Height - 50, Color.Red);
		}

		[Test]
		[AutoRetry]
		public void DecimalFormatterTest()
		{
			Run("UITests.Shared.Microsoft_UI_Xaml_Controls.NumberBoxTests.MUX_Test");

			var numBox = _app.Marked("TestNumberBox");
			Assert.AreEqual(double.NaN, numBox.GetDependencyPropertyValue<double>("Value"));

			_app.FastTap("MinCheckBox");
			_app.FastTap("MaxCheckBox");
			_app.Marked("CustomFormatterButton").FastTap();

			EnterTextInNumberBox(numBox, "۱٫۷");
			_app.PressEnter();
			_app.PressEnter();

			Assert.AreEqual("۱٫۷۵", numBox.GetDependencyPropertyValue<string>("Text"));
			Assert.AreEqual(1.75, numBox.GetDependencyPropertyValue<double>("Value"));
		}
	}
}
