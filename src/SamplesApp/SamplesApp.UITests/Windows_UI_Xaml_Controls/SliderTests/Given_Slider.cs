using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.SliderTests
{
	public partial class Given_Slider : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void When_Horizontal_Slider_Has_Header()
		{
			Run("UITests.Windows_UI_Xaml_Controls.Slider.Slider_Header", skipInitialScreenshot: true);

			var headerContentTextBlock = _app.Marked("HorizontalSliderHeaderContent");
			_app.WaitForElement(headerContentTextBlock);

			Assert.AreEqual("This is a Slider Header", headerContentTextBlock.GetDependencyPropertyValue("Text").ToString());
		}

		[Test]
		[AutoRetry]
		public void When_Vertical_Slider_Has_Header()
		{
			Run("UITests.Windows_UI_Xaml_Controls.Slider.Slider_Header", skipInitialScreenshot: true);

			var headerContentTextBlock = _app.Marked("VerticalSliderHeaderContent");
			_app.WaitForElement(headerContentTextBlock);

			Assert.AreEqual("This is a Slider Header", headerContentTextBlock.GetDependencyPropertyValue("Text").ToString());
		}
	}
}
