using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.SliderTests
{
	[TestFixture]
	public partial class Slider_Vertical_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void Slider_Vertical_Tap()
		{
			Run("Uno.UI.Samples.Content.UITests.Slider.Slider_Simple");
			var verticalSlider = _app.Marked("mySlider2");
			var sliderRect = verticalSlider.FirstResult().Rect;
			_app.WaitForElement(verticalSlider);
			_app.TapCoordinates(
				x: sliderRect.CenterX,
				y: sliderRect.CenterY + (sliderRect.Height * 0.3f));

			var tbSliderValue = _app.Marked("mySlider2Value");
			GetSliderValue(tbSliderValue).Should().BeLessThan(50, "Below 50% Value");

			_app.TapCoordinates(
				x: sliderRect.CenterX,
				y: sliderRect.CenterY - (sliderRect.Height * 0.3f));

			GetSliderValue(tbSliderValue).Should().BeGreaterThan(50, "Above 50% Value");
		}

		private int GetSliderValue(QueryEx mark)
		{
			_app.Wait(0.15f); // give time to app to execute the action
			var valueString = mark.GetDependencyPropertyValue("Text") as string;
			return int.TryParse(valueString, out var c) ? c : -1;
		}
	}
}
