using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
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
				y: sliderRect.CenterY);

			var tbSliderValue = _app.Marked("mySlider2Value");
			CheckValue(tbSliderValue, 56, "Center Tap Value");

			_app.TapCoordinates(
				x: sliderRect.CenterX,
				y: sliderRect.CenterY + 225);

			CheckValue(tbSliderValue, 20, "20% Tap Value");

			_app.TapCoordinates(
				x: sliderRect.CenterX,
				y: sliderRect.CenterY - 155);

			CheckValue(tbSliderValue, 80, "80% Tap Value");
		}

		private void CheckValue(QueryEx mark, int expected, string msg)
		{
			_app.Wait(0.15f); // give time to app to execute the action

			var valueString = mark.GetDependencyPropertyValue("Text") as string;
			int value = int.TryParse(valueString, out var c) ? c : -1;
			value.Should().Be(expected, msg);
		}
	}
}
