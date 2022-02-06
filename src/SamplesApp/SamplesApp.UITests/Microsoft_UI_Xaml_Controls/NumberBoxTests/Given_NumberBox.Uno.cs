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
	}
}
