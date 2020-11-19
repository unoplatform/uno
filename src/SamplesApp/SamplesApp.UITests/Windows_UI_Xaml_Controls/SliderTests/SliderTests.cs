using System;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Specialized;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.SliderTests
{
	[TestFixture]
	public partial class SliderTests : SampleControlUITestBase
	{
		private const double Tolerance = 0.5;
		private const int Delta = 100;

		[Test]
		[AutoRetry]
		public void When_VerticalSliderHasMaxValue()
		{
			Run("UITests.Windows_UI_Xaml_Controls.Slider.Slider_ThumbSize");

			var slider = _app.Marked("VerticalSlider1");
			_app.WaitForElement(slider);
			var sliderRect = slider.FirstResult().Rect;
			_app.DragCoordinates(
				sliderRect.CenterX,
				sliderRect.CenterY,
				sliderRect.CenterX,
				sliderRect.CenterY - (sliderRect.Height * 0.6f));

			var sliderThumb = slider.Descendant().Marked("VerticalThumb");
			var actualVerticalSliderThumbHeight = (double) _app.GetLogicalRect(sliderThumb).Height;
			var expectedVerticalSliderThumbHeight = sliderThumb.GetDependencyPropertyValue<double>("Height");

			actualVerticalSliderThumbHeight.Should().BeApproximately(expectedVerticalSliderThumbHeight, Tolerance);
		}

		[Test]
		[AutoRetry]
		public void When_VerticalSliderHasMaxValueWithoutHeight()
		{
			Run("UITests.Windows_UI_Xaml_Controls.Slider.Slider_ThumbSize");

			var slider = _app.Marked("VerticalSlider2");
			var expectedVerticalSliderHeight = _app.Marked("VerticalSlider2_MaxHeight").GetDependencyPropertyValue<double>("Text");
			_app.WaitForElement(slider);

			var sliderRect = slider.FirstResult().Rect;
			_app.DragCoordinates(
				sliderRect.CenterX,
				sliderRect.CenterY,
				sliderRect.CenterX,
				sliderRect.CenterY - Delta);

			double actualVerticalSliderHeight;
			if (AppInitializer.TestEnvironment.CurrentPlatform != Platform.Browser)
			{
				actualVerticalSliderHeight = new QueryEx(q => q.All().Marked("VerticalSlider2_MaxHeight")).GetDependencyPropertyValue<double>("Text");
			}
			else
			{
				actualVerticalSliderHeight = _app.Marked("VerticalSlider2_MaxHeight").GetDependencyPropertyValue<double>("Text");
			}

			Assert.True(Math.Abs(actualVerticalSliderHeight - expectedVerticalSliderHeight) < Tolerance,
				$"Expected slider height: {expectedVerticalSliderHeight} \n" +
				$"Actual slider height: {actualVerticalSliderHeight}");
		}

		[Test]
		[AutoRetry]
		public void When_HorizontalSliderHasMaxValueWithoutWidth()
		{
			Run("UITests.Windows_UI_Xaml_Controls.Slider.Slider_ThumbSize");

			var slider = _app.Marked("HorizontalSlider3");
			_app.WaitForElement(slider);

			var sliderRect = slider.FirstResult().Rect;
			_app.DragCoordinates(
				sliderRect.CenterX,
				sliderRect.CenterY,
				sliderRect.CenterX ,
				sliderRect.CenterY + Delta);

			var actualHorizontalSliderValue =  _app.Marked("HorizontalSlider3_MaxHeight").GetDependencyPropertyValue<double>("Text");

			var expectedHorizontalSliderValue = 100;
			Assert.True(Math.Abs(expectedHorizontalSliderValue - actualHorizontalSliderValue) < Tolerance,
				$"Expected slider value: {expectedHorizontalSliderValue} \n" +
				$"Actual slider value: {actualHorizontalSliderValue}");
		}
	}
}
